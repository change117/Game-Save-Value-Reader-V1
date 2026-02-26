using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveStructureSearch;

/// <summary>
/// Searches multiple public sources for community-documented save file structures.
///
/// Discovery strategy:
///   1) Use Brave Search (HTML) to find Fearless Revolution topic URLs for the game.
///   2) Fetch each topic via the Wayback Machine (bypasses Cloudflare protection).
///   3) If Wayback is unavailable, try fetching the page directly.
///   4) Fall back to the GitHub public code-search API.
///
/// Every scraped result is validated: the page must mention **all** significant
/// keywords from the game name before any offset data is extracted.
///
/// No API keys required; all sources are open and public.
/// </summary>
public class HttpSaveStructureSearcher : ISaveStructureSearcher
{
    private readonly HttpClient _http;

    // ---------------------------------------------------------------
    // Regex patterns for extracting save-structure data from page text
    // ---------------------------------------------------------------

    // "offset: 0x1A4", "offset 420", "at offset 0x1A4", "Offset=0x54"
    private static readonly Regex OffsetPattern =
        new(@"offset[=:\s]+(?:0x)?([0-9A-Fa-f]+)", RegexOptions.IgnoreCase);

    // Cheat Engine / trainer-style: "base+0x1A4", "[address]+44"
    private static readonly Regex AddressOffsetPattern =
        new(@"(?:base|\])\s*\+\s*(?:0x)?([0-9A-Fa-f]+)", RegexOptions.IgnoreCase);

    // Currency / value names common in game saves
    private static readonly Regex ValueNamePattern =
        new(@"\b(gold|money|coins?|currency|credits?|caps?|gil|septims?|souls?|ore|crowns?|cash|runes?|sparks?|will|experience|xp|gems?|diamonds?|platinum|silver|copper|tokens?|points?|essence|mana|stamina|health|hp|mp|level|lives?|ammo|arrows?|potions?|items?)\b",
            RegexOptions.IgnoreCase);

    // Data-type hints (including Cheat Engine terms)
    private static readonly Regex DataTypePattern =
        new(@"\b(int(?:eger)?(?:32|64)?|float|double|uint(?:32|64)?|byte|short|long|dword|qword|4\s*bytes?|8\s*bytes?)\b",
            RegexOptions.IgnoreCase);

    // Words too common to use for game-name matching
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "of", "and", "or", "in", "on", "at", "to", "for",
        "is", "it", "by", "with", "from", "as", "into", "game", "save",
        "cheat", "table", "trainer", "engine",
    };

    public HttpSaveStructureSearcher() : this(new HttpClient()) { }

    public HttpSaveStructureSearcher(HttpClient httpClient)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("GameSaveValueReader", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    /// <inheritdoc />
    public async Task<SaveInfo?> SearchAsync(string gameName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameName))
            return null;

        // Try Fearless Revolution via Brave Search + Wayback Machine
        var result = await SearchFearlessRevolutionViaBraveAsync(gameName, cancellationToken)
            .ConfigureAwait(false);
        if (result is not null) return result;

        // Fall back to GitHub code search
        result = await SearchGitHubAsync(gameName, cancellationToken).ConfigureAwait(false);
        if (result is not null) return result;

        return null;
    }

    // ------------------------------------------------------------------
    // Game-name validation
    // ------------------------------------------------------------------

    /// <summary>
    /// Extract meaningful keywords from the game name.
    /// E.g. "Black Myth: Wukong" → {"black", "myth", "wukong"}
    /// </summary>
    private static List<string> GetKeywords(string gameName)
    {
        return Regex.Split(gameName, @"[^a-zA-Z0-9]+")
            .Where(w => w.Length >= 2 && !StopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Returns true when the text contains ALL significant keywords from the game name.
    /// </summary>
    private static bool TextMatchesGame(string text, IReadOnlyList<string> keywords)
    {
        if (keywords.Count == 0)
            return false;

        var lowerText = text.ToLowerInvariant();
        return keywords.All(k => lowerText.Contains(k));
    }

    // ------------------------------------------------------------------
    // Source 1: Fearless Revolution via Brave Search + Wayback Machine
    // ------------------------------------------------------------------

    /// <summary>
    /// Step 1: Use Brave Search to find Fearless Revolution topic URLs.
    /// Step 2: Fetch each page from the Wayback Machine (bypasses Cloudflare).
    /// Step 3: If Wayback is unavailable, try direct fetch as fallback.
    /// </summary>
    private async Task<SaveInfo?> SearchFearlessRevolutionViaBraveAsync(string gameName, CancellationToken ct)
    {
        try
        {
            var keywords = GetKeywords(gameName);
            if (keywords.Count == 0)
                return null;

            // Use Brave Search to find FR topics for this game
            var topicUrls = await FindFrTopicUrlsViaBraveAsync(gameName, keywords, ct)
                .ConfigureAwait(false);

            if (topicUrls.Count == 0)
                return null;

            // Try to fetch each topic and extract save structure data
            foreach (var topicUrl in topicUrls)
            {
                // Try Wayback Machine first (bypasses Cloudflare)
                var html = await FetchViaWaybackAsync(topicUrl, ct).ConfigureAwait(false);

                // Fall back to direct access (may work from user's machine even if not from Codespace)
                if (html is null)
                    html = await FetchDirectAsync(topicUrl, ct).ConfigureAwait(false);

                if (html is null)
                    continue;

                var result = ExtractSaveInfoFromHtml(gameName, keywords, html, topicUrl);
                if (result is not null)
                    return result;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // Network issues – fall through to next source
        }

        return null;
    }

    /// <summary>
    /// Queries Brave Search for "site:fearlessrevolution.com 'game name'" and extracts
    /// unique viewtopic.php URLs whose link text matches the game name.
    /// </summary>
    private async Task<List<string>> FindFrTopicUrlsViaBraveAsync(
        string gameName, IReadOnlyList<string> keywords, CancellationToken ct)
    {
        var result = new List<string>();
        try
        {
            var query = Uri.EscapeDataString($"site:fearlessrevolution.com \"{gameName}\"");
            var searchUrl = $"https://search.brave.com/search?q={query}&source=web";

            var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

            var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return result;

            var html = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            // Extract all FR viewtopic links from Brave's response
            var linkMatches = Regex.Matches(html,
                @"href=""(https?://fearlessrevolution\.com/viewtopic\.php\?[^""]+)""[^>]*>(.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match m in linkMatches)
            {
                var url = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
                var title = System.Net.WebUtility.HtmlDecode(
                    Regex.Replace(m.Groups[2].Value, @"<[^>]+>", " ").Trim());

                // The link or its surrounding text must mention the game name
                if (!TextMatchesGame(title, keywords))
                    continue;

                // Normalise URL: strip page/start params, keep just the topic ID
                var tidMatch = Regex.Match(url, @"t=(\d+)");
                if (!tidMatch.Success)
                    continue;

                var canonicalUrl = $"https://fearlessrevolution.com/viewtopic.php?t={tidMatch.Groups[1].Value}";

                if (!result.Contains(canonicalUrl))
                    result.Add(canonicalUrl);

                if (result.Count >= 3)
                    break;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // Search engine unavailable – return whatever we have
        }

        return result;
    }

    /// <summary>
    /// Fetches a page from the Wayback Machine (Internet Archive).
    /// Uses the "web/2/" prefix which redirects to the latest available snapshot.
    /// Returns the HTML string or null on failure.
    /// </summary>
    private async Task<string?> FetchViaWaybackAsync(string originalUrl, CancellationToken ct)
    {
        try
        {
            // "web/2/" = latest available snapshot, follows redirect
            var waybackUrl = $"https://web.archive.org/web/2/{originalUrl}";

            // Wayback can be slow – use a dedicated 12-second timeout
            using var wayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            wayCts.CancelAfter(TimeSpan.FromSeconds(12));

            var response = await _http.GetAsync(waybackUrl, wayCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(wayCts.Token).ConfigureAwait(false);

            // Sanity check: Wayback wraps content – make sure we got a real page
            if (html.Length < 500)
                return null;

            return html;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts a direct fetch of the URL. This will fail if Cloudflare blocks it,
    /// but may succeed from the user's Windows machine (different IP / environment).
    /// </summary>
    private async Task<string?> FetchDirectAsync(string url, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var directCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            directCts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _http.SendAsync(request, directCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(directCts.Token).ConfigureAwait(false);

            // Detect Cloudflare challenge page (returns 200 but with JS challenge)
            if (html.Contains("Just a moment") || html.Contains("cf_chl_opt") || html.Contains("challenge-platform"))
                return null;

            return html;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    // ------------------------------------------------------------------
    // Source 2: GitHub public code search API
    // ------------------------------------------------------------------

    private async Task<SaveInfo?> SearchGitHubAsync(string gameName, CancellationToken ct)
    {
        try
        {
            var query = Uri.EscapeDataString($"{gameName} save file offset");
            var url = $"https://api.github.com/search/code?q={query}&per_page=5";

            var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ParseGitHubSearchResult(gameName, json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    // ------------------------------------------------------------------
    // HTML content → SaveInfo extraction
    // ------------------------------------------------------------------

    /// <summary>
    /// Strips HTML tags, decodes entities, validates the game name appears
    /// in the text, then extracts offset / value-name / data-type.
    /// </summary>
    private static SaveInfo? ExtractSaveInfoFromHtml(
        string gameName, IReadOnlyList<string> keywords, string html, string sourceUrl)
    {
        // Strip HTML tags for easier regex matching
        var text = Regex.Replace(html, @"<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);

        // ── VALIDATION: page must actually be about this game ──
        if (!TextMatchesGame(text, keywords))
            return null;

        // Try standard offset pattern first, then Cheat Engine address pattern
        var offsetMatch = OffsetPattern.Match(text);
        if (!offsetMatch.Success)
            offsetMatch = AddressOffsetPattern.Match(text);

        if (!offsetMatch.Success)
            return null;

        var valueNameMatch = ValueNamePattern.Match(text);
        var dataTypeMatch  = DataTypePattern.Match(text);

        long offset = ParseOffset(offsetMatch.Groups[1].Value);

        return new SaveInfo
        {
            GameName  = gameName,
            ValueName = valueNameMatch.Success ? Capitalise(valueNameMatch.Value) : "Currency",
            Offset    = offset,
            DataType  = NormaliseDataType(dataTypeMatch.Success ? dataTypeMatch.Value : "int32"),
            Source    = sourceUrl,
        };
    }

    // ------------------------------------------------------------------
    // GitHub JSON result parser
    // ------------------------------------------------------------------

    private static SaveInfo? ParseGitHubSearchResult(string gameName, string json)
    {
        var keywords = GetKeywords(gameName);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items))
            return null;

        foreach (var item in items.EnumerateArray())
        {
            string text = string.Empty;
            if (item.TryGetProperty("repository", out var repo))
            {
                if (repo.TryGetProperty("description", out var desc))
                    text += desc.GetString() ?? string.Empty;
                if (repo.TryGetProperty("full_name", out var fullName))
                    text += " " + (fullName.GetString() ?? string.Empty);
            }

            if (item.TryGetProperty("name", out var name))
                text += " " + name.GetString();
            if (item.TryGetProperty("path", out var path))
                text += " " + path.GetString();

            // ── VALIDATION: repo must reference this game ──
            if (!TextMatchesGame(text, keywords))
                continue;

            var offsetMatch    = OffsetPattern.Match(text);
            var valueNameMatch = ValueNamePattern.Match(text);
            var dataTypeMatch  = DataTypePattern.Match(text);

            if (!offsetMatch.Success)
                continue;

            long offset = ParseOffset(offsetMatch.Groups[1].Value);

            return new SaveInfo
            {
                GameName  = gameName,
                ValueName = valueNameMatch.Success ? Capitalise(valueNameMatch.Value) : "Currency",
                Offset    = offset,
                DataType  = NormaliseDataType(dataTypeMatch.Success ? dataTypeMatch.Value : "int32"),
                Source    = item.TryGetProperty("html_url", out var hu) ? hu.GetString() ?? string.Empty : string.Empty,
            };
        }

        return null;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static long ParseOffset(string raw)
    {
        if (raw.Any(c => c is >= 'A' and <= 'F' or >= 'a' and <= 'f'))
            return Convert.ToInt64(raw, 16);

        return long.TryParse(raw, out var val) ? val : 0;
    }

    private static string NormaliseDataType(string raw) =>
        raw.ToLowerInvariant() switch
        {
            "integer" or "int" or "int32" or "dword" or "4 bytes" or "4 byte" => "int32",
            "long"    or "int64" or "qword" or "8 bytes" or "8 byte"         => "int64",
            "uint"    or "uint32"                                             => "uint32",
            "uint64"                                                          => "uint64",
            "float"                                                           => "float",
            "double"                                                          => "double",
            _                                                                 => "int32",
        };

    private static string Capitalise(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
