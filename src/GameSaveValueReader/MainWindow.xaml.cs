using Microsoft.Win32;
using System.Windows;
using GameSaveValueReader.ViewModels;

namespace GameSaveValueReader;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm        = new MainViewModel();
        DataContext = _vm;
    }

    private void LoadSaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Select Game Save File",
            Filter = "All files (*.*)|*.*|Save files (*.sav)|*.sav|Data files (*.dat)|*.dat",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _vm.SaveFile = dialog.FileName;
            _vm.LoadSaveCommand.Execute(null);
        }
    }
}
