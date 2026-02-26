using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveParse;

/// <summary>
/// Step 3 – Save Parse.
/// Opens a binary save file and reads the value described by a <see cref="SaveInfo"/>.
/// </summary>
public interface ISaveParser
{
    /// <summary>
    /// Opens <paramref name="filePath"/>, seeks to <see cref="SaveInfo.Offset"/>,
    /// reads the value as <see cref="SaveInfo.DataType"/>, and returns it as a
    /// <see langword="long"/> (integer types are returned exactly; floating-point
    /// values are rounded to the nearest integer for display).
    /// </summary>
    /// <param name="filePath">Path to the binary save file.</param>
    /// <param name="saveInfo">Structure information from the search step.</param>
    /// <returns>The raw numeric value stored at the documented offset.</returns>
    /// <exception cref="FileNotFoundException">Save file does not exist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Offset exceeds file size.</exception>
    /// <exception cref="NotSupportedException">DataType is not recognised.</exception>
    long ParseValue(string filePath, SaveInfo saveInfo);
}
