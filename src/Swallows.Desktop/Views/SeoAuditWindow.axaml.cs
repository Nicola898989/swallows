using Avalonia.Controls;
using Swallows.Desktop.ViewModels;

namespace Swallows.Desktop.Views;

public partial class SeoAuditWindow : Window
{
    public SeoAuditWindow()
    {
        InitializeComponent();
    }

    public SeoAuditWindow(SeoAuditViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
