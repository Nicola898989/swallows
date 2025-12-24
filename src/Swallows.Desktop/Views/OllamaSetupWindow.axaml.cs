using Avalonia.Controls;
using Avalonia.Interactivity;
using Swallows.Desktop.ViewModels;

namespace Swallows.Desktop.Views;

public partial class OllamaSetupWindow : Window
{
    public OllamaSetupWindow()
    {
        InitializeComponent();
        DataContext = new OllamaSetupViewModel();
    }

    private void OnSkipClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
