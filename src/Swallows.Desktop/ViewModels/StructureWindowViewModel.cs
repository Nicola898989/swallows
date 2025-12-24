using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Swallows.Core.Data;
using Swallows.Core.Models;
using Microsoft.EntityFrameworkCore;
using Swallows.Core.Services;

namespace Swallows.Desktop.ViewModels;

public partial class StructureWindowViewModel : ViewModelBase
{
    private readonly System.Func<AppDbContext> _contextFactory;
    private readonly int _sessionId;

    [ObservableProperty]
    private ObservableCollection<StructureNode> _nodes = new();

    [ObservableProperty]
    private string _title = "Site Structure";

    [ObservableProperty]
    private string _status = "Loading...";

    public StructureWindowViewModel(System.Func<AppDbContext> contextFactory, int sessionId)
    {
        _contextFactory = contextFactory;
        _sessionId = sessionId;
        LoadStructure();
    }
    
    // Default ctor for Design
    public StructureWindowViewModel() : this(() => new AppDbContext(), 0) {}

    private async void LoadStructure()
    {
        if (_sessionId == 0) return;

        try
        {
            using var db = _contextFactory();
            var urls = await db.Pages
                .Where(p => p.SessionId == _sessionId)
                .Select(p => p.Url)
                .ToListAsync();

            var tree = StructureBuilder.BuildTree(urls);
            Nodes = tree;
            Status = $"Loaded {urls.Count} URLs from session {_sessionId}.";
        }
        catch (System.Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
    }
}
