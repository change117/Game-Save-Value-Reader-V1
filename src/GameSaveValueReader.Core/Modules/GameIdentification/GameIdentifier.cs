namespace GameSaveValueReader.Core.Modules.GameIdentification;

/// <summary>
/// Default implementation of <see cref="IGameIdentifier"/>.
/// In V1 the user types the game name, so identification is simply normalisation.
/// </summary>
public class GameIdentifier : IGameIdentifier
{
    /// <inheritdoc />
    public string IdentifyGame(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Game name cannot be empty.", nameof(input));

        return input.Trim();
    }
}
