using System;

namespace Swallows.Core.Models;

public class Page
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public double LoadTimeMs { get; set; }
    public int ContentLength { get; set; }
    public DateTime ScannedAt { get; set; }
    public int SessionId { get; set; }
    
    // Analysis Data
    public string? Title { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? MetaRobots { get; set; }
    public string? Hreflangs { get; set; } // Stored as JSON or comma-separated for simplicity in SQLite 
    
    // Pagination
    public string? RelNext { get; set; }
    public string? RelPrev { get; set; }
    
    // Diagnostics
    public string? FinalUrl { get; set; }
    public bool IsRedirect { get; set; }
    public string? RedirectChain { get; set; } // "A -> B -> C"
    public bool HasHsts { get; set; }
    public string? ContentHash { get; set; } // MD5 hash for duplicate detection

    public double SizeKb { get; set; }
    public double TextToHtmlRatio { get; set; }
    public int ImageCount { get; set; }
    public int InternalLinksCount { get; set; }
    public int ExternalLinksCount { get; set; }

    // Header Counts
    public int H1Count { get; set; }
    public int H2Count { get; set; }
    public int H3Count { get; set; }
    public int H4Count { get; set; }
    
    // Navigation
    // Remove "required" warning by making nullable or initializing. Keeping it simple as previous model.
    public ScanSession Session { get; set; } = null!;
    
    public ICollection<Link> Links { get; set; } = new List<Link>();
    public ICollection<ImageAsset> Images { get; set; } = new List<ImageAsset>();
    
    // Depth Level (0 = Root)
    public int Depth { get; set; }

    // Content Quality
    public int WordCount { get; set; }

    // SEO / Accessibility
    public int MissingAltCount { get; set; }

    // Social
    public bool HasOpenGraph { get; set; }
    public bool HasTwitterCard { get; set; }

    // Technical
    public int ScriptCount { get; set; }
    public int StyleCount { get; set; }
    public bool HasViewport { get; set; }
    public bool HasFavicon { get; set; }

    // SEO Validation
    public bool IsTitleOptimal { get; set; } // Title length 10-60 chars
    public bool IsDescriptionOptimal { get; set; } // Meta desc 50-160 chars
    public double KeywordDensity { get; set; } // % of title words in body

    // Link Quality
    public int BrokenLinksCount { get; set; } // Outbound links returning 4xx/5xx
    public bool IsOrphan { get; set; } // No incoming internal links
    
    // JS Rendering
    public bool HasRenderedDifferences { get; set; } // If JS content significantly differs from raw
    public string? JsErrors { get; set; } // Semicolon separated list of console errors

    // Custom Extraction & Schema
    public string? CustomDataJson { get; set; } // Dictionary<string, string> stored as JSON
    public string? SchemaType { get; set; } // e.g. "Product, BreadcrumbList"
    public string? SchemaErrors { get; set; } // Validation errors
    public bool IsRichSnippetEligible { get; set; }
    
    // Semantic Analysis
    public double ReadabilityScore { get; set; } // 0-100 (e.g. Flesch)
    public string? TopKeywords { get; set; } // JSON list of top 1-3 grams
}
