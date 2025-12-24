using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Models;
using Swallows.Core.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Swallows.Desktop.ViewModels;

public partial class SeoAuditViewModel : ViewModelBase
{
    private readonly SeoAuditService _seoAuditService;
    private readonly ScanSession? _scanSession;

    [ObservableProperty] private ObservableCollection<SeoAuditItem> _auditItems = new();
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _totalIssues;
    [ObservableProperty] private int _criticalIssues;
    [ObservableProperty] private double _averageSeoScore;
    [ObservableProperty] private string _summaryText = "No data";
    
    // Chart Properties
    public ObservableCollection<ISeries> SeoScoreSeries { get; set; } = new();
    public ObservableCollection<ISeries> StatusCodeSeries { get; set; } = new();
    public ObservableCollection<ISeries> TopIssuesSeries { get; set; } = new();
    public ObservableCollection<ISeries> WordCountSeries { get; set; } = new();
    public ObservableCollection<ISeries> TechnicalFeaturesSeries { get; set; } = new();
    public ObservableCollection<ISeries> CategorySeries { get; set; } = new();

    public SeoAuditViewModel(ScanSession? scanSession)
    {
        _seoAuditService = new SeoAuditService();
        _scanSession = scanSession;

        if (scanSession?.Pages != null && scanSession.Pages.Count > 0)
        {
            LoadAuditData();
        }
    }

    private void LoadAuditData()
    {
        if (_scanSession?.Pages == null)
            return;

        var auditItems = _seoAuditService.GenerateAuditReport(_scanSession.Pages.ToList());
        
        AuditItems = new ObservableCollection<SeoAuditItem>(auditItems);
        TotalPages = auditItems.Count;
        TotalIssues = auditItems.Sum(x => x.IssueCount);
        CriticalIssues = auditItems.Count(x => x.SeoScore == "Poor");
        
        // Calculate average score (Good=100, Needs Improvement=60, Poor=30)
        var scoreSum = auditItems.Sum(x => x.SeoScore switch
        {
            "Good" => 100,
            "Needs Improvement" => 60,
            "Poor" => 30,
            _ => 0
        });
        AverageSeoScore = auditItems.Count > 0 ? scoreSum / auditItems.Count : 0;

        SummaryText = $"{TotalPages} pages analyzed • {TotalIssues} total issues • {CriticalIssues} pages need attention";
        
        // Populate charts
        PopulateCharts(auditItems);
    }
    
    private void PopulateCharts(List<SeoAuditItem> auditItems)
    {
        // 1. SEO Score Distribution (Pie Chart)
        var goodCount = auditItems.Count(x => x.SeoScore == "Good");
        var needsImprovementCount = auditItems.Count(x => x.SeoScore == "Needs Improvement");
        var poorCount = auditItems.Count(x => x.SeoScore == "Poor");
        
        SeoScoreSeries.Add(new PieSeries<int> { Values = new[] { goodCount }, Name = "Good", Fill = new SolidColorPaint(SKColors.Green) });
        SeoScoreSeries.Add(new PieSeries<int> { Values = new[] { needsImprovementCount }, Name = "Needs Improvement", Fill = new SolidColorPaint(SKColors.Orange) });
        SeoScoreSeries.Add(new PieSeries<int> { Values = new[] { poorCount }, Name = "Poor", Fill = new SolidColorPaint(SKColors.Red) });
        
        // 2. Status Code Distribution (Pie Chart)
        var statusGroups = _scanSession!.Pages.GroupBy(p => p.StatusCode).OrderByDescending(g => g.Count()).Take(5);
        foreach (var group in statusGroups)
        {
            var color = group.Key switch
            {
                200 => SKColors.Green,
                404 => SKColors.Red,
                500 => SKColors.DarkRed,
                _ => SKColors.Gray
            };
            StatusCodeSeries.Add(new PieSeries<int> { Values = new[] { group.Count() }, Name = $"{group.Key}", Fill = new SolidColorPaint(color) });
        }
        
        // 3. Top Issues (Bar Chart)
        var issueTypes = new Dictionary<string, int>();
        foreach (var item in auditItems)
        {
            if (item.TitleStatus.Contains("❌") || item.TitleStatus.Contains("⚠"))
                issueTypes["Title Issues"] = issueTypes.GetValueOrDefault("Title Issues") + 1;
            if (item.MetaStatus.Contains("❌") || item.MetaStatus.Contains("⚠"))
                issueTypes["Meta Issues"] = issueTypes.GetValueOrDefault("Meta Issues") + 1;
            if (item.H1Status.Contains("❌") || item.H1Status.Contains("⚠"))
                issueTypes["H1 Issues"] = issueTypes.GetValueOrDefault("H1 Issues") + 1;
            if (item.MissingAlt > 0)
                issueTypes["Missing Alt"] = issueTypes.GetValueOrDefault("Missing Alt") + 1;
            if (item.BrokenLinks > 0)
                issueTypes["Broken Links"] = issueTypes.GetValueOrDefault("Broken Links") + 1;
        }
        
        TopIssuesSeries.Add(new ColumnSeries<int>
        {
            Values = issueTypes.OrderByDescending(x => x.Value).Take(5).Select(x => x.Value).ToArray(),
            Name = "Issue Count",
            Fill = new SolidColorPaint(new SKColor(19, 146, 236))
        });
        
        // 4. Word Count Distribution (Bar Chart)
        var wordRanges = new Dictionary<string, int>
        {
            ["0-300"] = auditItems.Count(x => x.WordCount < 300),
            ["300-600"] = auditItems.Count(x => x.WordCount >= 300 && x.WordCount < 600),
            ["600-1000"] = auditItems.Count(x => x.WordCount >= 600 && x.WordCount < 1000),
            ["1000+"] = auditItems.Count(x => x.WordCount >= 1000)
        };
        
        WordCountSeries.Add(new ColumnSeries<int>
        {
            Values = wordRanges.Values.ToArray(),
            Name = "Pages",
            Fill = new SolidColorPaint(new SKColor(147, 51, 234))
        });
        
        // 5. Technical Features (Donut/Pie Chart)
        var hasViewport = auditItems.Count(x => x.HasViewport);
        var hasOG = auditItems.Count(x => x.HasOpenGraph);
        var hasTwitter = auditItems.Count(x => x.HasTwitterCard);
        var hasCanonical = auditItems.Count(x => x.HasCanonical);
        
        TechnicalFeaturesSeries.Add(new PieSeries<int> { Values = new[] { hasViewport }, Name = "Viewport", Fill = new SolidColorPaint(new SKColor(34, 197, 94)), InnerRadius = 50 });
        TechnicalFeaturesSeries.Add(new PieSeries<int> { Values = new[] { hasOG }, Name = "Open Graph", Fill = new SolidColorPaint(new SKColor(59, 130, 246)), InnerRadius = 50 });
        TechnicalFeaturesSeries.Add(new PieSeries<int> { Values = new[] { hasTwitter }, Name = "Twitter Card", Fill = new SolidColorPaint(new SKColor(29, 161, 242)), InnerRadius = 50 });
        TechnicalFeaturesSeries.Add(new PieSeries<int> { Values = new[] { hasCanonical }, Name = "Canonical", Fill = new SolidColorPaint(new SKColor(251, 146, 60)), InnerRadius = 50 });
        
        // 6. Content Categories from Titles
        var categories = ExtractCategoriesFromTitles(auditItems);
        var colorPalette = new[]
        {
            new SKColor(239, 68, 68),   // Red
            new SKColor(59, 130, 246),   // Blue
            new SKColor(34, 197, 94),    // Green
            new SKColor(251, 146, 60),   // Orange
            new SKColor(147, 51, 234),   // Purple
            new SKColor(236, 72, 153),   // Pink
            new SKColor(14, 165, 233),   // Sky
            new SKColor(234, 179, 8),    // Yellow
        };
        
        int colorIndex = 0;
        foreach (var category in categories.OrderByDescending(x => x.Value).Take(8))
        {
            CategorySeries.Add(new PieSeries<int> 
            { 
                Values = new[] { category.Value }, 
                Name = category.Key, 
                Fill = new SolidColorPaint(colorPalette[colorIndex % colorPalette.Length])
            });
            colorIndex++;
        }
    }
    
    private Dictionary<string, int> ExtractCategoriesFromTitles(List<SeoAuditItem> auditItems)
    {
        var categories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        // Stop words da ignorare (italiano + inglese)
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "il", "lo", "la", "i", "gli", "le", "di", "a", "da", "in", "con", "su", "per", "tra", "fra",
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "home", "page", "about", "contact", "sitemap", "index", "main", "welcome", "404", "error",
            "e", "è", "del", "dei", "delle", "dell", "al", "ai", "alle"
        };
        
        foreach (var item in auditItems)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
                continue;
                
            // Estrai parole significative dal titolo (almeno 3 caratteri)
            var words = item.Title
                .Split(new[] { ' ', '-', '|', ':', '/', '\\', ',', '.', '!', '?', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && !stopWords.Contains(w))
                .Select(w => w.Trim().ToLowerInvariant())
                .Take(3); // Prendi solo le prime 3 parole significative
                
            foreach (var word in words)
            {
                // Capitalizza la prima lettera per display
                var category = char.ToUpper(word[0]) + word.Substring(1);
                categories[category] = categories.GetValueOrDefault(category) + 1;
            }
        }
        
        return categories;
    }

    [RelayCommand]
    private async Task ExportToCsv()
    {
        if (AuditItems.Count == 0)
            return;

        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime?.GetType()
                .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime) as Avalonia.Controls.Window;

            var file = await topLevel?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export SEO Audit to CSV",
                SuggestedFileName = $"seo_audit_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } }
                }
            })!;

            if (file != null)
            {
                var csvData = await _seoAuditService.ExportToCsvAsync(AuditItems.ToList());
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(csvData);
                
                LoggerService.Info($"SEO Audit exported to CSV: {file.Path.LocalPath}");
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error("Error exporting to CSV", ex);
        }
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        if (AuditItems.Count == 0)
            return;

        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime?.GetType()
                .GetProperty("MainWindow")?.GetValue(Avalonia.Application.Current.ApplicationLifetime) as Avalonia.Controls.Window;

            var file = await topLevel?.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export SEO Audit to Excel",
                SuggestedFileName = $"seo_audit_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Excel Files") { Patterns = new[] { "*.xlsx" } }
                }
            })!;

            if (file != null)
            {
                var excelData = await _seoAuditService.ExportToExcelAsync(AuditItems.ToList());
                await using var stream = await file.OpenWriteAsync();
                await stream.WriteAsync(excelData);
                
                LoggerService.Info($"SEO Audit exported to Excel: {file.Path.LocalPath}");
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error("Error exporting to Excel", ex);
        }
    }
}
