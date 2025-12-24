namespace Swallows.Core.Models;

public class ImageAsset
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public long? SizeBytes { get; set; }
    public string? ContentHash { get; set; }
    
    // Foreign Key
    public int PageId { get; set; }
    public Page Page { get; set; } = null!;
}
