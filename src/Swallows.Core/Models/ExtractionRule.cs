namespace Swallows.Core.Models;

public enum ExtractionType
{
    XPath,
    Regex
}

public class ExtractionRule
{
    public string Name { get; set; } = string.Empty;
    public ExtractionType Type { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public bool ExtractFirstOnly { get; set; }
}
