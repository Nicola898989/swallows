using System.Threading.Tasks;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class GraphExportService
{
    public Task ExportGraphAsync(ScanSession session, string path)
    {
        return Task.CompletedTask;
    }
}
