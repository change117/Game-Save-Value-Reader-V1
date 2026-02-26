namespace GameSaveReader.Core.SaveStructureSearch;

/// <summary>
/// Describes the location, type, and name of a value within a game save file.
/// </summary>
public sealed class SaveValueDescriptor
{
    /// <summary>
    /// Byte offset where the value is located in the save file.
    /// </summary>
    public long Offset { get; init; }

    /// <summary>
    /// Data type of the value (e.g., Int32, Single, Int64, Double).
    /// </summary>
    public SaveValueType DataType { get; init; }

    /// <summary>
    /// Community name for the value (e.g., "Gold", "Will", "Credits").
    /// </summary>
    public string ValueName { get; init; } = string.Empty;
}

/// <summary>
/// Supported data types for save file values.
/// </summary>
public enum SaveValueType
{
    Int32,
    Int64,
    Single,
    Double
}
