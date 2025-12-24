using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Models;
using Swallows.Core.Services;
using Swallows.Core.Data;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Swallows.Desktop.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;
using System.Threading;
using Avalonia.Threading;

namespace Swallows.Desktop.ViewModels;

public partial class ColumnOption : ObservableObject
{
    [ObservableProperty] private string _header = "";
    [ObservableProperty] private bool _isVisible = true;
}

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ColumnOption> ColumnOptions { get; } = new ObservableCollection<ColumnOption>
    {
        // 0-22 matching DataGrid columns order
        new ColumnOption { Header = "URL", IsVisible = true },
        new ColumnOption { Header = "Title", IsVisible = true },
        new ColumnOption { Header = "Meta Desc", IsVisible = true },
        new ColumnOption { Header = "Status", IsVisible = true },
        new ColumnOption { Header = "Load (ms)", IsVisible = true },
        new ColumnOption { Header = "Size (KB)", IsVisible = true },
        new ColumnOption { Header = "Images", IsVisible = true },
        new ColumnOption { Header = "TxtRatio", IsVisible = true },
        new ColumnOption { Header = "Int Links", IsVisible = true },
        new ColumnOption { Header = "Ext Links", IsVisible = true },
        new ColumnOption { Header = "H1", IsVisible = true },
        new ColumnOption { Header = "H2", IsVisible = true },
        new ColumnOption { Header = "H3", IsVisible = true },
        new ColumnOption { Header = "Canonical", IsVisible = true },
        new ColumnOption { Header = "Robots", IsVisible = true },
        new ColumnOption { Header = "Hreflangs", IsVisible = true },
        new ColumnOption { Header = "Next", IsVisible = true },
        new ColumnOption { Header = "Prev", IsVisible = true },
        new ColumnOption { Header = "HSTS", IsVisible = true },
        new ColumnOption { Header = "Redir", IsVisible = true },
        new ColumnOption { Header = "Final URL", IsVisible = true },
        new ColumnOption { Header = "Hash", IsVisible = true },
        new ColumnOption { Header = "Hash", IsVisible = true },
        new ColumnOption { Header = "Chain", IsVisible = true },
        
        // Extended Analysis
        new ColumnOption { Header = "Words", IsVisible = true },
        new ColumnOption { Header = "No Alt", IsVisible = true },
        new ColumnOption { Header = "OG", IsVisible = true },
        new ColumnOption { Header = "Twitter", IsVisible = true },
        new ColumnOption { Header = "Scripts", IsVisible = true },
        new ColumnOption { Header = "Styles", IsVisible = true },
        new ColumnOption { Header = "Viewport", IsVisible = true },
        new ColumnOption { Header = "Favicon", IsVisible = true },
        
        // SEO Validation
        new ColumnOption { Header = "Title OK", IsVisible = true },
        new ColumnOption { Header = "Desc OK", IsVisible = true },
        new ColumnOption { Header = "KW%", IsVisible = true },
        
        // Link Quality
        new ColumnOption { Header = "Broken", IsVisible = true },
        new ColumnOption { Header = "Orphan", IsVisible = true },
        new ColumnOption { Header = "Depth", IsVisible = true },
        
        // Module 9
        new ColumnOption { Header = "Schema", IsVisible = true },
        new ColumnOption { Header = "Rich Snippet", IsVisible = true },
        new ColumnOption { Header = "Readability", IsVisible = true },
        new ColumnOption { Header = "Keywords", IsVisible = true },
        new ColumnOption { Header = "Custom Data", IsVisible = true }
    };
    
    // Chart Properties
    public ObservableCollection<ISeries> DepthSeries { get; set; } = new ObservableCollection<ISeries>();
    public Axis[] DepthXAxes { get; set; } = new Axis[] { new Axis { Name = "Depth Level", LabelsRotation = 15 } };
    public Axis[] DepthYAxes { get; set; } = new Axis[] { new Axis { Name = "Pages" } };
    
    public ObservableCollection<ISeries> ResponseTimeSeries { get; set; } = new ObservableCollection<ISeries>();
    public Axis[] ResponseTimeXAxes { get; set; } = new Axis[] { new Axis { Name = "Sequence (Page #)" } };
    public Axis[] ResponseTimeYAxes { get; set; } = new Axis[] { new Axis { Name = "Load Time (ms)" } };
    

    
    private readonly CrawlerService _crawlerService;
    private readonly Func<AppDbContext> _contextFactory;
    
    [ObservableProperty]
    private string _urlToScan = "https://www.scrapethissite.com/";

    [ObservableProperty]
    private string _statusMessage = "Ready to scan.";

    [ObservableProperty] private int _pagesScanned;
    [ObservableProperty] private int _pagesRemaining;
    [ObservableProperty] private int _pagesTotal;
    [ObservableProperty] private string _progressText = "";
    
    // Scan control state
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(StopScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseScanCommand))]
    private bool _isScanRunning;
    [ObservableProperty] private bool _isScanPaused;
    [ObservableProperty] private string _scanButtonText = "Start Scan";
    
    private CancellationTokenSource? _scanCancellationSource;
    private bool _isPaused = false;
    
    // Progress tracking for ETA
    [ObservableProperty] private double _progressPercentage;
    [ObservableProperty] private string _scanSpeed = "";
    [ObservableProperty] private string _estimatedTimeRemaining = "";
    
    private DateTime _scanStartTime;
    private int _lastPageCount = 0;
    private DateTime _lastSpeedCheck;

    public ObservableCollection<string> UserAgents { get; } = new ObservableCollection<string>
    {
        "SwallowsBot/1.0",
        "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/W.X.Y.Z Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };
    
    // Pagination for large datasets
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _pageSize = 100;
    [ObservableProperty] private int _totalRecords;
    [ObservableProperty] private string _paginationInfo = "";
    
    public ObservableCollection<Page> CurrentPageData { get; } = new();
    
    // Export filters
    [ObservableProperty] private bool _filterAll = true;
    [ObservableProperty] private bool _filterErrors = false;
    [ObservableProperty] private bool _filter404 = false;
    [ObservableProperty] private bool _filterThinContent = false;
    [ObservableProperty] private bool _filterMissingMeta = false;
    [ObservableProperty] private bool _filterBrokenLinks = false;

    [ObservableProperty]
    private string _selectedUserAgent = "SwallowsBot/1.0";

    public ObservableCollection<ScanSession> RecentScans { get; } = new ObservableCollection<ScanSession>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateSitemapCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenAiCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSeoAuditCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenTrendCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportGraphCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenStructureCommand))]
    private ScanSession? _selectedScan;

    // Call this when SelectedScan changes or scan updates
    partial void OnSelectedScanChanged(ScanSession? value)
    {
        // Defer chart update to ensure UI is ready
        Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateDepthChart(), Avalonia.Threading.DispatcherPriority.Background);
    }
    
    private void UpdateDepthChart()
    {
        if (SelectedScan == null) 
        {
            DepthSeries.Clear();
            return;
        }

        var groupings = SelectedScan.Pages.GroupBy(p => p.Depth).OrderBy(g => g.Key).ToList();
        var counts = new List<int>();
        var labels = new List<string>();

        foreach (var group in groupings)
        {
            counts.Add(group.Count());
            labels.Add($"Level {group.Key}");
        }

        DepthSeries.Clear();
        DepthSeries.Add(new ColumnSeries<int>
        {
            Values = counts,
            Name = "Pages",
            Fill = new SolidColorPaint(SKColors.BlueViolet)
        });
        
        if (DepthXAxes.Length > 0)
        {
             DepthXAxes[0].Labels = labels;
        }

        // Response Time Chart
        // Order by ScannedAt to show timeline
        var timeline = SelectedScan.Pages.OrderBy(p => p.ScannedAt).ToList();
        var loadTimes = timeline.Select(p => p.LoadTimeMs).ToList();
        
        ResponseTimeSeries.Clear();
        ResponseTimeSeries.Add(new LineSeries<double>
        {
            Values = loadTimes,
            Name = "Load Time (ms)",
            GeometrySize = 5,
            Fill = null, // Line Chart (no area fill by default, or maybe small specific fill)
            Stroke = new SolidColorPaint(SKColors.DarkOrange) { StrokeThickness = 2 }
        });
    }

    // Default constructor for design-time/runtime default
    public MainWindowViewModel()
    {
        _contextFactory = () => new AppDbContext();

        // 1. Initialize DB (Persist data, but update schema if needed)
        using (var db = _contextFactory())
        {
            // Ensure DB exists first
            db.Database.EnsureCreated();
            
            // Check for missing columns and add them (Manual Migration)
            db.UpgradeSchemaIfNeeded();
            
            // Ensure default settings exist
            if (!db.AppSettings.Any())
            {
                db.AppSettings.Add(new AppSettings());
                db.SaveChanges();
            }
        }

        // 2. Initialize Dependencies (AFTER DB reset, so they see the valid file)
        var handler = new System.Net.Http.HttpClientHandler { AllowAutoRedirect = false };
        var httpClient = new System.Net.Http.HttpClient(handler);
        _crawlerService = new CrawlerService(httpClient, _contextFactory);

        // 3. Load recent history (will be empty after delete, but proper flow)
        using (var db = _contextFactory())
        {
            var recents = db.ScanSessions.Include(s => s.Pages).OrderByDescending(s => s.StartedAt).Take(10).ToList();
            foreach(var r in recents) RecentScans.Add(r);
        }
    }

    // DI Constructor
    public MainWindowViewModel(CrawlerService crawlerService, Func<AppDbContext> contextFactory)
    {
        _crawlerService = crawlerService;
        _contextFactory = contextFactory;
    }

    // Removed static CreateDefaultCrawler to avoid ordering issues

    [RelayCommand]
    private async Task OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);
        
        if (mainWindow != null)
        {
            await settingsWindow.ShowDialog(mainWindow);
        }
    }

    [RelayCommand]
    public async Task OpenHistory()
    {
        var vm = new HistoryWindowViewModel(_contextFactory);
        var win = new HistoryWindow { DataContext = vm };
        
        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);
        
        if (mainWindow != null)
        {
            var result = await win.ShowDialog<ScanSession?>(mainWindow);
            if (result != null)
            {
                StatusMessage = "Loading session...";
                try 
                {
                    using (var db = _contextFactory())
                    {
                        var fullSession = await db.ScanSessions
                            .Include(s => s.Pages)
                            .FirstOrDefaultAsync(s => s.Id == result.Id);
                            
                        if (fullSession != null)
                        {
                            // Fix ObservableCollection mapping if needed, 
                            // EF Core returns List for collection navigation by default usually, but we defined it as ObservableCollection in model?
                            // Checked model: public ObservableCollection<Page> Pages { get; set; } = new ObservableCollection<Page>();
                            // EF Core handles this fine if initialized.
                            
                            SelectedScan = fullSession;
                            StatusMessage = $"Loaded scan: {fullSession.BaseUrl} ({fullSession.TotalPagesScanned} pages)";
                            
                            // Ensure it's in the recent list for visibility
                            var recent = RecentScans.FirstOrDefault(s => s.Id == fullSession.Id);
                            if (recent == null)
                            {
                                RecentScans.Insert(0, fullSession);
                            }
                            else
                            {
                                // Move to top
                                RecentScans.Move(RecentScans.IndexOf(recent), 0);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error loading session: {ex.Message}";
                }
            }
        }
    }

    [RelayCommand]
    public async Task OpenComparison()
    {
        var vm = new ComparisonWindowViewModel(_contextFactory);
        var win = new Swallows.Desktop.Views.ComparisonWindow
        {
            DataContext = vm
        };

        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);

        if (mainWindow != null)
        {
            await win.ShowDialog(mainWindow); // Modal dialog
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenStructure))]
    public async Task OpenStructure()
    {
        if (SelectedScan == null) return;

        var vm = new StructureWindowViewModel(_contextFactory, SelectedScan.Id);
        var win = new Swallows.Desktop.Views.StructureWindow
        {
            DataContext = vm
        };

        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);

        if (mainWindow != null)
        {
            win.Show(mainWindow);
        }
    }
    private bool CanOpenStructure() => SelectedScan != null;

    [RelayCommand]
    public async Task StartScan()
    {
        if (string.IsNullOrWhiteSpace(UrlToScan)) return;
        
        // Initialize cancellation source and state
        _scanCancellationSource = new CancellationTokenSource();
        _isPaused = false;
        IsScanRunning = true;
        IsScanPaused = false;
        ScanButtonText = "Scanning...";

        // Initialize progress tracking
        _scanStartTime = DateTime.Now;
        _lastSpeedCheck = DateTime.Now;
        _lastPageCount = 0;
        ProgressPercentage = 0;
        ScanSpeed = "";
        EstimatedTimeRemaining = "";

        StatusMessage = $"Scanning {UrlToScan}...";
        
        // Load settings from database
        AppSettings? settings = null;
        using (var db = _contextFactory())
        {
            settings = db.AppSettings.FirstOrDefault();
        }
        
        // Use settings or defaults
        var maxPages = settings?.MaxPages ?? 999999;
        var maxDepth = settings?.MaxDepth ?? 999;
        var userAgent = settings?.UserAgent ?? SelectedUserAgent;
        var concurrentRequests = settings?.ConcurrentRequests ?? 1;
        
        // Capture context for callbacks
        var syncContext = System.Threading.SynchronizationContext.Current;
        
        // Throttle chart updates (update every 10 pages instead of every page)
        int pageCounter = 0;
        var chartUpdateLock = new object();
        
        try 
        {
            // Direct callback with Dispatcher - avoid double-marshaling with Progress<T>
            IProgress<ScanProgress> progress = new Progress<ScanProgress>(scanProgress =>
            {
                // Progress<T> already captures SynchronizationContext, so this runs on UI thread
                if (RecentScans.Count > 0)
                {
                    if (scanProgress.LatestPage != null)
                    {
                        RecentScans[0].Pages.Add(scanProgress.LatestPage);
                    }
                    
                    // Update Progress Metrics
                    PagesScanned = scanProgress.ScannedCount;
                    PagesRemaining = scanProgress.QueueCount;
                    PagesTotal = scanProgress.TotalKnownUrls;

                    // Calc %
                    double pct = PagesTotal > 0 ? (double)PagesScanned / PagesTotal * 100 : 0;
                    ProgressText = $"Scanned: {PagesScanned} | Remaining: {PagesRemaining} ({pct:F1}%) | Total: {PagesTotal}";
                    StatusMessage = $"Running... {ProgressText}";
                    
                    // Update progress bar and ETA
                    UpdateProgressMetrics(PagesScanned, PagesTotal);

                    // Note: Since SelectedScan reference is same as RecentScans[0], UI updates automatically
                    
                    // Update chart periodically to avoid flooding UI thread
                    lock (chartUpdateLock)
                    {
                        pageCounter++;
                        if (pageCounter % 10 == 0) // Update every 10 pages
                        {
                            UpdateDepthChart();
                        }
                    }
                }
            });

            var session = await _crawlerService.StartRecursiveScanAsync(
                UrlToScan,
                maxPages: maxPages,
                userAgent: userAgent,
                maxDepth: maxDepth,
                concurrentRequests: concurrentRequests,
                saveImages: settings?.SaveImages ?? false,
                progress: progress,
                onSessionStarted: (s) => 
                {
                    // Manual dispatch using captured context
                    if (syncContext != null)
                        syncContext.Post(_ => 
                        {
                            RecentScans.Insert(0, s);
                            SelectedScan = s; // Select the new scan immediately
                        }, null);
                    else
                    {
                        RecentScans.Insert(0, s);
                        SelectedScan = s;
                    }
                }
                ,
                cancellationToken: _scanCancellationSource.Token,
                isPausedCheck: () => _isPaused
            );
            
            StatusMessage = $"Scan complete. Found {session.TotalPagesScanned} pages.";
            LoggerService.Info($"UI: Scan complete. Total Pages: {session.TotalPagesScanned}");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Scan stopped by user.";
            LoggerService.Info("UI: Scan stopped by user");
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            LoggerService.Error("UI: Scan Error", ex);
        }
        finally
        {
             // Reset state
             IsScanRunning = false;
             IsScanPaused = false;
             ScanButtonText = "Start Scan";
             _scanCancellationSource?.Dispose();
             _scanCancellationSource = null;
             
             // Ensure chart is updated at end of scan
             UpdateDepthChart();
        }
    }

    [RelayCommand(CanExecute = nameof(CanPauseScan))]
    private void PauseScan()
    {
        _isPaused = !_isPaused;
        IsScanPaused = _isPaused;
        StatusMessage = _isPaused ? "Scan paused. Click Pause again to resume." : "Scan resumed.";
        LoggerService.Info(_isPaused ? "Scan paused by user" : "Scan resumed by user");
    }

    private bool CanPauseScan() => IsScanRunning && _scanCancellationSource != null && !_scanCancellationSource.Token.IsCancellationRequested;

    [RelayCommand(CanExecute = nameof(CanStopScan))]
    private void StopScan()
    {
        if (_scanCancellationSource != null)
        {
            _scanCancellationSource.Cancel();
            StatusMessage = "Stopping scan...";
            LoggerService.Info("User requested scan stop");
        }
    }

    private bool CanStopScan() => IsScanRunning;

    private void UpdateProgressMetrics(int current, int total)
    {
        ProgressPercentage = total > 0 ? (current * 100.0 / total) : 0;
        
        var now = DateTime.Now;
        if ((now - _lastSpeedCheck).TotalSeconds >= 2)
        {
            var elapsed = (now - _lastSpeedCheck).TotalSeconds;
            var pagesDone = current - _lastPageCount;
            var speed = elapsed > 0 ? pagesDone / elapsed : 0;
            ScanSpeed = speed > 0 ? $"{speed:F1} pg/sec" : "";
            
            var remaining = total - current;
            if (speed > 0 && remaining > 0)
            {
                var secondsLeft = remaining / speed;
                EstimatedTimeRemaining = $"ETA: {FormatTime(secondsLeft)}";
            }
            else
            {
                EstimatedTimeRemaining = "";
            }
            
            _lastPageCount = current;
            _lastSpeedCheck = now;
        }
    }
    
    private string FormatTime(double seconds)
    {
        if (seconds < 60) return $"{seconds:F0}s";
        if (seconds < 3600) return $"{seconds / 60:F1}m";
        return $"{seconds / 3600:F1}h";
    }

    [RelayCommand(CanExecute = nameof(CanExportScan))]
    public async Task ExportScan()
    {
        if (SelectedScan == null) return;

        try
        {
            StatusMessage = "Exporting to CSV...";
            var exportService = new CsvExportService();
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"scan_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            
            using (var db = _contextFactory())
            {
                 var pages = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                     Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                        db.Pages.Where(p => p.SessionId == SelectedScan.Id)
                     )
                 );
                 
                 await exportService.ExportToCsvAsync(pages, path);
                 StatusMessage = $"Exported to {path}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export Failed: {ex.Message}";
        }
    }
    private bool CanExportScan() => SelectedScan != null;

    [RelayCommand]
    private void OpenMarketplace()
    {
        var vm = new MarketplaceViewModel();
        var win = new Swallows.Desktop.Views.MarketplaceWindow
        {
            DataContext = vm
        };
        
        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);

        if (mainWindow != null)
        {
            win.Show(mainWindow);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGenerateSitemap))]
    public async Task GenerateSitemap()
    {
        if (SelectedScan == null) return;

        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow == null) return;

                // 1. Ask for Options
                var optionsWindow = new Swallows.Desktop.Views.SitemapExportWindow();
                var options = await optionsWindow.ShowDialog<SitemapOptions?>(mainWindow);
                
                if (options == null) return; // User cancelled

                StatusMessage = "Generating sitemap...";
                var sitemapService = new SitemapService();
                
                using (var db = _contextFactory())
                {
                    // Include Images for export
                    var fullScan = await db.ScanSessions
                        .Include(s => s.Pages)
                        .ThenInclude(p => p.Images)
                        .FirstOrDefaultAsync(s => s.Id == SelectedScan.Id);
                        
                    if (fullScan != null)
                    {
                        var sitemaps = sitemapService.GenerateSitemaps(fullScan, options);

                        // 2. Ask for Destination
                        if (options.SplitFiles)
                        {
                            // Folder Picker
                            var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                            {
                                Title = "Select Folder for Sitemap Files",
                                AllowMultiple = false
                            });

                            if (folders.Count > 0)
                            {
                                var folderPath = folders[0].Path.LocalPath;
                                int count = 0;
                                foreach (var kvp in sitemaps)
                                {
                                    await File.WriteAllTextAsync(Path.Combine(folderPath, kvp.Key), kvp.Value);
                                    count++;
                                }
                                StatusMessage = $"Sitemap exported: {count} files to {folderPath}";
                                LoggerService.Info($"Sitemap exported to folder: {folderPath}");
                            }
                        }
                        else
                        {
                            // File Picker
                            var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                            {
                                Title = "Save Sitemap XML",
                                DefaultExtension = "xml",
                                SuggestedFileName = "sitemap.xml",
                                FileTypeChoices = new[]
                                {
                                    new Avalonia.Platform.Storage.FilePickerFileType("XML Files") { Patterns = new[] { "*.xml" } }
                                }
                            });

                            if (file != null)
                            {
                                var path = file.Path.LocalPath;
                                // Only one file in dict
                                await File.WriteAllTextAsync(path, sitemaps.Values.First());
                                StatusMessage = $"Sitemap saved to {path}";
                                LoggerService.Info($"Sitemap saved: {path}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sitemap generation failed: {ex.Message}";
            LoggerService.Error("Sitemap generation error", ex);
        }
    }
    private bool CanGenerateSitemap() => SelectedScan != null;

    [RelayCommand(CanExecute = nameof(CanOpenAi))]
    private void OpenAi()
    {
        if (SelectedScan == null) return;
        
        var pagesToAnalyze = SelectedScan.Pages == null ? new List<Swallows.Core.Models.Page>() : SelectedScan.Pages.Take(50).ToList(); 
        
        var llmService = new Swallows.Core.Services.AI.LlmService(new System.Net.Http.HttpClient(), _contextFactory);
        var vm = new AiAnalysisViewModel(llmService, pagesToAnalyze);
        
        var win = new Swallows.Desktop.Views.AiAnalysisWindow
        {
            DataContext = vm
        };
        
        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);

        if (mainWindow != null)
        {
            win.Show(mainWindow);
        }
    }
    private bool CanOpenAi() => SelectedScan != null;

    [RelayCommand(CanExecute = nameof(CanOpenSeoAudit))]
    private void OpenSeoAudit()
    {
        if (SelectedScan == null) return;

        var viewModel = new SeoAuditViewModel(SelectedScan);
        var win = new SeoAuditWindow(viewModel);
        
        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);

        if (mainWindow != null)
        {
            win.Show(mainWindow);
        }
    }
    private bool CanOpenSeoAudit() => SelectedScan != null;

    [RelayCommand(CanExecute = nameof(CanOpenTrend))]
    public async Task OpenTrend()
    {
        var vm = new TrendViewModel(_contextFactory, SelectedScan?.BaseUrl ?? "");
        var win = new TrendWindow
        {
            DataContext = vm
        };
        
        var mainWindow = (Avalonia.Controls.Window?)Avalonia.Application.Current?.ApplicationLifetime?.GetType()
            .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime);

        if (mainWindow != null)
        {
            win.Show(mainWindow);
        }
    }
    private bool CanOpenTrend() => SelectedScan != null;

    [RelayCommand(CanExecute = nameof(CanExportGraph))]
    public async Task ExportGraph()
    {
        if (SelectedScan == null) return;

        try
        {
            StatusMessage = "Generating Graph Visualization...";
            var graphService = new GraphExportService();
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"swallows_graph_{DateTime.Now:yyyyMMdd_HHmmss}.html");

            using (var db = _contextFactory())
            {
                var session = await db.ScanSessions
                    .Include(s => s.Pages)
                    .ThenInclude(p => p.Links)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == SelectedScan.Id);

                if (session != null)
                {
                   await graphService.ExportGraphAsync(session, path);
                   StatusMessage = $"Graph generated: {path}";
                   try 
                   {
                        var p = new System.Diagnostics.Process();
                        p.StartInfo = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true };
                        p.Start();
                   }
                   catch { }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Graph Generation Failed: {ex.Message}";
        }
    }
    private bool CanExportGraph() => SelectedScan != null;
}
