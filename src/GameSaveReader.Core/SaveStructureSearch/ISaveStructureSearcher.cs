namespace GameSaveReader.Core.SaveStructureSearch;

/// <summary>
/// Interface for searching save file structure information.
/// Implementations can use local knowledge bases, web scraping, or APIs.
/// </summary>
public interface ISaveStructureSearcher
{
    /// <summary>
    /// Searches for the currency-equivalent value descriptor for the given game.
    /// </summary>
    /// <param name="gameName">The identified game name.</param>
    /// <returns>
    /// A <see cref="SaveValueDescriptor"/> with offset, data type, and value name,
    /// or null if no information was found.
    /// </returns>
    SaveValueDescriptor? Search(string gameName);
}
