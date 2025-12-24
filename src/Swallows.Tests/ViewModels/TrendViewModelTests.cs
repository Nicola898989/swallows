using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Swallows.Core.Data;
using Swallows.Core.Models;
using Swallows.Desktop.ViewModels;
using Xunit;
using System.Threading.Tasks;
using System.Linq;

namespace Swallows.Tests.ViewModels;

public class TrendViewModelTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public TrendViewModelTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "SwallowsTrendTestDb_" + Guid.NewGuid())
            .Options;
    }

    private void SeedDatabase(AppDbContext context, string baseUrl)
    {
        var session1 = new ScanSession
        {
            BaseUrl = baseUrl,
            StartedAt = DateTime.Now.AddDays(-10),
            Pages = new System.Collections.ObjectModel.ObservableCollection<Page>
            {
                new Page { Url = baseUrl, StatusCode = 200, LoadTimeMs = 200, Title = "Home" },
                new Page { Url = baseUrl + "/404", StatusCode = 404, LoadTimeMs = 50, Title = "Not Found" }
            }
        };

        var session2 = new ScanSession
        {
            BaseUrl = baseUrl,
            StartedAt = DateTime.Now.AddDays(-1),
            Pages = new System.Collections.ObjectModel.ObservableCollection<Page>
            {
                new Page { Url = baseUrl, StatusCode = 200, LoadTimeMs = 150, Title = "Home Optimized" },
                new Page { Url = baseUrl + "/new", StatusCode = 200, LoadTimeMs = 100, Title = "New Page" }
            }
        };

        context.ScanSessions.AddRange(session1, session2);
        context.SaveChanges();
    }

    [Fact]
    public async Task Constructor_ShouldLoadDataAndGenerateInsights()
    {
        // Arrange
        using var context = new AppDbContext(_dbOptions);
        SeedDatabase(context, "https://example.com");
        
        Func<AppDbContext> contextFactory = () => new AppDbContext(_dbOptions);
        
        // Act
        // Constructor fires async load fire-and-forget, so we might need to wait a bit
        // However, since it's in-memory and synchronous effectively for db, we just need to wait for the task if exposed 
        // OR we can just wait a small delay. Ideally ViewModel should expose a LoadingTask.
        // For this test, we accept the fire-and-forget nature and use a small delay or loop.
        var viewModel = new TrendViewModel(contextFactory, "https://example.com");
        
        // Wait for async initialization which is fired in constructor
        await Task.Delay(100); 

        // Assert
        Assert.Equal(2, viewModel.TotalScans);
        Assert.DoesNotContain("Loading", viewModel.InsightsSummary);
        Assert.NotEmpty(viewModel.SeoScoreSeries);
        Assert.NotEmpty(viewModel.ErrorCountSeries);
        Assert.NotEmpty(viewModel.PageCountSeries);
        Assert.NotEmpty(viewModel.LoadTimeSeries);
        
        // Verify specific data points
        // First scan had 1 error, second had 0. Error count should go down.
        // Page count: 2 -> 2.
    }

    [Fact]
    public async Task OnTimeRangeChanged_ShouldReloadData()
    {
         // Arrange
        using var context = new AppDbContext(_dbOptions);
        SeedDatabase(context, "https://example.com");
        // Add a very old scan
        context.ScanSessions.Add(new ScanSession 
        { 
            BaseUrl = "https://example.com", 
            StartedAt = DateTime.Now.AddDays(-100),
            Pages = new System.Collections.ObjectModel.ObservableCollection<Page>()
        });
        context.SaveChanges();
        
        Func<AppDbContext> contextFactory = () => new AppDbContext(_dbOptions);
        var viewModel = new TrendViewModel(contextFactory, "https://example.com");
        await Task.Delay(100); // Initial load (Default 30 days)
        
        Assert.Equal(2, viewModel.TotalScans); // Old scan excluded
        
        // Act
        viewModel.TimeRange = "All Time"; // Should trigger reload via OnTimeRangeChanged partial method if implemented as such
        // Wait for reload
        await Task.Delay(100);

        // Assert
        // This fails if the partial OnTimeRangeChanged is not correctly modifying the property or triggering logic
        // But code showed: partial void OnTimeRangeChanged(string value) { _ = LoadTrendDataAsync(); }
        // Note: "All Time" logic in ViewModel: _ => DateTime.MinValue. So it should include everything.
        // Wait, "Last 90 Days" is handled, "All Time" falls into default?
        // Code: 
        // "Last 7 Days" ...
        // "Last 30 Days" ...
        // "Last 90 Days" ...
        // _ => DateTime.MinValue
        // So yes, All Time = MinValue.
        
        Assert.Equal(3, viewModel.TotalScans);
    }
}
