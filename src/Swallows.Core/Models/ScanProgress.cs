namespace Swallows.Core.Models;

public class ScanProgress
{
    public int ScannedCount { get; set; }
    public int QueueCount { get; set; }
    public int TotalKnownUrls { get; set; }
    public string CurrentUrl { get; set; } = string.Empty;
    public Page? LatestPage { get; set; }
}
