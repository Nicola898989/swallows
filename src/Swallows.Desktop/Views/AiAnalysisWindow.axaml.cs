using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Swallows.Desktop.Views;

public partial class AiAnalysisWindow : Window
{
    public AiAnalysisWindow()
    {
        InitializeComponent();
    }
    
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
