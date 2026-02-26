using GameSaveReader.Core.GameIdentification;
using GameSaveReader.Core.SaveParser;
using GameSaveReader.Core.SaveStructureSearch;

namespace GameSaveReader.Core;

/// <summary>
/// Orchestrates the three-step pipeline: identify → search → parse.
/// Each step is a separate module that can be independently tested and replaced.
/// </summary>
public sealed class Pipeline
{
    private readonly GameIdentifier _identifier;
    private readonly ISaveStructureSearcher _searcher;
    private readonly SaveFileParser _parser;

    public Pipeline(GameIdentifier identifier, ISaveStructureSearcher searcher, SaveFileParser parser)
    {
        _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    /// <summary>
    /// Runs the full pipeline: identify game, search for value location, parse the save file.
    /// </summary>
    /// <param name="gameNameInput">Raw user input for game name.</param>
    /// <param name="saveFilePath">Path to the game save file.</param>
    /// <returns>A result containing either the parsed value or an error message.</returns>
    public PipelineResult Execute(string? gameNameInput, string saveFilePath)
    {
        // Step 1: Identify
        var gameName = _identifier.Identify(gameNameInput);
        if (gameName is null)
            return PipelineResult.Failure("Please enter a game name.");

        // Step 2: Search
        var descriptor = _searcher.Search(gameName);
        if (descriptor is null)
            return PipelineResult.Failure(
                $"No save structure information found for \"{gameName}\". " +
                "The game may not be in the knowledge base yet.");

        // Step 3: Parse
        try
        {
            var parsed = _parser.Parse(saveFilePath, descriptor);
            return PipelineResult.Success(parsed);
        }
        catch (FileNotFoundException)
        {
            return PipelineResult.Failure("Save file not found.");
        }
        catch (ArgumentException ex)
        {
            return PipelineResult.Failure(ex.Message);
        }
    }
}

/// <summary>
/// Result of running the pipeline.
/// </summary>
public sealed class PipelineResult
{
    public bool IsSuccess { get; private init; }
    public ParsedValue? Value { get; private init; }
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Display string: either "ValueName: Amount" or the error message.
    /// </summary>
    public string DisplayText => IsSuccess ? Value!.ToString() : ErrorMessage!;

    public static PipelineResult Success(ParsedValue value) =>
        new() { IsSuccess = true, Value = value };

    public static PipelineResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
