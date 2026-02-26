using GameSaveReader.Core.SaveStructureSearch;

namespace GameSaveReader.Core.Tests;

public class LocalKnowledgeBaseSearcherTests
{
    private readonly LocalKnowledgeBaseSearcher _searcher = new();

    [Fact]
    public void Search_KnownGame_ReturnsDescriptor()
    {
        var result = _searcher.Search("Terraria");

        Assert.NotNull(result);
        Assert.Equal("Health", result.ValueName);
        Assert.Equal(SaveValueType.Int32, result.DataType);
        Assert.Equal(0x4C, result.Offset);
    }

    [Fact]
    public void Search_CaseInsensitive_ReturnsDescriptor()
    {
        var result = _searcher.Search("terraria");

        Assert.NotNull(result);
        Assert.Equal("Health", result.ValueName);
    }

    [Fact]
    public void Search_UnknownGame_ReturnsNull()
    {
        var result = _searcher.Search("NonExistentGame12345");
        Assert.Null(result);
    }

    [Fact]
    public void Search_Null_ReturnsNull()
    {
        var result = _searcher.Search(null!);
        Assert.Null(result);
    }

    [Fact]
    public void Search_Empty_ReturnsNull()
    {
        var result = _searcher.Search("");
        Assert.Null(result);
    }

    [Fact]
    public void GetKnownGameNames_ReturnsNonEmptyList()
    {
        var names = _searcher.GetKnownGameNames();
        Assert.NotEmpty(names);
        Assert.Contains(names, n => n.Contains("Terraria", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_FableTLC_ReturnsGoldDescriptor()
    {
        var result = _searcher.Search("Fable - The Lost Chapters");

        Assert.NotNull(result);
        Assert.Equal("Gold", result.ValueName);
        Assert.Equal(SaveValueType.Int32, result.DataType);
    }
}
