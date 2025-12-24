using System.Collections.Generic;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class CsvExportService
{
    public Task ExportToCsvAsync(IEnumerable<Page> pages, string path)
    {
        return Task.CompletedTask;
    }
}
