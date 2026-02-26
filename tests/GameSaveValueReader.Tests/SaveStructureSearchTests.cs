using GameSaveValueReader.Core.Modules.SaveStructureSearch;

namespace GameSaveValueReader.Tests;

public class SaveStructureSearchTests
{
    private readonly LocalSaveStructureSearcher _sut = new();

    [Theory]
    [InlineData("Fable: The Lost Chapters")]
    [InlineData("fable: the lost chapters")]
    [InlineData("FABLE: THE LOST CHAPTERS")]
    public async Task SearchAsync_FindsFableTLC_ByCaseInsensitiveName(string gameName)
    {
        var result = await _sut.SearchAsync(gameName);

        Assert.NotNull(result);
        Assert.Equal("Fable: The Lost Chapters", result.GameName);
        Assert.Equal("Gold", result.ValueName);
        Assert.Equal("int32", result.DataType);
    }

    [Theory]
    [InlineData("fable tlc")]
    [InlineData("fable lost chapters")]
    [InlineData("fable the lost chapters")]
    public async Task SearchAsync_FindsFableTLC_ByAlias(string alias)
    {
        var result = await _sut.SearchAsync(alias);

        Assert.NotNull(result);
        Assert.Equal("Gold", result.ValueName);
    }

    [Fact]
    public async Task SearchAsync_ReturnsNull_ForUnknownGame()
    {
        var result = await _sut.SearchAsync("NonExistentGame12345");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public async Task SearchAsync_ReturnsNull_ForNullOrWhitespace(string? gameName)
    {
        var result = await _sut.SearchAsync(gameName!);
        Assert.Null(result);
    }

    [Fact]
    public async Task CompositeSaveStructureSearcher_ReturnsLocalResult_WhenFound()
    {
        var composite = new CompositeSaveStructureSearcher([new LocalSaveStructureSearcher()]);
        var result = await composite.SearchAsync("Fable: The Lost Chapters");

        Assert.NotNull(result);
        Assert.Equal("Gold", result.ValueName);
    }

    [Fact]
    public async Task CompositeSaveStructureSearcher_ReturnsNull_WhenNoSearcherFindsResult()
    {
        var composite = new CompositeSaveStructureSearcher([new LocalSaveStructureSearcher()]);
        var result = await composite.SearchAsync("GameThatDefinitelyDoesNotExist99999");

        Assert.Null(result);
    }
}
