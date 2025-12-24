namespace Swallows.Core.Models;

public class PageDiff
{
    public string Url { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // New, Removed, Status, Title, Meta
    
    // For specific changes
    public int? OldStatusCode { get; set; }
    public int? NewStatusCode { get; set; }
    
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
