using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Swallows.Desktop.Views;

public partial class StructureWindow : Window
{
    public StructureWindow()
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
