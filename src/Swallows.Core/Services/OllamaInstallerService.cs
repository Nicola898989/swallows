using System.Net.Http;

namespace Swallows.Core.Services.AI;

public class OllamaInstallerService
{
    public OllamaInstallerService(HttpClient http)
    {
    }

    public bool IsOllamaInstalled() => true;
    public string GetLocalOllamaPath() => "/usr/local/bin/ollama";
    
    public Task InstallAsync() => Task.CompletedTask;

    public Task<bool> CheckDiskSpaceAsync(long bytesNeeded)
    {
        // Mock check
        return Task.FromResult(true);
    }

    public Task<bool> DownloadAndInstallAsync(IProgress<string> progress)
    {
        progress.Report("Downloading...");
        return Task.FromResult(true);
    }
}
