using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Data;
using Swallows.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Avalonia.Controls;

namespace Swallows.Desktop.ViewModels;

public partial class HistoryWindowViewModel : ViewModelBase
{
    private readonly Func<AppDbContext> _contextFactory;
    
    public ObservableCollection<ScanSession> Sessions { get; } = new ObservableCollection<ScanSession>();

    [ObservableProperty]
    private ScanSession? _selectedSession;

    public HistoryWindowViewModel(Func<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        LoadSessions();
    }
    
    // Default constructor for design-time
    public HistoryWindowViewModel()
    {
        _contextFactory = () => new AppDbContext();
        if (Design.IsDesignMode)
        {
            Sessions.Add(new ScanSession { BaseUrl = "https://example.com", StartedAt = DateTime.Now, TotalPagesScanned = 42 });
            Sessions.Add(new ScanSession { BaseUrl = "https://google.com", StartedAt = DateTime.Now.AddDays(-1), TotalPagesScanned = 150 });
        }
    }

    public void LoadSessions()
    {
        try 
        {
            Sessions.Clear();
            using (var db = _contextFactory())
            {
                var list = db.ScanSessions
                    .OrderByDescending(s => s.StartedAt)
                    .ToList();
                    
                foreach(var s in list) Sessions.Add(s);
            }
        }
        catch (Exception ex)
        {
            // In a real app, we'd show an error dialog
            Console.WriteLine($"Error loading sessions: {ex.Message}");
        }
    }

    [RelayCommand]
    public void DeleteScan()
    {
        if (SelectedSession == null) return;
        
        try 
        {
            using (var db = _contextFactory())
            {
                var session = db.ScanSessions.Find(SelectedSession.Id);
                if (session != null)
                {
                    db.ScanSessions.Remove(session);
                    // Cascade delete should handle Pages/Links if configured, 
                    // otherwise EF Core conventions usually handle it if relationships are set up.
                    // Given our explicit schema usage, we might need to be careful, but SQLite handles FKs well.
                    
                    // For safety, let's manually delete pages to be sure if cascade isn't automatic in our simple setup
                    var pages = db.Pages.Where(p => p.SessionId == session.Id);
                    db.Pages.RemoveRange(pages);
                    
                    db.SaveChanges();
                }
            }
            Sessions.Remove(SelectedSession);
            SelectedSession = null;
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Error deleting session: {ex.Message}");
        }
    }
}
