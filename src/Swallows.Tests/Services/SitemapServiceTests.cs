using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swallows.Core.Models;
using Swallows.Core.Services;
using Xunit;

namespace Swallows.Tests.Services;

public class SitemapServiceTests
{
    private readonly SitemapService _service;

    public SitemapServiceTests()
    {
        _service = new SitemapService();
    }

    [Fact]
    public async Task GenerateSitemapXml_ShouldCreateValidXml()
    {
        // Arrange
        var session = new ScanSession();
        session.Pages = new System.Collections.ObjectModel.ObservableCollection<Page>
        {
            new Page { Url = "https://example.com", StatusCode = 200, Depth = 0 },
            new Page { Url = "https://example.com/about", StatusCode = 200, Depth = 1 },
            new Page { Url = "https://example.com/404", StatusCode = 404, Depth = 1 }, // Should be excluded
        };

        // Act
        var result = await _service.GenerateSitemapXml(session);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", result);
        Assert.Contains("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">", result);
        Assert.Contains("<loc>https://example.com</loc>", result);
        Assert.Contains("<loc>https://example.com/about</loc>", result);
        Assert.DoesNotContain("<loc>https://example.com/404</loc>", result);
        
        // Check Metadata
        Assert.Contains("<priority>1.0</priority>", result);
        Assert.Contains("<changefreq>daily</changefreq>", result);
        Assert.Contains("<priority>0.8</priority>", result);
        Assert.Contains("<changefreq>weekly</changefreq>", result);
    }

    [Fact]
    public async Task GenerateSitemapXml_ShouldEscapeSpecialCharacters()
    {
        // Arrange
        var session = new ScanSession();
        var urlWithSpecialChars = "https://example.com/search?q=test&lang=en";
        session.Pages = new System.Collections.ObjectModel.ObservableCollection<Page>
        {
            new Page { Url = urlWithSpecialChars, StatusCode = 200, Depth = 1 }
        };

        // Act
        var result = await _service.GenerateSitemapXml(session);

        // Assert
        Assert.Contains("https://example.com/search?q=test&amp;lang=en", result);
    }
}
