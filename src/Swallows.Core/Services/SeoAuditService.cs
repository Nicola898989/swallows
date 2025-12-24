using System.Collections.Generic;
using Swallows.Core.Models;
using System.Text;

namespace Swallows.Core.Services;

public class SeoAuditService
{
    public List<SeoAuditItem> GenerateAuditReport(List<Page> pages)
    {
        // Stub implementation
        var items = new List<SeoAuditItem>();
        if (pages == null) return items;

        foreach(var page in pages)
        {
            items.Add(new SeoAuditItem
            {
                Url = page.Url,
                Title = page.Title ?? "",
                SeoScore = "Good",
                TitleStatus = "OK",
                MetaStatus = "OK",
                H1Status = "OK",
                HasViewport = true,
                HasOpenGraph = false,
                HasTwitterCard = false,
                HasCanonical = true
            });
        }
        return items;
    }

    public Task<byte[]> ExportToCsvAsync(List<SeoAuditItem> items)
    {
        var csv = "Url,SeoScore\n";
        return Task.FromResult(Encoding.UTF8.GetBytes(csv));
    }

    public Task<byte[]> ExportToExcelAsync(List<SeoAuditItem> items)
    {
        // Return dummy bytes
        return Task.FromResult(new byte[0]);
    }
}
