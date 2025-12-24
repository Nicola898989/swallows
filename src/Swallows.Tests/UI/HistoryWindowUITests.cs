using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Swallows.Core.Models;
using Swallows.Desktop.ViewModels;
using Swallows.Desktop.Views;
using Xunit;

namespace Swallows.Tests.UI;

public class HistoryWindowUITests : UITestBase
{
    [AvaloniaFact]
    public async Task Test_HistoryWindow_Initializes()
    {
        // Arrange & Act
        var window = await RunOnUIThreadAsync(async () =>
        {
            var viewModel = new HistoryWindowViewModel(ContextFactory);
            var historyWindow = new HistoryWindow
            {
                DataContext = viewModel
            };
            return historyWindow;
        });

        // Assert
        Assert.NotNull(window);
        Assert.NotNull(window.DataContext);
        Assert.IsType<HistoryWindowViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public async Task Test_HistoryViewModel_LoadsAllScans()
    {
        // Arrange - Create multiple test scans
        await CreateMultipleTestScans(5);

        // Act
        var viewModel = await RunOnUIThreadAsync(async () =>
        {
            return new HistoryWindowViewModel(ContextFactory);
        });

        // Allow time for async loading
        await Task.Delay(300);

        // Assert
        var scanCount = await RunOnUIThread(() => viewModel.Sessions?.Count ?? 0);
        Assert.True(scanCount >= 5);
    }

    [AvaloniaFact]
    public async Task Test_LoadScan_UpdatesSelectedScan()
    {
        // Arrange
        var testScan = await CreateSingleTestScan();
        var viewModel = new HistoryWindowViewModel(ContextFactory);

        // Act
        await RunOnUIThread(() =>
        {
            viewModel.SelectedSession = testScan;
        });

        // Assert
        var selectedScan = await RunOnUIThread(() => viewModel.SelectedSession);
        Assert.NotNull(selectedScan);
        Assert.Equal(testScan.Id, selectedScan.Id);
    }

    [AvaloniaFact]
    public async Task Test_DeleteScan_RemovesFromDatabase()
    {
        // Arrange
        var testScan = await CreateSingleTestScan();
        var viewModel = new HistoryWindowViewModel(ContextFactory);

        // Get initial count
        using (var context = ContextFactory())
        {
            var initialCount = context.ScanSessions.Count();
            Assert.True(initialCount > 0);
        }

        // Act
        await RunOnUIThread(() =>
        {
            viewModel.SelectedSession = testScan;
        });

        viewModel.DeleteScanCommand.Execute(null);

            // Assert
            using var deleteContext = ContextFactory();
            var scan = deleteContext.ScanSessions.Find(testScan.Id);
            Assert.Null(scan); // Scan should be deleted
        }

    private async Task CreateMultipleTestScans(int count)
    {
        using var context = ContextFactory();

        for (int i = 0; i < count; i++)
        {
            var session = TestDataHelper.CreateTestScanSession($"https://test{i}.example.com", 10 + i);
            context.ScanSessions.Add(session);
        }

        await context.SaveChangesAsync();
    }

    private async Task<ScanSession> CreateSingleTestScan()
    {
        using var context = ContextFactory();

        var session = TestDataHelper.CreateTestScanSession("https://single-test.example.com", 5);

        context.ScanSessions.Add(session);
        await context.SaveChangesAsync();

        return session;
    }
}
