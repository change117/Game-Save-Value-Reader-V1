using GameSaveReader.Core.GameIdentification;
using GameSaveReader.Core.SaveParser;
using GameSaveReader.Core.SaveStructureSearch;

namespace GameSaveReader.Core.Tests;

public class PipelineTests
{
    private Pipeline CreatePipeline()
    {
        return new Pipeline(
            new GameIdentifier(),
            new LocalKnowledgeBaseSearcher(),
            new SaveFileParser());
    }

    [Fact]
    public void Execute_EmptyGameName_ReturnsFailure()
    {
        var pipeline = CreatePipeline();
        var result = pipeline.Execute("", "/some/path.sav");

        Assert.False(result.IsSuccess);
        Assert.Contains("game name", result.DisplayText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_UnknownGame_ReturnsFailure()
    {
        var pipeline = CreatePipeline();
        var result = pipeline.Execute("UnknownGame99", "/some/path.sav");

        Assert.False(result.IsSuccess);
        Assert.Contains("not", result.DisplayText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_KnownGame_NonExistentFile_ReturnsFailure()
    {
        var pipeline = CreatePipeline();
        var result = pipeline.Execute("Terraria", "/nonexistent/save.wld");

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.DisplayText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_KnownGame_ValidFile_ReturnsSuccess()
    {
        var pipeline = CreatePipeline();
        var tempFile = Path.GetTempFileName();
        try
        {
            // Create a file large enough with known value at Terraria's Health offset (0x4C = 76)
            var data = new byte[256];
            BitConverter.GetBytes(500).CopyTo(data, 0x4C);
            File.WriteAllBytes(tempFile, data);

            var result = pipeline.Execute("Terraria", tempFile);

            Assert.True(result.IsSuccess);
            Assert.Equal("Health: 500", result.DisplayText);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Execute_NullGameName_ReturnsFailure()
    {
        var pipeline = CreatePipeline();
        var result = pipeline.Execute(null, "/some/path.sav");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Execute_FileTooSmall_ReturnsFailure()
    {
        var pipeline = CreatePipeline();
        var tempFile = Path.GetTempFileName();
        try
        {
            // Create a file that's too small for the offset
            File.WriteAllBytes(tempFile, new byte[10]);

            var result = pipeline.Execute("Terraria", tempFile);

            Assert.False(result.IsSuccess);
            Assert.Contains("too small", result.DisplayText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
