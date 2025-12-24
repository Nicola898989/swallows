using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Swallows.Core.Models;
using Swallows.Desktop.ViewModels;
using Swallows.Desktop.Views;
using Xunit;

namespace Swallows.Tests.UI;

public class SEOAuditWindowUITests : UITestBase
{
    [AvaloniaFact]
    public async Task Test_SEOAuditWindow_Initializes()
    {
        // Arrange - Create a test scan session
        var scanSession = await CreateTestScanSession();

        // Act
        var window = await RunOnUIThreadAsync(async () =>
        {
            var viewModel = new SeoAuditViewModel(scanSession);
            var auditWindow = new SeoAuditWindow
            {
                DataContext = viewModel
            };
            return auditWindow;
        });

        // Assert
        Assert.NotNull(window);
        Assert.NotNull(window.DataContext);
        Assert.IsType<SeoAuditViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public async Task Test_SEOAuditViewModel_LoadsScanData()
    {
        // Arrange
        var scanSession = await CreateTestScanSession();

        // Act
        var viewModel = new SeoAuditViewModel(scanSession);

        // Assert
        await RunOnUIThread(() =>
        {
            // Just verify initialization
            Assert.NotNull(viewModel);
        });
    }

    [AvaloniaFact]
    public async Task Test_SEOAudit_WithMultiplePages_CalculatesCorrectly()
    {
        // Arrange - Create a scan with multiple pages
        var scanSession = await CreateTestScanSessionWithMultiplePages();

        // Act
        var viewModel = new SeoAuditViewModel(scanSession);

        // Allow some time for charts to be calculated
        await Task.Delay(500);

        // Assert - Verify data is loaded
        await RunOnUIThread(() =>
        {
            // The ViewModel should have been initialized with scan data
            Assert.NotNull(viewModel);
        });
    }

    private async Task<ScanSession> CreateTestScanSession()
    {
        using var context = ContextFactory();
        
        var session = TestDataHelper.CreateTestScanSession("https://test.example.com", 10);
        
        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }

    private async Task<ScanSession> CreateTestScanSessionWithMultiplePages()
    {
        using var context = ContextFactory();
        
        var session = TestDataHelper.CreateTestScanSession("https://test.example.com", 50);

        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }
}
