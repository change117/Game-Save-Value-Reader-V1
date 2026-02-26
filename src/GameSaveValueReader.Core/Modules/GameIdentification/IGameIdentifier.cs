namespace GameSaveValueReader.Core.Modules.GameIdentification;

/// <summary>
/// Step 1 – Game Identification.
/// In V1 the user supplies the game name directly; this module normalises the input.
/// </summary>
public interface IGameIdentifier
{
    /// <summary>
    /// Accepts raw user input and returns a normalised game name string
    /// suitable for use in the search step.
    /// </summary>
    /// <param name="input">Raw text entered by the user.</param>
    /// <returns>Trimmed, non-empty game name.</returns>
    /// <exception cref="ArgumentException">Thrown when input is null or whitespace.</exception>
    string IdentifyGame(string input);
}
