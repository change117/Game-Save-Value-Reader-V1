using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveStructureSearch;

/// <summary>
/// Searches GitHub's public code-search API for community-documented save file structures.
/// Looks for offset/data-type information in repository files (READMEs, wikis, scripts).
/// No authentication is required; the unauthenticated rate limit is 10 requests per minute.
/// </summary>
public class HttpSaveStructureSearcher : ISaveStructureSearcher
{
    private readonly HttpClient _http;

    // Matches patterns like "offset: 0x1A4", "offset 420", "at offset 0x1A4"
    private static readonly Regex OffsetPattern =
        new(@"offset[:\s]+(?:0x)?([0-9A-Fa-f]+)", RegexOptions.IgnoreCase);

    // Matches currency/value names common in game saves
    private static readonly Regex ValueNamePattern =
        new(@"\b(gold|money|coins?|currency|credits?|caps?|gil|septims?|souls?|ore|crowns?|cash)\b",
            RegexOptions.IgnoreCase);

    // Matches data-type hints
    private static readonly Regex DataTypePattern =
        new(@"\b(int(?:eger)?(?:32|64)?|float|double|uint(?:32|64)?|byte|short|long)\b",
            RegexOptions.IgnoreCase);

    public HttpSaveStructureSearcher() : this(new HttpClient()) { }

    public HttpSaveStructureSearcher(HttpClient httpClient)
    {
        _http = httpClient;
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("GameSaveValueReader", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <inheritdoc />
    public async Task<SaveInfo?> SearchAsync(string gameName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameName))
            return null;

        var query = Uri.EscapeDataString($"{gameName} save file offset gold");
        var url   = $"https://api.github.com/search/code?q={query}&per_page=5";

        try
        {
            var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseGitHubSearchResult(gameName, json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return null;
        }
    }

    private static SaveInfo? ParseGitHubSearchResult(string gameName, string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items))
            return null;

        foreach (var item in items.EnumerateArray())
        {
            // Use the repository description or file name as a hint
            string text = string.Empty;
            if (item.TryGetProperty("repository", out var repo) &&
                repo.TryGetProperty("description", out var desc))
                text += desc.GetString() ?? string.Empty;

            if (item.TryGetProperty("name", out var name))
                text += " " + name.GetString();

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

    private static long ParseOffset(string raw)
    {
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToInt64(raw, 16);

        return long.TryParse(raw, out var val) ? val : 0;
    }

    private static string NormaliseDataType(string raw) =>
        raw.ToLowerInvariant() switch
        {
            "integer" or "int" or "int32" => "int32",
            "long"    or "int64"          => "int64",
            "uint"    or "uint32"         => "uint32",
            "uint64"                      => "uint64",
            "float"                       => "float",
            "double"                      => "double",
            _                             => "int32",
        };

    private static string Capitalise(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
