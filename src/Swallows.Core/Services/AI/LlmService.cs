using System.Net.Http;
using Swallows.Core.Data;
using Swallows.Core.Models;

namespace Swallows.Core.Services.AI;

public class LlmService
{
    private readonly HttpClient _http;
    private readonly Func<AppDbContext> _contextFactory;

    public LlmService(HttpClient http) : this(http, () => new AppDbContext())
    {
    }

    public LlmService(HttpClient http, Func<AppDbContext> contextFactory)
    {
        _http = http;
        _contextFactory = contextFactory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task<string> AnalyzePageAsync(Page page, string analysisType) => Task.FromResult("{}");
}
