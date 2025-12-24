using Avalonia.Controls;
using Swallows.Desktop.ViewModels;

namespace Swallows.Desktop.Views;

public partial class SitemapExportWindow : Window
{
    public SitemapExportWindow()
    {
        InitializeComponent();
        DataContext = new SitemapExportViewModel(this);
    }
}
