using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveParse;

/// <summary>
/// Default implementation of <see cref="ISaveParser"/>.
/// Reads little-endian values from a binary save file at the documented offset.
/// </summary>
public class SaveParser : ISaveParser
{
    /// <inheritdoc />
    public long ParseValue(string filePath, SaveInfo saveInfo)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}", filePath);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (saveInfo.Offset < 0 || saveInfo.Offset >= stream.Length)
            throw new ArgumentOutOfRangeException(nameof(saveInfo),
                $"Offset {saveInfo.Offset} is outside the file (length {stream.Length}).");

        stream.Seek(saveInfo.Offset, SeekOrigin.Begin);
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        return saveInfo.DataType.ToLowerInvariant() switch
        {
            "int32"  => reader.ReadInt32(),
            "int64"  => reader.ReadInt64(),
            "uint32" => (long)reader.ReadUInt32(),
            "uint64" => (long)reader.ReadUInt64(),
            "float"  => (long)Math.Round(reader.ReadSingle()),
            "double" => (long)Math.Round(reader.ReadDouble()),
            _        => throw new NotSupportedException(
                            $"Data type '{saveInfo.DataType}' is not supported. " +
                            "Supported types: int32, int64, uint32, uint64, float, double."),
        };
    }
}
