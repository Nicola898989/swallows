using System.Net.Http;

namespace Swallows.Core.Services.AI;

public class OllamaProcessService
{
    public OllamaProcessService(HttpClient http)
    {
    }

    public (string Disk, string Mem) GetSystemResources()
    {
        return ("100 GB Free", "16 GB RAM");
    }

    public bool IsOllamaInstalled() => true;
    
    public string GetOllamaPath() => "/usr/local/bin/ollama";
    
    public Task<bool> IsServiceRunningAsync() => Task.FromResult(true);
    public Task<bool> IsRunningAsync(string url) => Task.FromResult(true);
    public Task<bool> IsRunningAsync() => Task.FromResult(true);
    
    public Task<bool> StartServiceAsync() => Task.FromResult(true);
    public Task StopServiceAsync() => Task.CompletedTask;
    
    public Task<List<string>> ListInstalledModelsAsync() 
    {
        return Task.FromResult(new List<string> { "llama2", "mistral" });
    }

    public Task<List<string>> ListModelsAsync() => ListInstalledModelsAsync();

    public Task<string> PullModelAsync(string modelName, IProgress<string>? progress = null)
    {
        return Task.FromResult("Success");
    }
}
