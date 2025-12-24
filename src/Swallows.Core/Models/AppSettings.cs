using System.Collections.ObjectModel;

namespace Swallows.Core.Models;

public class AppSettings
{
    public int Id { get; set; }
    
    // Crawler
    public string UserAgent { get; set; } = "SwallowsBot/1.0";
    public int DelayBetweenRequestsMs { get; set; } = 500;
    public string? ProxyUrl { get; set; }
    public int ConcurrentRequests { get; set; } = 3;
    public int MaxPages { get; set; } = 1000;
    public int MaxDepth { get; set; } = 5;
    public bool SaveImages { get; set; } = false; // Save images to DB for sitemap export
    public int TimeoutSeconds { get; set; } = 30;

    // Headless
    public bool EnableJavaScript { get; set; }
    public int AjaxTimeoutSeconds { get; set; } = 10;
    public bool EnableHeadlessImages { get; set; }

    // Stored as JSON for simplicity
    public string ExtractionRulesJson { get; set; } = "";
    public string LlmSettingsJson { get; set; } = "";
    
    public AppSettings() 
    {
    }
}

public class LlmSettings
{
    public LlmProviderType Provider { get; set; }
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ModelName { get; set; } = "";
}
