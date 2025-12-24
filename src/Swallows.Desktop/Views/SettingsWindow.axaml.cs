using Avalonia.Controls;
using Avalonia.Interactivity;
using Swallows.Desktop.ViewModels;

namespace Swallows.Desktop.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(true); // Return true to indicate settings were saved
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false); // Return false to indicate cancel
    }
}
