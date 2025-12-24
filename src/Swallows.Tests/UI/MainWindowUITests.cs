using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Swallows.Core.Services;
using Swallows.Desktop.ViewModels;
using Swallows.Desktop.Views;
using Xunit;
using System.Net.Http;

namespace Swallows.Tests.UI;

public class MainWindowUITests : UITestBase
{
    [AvaloniaFact]
    public async Task Test_MainWindow_Initializes()
    {
        // Arrange & Act
        var window = await RunOnUIThreadAsync(async () =>
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            var httpClient = new HttpClient(handler);
            var crawlerService = new CrawlerService(httpClient, ContextFactory);
            var viewModel = new MainWindowViewModel(crawlerService, ContextFactory);
            
            var mainWindow = new MainWindow
            {
                DataContext = viewModel
            };
            
            return mainWindow;
        });

        // Assert
        Assert.NotNull(window);
        Assert.NotNull(window.DataContext);
        Assert.IsType<MainWindowViewModel>(window.DataContext);
    }

    [AvaloniaFact]
    public async Task Test_StartScan_WithValidUrl_UpdatesViewModel()
    {
        // Arrange
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new HttpClient(handler);
        var crawlerService = new CrawlerService(httpClient, ContextFactory);
        var viewModel = new MainWindowViewModel(crawlerService, ContextFactory);

        // Act
        await RunOnUIThread(() =>
        {
            viewModel.UrlToScan = "https://example.com";
            viewModel.SelectedUserAgent = "SwallowsBot/1.0";
        });

        // Execute scan command (this will actually make HTTP requests in a real test)
        // For now, we just verify the command can be created and is executable
        Assert.NotNull(viewModel.StartScanCommand);
        
        // Assert - check initial state
        var urlToScan = await RunOnUIThread(() => viewModel.UrlToScan);
        Assert.Equal("https://example.com", urlToScan);
    }

    [AvaloniaFact]
    public async Task Test_ViewModel_Properties_AreInitialized()
    {
        // Arrange & Act
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new HttpClient(handler);
        var crawlerService = new CrawlerService(httpClient, ContextFactory);
        var viewModel = new MainWindowViewModel(crawlerService, ContextFactory);

        // Assert
        await RunOnUIThread(() =>
        {
            Assert.NotNull(viewModel.StartScanCommand);
            Assert.NotNull(viewModel.PauseScanCommand);
            Assert.NotNull(viewModel.StopScanCommand);
            Assert.NotNull(viewModel.OpenSettingsCommand);
            Assert.NotNull(viewModel.OpenHistoryCommand);
            Assert.NotNull(viewModel.ExportScanCommand);
            Assert.NotNull(viewModel.RecentScans);
        });
    }

    [AvaloniaFact]
    public async Task Test_ScanCommands_CanExecuteLogic()
    {
        // Arrange
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new HttpClient(handler);
        var crawlerService = new CrawlerService(httpClient, ContextFactory);
        var viewModel = new MainWindowViewModel(crawlerService, ContextFactory);

        // Act & Assert - Initially, pause and stop should not be executable
        var canPause = await RunOnUIThread(() => viewModel.PauseScanCommand.CanExecute(null));
        var canStop = await RunOnUIThread(() => viewModel.StopScanCommand.CanExecute(null));

        Assert.False(canPause);
        Assert.False(canStop);
    }

    [AvaloniaFact]
    public async Task Test_RecentScans_Collection_Initialized()
    {
        // Arrange
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new HttpClient(handler);
        var crawlerService = new CrawlerService(httpClient, ContextFactory);
        var viewModel = new MainWindowViewModel(crawlerService, ContextFactory);

        // Act
        var recentScans = await RunOnUIThread(() => viewModel.RecentScans);

        // Assert
        Assert.NotNull(recentScans);
        // Should be empty initially since we're using a fresh test database
    }
}
