using System.Collections.ObjectModel;

namespace Swallows.Core.Models;

public class ScanSession
{
    public int Id { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? FinishedAt { get; set; }
    public int TotalPagesScanned { get; set; }
    public string UserAgent { get; set; } = "SwallowsBot/1.0";
    public string Status { get; set; } = "Running"; // Running, Paused, Stopped, Completed
    
    public ObservableCollection<Page> Pages { get; set; } = new ObservableCollection<Page>();
}
