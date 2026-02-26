using System.Windows;
using GameSaveReader.Core;
using GameSaveReader.Core.GameIdentification;
using GameSaveReader.Core.SaveParser;
using GameSaveReader.Core.SaveStructureSearch;
using Microsoft.Win32;

namespace GameSaveReader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Pipeline _pipeline;

    public MainWindow()
    {
        InitializeComponent();

        var searcher = new LocalKnowledgeBaseSearcher();
        _pipeline = new Pipeline(
            new GameIdentifier(),
            searcher,
            new SaveFileParser());

        // Populate the combo box with known game names
        foreach (var name in searcher.GetKnownGameNames())
            GameNameComboBox.Items.Add(name);
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Game Save File",
            Filter = "All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        var gameName = GameNameComboBox.Text;
        var result = _pipeline.Execute(gameName, dialog.FileName);

        ResultTextBlock.Text = result.DisplayText;
        ResultTextBlock.Foreground = result.IsSuccess
            ? System.Windows.Media.Brushes.Black
            : System.Windows.Media.Brushes.DarkRed;
    }
}