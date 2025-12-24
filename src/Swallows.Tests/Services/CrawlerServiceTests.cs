using System.Net;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Swallows.Core.Data;
using Swallows.Core.Models;
using Swallows.Core.Services;

namespace Swallows.Tests.Services;

public class CrawlerServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public CrawlerServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com")
        };

        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task StartRecursiveScanAsync_ShouldCrawlInternalLinksOnly_AndRespectMaxPages()
    {
        // Arrange
        // Mock Robots.txt
        SetupResponse("https://example.com/robots.txt", "User-agent: *\nAllow: /");
        
        // Mock Home Page
        SetupResponse("https://example.com/", @"
            <html>
                <body>
                    <a href='/page1'>Internal 1</a>
                    <a href='https://external.com'>External</a>
                </body>
            </html>");

        // Mock Page 1
        SetupResponse("https://example.com/page1", @"
            <html>
                <body>
                    <a href='/page2'>Internal 2</a>
                </body>
            </html>");

        // Mock Page 2 (should be skipped if MaxPages=2)
        SetupResponse("https://example.com/page2", "<html></html>");

        // Since the service uses a factory, we can't just pass 'context'.
        // We need to pass a factory that returns a context connected to the SAME in-memory DB.
        Func<AppDbContext> contextFactory = () => new AppDbContext(_dbOptions);
        var service = new CrawlerService(_httpClient, contextFactory);

        // Act
        // MaxPages = 2 (Home + Page1). Page 2 should be in queue but not processed if check is < MaxPages
        var session = await service.StartRecursiveScanAsync("https://example.com/", maxPages: 2);

        // Assert
        Assert.Equal(2, session.TotalPagesScanned);
        
        using var context = new AppDbContext(_dbOptions);
        var pages = await context.Pages.Where(p => p.SessionId == session.Id).ToListAsync();
        Assert.Equal(2, pages.Count);
        Assert.Contains(pages, p => p.Url == "https://example.com/");
        Assert.Contains(pages, p => p.Url == "https://example.com/page1");
        Assert.DoesNotContain(pages, p => p.Url == "https://example.com/page2");
        
        var homePage = pages.First(p => p.Url == "https://example.com/");
        Assert.Equal(1, homePage.InternalLinksCount);
        Assert.Equal(1, homePage.ExternalLinksCount);
    }

    [Fact]
    public async Task StartRecursiveScanAsync_ShouldRespectRobotsTxt()
    {
        // Arrange
        // Disallow /private
        SetupResponse("https://example.com/robots.txt", "User-agent: *\nDisallow: /private");
        
        SetupResponse("https://example.com/", @"
            <html>
                <body>
                    <a href='/public'>Public</a>
                    <a href='/private'>Private</a>
                </body>
            </html>");

        SetupResponse("https://example.com/public", "<html></html>");
        // /private response shouldn't be fetched

        // Use a new context for the factory to simulate real world behavior, 
        // or share if we want to assert on the same instance (careful with disposal).
        // Since we are checking 'context' at the end, we should probably let the service use its own contexts
        // but we need to verify against the DB. The InMemory DB is shared by name/options.
        
        Func<AppDbContext> contextFactory = () => new AppDbContext(_dbOptions);
        var service = new CrawlerService(_httpClient, contextFactory);

        // Act
        var session = await service.StartRecursiveScanAsync("https://example.com/");

        // Assert
        using var context = new AppDbContext(_dbOptions);
        var pages = await context.Pages.Where(p => p.SessionId == session.Id).ToListAsync();
        Assert.Contains(pages, p => p.Url == "https://example.com/");
        Assert.Contains(pages, p => p.Url == "https://example.com/public");
        Assert.DoesNotContain(pages, p => p.Url == "https://example.com/private");
    }

    private void SetupResponse(string url, string content)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });
    }
}
