namespace Swallows.Core.Models;

public class Link
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;
}
