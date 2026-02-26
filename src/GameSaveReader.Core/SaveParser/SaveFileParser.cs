using GameSaveReader.Core.SaveStructureSearch;

namespace GameSaveReader.Core.SaveParser;

/// <summary>
/// Module responsible for reading a value from a binary save file
/// at a specific offset using the specified data type.
/// </summary>
public sealed class SaveFileParser
{
    /// <summary>
    /// Parses a value from the given save file using the provided descriptor.
    /// </summary>
    /// <param name="filePath">Path to the save file.</param>
    /// <param name="descriptor">Describes where and how to read the value.</param>
    /// <returns>The parsed value, or null if reading fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath or descriptor is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the save file does not exist.</exception>
    public ParsedValue Parse(string filePath, SaveValueDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Save file not found.", filePath);

        return ParseFromStream(File.OpenRead(filePath), descriptor);
    }

    /// <summary>
    /// Parses a value from a stream using the provided descriptor.
    /// Useful for testing without file system dependencies.
    /// </summary>
    /// <param name="stream">The stream containing save data.</param>
    /// <param name="descriptor">Describes where and how to read the value.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentException">Thrown when the stream is too short.</exception>
    public ParsedValue ParseFromStream(Stream stream, SaveValueDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(descriptor);

        int bytesNeeded = GetByteSize(descriptor.DataType);
        long requiredLength = descriptor.Offset + bytesNeeded;

        if (stream.Length < requiredLength)
            throw new ArgumentException(
                $"Save file is too small. Expected at least {requiredLength} bytes, but file is {stream.Length} bytes.");

        stream.Seek(descriptor.Offset, SeekOrigin.Begin);

        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true);

        object value = descriptor.DataType switch
        {
            SaveValueType.Int32 => (object)reader.ReadInt32(),
            SaveValueType.Int64 => (object)reader.ReadInt64(),
            SaveValueType.Single => (object)reader.ReadSingle(),
            SaveValueType.Double => (object)reader.ReadDouble(),
            _ => throw new NotSupportedException($"Unsupported data type: {descriptor.DataType}")
        };

        return new ParsedValue
        {
            ValueName = descriptor.ValueName,
            Value = value
        };
    }

    private static int GetByteSize(SaveValueType dataType) => dataType switch
    {
        SaveValueType.Int32 => 4,
        SaveValueType.Int64 => 8,
        SaveValueType.Single => 4,
        SaveValueType.Double => 8,
        _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
    };
}
