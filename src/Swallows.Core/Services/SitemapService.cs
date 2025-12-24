using System.Text;
using System.Globalization;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class SitemapService
{
    private const string NsImage = "http://www.google.com/schemas/sitemap-image/1.1";

    public Dictionary<string, string> GenerateSitemaps(ScanSession session, SitemapOptions options)
    {
        var validPages = session.Pages?.Where(p => p.StatusCode == 200).ToList() ?? new List<Page>();
        var result = new Dictionary<string, string>();

        if (!options.SplitFiles || validPages.Count <= options.MaxUrlsPerFile)
        {
            // Single file
            var xml = GenerateUrlSet(validPages, options);
            result.Add("sitemap.xml", xml);
        }
        else
        {
            // Split files
            var chunks = validPages.Chunk(options.MaxUrlsPerFile).ToList();
            var indexXml = new StringBuilder();
            indexXml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            indexXml.AppendLine("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            
            var hostingBase = string.IsNullOrWhiteSpace(options.HostingBaseUrl) 
                ? session.BaseUrl.TrimEnd('/') 
                : options.HostingBaseUrl.TrimEnd('/');

            for (int i = 0; i < chunks.Count; i++)
            {
                var filename = $"sitemap-{i + 1}.xml";
                var xml = GenerateUrlSet(chunks[i], options);
                result.Add(filename, xml);

                indexXml.AppendLine("  <sitemap>");
                indexXml.AppendLine($"    <loc>{hostingBase}/{filename}</loc>");
                indexXml.AppendLine($"    <lastmod>{DateTime.Now:yyyy-MM-dd}</lastmod>");
                indexXml.AppendLine("  </sitemap>");
            }
            
            indexXml.AppendLine("</sitemapindex>");
            result.Add("sitemap_index.xml", indexXml.ToString());
        }

        return result;
    }

    // Keep old method for compatibility, delegating to new
    public Task<string> GenerateSitemapXml(ScanSession session)
    {
        var dict = GenerateSitemaps(session, new SitemapOptions());
        return Task.FromResult(dict.Values.First());
    }

    private string GenerateUrlSet(IEnumerable<Page> pages, SitemapOptions options)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        
        if (options.IncludeImages)
        {
            xml.AppendLine($"<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:image=\"{NsImage}\">");
        }
        else
        {
            xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        }

        foreach (var page in pages)
        {
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{EscapeXml(page.Url)}</loc>");
            
            var priority = CalculatePriority(page);
            xml.AppendLine($"    <priority>{priority.ToString("F1", CultureInfo.InvariantCulture)}</priority>");
            
            var changeFreq = GetChangeFrequency(page);
            xml.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
            
            if (options.IncludeImages && page.Images != null)
            {
                 // Avoid duplicates
                 var uniqueImages = page.Images.Select(i => (i.Url, i.AltText)).Distinct().ToList();
                 foreach(var img in uniqueImages)
                 {
                     xml.AppendLine("    <image:image>");
                     xml.AppendLine($"      <image:loc>{EscapeXml(img.Url)}</image:loc>");
                     if (!string.IsNullOrWhiteSpace(img.AltText))
                     {
                         xml.AppendLine($"      <image:title>{EscapeXml(img.AltText)}</image:title>");
                     }
                     xml.AppendLine("    </image:image>");
                 }
            }
            
            xml.AppendLine("  </url>");
        }

        xml.AppendLine("</urlset>");
        return xml.ToString();
    }

    private double CalculatePriority(Page page)
    {
        if (page.Depth == 0) return 1.0;
        if (page.Depth == 1) return 0.8;
        if (page.Depth == 2) return 0.6;
        return 0.5;
    }

    private string GetChangeFrequency(Page page)
    {
        if (page.Depth == 0) return "daily";
        if (page.Depth == 1) return "weekly";
        return "monthly";
    }

    private string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }
}
