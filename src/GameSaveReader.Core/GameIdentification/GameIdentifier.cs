namespace GameSaveReader.Core.GameIdentification;

/// <summary>
/// Module responsible for identifying the game. For V1, this simply
/// accepts a user-provided game name.
/// </summary>
public sealed class GameIdentifier
{
    /// <summary>
    /// Identifies the game from user input.
    /// </summary>
    /// <param name="userInput">The game name typed or selected by the user.</param>
    /// <returns>A normalized game name, or null if input is empty.</returns>
    public string? Identify(string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return null;

        return userInput.Trim();
    }
}
