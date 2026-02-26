using System.Reflection;
using System.Text.Json;
using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveStructureSearch;

/// <summary>
/// Searches a bundled JSON knowledge base of community-documented save structures.
/// This is the first searcher tried in the composite pipeline and needs no network access.
/// </summary>
public class LocalSaveStructureSearcher : ISaveStructureSearcher
{
    private readonly IReadOnlyList<KnowledgeBaseEntry> _entries;

    public LocalSaveStructureSearcher()
    {
        _entries = LoadEntries();
    }

    /// <inheritdoc />
    public Task<SaveInfo?> SearchAsync(string gameName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameName))
            return Task.FromResult<SaveInfo?>(null);

        var normalised = gameName.Trim().ToLowerInvariant();

        foreach (var entry in _entries)
        {
            // Check primary name
            if (entry.GameName.ToLowerInvariant() == normalised)
                return Task.FromResult<SaveInfo?>(ToSaveInfo(entry));

            // Check aliases
            if (entry.Aliases?.Any(a => a.ToLowerInvariant() == normalised) == true)
                return Task.FromResult<SaveInfo?>(ToSaveInfo(entry));
        }

        return Task.FromResult<SaveInfo?>(null);
    }

    private static SaveInfo ToSaveInfo(KnowledgeBaseEntry entry) => new()
    {
        GameName  = entry.GameName,
        ValueName = entry.ValueName,
        Offset    = entry.Offset,
        DataType  = entry.DataType,
        Source    = entry.Source,
    };

    private static IReadOnlyList<KnowledgeBaseEntry> LoadEntries()
    {
        var assembly     = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
                                   .First(n => n.EndsWith("games.json", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var entries = JsonSerializer.Deserialize<List<KnowledgeBaseEntry>>(stream,
                          new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return entries ?? new List<KnowledgeBaseEntry>();
    }
}
