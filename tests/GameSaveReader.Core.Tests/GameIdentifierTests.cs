using GameSaveReader.Core.GameIdentification;

namespace GameSaveReader.Core.Tests;

public class GameIdentifierTests
{
    private readonly GameIdentifier _identifier = new();

    [Fact]
    public void Identify_WithValidName_ReturnsTrimmedName()
    {
        var result = _identifier.Identify("  Terraria  ");
        Assert.Equal("Terraria", result);
    }

    [Fact]
    public void Identify_WithNull_ReturnsNull()
    {
        Assert.Null(_identifier.Identify(null));
    }

    [Fact]
    public void Identify_WithEmpty_ReturnsNull()
    {
        Assert.Null(_identifier.Identify(""));
    }

    [Fact]
    public void Identify_WithWhitespace_ReturnsNull()
    {
        Assert.Null(_identifier.Identify("   "));
    }

    [Fact]
    public void Identify_WithExactName_ReturnsSameName()
    {
        var result = _identifier.Identify("Dark Souls");
        Assert.Equal("Dark Souls", result);
    }
}
