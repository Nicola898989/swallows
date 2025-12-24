using System.Collections.Generic;
using Swallows.Core.Data;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class ComparisonResult
{
    public List<PageDiff> NewPages { get; set; } = new();
    public List<PageDiff> RemovedPages { get; set; } = new();
    public List<PageDiff> StatusChanges { get; set; } = new();
    public List<PageDiff> MetaChanges { get; set; } = new();
}

public class ComparisonService
{
    private readonly Func<AppDbContext> _contextFactory;

    public ComparisonService(Func<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task<ComparisonResult> CompareSessionsAsync(int baselineId, int comparisonId)
    {
        // Stub
        return Task.FromResult(new ComparisonResult());
    }
}
