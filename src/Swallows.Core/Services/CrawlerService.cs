using System.Net;
using HtmlAgilityPack;
using Swallows.Core.Data;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class CrawlerService
{
    private readonly HttpClient _httpClient;
    private readonly Func<AppDbContext> _contextFactory;

    public CrawlerService(HttpClient httpClient, Func<AppDbContext> contextFactory)
    {
        _httpClient = httpClient;
        _contextFactory = contextFactory;
    }

    public async Task<ScanSession> StartRecursiveScanAsync(string baseUrl, int maxPages = 1000, int concurrentRequests = 1, int maxDepth = 5, string userAgent = "SwallowsBot/1.0", bool saveImages = false, Action<ScanSession>? onSessionStarted = null, IProgress<ScanProgress>? progress = null, Func<bool>? isPausedCheck = null, CancellationToken cancellationToken = default)
    {
        LoggerService.Info($"Starting Recursive Scan: {baseUrl} | MaxPages: {maxPages}");
        
        var session = new ScanSession
        {
            BaseUrl = baseUrl,
            StartedAt = DateTime.Now,
            Status = "Running"
        };
        
        using (var db = _contextFactory())
        {
            db.ScanSessions.Add(session);
            await db.SaveChangesAsync();
        }

        onSessionStarted?.Invoke(session);

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(baseUrl);
        
        // Initialize robust Robots.txt parser
        var robotsParser = new RobotsTxtParser(_httpClient);
        await robotsParser.ParseRobotsTxtAsync(baseUrl, userAgent);

        int pagesScanned = 0;

        while (queue.Count > 0 && pagesScanned < maxPages)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (isPausedCheck != null && isPausedCheck()) 
            {
                await Task.Delay(1000);
                continue;
            }

            var url = queue.Dequeue();
            
            if (visited.Contains(url)) continue;
            
            // Check robots.txt using parser
            var uri = new Uri(url);
            if (!robotsParser.IsPathAllowed(uri.PathAndQuery, userAgent))
            {
                LoggerService.Info($"Skipping disallowed URL: {url}");
                visited.Add(url); // Mark as visited to avoid re-queueing
                continue;
            }

            visited.Add(url);
            pagesScanned++;

            var page = await CrawlPageAsync(url, session.Id);
            
            // Progress update with latest page
            progress?.Report(new ScanProgress 
            { 
                ScannedCount = pagesScanned, 
                QueueCount = queue.Count, 
                TotalKnownUrls = visited.Count + queue.Count,
                CurrentUrl = url,
                LatestPage = page
            });
            
            if (page != null)
            {
                 // Store links/images in memory for processing
                 var linksForQueue = page.Links.ToList();
                 var imagesForMetrics = page.Images.ToList();
                 
                 // Clear navigation properties to avoid FK constraint issues during save
                 page.Links.Clear();
                 if (!saveImages)
                 {
                     page.Images.Clear(); // Only clear if not saving
                 }
                 
                 using (var db = _contextFactory())
                {
                    db.Pages.Add(page);
                    await db.SaveChangesAsync();
                }

                if (page.StatusCode == 200 && page.ContentLength > 0 && page.InternalLinksCount > 0)
                {
                     // Add internal links to queue (using in-memory snapshot)
                     LoggerService.Info($"Found {page.InternalLinksCount} internal links on {url}");
                     
                     int queuedCount = 0;
                     foreach(var link in linksForQueue)
                     {
                         if(link.IsInternal && !visited.Contains(link.Url))
                         {
                             queue.Enqueue(link.Url);
                             queuedCount++;
                         }
                     }
                     LoggerService.Info($"Queued {queuedCount} new links. Queue size: {queue.Count}");
                }
                else
                {
                    LoggerService.Info($"Page {url} - Status: {page.StatusCode}, ContentLength: {page.ContentLength}, InternalLinks: {page.InternalLinksCount}");
                }
            }
        }
        
        using (var db = _contextFactory())
        {
            var s = await db.ScanSessions.FindAsync(session.Id);
            if (s != null)
            {
                s.FinishedAt = DateTime.Now;
                s.TotalPagesScanned = pagesScanned;
                s.Status = "Completed";
                await db.SaveChangesAsync();
            }
        }

        session.TotalPagesScanned = pagesScanned;
        session.Status = "Completed";
        return session;
    }



    private async Task<Page?> CrawlPageAsync(string url, int sessionId)
    {
        try
        {
            LoggerService.Info($"Crawling: {url}");
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            var page = new Page
            {
                Url = url,
                StatusCode = (int)response.StatusCode,
                LoadTimeMs = 100, // Dummy
                ContentLength = content.Length,
                ScannedAt = DateTime.Now,
                SessionId = sessionId
            };

            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            // Extract SEO Metadata
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            page.Title = titleNode?.InnerText?.Trim();
            
            var metaDesc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
            page.MetaDescription = metaDesc?.GetAttributeValue("content", null);
            
            var canonical = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']");
            page.CanonicalUrl = canonical?.GetAttributeValue("href", null);
            
            var metaRobots = doc.DocumentNode.SelectSingleNode("//meta[@name='robots']");
            page.MetaRobots = metaRobots?.GetAttributeValue("content", null);
            
            // Extract heading counts
            page.H1Count = doc.DocumentNode.SelectNodes("//h1")?.Count ?? 0;
            page.H2Count = doc.DocumentNode.SelectNodes("//h2")?.Count ?? 0;
            page.H3Count = doc.DocumentNode.SelectNodes("//h3")?.Count ?? 0;
            page.H4Count = doc.DocumentNode.SelectNodes("//h4")?.Count ?? 0;
            
            // Check for viewport
            var viewport = doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']");
            page.HasViewport = viewport != null;
            
            // Check for Open Graph
            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            page.HasOpenGraph = ogTitle != null;
            
            // Check for Twitter Card
            var twitterCard = doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:card']");
            page.HasTwitterCard = twitterCard != null;
            
            // Count scripts and styles
            page.ScriptCount = doc.DocumentNode.SelectNodes("//script")?.Count ?? 0;
            page.StyleCount = doc.DocumentNode.SelectNodes("//style")?.Count ?? 0;
            
            // SEO validations
            if (!string.IsNullOrEmpty(page.Title))
            {
                page.IsTitleOptimal = page.Title.Length >= 10 && page.Title.Length <= 60;
            }
            if (!string.IsNullOrEmpty(page.MetaDescription))
            {
                page.IsDescriptionOptimal = page.MetaDescription.Length >= 50 && page.MetaDescription.Length <= 160;
            }
            
            // Size calculations
            page.SizeKb = content.Length / 1024.0;
            
            // Text to HTML ratio & Word count
            var bodyText = doc.DocumentNode.SelectSingleNode("//body")?.InnerText ?? "";
            var visibleText = System.Text.RegularExpressions.Regex.Replace(bodyText, @"\s+", " ").Trim();
            page.WordCount = visibleText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (content.Length > 0)
            {
                page.TextToHtmlRatio = (double)visibleText.Length / content.Length;
            }
            
            // Pagination links
            var relNext = doc.DocumentNode.SelectSingleNode("//link[@rel='next']");
            page.RelNext = relNext?.GetAttributeValue("href", null);
            
            var relPrev = doc.DocumentNode.SelectSingleNode("//link[@rel='prev']");
            page.RelPrev = relPrev?.GetAttributeValue("href", null);
            
            // Hreflangs
            var hreflangs = doc.DocumentNode.SelectNodes("//link[@rel='alternate' and @hreflang]");
            if (hreflangs != null && hreflangs.Count > 0)
            {
                var hreflangList = hreflangs.Select(h => h.GetAttributeValue("hreflang", "")).Where(h => !string.IsNullOrEmpty(h));
                page.Hreflangs = string.Join(", ", hreflangList);
            }
            
            // Favicon
            var favicon = doc.DocumentNode.SelectSingleNode("//link[@rel='icon' or @rel='shortcut icon']");
            page.HasFavicon = favicon != null;
            
            // Redirect handling
            page.FinalUrl = url;
            if (response.RequestMessage?.RequestUri?.ToString() != url)
            {
                page.IsRedirect = true;
                page.RedirectChain = $"{url} -> {response.RequestMessage?.RequestUri}";
            }
            
            // HSTS check
            if (response.Headers.Contains("Strict-Transport-Security"))
            {
                page.HasHsts = true;
            }
            
            // Content hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
                page.ContentHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                var baseUri = new Uri(url);
                foreach (var link in links)
                {
                    var href = link.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    
                    try
                    {
                        var absoluteUri = new Uri(baseUri, href);
                        
                        // Robust internal check (ignore www. prefix difference)
                        var host1 = baseUri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
                        var host2 = absoluteUri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
                        bool isInternal = string.Equals(host1, host2, StringComparison.OrdinalIgnoreCase);
                        
                        page.Links.Add(new Link
                        {
                            Url = absoluteUri.ToString(),
                            Text = link.InnerText,
                            IsInternal = isInternal
                        });

                        if (isInternal) page.InternalLinksCount++;
                        else page.ExternalLinksCount++;
                    }
                    catch { }
                }
            }

            // Extract Images
            var images = doc.DocumentNode.SelectNodes("//img[@src]");
            if (images != null)
            {
                var baseUri = new Uri(url);
                int missingAltCount = 0;
                foreach (var img in images)
                {
                    var src = img.GetAttributeValue("src", "");
                    if (string.IsNullOrWhiteSpace(src)) continue;
                    var alt = img.GetAttributeValue("alt", "");
                    var title = img.GetAttributeValue("title", "");
                    
                    if (string.IsNullOrWhiteSpace(alt))
                    {
                        missingAltCount++;
                    }

                    try
                    {
                        var absoluteUri = new Uri(baseUri, src);
                        page.Images.Add(new ImageAsset
                        {
                            Url = absoluteUri.ToString(),
                            AltText = alt,
                            Title = title
                        });
                    }
                    catch {}
                }
                page.ImageCount = page.Images.Count;
                page.MissingAltCount = missingAltCount;
            }

            return page;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Error crawling {url}: {ex.Message}");
            return new Page
            {
                 Url = url,
                 StatusCode = 0, // Error
                 SessionId = sessionId,
                 ScannedAt = DateTime.Now
            };
        }
    }
}
