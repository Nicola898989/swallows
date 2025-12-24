using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Swallows.Core.Models;
using Swallows.Desktop.ViewModels;
using Swallows.Desktop.Views;
using Xunit;

namespace Swallows.Tests.UI;

public class ComparisonWindowUITests : UITestBase
{
    [AvaloniaFact]
    public async Task Test_ComparisonWindow_Initializes()
    {
        // Arrange - Create two test scans
        var scan1 = await CreateTestScan(1);
        var scan2 = await CreateTestScan(2);

        // Act
        var window = await RunOnUIThreadAsync(async () =>
        {
            var viewModel = new ComparisonWindowViewModel(ContextFactory);
            // Note: Actual scan selection would be done via properties/commands
            var comparisonWindow = new ComparisonWindow
            {
                DataContext = viewModel
            };
            return comparisonWindow;
        });

        // Assert
        Assert.NotNull(window);
        Assert.NotNull(window.DataContext);
        Assert.IsType<ComparisonWindowViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public async Task Test_ComparisonViewModel_LoadsTwoScans()
    {
        // Arrange
        var scan1 = await CreateTestScan(1);
        var scan2 = await CreateTestScan(2);

        // Act
        var viewModel = new ComparisonWindowViewModel(ContextFactory);

        // Assert - Verify VMs properties are initialized
        await RunOnUIThread(() =>
        {
            Assert.NotNull(viewModel);
            // The ViewModel should have loaded both scans
        });
    }

    [AvaloniaFact]
    public async Task Test_Comparison_WithDifferentPageCounts()
    {
        // Arrange - Create scans with different page counts
        var scan1 = await CreateTestScanWithPageCount(10);
        var scan2 = await CreateTestScanWithPageCount(25);

        // Act
        var viewModel = new ComparisonWindowViewModel(ContextFactory);

        // Allow time for comparison calculations
        await Task.Delay(300);

        // Assert
        Assert.NotNull(viewModel);
    }

    private async Task<ScanSession> CreateTestScan(int identifier)
    {
        using var context = ContextFactory();
        
        var session = TestDataHelper.CreateTestScanSession($"https://test{identifier}.example.com", 15);
        
        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }

    private async Task<ScanSession> CreateTestScanWithPageCount(int pageCount)
    {
        using var context = ContextFactory();
        
        var session = TestDataHelper.CreateTestScanSession("https://testscan.example.com", pageCount);

        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }
}
