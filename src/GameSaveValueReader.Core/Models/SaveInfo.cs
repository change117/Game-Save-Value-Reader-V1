namespace GameSaveValueReader.Core.Models;

/// <summary>
/// Holds the documented save file structure for a game's currency-equivalent value.
/// </summary>
public class SaveInfo
{
    /// <summary>The normalised game name as recognised by the knowledge base or search result.</summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>The community name for the value (e.g. "Gold", "Caps", "Gil").</summary>
    public string ValueName { get; set; } = string.Empty;

    /// <summary>Byte offset within the save file where the value begins.</summary>
    public long Offset { get; set; }

    /// <summary>
    /// CLR-friendly data type string.
    /// Supported values: "int32", "int64", "uint32", "uint64", "float", "double".
    /// </summary>
    public string DataType { get; set; } = "int32";

    /// <summary>The public source that documents this structure (URL or description).</summary>
    public string Source { get; set; } = string.Empty;
}
