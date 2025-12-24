namespace Swallows.Core.Models;

public class SitemapOptions
{
    public bool IncludeImages { get; set; }
    public bool SplitFiles { get; set; }
    public int MaxUrlsPerFile { get; set; } = 50000;
    public string HostingBaseUrl { get; set; } = ""; // Required for Sitemap Index
}
