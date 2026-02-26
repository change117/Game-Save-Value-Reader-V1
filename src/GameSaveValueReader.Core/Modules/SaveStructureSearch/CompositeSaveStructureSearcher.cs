using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveStructureSearch;

/// <summary>
/// Combines multiple <see cref="ISaveStructureSearcher"/> implementations, trying each in order.
/// Returns the first non-null result. If all searchers return <c>null</c>, returns <c>null</c>.
/// </summary>
public class CompositeSaveStructureSearcher : ISaveStructureSearcher
{
    private readonly IReadOnlyList<ISaveStructureSearcher> _searchers;

    /// <param name="searchers">
    /// Ordered list of searchers to try. Typically: local knowledge base first,
    /// followed by HTTP-based searchers.
    /// </param>
    public CompositeSaveStructureSearcher(IEnumerable<ISaveStructureSearcher> searchers)
    {
        _searchers = searchers.ToList();
    }

    /// <summary>Creates the default pipeline: local knowledge base, then GitHub search.</summary>
    public static CompositeSaveStructureSearcher CreateDefault() =>
        new([new LocalSaveStructureSearcher(), new HttpSaveStructureSearcher()]);

    /// <inheritdoc />
    public async Task<SaveInfo?> SearchAsync(string gameName, CancellationToken cancellationToken = default)
    {
        foreach (var searcher in _searchers)
        {
            var result = await searcher.SearchAsync(gameName, cancellationToken).ConfigureAwait(false);
            if (result is not null)
                return result;
        }

        return null;
    }
}
