using GameSaveValueReader.Core.Modules.GameIdentification;

namespace GameSaveValueReader.Tests;

public class GameIdentificationTests
{
    private readonly GameIdentifier _sut = new();

    [Fact]
    public void IdentifyGame_ReturnsTrimmedName()
    {
        var result = _sut.IdentifyGame("  Fable  ");
        Assert.Equal("Fable", result);
    }

    [Theory]
    [InlineData("Fable: The Lost Chapters")]
    [InlineData("Gothic 2")]
    [InlineData("Baldur's Gate")]
    public void IdentifyGame_ReturnsInputUnchanged_WhenAlreadyTrimmed(string name)
    {
        Assert.Equal(name, _sut.IdentifyGame(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void IdentifyGame_Throws_WhenInputIsNullOrWhitespace(string? input)
    {
        Assert.Throws<ArgumentException>(() => _sut.IdentifyGame(input!));
    }
}
