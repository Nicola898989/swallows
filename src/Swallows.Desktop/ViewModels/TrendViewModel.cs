using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Swallows.Core.Data;
using Swallows.Core.Models;

namespace Swallows.Desktop.ViewModels;

public partial class TrendViewModel : ViewModelBase
{
    private readonly Func<AppDbContext> _contextFactory;
    private readonly string _baseUrl;
    
    [ObservableProperty] private string _scanUrl = "";
    [ObservableProperty] private string _timeRange = "Last 30 Days";
    [ObservableProperty] private string _insightsSummary = "Loading...";
    [ObservableProperty] private int _totalScans;
    
    public ObservableCollection<ISeries> SeoScoreSeries { get; } = new();
    public ObservableCollection<ISeries> ErrorCountSeries { get; } = new();
    public ObservableCollection<ISeries> PageCountSeries { get; } = new();
    public ObservableCollection<ISeries> LoadTimeSeries { get; } = new();
    
    public TrendViewModel(Func<AppDbContext> contextFactory, string baseUrl)
    {
        _contextFactory = contextFactory;
        _baseUrl = baseUrl;
        ScanUrl = baseUrl;
        
        _ = LoadTrendDataAsync();
    }
    
    partial void OnTimeRangeChanged(string value)
    {
        _ = LoadTrendDataAsync();
    }
    
    private async Task LoadTrendDataAsync()
    {
        try
        {
            using var db = _contextFactory();
            
            var threshold = GetDateThreshold(TimeRange);
            
            var scans = await db.ScanSessions
                .Where(s => s.BaseUrl == _baseUrl && s.StartedAt >= threshold)
                .OrderBy(s => s.StartedAt)
                .Include(s => s.Pages)
                .ToListAsync();
            
            TotalScans = scans.Count;
            
            if (scans.Count == 0)
            {
                InsightsSummary = "📊 No historical data available for this URL.\n\nRun more scans to track SEO trends over time!";
                ClearAllCharts();
                return;
            }
            
            BuildSeoScoreChart(scans);
            BuildErrorCountChart(scans);
            BuildPageCountChart(scans);
            BuildLoadTimeChart(scans);
            GenerateInsights(scans);
        }
        catch (Exception ex)
        {
            InsightsSummary = $"❌ Error loading trend data: {ex.Message}";
        }
    }
    
    private DateTime GetDateThreshold(string range)
    {
        return range switch
        {
            "Last 7 Days" => DateTime.Now.AddDays(-7),
            "Last 30 Days" => DateTime.Now.AddDays(-30),
            "Last 90 Days" => DateTime.Now.AddDays(-90),
            _ => DateTime.MinValue
        };
    }
    
    private void BuildSeoScoreChart(List<ScanSession> scans)
    {
        var values = scans.Select(s => new
        {
            Date = s.StartedAt,
            Score = CalculateAverageSeoScore(s.Pages)
        }).ToList();
        
        SeoScoreSeries.Clear();
        SeoScoreSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = values.Select(v => new DateTimePoint(v.Date, v.Score)).ToList(),
            Name = "SEO Score",
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
            GeometrySize = 10,
            GeometryStroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(SKColors.LightGreen)
        });
    }
    
    private double CalculateAverageSeoScore(ICollection<Page>? pages)
    {
        if (pages == null || pages.Count == 0) return 0;
        
        var scores = pages.Select(p =>
        {
            int score = 100;
            
            if (string.IsNullOrEmpty(p.Title) || p.Title.Length < 10) score -= 20;
            if (string.IsNullOrEmpty(p.MetaDescription)) score -= 15;
            if (p.H1Count == 0) score -= 10;
            if (p.StatusCode != 200) score -= 30;
            if (p.LoadTimeMs > 3000) score -= 10;
            if (p.WordCount < 300) score -= 5;
            
            return Math.Max(0, score);
        });
        
        return scores.Average();
    }
    
    private void BuildErrorCountChart(List<ScanSession> scans)
    {
        var values = scans.Select(s => new
        {
            Date = s.StartedAt,
            Errors = s.Pages?.Count(p => p.StatusCode != 200) ?? 0
        }).ToList();
        
        ErrorCountSeries.Clear();
        ErrorCountSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = values.Select(v => new DateTimePoint(v.Date, v.Errors)).ToList(),
            Name = "Error Pages",
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
            GeometrySize = 10,
            GeometryStroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(SKColors.LightCoral)
        });
    }
    
    private void BuildPageCountChart(List<ScanSession> scans)
    {
        var values = scans.Select(s => new
        {
            Date = s.StartedAt,
            Count = s.Pages?.Count ?? 0
        }).ToList();
        
        PageCountSeries.Clear();
        PageCountSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = values.Select(v => new DateTimePoint(v.Date, v.Count)).ToList(),
            Name = "Total Pages",
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
            GeometrySize = 10,
            GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(SKColors.LightBlue)
        });
    }
    
    private void BuildLoadTimeChart(List<ScanSession> scans)
    {
        var values = scans.Select(s => new
        {
            Date = s.StartedAt,
            AvgTime = s.Pages?.Any() == true ? s.Pages.Average(p => p.LoadTimeMs) : 0
        }).ToList();
        
        LoadTimeSeries.Clear();
        LoadTimeSeries.Add(new LineSeries<DateTimePoint>
        {
            Values = values.Select(v => new DateTimePoint(v.Date, v.AvgTime)).ToList(),
            Name = "Avg Load Time (ms)",
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 },
            GeometrySize = 10,
            GeometryStroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 },
            GeometryFill = new SolidColorPaint(SKColors.LightYellow)
        });
    }
    
    private void GenerateInsights(List<ScanSession> scans)
    {
        if (scans.Count < 2)
        {
            InsightsSummary = $"📊 Found {scans.Count} scan.\n\nRun more scans to track trends and generate insights!";
            return;
        }
        
        var first = scans.First();
        var last = scans.Last();
        
        var firstScore = CalculateAverageSeoScore(first.Pages);
        var lastScore = CalculateAverageSeoScore(last.Pages);
        var scoreChange = firstScore > 0 ? ((lastScore - firstScore) / firstScore) * 100 : 0;
        
        var firstErrors = first.Pages?.Count(p => p.StatusCode != 200) ?? 0;
        var lastErrors = last.Pages?.Count(p => p.StatusCode != 200) ?? 0;
        var errorChange = firstErrors > 0 ? ((lastErrors - firstErrors) / (double)firstErrors) * 100 : 0;
        
        var firstPages = first.Pages?.Count ?? 0;
        var lastPages = last.Pages?.Count ?? 0;
        var pageGrowth = firstPages > 0 ? ((lastPages - firstPages) / (double)firstPages) * 100 : 0;
        
        var firstAvg = first.Pages?.Any() == true ? first.Pages.Average(p => p.LoadTimeMs) : 0;
        var lastAvg = last.Pages?.Any() == true ? last.Pages.Average(p => p.LoadTimeMs) : 0;
        var perfChange = firstAvg > 0 ? ((lastAvg - firstAvg) / firstAvg) * 100 : 0;
        
        var timeSpan = last.StartedAt - first.StartedAt;
        var daysAgo = (int)timeSpan.TotalDays;
        
        InsightsSummary = $"""
            📊 Key Insights ({TimeRange} - {scans.Count} scans over {daysAgo} days):
            
            • SEO Score: {scoreChange:+0.0;-0.0;0.0}% ({firstScore:F0} → {lastScore:F0})
              {GetTrendEmoji(scoreChange)} {GetTrendText(scoreChange)}
            
            • Error Pages: {errorChange:+0.0;-0.0;0.0}% ({firstErrors} → {lastErrors})
              {GetTrendEmoji(-errorChange)} {(errorChange < 0 ? "Great progress!" : errorChange > 0 ? "More errors detected" : "Stable")}
            
            • Total Pages: {pageGrowth:+0.0;-0.0;0.0}% ({firstPages} → {lastPages})
              {(pageGrowth > 10 ? "📈 Significant growth!" : pageGrowth > 0 ? "📊 Steady growth" : "→ Stable")}
            
            • Performance: {perfChange:+0.0;-0.0;0.0}% ({firstAvg:F0}ms → {lastAvg:F0}ms)
              {GetTrendEmoji(-perfChange)} {(perfChange < 0 ? "Faster!" : perfChange > 0 ? "Slower" : "Stable")}
            """;
    }
    
    private string GetTrendEmoji(double change)
    {
        if (change > 5) return "✅";
        if (change < -5) return "⚠️";
        return "→";
    }
    
    private string GetTrendText(double change)
    {
        if (change > 10) return "Excellent improvement!";
        if (change > 5) return "Improving!";
        if (change < -10) return "Significant decline";
        if (change < -5) return "Declining";
        return "Stable";
    }
    
    private void ClearAllCharts()
    {
        SeoScoreSeries.Clear();
        ErrorCountSeries.Clear();
        PageCountSeries.Clear();
        LoadTimeSeries.Clear();
    }
}
