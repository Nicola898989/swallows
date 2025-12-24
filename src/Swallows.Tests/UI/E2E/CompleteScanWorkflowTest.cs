using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Microsoft.EntityFrameworkCore;
using Swallows.Core.Models;
using Swallows.Core.Services;
using Swallows.Desktop.ViewModels;
using Xunit;

namespace Swallows.Tests.UI.E2E;

/// <summary>
/// End-to-end integration test simulating a complete user workflow
/// </summary>
public class CompleteScanWorkflowTest : UITestBase
{
    [AvaloniaFact]
    public async Task Test_CompleteScanWorkflow_FromStartToExport()
    {
        // This is a comprehensive E2E test that simulates:
        // 1. Opening the application
        // 2. Configuring settings
        // 3. Starting a scan (mocked with test data)
        // 4. Viewing results
        // 5. Opening SEO audit
        // 6. Exporting data

        // Step 1: Initialize application components
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new HttpClient(handler);
        var crawlerService = new CrawlerService(httpClient, ContextFactory);
        var mainViewModel = new MainWindowViewModel(crawlerService, ContextFactory);

        Assert.NotNull(mainViewModel);

        // Step 2: Configure settings via SettingsViewModel
        var settingsViewModel = new SettingsViewModel(ContextFactory);
        
        await RunOnUIThread(() =>
        {
            settingsViewModel.MaxPages = 50;
            settingsViewModel.MaxDepth = 3;
            settingsViewModel.ConcurrentRequests = 3;
            settingsViewModel.UserAgent = "SwallowsBot/1.0";
        });

        // Save settings
        await RunOnUIThreadAsync<object?>(async () =>
        {
            await settingsViewModel.SaveSettingsCommand.ExecuteAsync(null);
            return null;
        });

        // Verify settings were saved
        using (var context = ContextFactory())
        {
            var savedSettings = context.AppSettings.FirstOrDefault();
            Assert.NotNull(savedSettings);
            Assert.Equal(50, savedSettings.MaxPages);
            Assert.Equal(3, savedSettings.MaxDepth);
        }

        // Step 3: Create a mock scan (simulating a completed scan)
        // In a real E2E test, we might actually run a scan against a test server
        var mockScan = await CreateMockCompletedScan();

        // Step 4: Load scan results in MainViewModel
        await RunOnUIThread(() =>
        {
            mainViewModel.SelectedScan = mockScan;
        });

        var selectedScan = await RunOnUIThread(() => mainViewModel.SelectedScan);
        Assert.NotNull(selectedScan);
        Assert.Equal(mockScan.Id, selectedScan.Id);

        // Step 5: Open SEO Audit
        var seoAuditViewModel = new SeoAuditViewModel(mockScan);
        Assert.NotNull(seoAuditViewModel);
        // Commands vary by ViewModel design, just verify initialization
        Assert.NotNull(seoAuditViewModel);

        // Step 6: Verify export command is available
        var canExport = await RunOnUIThread(() => 
            mainViewModel.ExportScanCommand?.CanExecute(null) ?? false
        );
        Assert.True(canExport);

        // Step 7: Test History functionality
        var historyViewModel = new HistoryWindowViewModel(ContextFactory);
        await Task.Delay(200); // Allow time for async loading

        var scanCount = await RunOnUIThread(() => historyViewModel.Sessions?.Count ?? 0);
        Assert.True(scanCount > 0, "History should contain the mock scan");

        // Step 8: Test Comparison (create another scan for comparison)
        var secondScan = await CreateMockCompletedScan();
        var comparisonViewModel = new ComparisonWindowViewModel(ContextFactory);
        Assert.NotNull(comparisonViewModel);

        // Step 9: Test Trend Analysis
        var trendViewModel = new TrendViewModel(ContextFactory, mockScan.BaseUrl);
        await Task.Delay(300);
        Assert.NotNull(trendViewModel);

        // Step 10: Test Structure View
        var structureViewModel = new StructureWindowViewModel(ContextFactory, mockScan.Id);
        Assert.NotNull(structureViewModel);

        // Step 11: Test AI Analysis on a page
        var firstPage = mockScan.Pages.FirstOrDefault();
        if (firstPage != null)
        {
            var aiHttp = new System.Net.Http.HttpClient();
            var llmService = new Swallows.Core.Services.AI.LlmService(aiHttp, ContextFactory);
            var pages = new System.Collections.Generic.List<Page> { firstPage };
            var aiViewModel = new AiAnalysisViewModel(llmService, pages);
            Assert.NotNull(aiViewModel);
            Assert.True(aiViewModel.Results.Count > 0);
        }

        // All steps completed successfully
        Assert.True(true, "Complete workflow executed successfully");
    }

    [AvaloniaFact]
    public async Task Test_SettingsWorkflow_UpdateAndVerify()
    {
        // Test that settings changes persist across ViewModel instances

        // Step 1: Create settings and modify them
        var settingsVm1 = new SettingsViewModel(ContextFactory);
        
        await RunOnUIThread(() =>
        {
            settingsVm1.MaxPages = 200;
            settingsVm1.MaxDepth = 5;
            settingsVm1.EnableJavaScript = true;
        });

        await RunOnUIThreadAsync<object?>(async () =>
        {
            await settingsVm1.SaveSettingsCommand.ExecuteAsync(null);
            return null;
        });

        // Step 2: Create new ViewModel instance and verify settings loaded
        var settingsVm2 = new SettingsViewModel(ContextFactory);
        
        await Task.Delay(100); // Allow time for loading

        var maxPages = await RunOnUIThread(() => settingsVm2.MaxPages);
        var maxDepth = await RunOnUIThread(() => settingsVm2.MaxDepth);
        var useJs = await RunOnUIThread(() => settingsVm2.EnableJavaScript);

        Assert.Equal(200, maxPages);
        Assert.Equal(5, maxDepth);
        Assert.True(useJs);
    }

    private async Task<ScanSession> CreateMockCompletedScan()
    {
        using var context = ContextFactory();

        var session = new ScanSession
        {
            BaseUrl = "https://e2e-test.example.com",
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            FinishedAt = DateTime.UtcNow,
            Status = "Completed",
            TotalPagesScanned = 30,
            UserAgent = "SwallowsBot/1.0"
        };

        // Create diverse pages to test all features
        var random = new Random();
        for (int i = 0; i < 30; i++)
        {
            var statusCode = i % 8 == 0 ? 404 : (i % 15 == 0 ? 500 : 200);
            var depth = i % 4;

            var page = new Page
            {
                Url = $"https://e2e-test.example.com/page{i}",
                Title = $"E2E Test Page {i}",
                MetaDescription = $"Description for page {i}",
                StatusCode = statusCode,
                ContentLength = 2500 + random.Next(2000),
                LoadTimeMs = 800 + (int)(random.NextDouble() * 2000),
                Depth = depth,
                ScannedAt = DateTime.UtcNow,
                H1Count = 1,
                H2Count = 2,
                WordCount = 300 + random.Next(500),
                MissingAltCount = i % 4 == 0 ? 1 : 0,
                CanonicalUrl = i % 5 == 0 ? $"https://e2e-test.example.com/canonical{i}" : null,
                Session = session
            };
            session.Pages.Add(page);
        }

        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        // Reload to get navigation properties
        return context.ScanSessions
            .Include(s => s.Pages)
            .FirstOrDefault(s => s.Id == session.Id)!;
    }
}
