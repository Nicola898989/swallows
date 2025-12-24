using System.Collections.Generic;
using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class AiAnalysisService
{
    public IEnumerable<string> GetAnalysisTypes()
    {
        return new[] { "Semantic Audit", "Content Categorization", "Sentiment Analysis" };
    }

    public Task ProcessAsync(ScanSession session, string analysisType, IProgress<double> progress)
    {
        return Task.CompletedTask;
    }
}
