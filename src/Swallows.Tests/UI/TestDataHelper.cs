using System;
using System.Linq;
using Swallows.Core.Models;

namespace Swallows.Tests.UI;

/// <summary>
/// Helper class to create test data for UI tests with proper model properties
/// </summary>
public static class TestDataHelper
{
    public static ScanSession CreateTestScanSession(string url = "https://test.example.com", int pageCount = 10)
    {
        var session = new ScanSession
        {
            BaseUrl = url,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            FinishedAt = DateTime.UtcNow,
            Status = "Completed",
            TotalPagesScanned = pageCount,
            UserAgent = "SwallowsBot/1.0 Test"
        };

        // Create test pages
        for (int i = 0; i < pageCount; i++)
        {
            var page = CreateTestPage(session, url, i);
            session.Pages.Add(page);
        }

        return session;
    }

    public static Page CreateTestPage(ScanSession session, string baseUrl, int index = 0)
    {
        var page = new Page
        {
            Url = $"{baseUrl}/page{index}",
            Title = $"Test Page {index}",
            MetaDescription = $"Description for test page {index}",
            StatusCode = index % 10 == 0 ? 404 : 200,
            LoadTimeMs = 1000 + (index * 100), // in milliseconds
            ContentLength = 3000 + (index * 100),
            ScannedAt = DateTime.UtcNow,
            Depth = index % 3,
            WordCount = 400 + (index * 10),
            H1Count = 1,
            H2Count = 3,
            MissingAltCount = index % 4 == 0 ? 1 : 0,
            HasOpenGraph = true,
            HasTwitterCard = index % 2 == 0,
            HasViewport = true,
            IsTitleOptimal = true,
            IsDescriptionOptimal = true,
            Session = session,
            SessionId = session.Id
        };

        return page;
    }
}
