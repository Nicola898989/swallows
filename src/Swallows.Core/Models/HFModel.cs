namespace Swallows.Core.Models;

public class HFModel
{
    public string Id { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public int Likes { get; set; }
    public string Parameters { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    
    // For sorting/display
    public string Name => Id.Contains("/") ? Id.Split('/')[1] : Id;
}
