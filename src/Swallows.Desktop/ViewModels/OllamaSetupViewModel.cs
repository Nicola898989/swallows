using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Services;
using Swallows.Core.Services.AI;

namespace Swallows.Desktop.ViewModels;

public partial class OllamaSetupViewModel : ViewModelBase
{
    private readonly OllamaInstallerService _installerService;
    private readonly OllamaProcessService _processService;
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private string _installProgress = "Ready to install";
    [ObservableProperty] private bool _installSuccessful;
    [ObservableProperty] private string? _errorMessage;
    
    [ObservableProperty] private bool _isServiceRunning;
    [ObservableProperty] private string _serviceStatus = "Checking...";
    [ObservableProperty] private bool _isCheckingService;
    
    [ObservableProperty] private ObservableCollection<string> _installedModels = new();
    [ObservableProperty] private string _modelsStatus = "No models loaded";

    public bool IsLocalOllamaInstalled { get; private set; }
    public string LocalOllamaPath { get; private set; } = "";

    public OllamaSetupViewModel()
    {
        var http = new System.Net.Http.HttpClient();
        _installerService = new OllamaInstallerService(http);
        _processService = new OllamaProcessService(http);
        
        CheckLocalInstallation();
        _ = CheckServiceStatus();
        _ = LoadInstalledModels();
    }

    private void CheckLocalInstallation()
    {
        IsLocalOllamaInstalled = _installerService.IsOllamaInstalled();
        LocalOllamaPath = _installerService.GetLocalOllamaPath();
        
        if (IsLocalOllamaInstalled)
        {
            InstallProgress = "Ollama is already installed";
            InstallSuccessful = true;
        }
    }

    [RelayCommand]
    private async Task Install()
    {
        if (IsInstalling) return;

        IsInstalling = true;
        InstallSuccessful = false;
        ErrorMessage = null;
        InstallProgress = "Checking disk space...";

        try
        {
            // Check disk space (1GB required)
            var hasSpace = await _installerService.CheckDiskSpaceAsync(1L * 1024 * 1024 * 1024);
            if (!hasSpace)
            {
                ErrorMessage = "Insufficient disk space. At least 1GB of free space is required.";
                InstallProgress = "Installation failed: insufficient disk space";
                LoggerService.Error(ErrorMessage);
                return;
            }

            var progress = new Progress<string>(update =>
            {
                InstallProgress = update;
                LoggerService.Info($"Install progress: {update}");
            });

            var success = await _installerService.DownloadAndInstallAsync(progress);

            if (success)
            {
                InstallSuccessful = true;
                IsLocalOllamaInstalled = true;
                InstallProgress = "Installation complete! You can now use AI features.";
                LoggerService.Info("Ollama installation completed successfully");
            }
            else
            {
                ErrorMessage = "Installation failed. Please check logs for details.";
                InstallProgress = "Installation failed";
                LoggerService.Error("Ollama installation failed");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Installation error: {ex.Message}";
            InstallProgress = "Installation failed";
            LoggerService.Error("Ollama installation exception", ex);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    [RelayCommand]
    private void Skip()
    {
        LoggerService.Info("User skipped Ollama installation");
        // Window will be closed by caller
    }

    private async Task CheckServiceStatus()
    {
        if (!IsLocalOllamaInstalled)
        {
            ServiceStatus = "Not installed";
            IsServiceRunning = false;
            return;
        }

        IsCheckingService = true;
        try
        {
            var isRunning = await _processService.IsRunningAsync();
            IsServiceRunning = isRunning;
            ServiceStatus = isRunning ? "🟢 Running" : "🔴 Stopped";
            LoggerService.Info($"Ollama service status: {ServiceStatus}");
        }
        catch (Exception ex)
        {
            ServiceStatus = "⚠️ Unknown";
            LoggerService.Error("Failed to check Ollama service status", ex);
        }
        finally
        {
            IsCheckingService = false;
        }
    }

    [RelayCommand]
    private async Task StartService()
    {
        ServiceStatus = "Starting...";
        LoggerService.Info("Attempting to start Ollama service");

        try
        {
            var success = await _processService.StartServiceAsync();
            
            if (success)
            {
                IsServiceRunning = true;
                ServiceStatus = "🟢 Running";
                LoggerService.Info("Ollama service started successfully");
                
                // Auto-load installed models after service start
                await LoadInstalledModels();
            }
            else
            {
                ServiceStatus = "❌ Failed to start";
                ErrorMessage = "Could not start Ollama. Check logs for details.";
                LoggerService.Error("Failed to start Ollama service");
            }
        }
        catch (Exception ex)
        {
            ServiceStatus = "❌ Error";
            ErrorMessage = $"Start error: {ex.Message}";
            LoggerService.Error("Exception starting Ollama service", ex);
        }
    }

    [RelayCommand]
    private async Task StopService()
    {
        ServiceStatus = "Stopping...";
        LoggerService.Info("Attempting to stop Ollama service");

        try
        {
            // Find and kill all Ollama processes
            var ollamaProcesses = System.Diagnostics.Process.GetProcessesByName("ollama");
            
            if (ollamaProcesses.Length == 0)
            {
                ServiceStatus = "🔴 Stopped";
                IsServiceRunning = false;
                LoggerService.Info("No Ollama processes found");
                return;
            }

            LoggerService.Info($"Found {ollamaProcesses.Length} Ollama process(es), terminating...");
            
            foreach (var process in ollamaProcesses)
            {
                try
                {
                    LoggerService.Info($"Killing Ollama process: PID {process.Id}");
                    process.Kill();
                    await process.WaitForExitAsync();
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    LoggerService.Warn($"Failed to kill process {process.Id}: {ex.Message}");
                }
            }

            // Give it a moment to clean up
            await Task.Delay(500);
            
            // Verify it's stopped
            await CheckServiceStatus();
            
            if (!IsServiceRunning)
            {
                LoggerService.Info("Ollama service stopped successfully");
            }
            else
            {
                ServiceStatus = "⚠️ Some processes may still be running";
                ErrorMessage = "Some Ollama processes could not be stopped. Check Activity Monitor.";
                LoggerService.Warn("Unable to stop all Ollama processes");
            }
        }
        catch (Exception ex)
        {
            ServiceStatus = "❌ Error";
            ErrorMessage = $"Stop error: {ex.Message}";
            LoggerService.Error("Exception stopping Ollama service", ex);
        }
    }

    [RelayCommand]
    private async Task RefreshStatus()
    {
        await CheckServiceStatus();
        await LoadInstalledModels();
    }

    private async Task LoadInstalledModels()
    {
        try
        {
            InstalledModels.Clear();
            
            if (!IsLocalOllamaInstalled)
            {
                ModelsStatus = "Ollama not installed";
                return;
            }

            if (!IsServiceRunning)
            {
                ModelsStatus = "Start Ollama to see models";
                return;
            }

            LoggerService.Info("Loading installed Ollama models...");
            var models = await _processService.ListModelsAsync();

            if (models.Count == 0)
            {
                ModelsStatus = "No models installed yet";
                LoggerService.Info("No models found");
            }
            else
            {
                foreach (var model in models)
                {
                    InstalledModels.Add(model);
                }
                ModelsStatus = $"{models.Count} model(s) installed";
                LoggerService.Info($"Loaded {models.Count} models");
            }
        }
        catch (Exception ex)
        {
            ModelsStatus = "Failed to load models";
            LoggerService.Error("Error loading installed models", ex);
        }
    }

    [RelayCommand]
    private async Task RunModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            LoggerService.Warn("RunModel called with empty model name");
            return;
        }

        try
        {
            ModelsStatus = $"Loading {modelName} into memory...";
            LoggerService.Info($"Loading model into memory: {modelName}");

            // Use /api/show to load model info (lighter than generate)
            var requestBody = new
            {
                name = modelName
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:11434/api/show", content);
            
            if (response.IsSuccessStatusCode)
            {
                // Read the response with a timeout to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var responseText = await response.Content.ReadAsStringAsync(cts.Token);
                LoggerService.Debug($"Model info loaded: {responseText.Substring(0, Math.Min(200, responseText.Length))}...");
                
                ModelsStatus = $"✓ {modelName} ready";
                LoggerService.Info($"Model {modelName} loaded into memory");
            }
            else
            {
                ModelsStatus = $"Failed to load {modelName}";
                LoggerService.Error($"Failed to load model {modelName}: {response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            ModelsStatus = $"Timeout loading {modelName}";
            LoggerService.Warn($"Timeout while loading model {modelName} - model may still be loading in background");
        }
        catch (Exception ex)
        {
            ModelsStatus = $"Error loading {modelName}";
            LoggerService.Error($"Exception loading model {modelName}", ex);
        }
    }
}
