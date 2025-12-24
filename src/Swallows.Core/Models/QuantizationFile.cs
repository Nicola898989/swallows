namespace Swallows.Core.Models;

public class QuantizationFile
{
    public string Filename { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty; // Local path
    public string QuantizationLevel { get; set; } = string.Empty; // e.g. Q4_K_M
    public long Size { get; set; }
    
    public string SizeFormatted { get; set; } = string.Empty;
}
