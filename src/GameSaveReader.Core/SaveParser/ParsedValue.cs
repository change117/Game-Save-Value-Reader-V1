namespace GameSaveReader.Core.SaveParser;

/// <summary>
/// Result of parsing a value from a save file.
/// </summary>
public sealed class ParsedValue
{
    /// <summary>
    /// The community name for the value (e.g., "Gold", "Souls").
    /// </summary>
    public string ValueName { get; init; } = string.Empty;

    /// <summary>
    /// The parsed numeric value.
    /// </summary>
    public object Value { get; init; } = 0;

    /// <summary>
    /// Returns the display string in "ValueName: Amount" format.
    /// </summary>
    public override string ToString() => $"{ValueName}: {Value}";
}
