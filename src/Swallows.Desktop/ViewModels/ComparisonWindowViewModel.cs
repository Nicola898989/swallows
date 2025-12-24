using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Data;
using Swallows.Core.Models;
using Swallows.Core.Services;

namespace Swallows.Desktop.ViewModels;

public partial class ComparisonWindowViewModel : ViewModelBase
{
    private readonly System.Func<AppDbContext> _contextFactory;
    private readonly ComparisonService _comparisonService;

    [ObservableProperty]
    private ObservableCollection<ScanSession> _sessions = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private ScanSession? _selectedBaseline;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompareCommand))]
    private ScanSession? _selectedComparison;

    [ObservableProperty]
    private ObservableCollection<PageDiff> _newPages = new();
    
    [ObservableProperty]
    private ObservableCollection<PageDiff> _removedPages = new();
    
    [ObservableProperty]
    private ObservableCollection<PageDiff> _statusChanges = new();
    
    [ObservableProperty]
    private ObservableCollection<PageDiff> _metaChanges = new();

    [ObservableProperty]
    private string _statusMessage = "Select two scans to compare.";

    [ObservableProperty]
    private bool _isComparing;

    public ComparisonWindowViewModel() : this(() => new AppDbContext())
    {
    }

    public ComparisonWindowViewModel(System.Func<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _comparisonService = new ComparisonService(contextFactory);
        LoadSessions();
    }

    private async void LoadSessions()
    {
        using var db = _contextFactory();
        var list = db.ScanSessions.OrderByDescending(s => s.StartedAt).ToList();
        Sessions = new ObservableCollection<ScanSession>(list);
    }

    private bool CanCompare => SelectedBaseline != null && SelectedComparison != null && !IsComparing;

    [RelayCommand(CanExecute = nameof(CanCompare))]
    private async Task Compare()
    {
        if (SelectedBaseline == null || SelectedComparison == null) return;

        IsComparing = true;
        StatusMessage = "Comparing scans... (This may take a moment)";
        
        NewPages.Clear();
        RemovedPages.Clear();
        StatusChanges.Clear();
        MetaChanges.Clear();

        try 
        {
            var result = await Task.Run(() => _comparisonService.CompareSessionsAsync(SelectedBaseline.Id, SelectedComparison.Id));
            
            NewPages = new ObservableCollection<PageDiff>(result.NewPages);
            RemovedPages = new ObservableCollection<PageDiff>(result.RemovedPages);
            StatusChanges = new ObservableCollection<PageDiff>(result.StatusChanges);
            MetaChanges = new ObservableCollection<PageDiff>(result.MetaChanges);

            StatusMessage = $"Comparison Complete. Found {NewPages.Count} new, {RemovedPages.Count} removed, {StatusChanges.Count} status changes, {MetaChanges.Count} meta changes.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }
}
