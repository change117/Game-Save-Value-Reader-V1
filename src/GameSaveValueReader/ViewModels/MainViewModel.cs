using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using GameSaveValueReader.Core.Modules.GameIdentification;
using GameSaveValueReader.Core.Modules.SaveParse;
using GameSaveValueReader.Core.Modules.SaveStructureSearch;

namespace GameSaveValueReader.ViewModels;

/// <summary>
/// Coordinates the three-step pipeline: identify → search → parse.
/// Exposes bindable properties consumed by <see cref="MainWindow"/>.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IGameIdentifier _gameIdentifier;
    private readonly ISaveStructureSearcher _searcher;
    private readonly ISaveParser _parser;

    private string _gameName   = string.Empty;
    private string _saveFile   = string.Empty;
    private string _result     = string.Empty;
    private bool   _isBusy;

    public MainViewModel()
        : this(new GameIdentifier(),
               CompositeSaveStructureSearcher.CreateDefault(),
               new SaveParser())
    { }

    public MainViewModel(
        IGameIdentifier gameIdentifier,
        ISaveStructureSearcher searcher,
        ISaveParser parser)
    {
        _gameIdentifier = gameIdentifier;
        _searcher       = searcher;
        _parser         = parser;

        LoadSaveCommand = new RelayCommand(
            _ => RunPipelineAsync(),
            _ => !IsBusy && !string.IsNullOrWhiteSpace(GameName));
    }

    // ---------------------------------------------------------------
    // Bindable properties
    // ---------------------------------------------------------------

    public string GameName
    {
        get => _gameName;
        set { _gameName = value; OnPropertyChanged(); ((RelayCommand)LoadSaveCommand).RaiseCanExecuteChanged(); }
    }

    public string SaveFile
    {
        get => _saveFile;
        set { _saveFile = value; OnPropertyChanged(); }
    }

    public string Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); ((RelayCommand)LoadSaveCommand).RaiseCanExecuteChanged(); }
    }

    public ICommand LoadSaveCommand { get; }

    // ---------------------------------------------------------------
    // Pipeline
    // ---------------------------------------------------------------

    private async void RunPipelineAsync()
    {
        IsBusy = true;
        Result = "Searching for save structure…";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Step 1 – identify game
            string identified = _gameIdentifier.IdentifyGame(GameName);

            // Step 2 – search for save structure
            var saveInfo = await _searcher.SearchAsync(identified, cts.Token).ConfigureAwait(true);
            if (saveInfo is null)
            {
                Result = $"No documented save structure found for \"{identified}\".\n" +
                         "Try a different game name or check community sources manually.";
                return;
            }

            // Step 3 – parse the save file
            long value = _parser.ParseValue(SaveFile, saveInfo);
            Result = $"{saveInfo.ValueName}: {value}";
        }
        catch (System.IO.FileNotFoundException ex)
        {
            Result = $"Save file not found: {ex.FileName}";
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Result = $"Parse error: {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            Result = $"Input error: {ex.Message}";
        }
        catch (NotSupportedException ex)
        {
            Result = $"Unsupported format: {ex.Message}";
        }
        catch (Exception ex)
        {
            Result = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------
    // INotifyPropertyChanged
    // ---------------------------------------------------------------

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
