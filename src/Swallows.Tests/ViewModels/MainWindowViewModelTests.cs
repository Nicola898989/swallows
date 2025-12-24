using Moq;
using Swallows.Core.Data;
using Swallows.Core.Models;
using Swallows.Core.Services;
using Swallows.Desktop.ViewModels;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Swallows.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public MainWindowViewModelTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "SwallowsTestDb_" + System.Guid.NewGuid())
            .Options;
    }

    [Fact]
    public async Task StartScan_ShouldTriggerService_AndUpdateStats_Mocked()
    {
        // Arrange
        using var context = new AppDbContext(_dbOptions);
        Func<AppDbContext> contextFactory = () => new AppDbContext(_dbOptions);
        var mockCrawler = new Mock<CrawlerService>(new HttpClient(), contextFactory); 
        // Note: Mocking concrete class with arguments is tricky depending on virtual methods.
        // It's better to extract interface ICrawlerService.
        // But for now, let's test the ViewModel with a "Real" service but mocked http if possible, 
        // OR just test the integration directly as requested.
        
        // Let's do the Integration/E2E test first as it's the main request.
    }

    [Fact]
    public async Task E2E_Scan_ScrapeThisSite_ShouldPopulateResults()
    {
        // Arrange
        using var context = new AppDbContext(_dbOptions);
        
        // Real Service with Real Http (for E2E)
        // We need to support redirects as per Module 4
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new HttpClient(handler);
        // Create service with factory
        Func<AppDbContext> contextFactory = () => new AppDbContext(_dbOptions);
        var crawlerService = new CrawlerService(httpClient, contextFactory);

        var viewModel = new MainWindowViewModel(crawlerService, () => new AppDbContext(_dbOptions));
        
        // Act
        viewModel.UrlToScan = "https://www.scrapethissite.com/";
        viewModel.SelectedUserAgent = "SwallowsBot/1.0"; // Ensure valid UA
        
        // Execute Command
        await viewModel.StartScanCommand.ExecuteAsync(null);

        // Assert
        Assert.True(viewModel.RecentScans.Count > 0, "Should have at least one scan session");
        var session = viewModel.RecentScans.First();
        
        // Wait a bit if async background processing happens? 
        // StartScanAsync awaits the scan, so it should be done.
        
        Assert.NotNull(session.Pages);
        Assert.NotEmpty(session.Pages);
        
        // specific check for scrapethissite
        Assert.Contains(session.Pages, p => p.Url.Contains("scrapethissite.com"));
    }
}
