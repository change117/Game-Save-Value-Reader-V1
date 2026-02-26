namespace GameSaveReader.Core.SaveStructureSearch;

/// <summary>
/// A save structure searcher backed by a local knowledge base of known game
/// save formats. Sources include publicly documented offsets from community
/// modding resources (Fearless Revolution, Nexus Mods, GitHub, modding wikis).
/// 
/// This implementation is used for V1 validation. Future versions will add
/// live web search capabilities via additional ISaveStructureSearcher implementations.
/// </summary>
public sealed class LocalKnowledgeBaseSearcher : ISaveStructureSearcher
{
    private static readonly Dictionary<string, SaveValueDescriptor> KnownGames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fable - The Lost Chapters"] = new SaveValueDescriptor
            {
                Offset = 0x218,
                DataType = SaveValueType.Int32,
                ValueName = "Gold"
            },
            ["Fable TLC"] = new SaveValueDescriptor
            {
                Offset = 0x218,
                DataType = SaveValueType.Int32,
                ValueName = "Gold"
            },
            ["Terraria"] = new SaveValueDescriptor
            {
                Offset = 0x4C,
                DataType = SaveValueType.Int32,
                ValueName = "Health"
            },
            ["Dark Souls"] = new SaveValueDescriptor
            {
                Offset = 0x2CC,
                DataType = SaveValueType.Int32,
                ValueName = "Souls"
            }
        };

    /// <inheritdoc/>
    public SaveValueDescriptor? Search(string gameName)
    {
        if (string.IsNullOrWhiteSpace(gameName))
            return null;

        return KnownGames.TryGetValue(gameName.Trim(), out var descriptor) ? descriptor : null;
    }

    /// <summary>
    /// Returns the list of game names in the local knowledge base.
    /// </summary>
    public IReadOnlyList<string> GetKnownGameNames()
    {
        return KnownGames.Keys.ToList().AsReadOnly();
    }
}
