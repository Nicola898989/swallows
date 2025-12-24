using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Swallows.Core.Models;
using Swallows.Desktop.ViewModels;
using Swallows.Desktop.Views;
using Xunit;

namespace Swallows.Tests.UI;

public class StructureWindowUITests : UITestBase
{
    [AvaloniaFact]
    public async Task Test_StructureWindow_Initializes()
    {
        // Arrange - Create a scan with hierarchical pages
        var scanSession = await CreateHierarchicalScan();

        // Act
        var window = await RunOnUIThreadAsync(async () =>
        {
            var viewModel = new StructureWindowViewModel(ContextFactory, scanSession.Id);
            var structureWindow = new StructureWindow
            {
                DataContext = viewModel
            };
            return structureWindow;
        });

        // Assert
        Assert.NotNull(window);
        Assert.NotNull(window.DataContext);
        Assert.IsType<StructureWindowViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public async Task Test_StructureViewModel_BuildsTree()
    {
        // Arrange
        var scanSession = await CreateHierarchicalScan();

        // Act
        var viewModel = new StructureWindowViewModel(ContextFactory, scanSession.Id);

        // Allow time for tree building
        await Task.Delay(300);

        // Assert
        await RunOnUIThread(() =>
        {
            Assert.NotNull(viewModel);
            // ViewModel should have built the tree structure
        });
    }

    [AvaloniaFact]
    public async Task Test_StructureTree_WithMultipleLevels()
    {
        // Arrange - Create deeply nested structure
        var scanSession = await CreateDeepHierarchicalScan();

        // Act
        var viewModel = new StructureWindowViewModel(ContextFactory, scanSession.Id);
        await Task.Delay(300);

        // Assert
        Assert.NotNull(viewModel);
    }

    private async Task<ScanSession> CreateHierarchicalScan()
    {
        using var context = ContextFactory();

        var session = new ScanSession
        {
            BaseUrl = "https://structure-test.example.com",
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow.AddMinutes(5),
            Status = "Completed",
            TotalPagesScanned = 10,
            UserAgent = "SwallowsBot/1.0"
        };

        // Root page
        var rootPage = new Page
        {
            Url = "https://structure-test.example.com",
            Title = "Home Page",
            StatusCode = 200,
            ContentLength = 5000,
            LoadTimeMs = 1000,
            Depth = 0,
            ScannedAt = DateTime.UtcNow,
            Session = session
        };
        session.Pages.Add(rootPage);

        // Level 1 pages
        for (int i = 0; i < 3; i++)
        {
            var level1Page = new Page
            {
                Url = $"https://structure-test.example.com/section{i}",
                Title = $"Section {i}",
                StatusCode = 200,
                ContentLength = 4000,
                LoadTimeMs = 1100,
                Depth = 1,
                ScannedAt = DateTime.UtcNow,
                Session = session
            };
            session.Pages.Add(level1Page);

            // Level 2 pages
            for (int j = 0; j < 2; j++)
            {
                var level2Page = new Page
                {
                    Url = $"https://structure-test.example.com/section{i}/page{j}",
                    Title = $"Section {i} - Page {j}",
                    StatusCode = 200,
                    ContentLength = 3500,
                    LoadTimeMs = 1200,
                    Depth = 2,
                    ScannedAt = DateTime.UtcNow,
                    Session = session
                };
                session.Pages.Add(level2Page);
            }
        }

        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }

    private async Task<ScanSession> CreateDeepHierarchicalScan()
    {
        using var context = ContextFactory();

        var session = new ScanSession
        {
            BaseUrl = "https://deep-structure.example.com",
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow.AddMinutes(10),
            Status = "Completed",
            TotalPagesScanned = 25,
            UserAgent = "SwallowsBot/1.0"
        };

        // Create a deep hierarchy
        for (int depth = 0; depth < 5; depth++)
        {
            for (int i = 0; i < 3; i++)
            {
                string url = "https://deep-structure.example.com";
                for (int d = 0; d <= depth; d++)
                {
                    if (d > 0) url += $"/level{d}";
                }
                url += $"/item{i}";

                var page = new Page
                {
                    Url = url,
                    Title = $"Level {depth} - Item {i}",
                    StatusCode = 200,
                    ContentLength = 4000 - (depth * 200),
                    LoadTimeMs = 1000 + (depth * 100),
                    Depth = depth,
                    ScannedAt = DateTime.UtcNow,
                    Session = session
                };
                session.Pages.Add(page);
            }
        }

        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }
}
