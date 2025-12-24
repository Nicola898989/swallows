using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Swallows.Core.Models;
using Swallows.Core.Services.AI;
using Swallows.Desktop.ViewModels;
using Swallows.Desktop.Views;
using Xunit;

namespace Swallows.Tests.UI;

public class AIAnalysisWindowUITests : UITestBase
{
    [AvaloniaFact]
    public async Task Test_AIAnalysisWindow_Initializes()
    {
        // Arrange - Create a test page
        var testPage = await CreateTestPage();
        var httpClient = new System.Net.Http.HttpClient();
        var llmService = new LlmService(httpClient, ContextFactory);
        var pages = new List<Page> { testPage };

        // Act
        var window = await RunOnUIThreadAsync(async () =>
        {
            var viewModel = new AiAnalysisViewModel(llmService, pages);
            var aiWindow = new AiAnalysisWindow
            {
                DataContext = viewModel
            };
            return aiWindow;
        });

        // Assert
        Assert.NotNull(window);
        Assert.NotNull(window.DataContext);
        Assert.IsType<AiAnalysisViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public async Task Test_AIAnalysisViewModel_LoadsPageData()
    {
        // Arrange
        var testPage = await CreateTestPage();
        var httpClient = new System.Net.Http.HttpClient();
        var llmService = new LlmService(httpClient, ContextFactory);
        var pages = new List<Page> { testPage };

        // Act
        var viewModel = new AiAnalysisViewModel(llmService, pages);

        // Assert
        await RunOnUIThread(() =>
        {
            Assert.NotNull(viewModel.Results);
            Assert.True(viewModel.Results.Count > 0);
        });
    }

    private async Task<Page> CreateTestPage()
    {
        using var context = ContextFactory();

        var session = new ScanSession
        {
            BaseUrl = "https://ai-test.example.com",
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow.AddMinutes(5),
            Status = "Completed",
            TotalPagesScanned = 1,
            UserAgent = "SwallowsBot/1.0"
        };

        var page = new Page
        {
            Url = "https://ai-test.example.com/test-page",
            Title = "AI Analysis Test Page",
            MetaDescription = "A test page for AI analysis",
            StatusCode = 200,
            ContentLength = 5000,
            LoadTimeMs = 1200,
            Depth = 1,
            ScannedAt = DateTime.UtcNow,
            H1Count = 1,
            WordCount = 500,
            Session = session
        };

        session.Pages.Add(page);
        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return page;
    }
}
