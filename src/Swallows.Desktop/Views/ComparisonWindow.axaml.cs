using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Swallows.Desktop.ViewModels;

namespace Swallows.Desktop.Views;

public partial class ComparisonWindow : Window
{
    public ComparisonWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
