using GameSaveValueReader.Core.Models;

namespace GameSaveValueReader.Core.Modules.SaveStructureSearch;

/// <summary>
/// Step 2 – Save Structure Search.
/// Implementations search public sources for documented save file structure information
/// and return a <see cref="SaveInfo"/> describing where the currency-equivalent value lives.
/// </summary>
public interface ISaveStructureSearcher
{
    /// <summary>
    /// Searches for save structure information for the given game.
    /// </summary>
    /// <param name="gameName">Normalised game name from the identification step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SaveInfo"/> when structure information is found; <c>null</c> otherwise.
    /// </returns>
    Task<SaveInfo?> SearchAsync(string gameName, CancellationToken cancellationToken = default);
}
