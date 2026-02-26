using GameSaveReader.Core.SaveParser;
using GameSaveReader.Core.SaveStructureSearch;

namespace GameSaveReader.Core.Tests;

public class SaveFileParserTests
{
    private readonly SaveFileParser _parser = new();

    [Fact]
    public void ParseFromStream_Int32_ReadsCorrectValue()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 4,
            DataType = SaveValueType.Int32,
            ValueName = "Gold"
        };

        // Create a stream with known data: 4 filler bytes + Int32 value 2456
        var data = new byte[8];
        BitConverter.GetBytes(2456).CopyTo(data, 4);

        using var stream = new MemoryStream(data);
        var result = _parser.ParseFromStream(stream, descriptor);

        Assert.Equal("Gold", result.ValueName);
        Assert.Equal(2456, (int)result.Value);
        Assert.Equal("Gold: 2456", result.ToString());
    }

    [Fact]
    public void ParseFromStream_Int64_ReadsCorrectValue()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 0,
            DataType = SaveValueType.Int64,
            ValueName = "Credits"
        };

        var data = new byte[8];
        BitConverter.GetBytes(999999999L).CopyTo(data, 0);

        using var stream = new MemoryStream(data);
        var result = _parser.ParseFromStream(stream, descriptor);

        Assert.Equal("Credits", result.ValueName);
        Assert.Equal(999999999L, (long)result.Value);
    }

    [Fact]
    public void ParseFromStream_Single_ReadsCorrectValue()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 0,
            DataType = SaveValueType.Single,
            ValueName = "Health"
        };

        var data = new byte[4];
        BitConverter.GetBytes(100.5f).CopyTo(data, 0);

        using var stream = new MemoryStream(data);
        var result = _parser.ParseFromStream(stream, descriptor);

        Assert.Equal("Health", result.ValueName);
        Assert.Equal(100.5f, (float)result.Value);
    }

    [Fact]
    public void ParseFromStream_Double_ReadsCorrectValue()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 0,
            DataType = SaveValueType.Double,
            ValueName = "Experience"
        };

        var data = new byte[8];
        BitConverter.GetBytes(12345.6789).CopyTo(data, 0);

        using var stream = new MemoryStream(data);
        var result = _parser.ParseFromStream(stream, descriptor);

        Assert.Equal("Experience", result.ValueName);
        Assert.Equal(12345.6789, (double)result.Value);
    }

    [Fact]
    public void ParseFromStream_StreamTooShort_ThrowsArgumentException()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 10,
            DataType = SaveValueType.Int32,
            ValueName = "Gold"
        };

        using var stream = new MemoryStream(new byte[5]); // too short
        Assert.Throws<ArgumentException>(() => _parser.ParseFromStream(stream, descriptor));
    }

    [Fact]
    public void ParseFromStream_NullStream_ThrowsArgumentNullException()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 0,
            DataType = SaveValueType.Int32,
            ValueName = "Gold"
        };

        Assert.Throws<ArgumentNullException>(() => _parser.ParseFromStream(null!, descriptor));
    }

    [Fact]
    public void ParseFromStream_NullDescriptor_ThrowsArgumentNullException()
    {
        using var stream = new MemoryStream(new byte[4]);
        Assert.Throws<ArgumentNullException>(() => _parser.ParseFromStream(stream, null!));
    }

    [Fact]
    public void Parse_NonExistentFile_ThrowsFileNotFoundException()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 0,
            DataType = SaveValueType.Int32,
            ValueName = "Gold"
        };

        Assert.Throws<FileNotFoundException>(() => _parser.Parse("/nonexistent/path/save.dat", descriptor));
    }

    [Fact]
    public void Parse_RealFile_ReadsCorrectly()
    {
        var descriptor = new SaveValueDescriptor
        {
            Offset = 8,
            DataType = SaveValueType.Int32,
            ValueName = "Coins"
        };

        var tempFile = Path.GetTempFileName();
        try
        {
            var data = new byte[16];
            BitConverter.GetBytes(42).CopyTo(data, 8);
            File.WriteAllBytes(tempFile, data);

            var result = _parser.Parse(tempFile, descriptor);

            Assert.Equal("Coins", result.ValueName);
            Assert.Equal(42, (int)result.Value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
