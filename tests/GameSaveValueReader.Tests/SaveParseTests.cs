using GameSaveValueReader.Core.Models;
using GameSaveValueReader.Core.Modules.SaveParse;

namespace GameSaveValueReader.Tests;

public class SaveParseTests : IDisposable
{
    private readonly SaveParser _sut = new();
    private readonly List<string> _tempFiles = new();

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private string CreateTempFile(byte[] contents)
    {
        var path = Path.GetTempFileName();
        File.WriteAllBytes(path, contents);
        _tempFiles.Add(path);
        return path;
    }

    // Writes a little-endian int32 at offset 0.
    private string CreateInt32File(int value)
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return CreateTempFile(bytes);
    }

    // ---------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(2456)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    public void ParseValue_ReadsInt32_AtOffset0(int expected)
    {
        var path     = CreateInt32File(expected);
        var saveInfo = new SaveInfo { Offset = 0, DataType = "int32" };

        var result = _sut.ParseValue(path, saveInfo);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseValue_ReadsInt32_AtNonZeroOffset()
    {
        // 4 padding bytes then an int32 = 2456
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 2456);
        var path     = CreateTempFile(bytes);
        var saveInfo = new SaveInfo { Offset = 4, DataType = "int32" };

        var result = _sut.ParseValue(path, saveInfo);

        Assert.Equal(2456, result);
    }

    [Fact]
    public void ParseValue_ReadsInt64()
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes, 9_999_999_999L);
        var path     = CreateTempFile(bytes);
        var saveInfo = new SaveInfo { Offset = 0, DataType = "int64" };

        Assert.Equal(9_999_999_999L, _sut.ParseValue(path, saveInfo));
    }

    [Fact]
    public void ParseValue_ReadsUInt32()
    {
        var bytes = new byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes, 4_000_000_000u);
        var path     = CreateTempFile(bytes);
        var saveInfo = new SaveInfo { Offset = 0, DataType = "uint32" };

        Assert.Equal(4_000_000_000L, _sut.ParseValue(path, saveInfo));
    }

    [Fact]
    public void ParseValue_ReadsFloat_RoundedToLong()
    {
        // Write 1234.7f as 4 bytes
        var bytes = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(bytes, 1234.7f);
        var path     = CreateTempFile(bytes);
        var saveInfo = new SaveInfo { Offset = 0, DataType = "float" };

        Assert.Equal(1235L, _sut.ParseValue(path, saveInfo));
    }

    [Fact]
    public void ParseValue_Throws_WhenFileNotFound()
    {
        var saveInfo = new SaveInfo { Offset = 0, DataType = "int32" };
        Assert.Throws<FileNotFoundException>(() =>
            _sut.ParseValue(@"C:\does\not\exist.sav", saveInfo));
    }

    [Fact]
    public void ParseValue_Throws_WhenOffsetBeyondFile()
    {
        var path     = CreateInt32File(42);
        var saveInfo = new SaveInfo { Offset = 1000, DataType = "int32" };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _sut.ParseValue(path, saveInfo));
    }

    [Fact]
    public void ParseValue_Throws_ForUnsupportedDataType()
    {
        var path     = CreateInt32File(42);
        var saveInfo = new SaveInfo { Offset = 0, DataType = "nybble" };

        Assert.Throws<NotSupportedException>(() =>
            _sut.ParseValue(path, saveInfo));
    }

    // ---------------------------------------------------------------
    // Cleanup
    // ---------------------------------------------------------------

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }
}
