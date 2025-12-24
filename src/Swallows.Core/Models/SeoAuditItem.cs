namespace Swallows.Core.Models;

public class SeoAuditItem
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty; // Title text
    public string TitleStatus { get; set; } = string.Empty;
    public string MetaStatus { get; set; } = string.Empty;
    public string H1Status { get; set; } = string.Empty;
    public int MissingAlt { get; set; }
    public int BrokenLinks { get; set; }
    public int WordCount { get; set; }
    public string SeoScore { get; set; } = "Good"; // Changed from int to string based on usage (Good, Needs Improvement, Poor)
    public int IssueCount { get; set; }
    public string TopIssues { get; set; } = string.Empty;
    
    // Technical checks
    public bool HasViewport { get; set; }
    public bool HasOpenGraph { get; set; }
    public bool HasTwitterCard { get; set; }
    public bool HasCanonical { get; set; }
}
