using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Models;
using Swallows.Core.Services;
using Swallows.Core.Services.AI;
using System.Linq;
using Swallows.Desktop.Views;

namespace Swallows.Desktop.ViewModels;

public partial class MarketplaceViewModel : ViewModelBase
{
    private readonly HuggingFaceService _hfService;
    private readonly OllamaProcessService _processService;
    private readonly OllamaModelfileService _modelfileService;
    private readonly SystemResourceService _resourceService;

    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private string _downloadProgress = "";
    [ObservableProperty] private string _systemResources = "";
    
    // Hardware Awareness Properties
    [ObservableProperty] private ObservableCollection<QuantizationFile> _selectedModelFiles = new();
    [ObservableProperty] private string _hardwareStatus = "";
    [ObservableProperty] private Avalonia.Media.IBrush _hardwareStatusColor = Avalonia.Media.Brushes.Gray;

    public ObservableCollection<HFModel> SearchResults { get; } = new();

    // Manual properties to handle change logic reliably
    private HFModel? _selectedModel;
    public HFModel? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetProperty(ref _selectedModel, value))
            {
                _ = LoadModelFiles(value);
            }
        }
    }

    private QuantizationFile? _selectedFile;
    public QuantizationFile? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                CheckHardware(value);
            }
        }
    }

    public MarketplaceViewModel() 
    {
        // Default constructor for design/preview
        var http = new System.Net.Http.HttpClient();
        _hfService = new HuggingFaceService(http);
        _processService = new OllamaProcessService(http);
        _modelfileService = new OllamaModelfileService();
        _resourceService = new SystemResourceService();
        
        LoadSystemResources();
        // Load default models on open
        _ = Search();
    }
    
    private void LoadSystemResources()
    {
        var (disk, mem) = _processService.GetSystemResources();
        SystemResources = $"Disk: {disk} | VRAM Hint: {mem}";
    }

    [RelayCommand]
    private async Task Search()
    {
        IsSearching = true;
        StatusMessage = "Searching Hugging Face...";
        SearchResults.Clear();
        
        var results = await _hfService.SearchGGUFModelsAsync(SearchQuery);
        
        foreach (var model in results)
        {
            SearchResults.Add(model);
        }
        
        StatusMessage = $"{results.Count} models found.";
        IsSearching = false;
    }

    private async Task LoadModelFiles(HFModel? model)
    {
        if (model == null) 
        {
            SelectedModelFiles.Clear();
            LoggerService.Info("Model selection cleared");
            return;
        }
        
        LoggerService.Info($"Loading files for model: {model.Id}");
        StatusMessage = "Fetching file details...";
        
        try
        {
            var files = await _hfService.GetModelFilesAsync(model.Id);
            
            LoggerService.Info($"Retrieved {files.Count} files from Hugging Face API");
            
            SelectedModelFiles.Clear();
            foreach(var f in files) 
            {
                SelectedModelFiles.Add(f);
                LoggerService.Debug($"  - {f.QuantizationLevel}: {f.SizeFormatted} ({f.Path})");
            }
            
            // Auto-select Q4_K_M if exists, or first
            SelectedFile = SelectedModelFiles.FirstOrDefault(f => f.Path.Contains("Q4_K_M", StringComparison.OrdinalIgnoreCase)) 
                           ?? SelectedModelFiles.FirstOrDefault();
            
            if (SelectedFile != null)
            {
                LoggerService.Info($"Auto-selected quantization: {SelectedFile.QuantizationLevel} ({SelectedFile.SizeFormatted})");
            }
                           
            StatusMessage = $"Loaded {files.Count} variants.";
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Failed to load model files for {model.Id}", ex);
            StatusMessage = $"Error loading variants: {ex.Message}";
        }
    }

    private void CheckHardware(QuantizationFile? file)
    {
        if (file == null)
        {
            HardwareStatus = "";
            return;
        }
        
        var status = _resourceService.GetRamStatus(file.Size);
        switch(status)
        {
            case RamStatus.Safe:
                 HardwareStatus = "🟢 Compatible (Safe)";
                 HardwareStatusColor = Avalonia.Media.Brushes.Green;
                 break;
            case RamStatus.Risky:
                 HardwareStatus = "🟠 Risky (High RAM Usage)";
                 HardwareStatusColor = Avalonia.Media.Brushes.Orange;
                 break;
            case RamStatus.Unsupported:
                 HardwareStatus = "🔴 Unsupported (Insuffcient RAM)";
                 HardwareStatusColor = Avalonia.Media.Brushes.Red;
                 break;
        }
    }

    [RelayCommand]
    private async Task Download(HFModel model)
    {
       LoggerService.Info($"Download requested for model: {model.Id}");
       
       // Use SelectedFile if available
       var fileToDownload = SelectedFile; 
       
       if (fileToDownload == null) 
       {
           LoggerService.Warn("No quantization variant selected");
           StatusMessage = "Please select a quantization variant first.";
           return;
       }
       
       if (IsDownloading) 
       {
           LoggerService.Warn("Download already in progress, ignoring request");
           return;
       }
        
        IsDownloading = true;
        
        string qTag = fileToDownload.QuantizationLevel; 
        if (qTag == "Unknown") qTag = ""; // Fallback
        else qTag = $":{qTag}";
        
        string pullName = $"hf.co/{model.Id}{qTag}";
        
        LoggerService.Info($"Starting download: {pullName} (Size: {fileToDownload.SizeFormatted})");
        StatusMessage = $"Downloading {model.Id} ({fileToDownload.QuantizationLevel})...";
        
        DownloadProgress = "Starting...";
        
        var progress = new Progress<string>(update => 
        {
            DownloadProgress = update;
            LoggerService.Debug($"Download progress: {update}");
        });
        
        try
        {
            string result = await _processService.PullModelAsync(pullName, progress);
            
            LoggerService.Info($"Download completed: {result}");
            StatusMessage = result;
            DownloadProgress = result.Contains("Success") ? "Completed" : "Failed";
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Download failed for {pullName}", ex);
            StatusMessage = $"Download failed: {ex.Message}";
            DownloadProgress = "Failed";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private async Task CreateAgent()
    {
         if (SelectedModel == null)
         {
             StatusMessage = "Please select a model first.";
             LoggerService.Warn("CreateAgent called with null SelectedModel");
             return;
         }

         string baseName = $"hf.co/{SelectedModel.Id}";
         string newName = $"swallow-{SelectedModel.Author}".ToLower();
         
         StatusMessage = $"Creating {newName}...";
         
         string result = await _modelfileService.CreateCustomModelAsync(baseName, newName);
         StatusMessage = result;
    }

    [RelayCommand]
    private async Task OpenSetup()
    {
        LoggerService.Info("Opening Ollama setup window");
        var setupWindow = new OllamaSetupWindow();
        var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
            ? desktop.MainWindow 
            : null;
            
        if (mainWindow != null)
        {
            await setupWindow.ShowDialog(mainWindow);
        }
    }
}
