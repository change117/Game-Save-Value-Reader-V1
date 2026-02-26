namespace GameSaveValueReader.Core.Modules.SaveStructureSearch;

/// <summary>Internal DTO that mirrors a single entry in games.json.</summary>
internal class KnowledgeBaseEntry
{
    public string GameName { get; set; } = string.Empty;
    public List<string>? Aliases { get; set; }
    public string ValueName { get; set; } = string.Empty;
    public long Offset { get; set; }
    public string DataType { get; set; } = "int32";
    public string Source { get; set; } = string.Empty;
}
