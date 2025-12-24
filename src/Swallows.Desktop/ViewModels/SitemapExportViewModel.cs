using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Models;
using Avalonia.Controls;

namespace Swallows.Desktop.ViewModels;

public partial class SitemapExportViewModel : ObservableObject
{
    private readonly Window _view;

    [ObservableProperty]
    private bool _includeImages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSplitEnabled))]
    private bool _splitFiles;

    [ObservableProperty]
    private int _maxUrlsPerFile = 50000;

    [ObservableProperty]
    private string _hostingBaseUrl = "";

    public bool IsSplitEnabled => SplitFiles;

    public SitemapOptions? Result { get; private set; }

    public SitemapExportViewModel(Window view)
    {
         _view = view;
    }
    
    // Parameterless constructor for design-time preview
    public SitemapExportViewModel()
    {
        _view = null!;
    }

    [RelayCommand]
    public void Export()
    {
        Result = new SitemapOptions
        {
            IncludeImages = IncludeImages,
            SplitFiles = SplitFiles,
            MaxUrlsPerFile = MaxUrlsPerFile,
            HostingBaseUrl = HostingBaseUrl
        };
        _view.Close(Result);
    }

    [RelayCommand]
    public void Cancel()
    {
        _view.Close(null);
    }
}
