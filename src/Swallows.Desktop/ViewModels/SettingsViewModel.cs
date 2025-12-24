using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Models;
using Swallows.Core.Data;
using System.Linq;

namespace Swallows.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _userAgent = "SwallowsBot/1.0";

    [ObservableProperty]
    private int _delayBetweenRequestsMs = 0;

    [ObservableProperty]
    private string? _proxyUrl;

    [ObservableProperty]
    private int _concurrentRequests = 1;

    [ObservableProperty]
    private int _maxPages = 999999;

    [ObservableProperty]
    private int _maxDepth = 999;

    [ObservableProperty]
    private bool _saveImages;

    [ObservableProperty]
    private int _timeoutSeconds = 30;

    // JS Rendering properties
    [ObservableProperty]
    private bool _enableJavaScript;

    [ObservableProperty]
    private int _ajaxTimeoutSeconds = 10;

    [ObservableProperty]
    private bool _enableHeadlessImages;

    // Status message for Install command
    [ObservableProperty]
    private string _settingsStatus = "";

    public ObservableCollection<string> PredefinedUserAgents { get; } = new ObservableCollection<string>
    {
        "SwallowsBot/1.0",
        "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/W.X.Y.Z Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    private readonly System.Func<AppDbContext> _contextFactory;

    public SettingsViewModel() : this(() => new AppDbContext())
    {
    }

    // --- Module 10/12 merged properties ---
    [ObservableProperty] private LlmProviderType _llmProvider = LlmProviderType.Ollama;
    [ObservableProperty] private string _llmApiKey = "";
    [ObservableProperty] private string _llmBaseUrl = "http://localhost:11434";
    [ObservableProperty] private string _llmModelName = "tinyllama:latest";
    
    [ObservableProperty] private string _ollamaStatus = "Checking...";
    [ObservableProperty] private bool _isOllamaRunning;
    [ObservableProperty] private bool _isPullingModel;
    
    public System.Collections.Generic.List<LlmProviderType> LlmProviders { get; } = System.Enum.GetValues(typeof(LlmProviderType)).Cast<LlmProviderType>().ToList();

    // Helper to start the loop
    private async Task CheckOllamaStatusLoop()
    {
        var service = new Swallows.Core.Services.AI.OllamaProcessService(new System.Net.Http.HttpClient());
        while(true)
        {
            // Simple robust check loop
            try 
            {
                if (LlmProvider == LlmProviderType.Ollama)
                {
                    IsOllamaRunning = await service.IsRunningAsync(LlmBaseUrl);
                    OllamaStatus = IsOllamaRunning ? "Running" : "Stopped";
                }
                else
                {
                    OllamaStatus = "Cloud Mode";
                }
            }
            catch {}
            await Task.Delay(5000); 
        }
    }

    [RelayCommand]
    private async Task StartOllama()
    {
        OllamaStatus = "Starting...";
        var service = new Swallows.Core.Services.AI.OllamaProcessService(new System.Net.Http.HttpClient());
        bool success = await service.StartServiceAsync();
        if (success) 
        {
            OllamaStatus = "Running";
            IsOllamaRunning = true;
        }
        else
        {
            OllamaStatus = "Failed to start";
        }
    }
    
    [RelayCommand]
    private async Task PullModel()
    {
        if (IsPullingModel) return;
        IsPullingModel = true;
        OllamaStatus = $"Downloading {LlmModelName}...";
        
        var service = new Swallows.Core.Services.AI.OllamaProcessService(new System.Net.Http.HttpClient());
        string result = await service.PullModelAsync(LlmModelName);
        
        OllamaStatus = result.Contains("Success") ? "Model Ready" : "Download Failed";
        IsPullingModel = false;
    }
    // --------------------------------------

    public SettingsViewModel(System.Func<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        LoadSettings();
        _ = CheckOllamaStatusLoop();
    }

    private void LoadSettings()
    {
        using var db = _contextFactory();
        db.Database.EnsureCreated();
        
        var settings = db.AppSettings.FirstOrDefault();
        if (settings != null)
        {
            UserAgent = settings.UserAgent;
            DelayBetweenRequestsMs = settings.DelayBetweenRequestsMs;
            ProxyUrl = settings.ProxyUrl;
            ConcurrentRequests = settings.ConcurrentRequests;
            MaxPages = settings.MaxPages;
            MaxDepth = settings.MaxDepth;
            SaveImages = settings.SaveImages;
            TimeoutSeconds = settings.TimeoutSeconds;
            
            EnableJavaScript = settings.EnableJavaScript;
            AjaxTimeoutSeconds = settings.AjaxTimeoutSeconds;
            EnableHeadlessImages = settings.EnableHeadlessImages;
            
            // Load Rules
            if (!string.IsNullOrEmpty(settings.ExtractionRulesJson))
            {
                try 
                {
                    var rules = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ExtractionRule>>(settings.ExtractionRulesJson);
                    if (rules != null)
                    {
                        ExtractionRules.Clear();
                        foreach (var r in rules)
                        {
                            ExtractionRules.Add(new ExtractionRuleViewModel 
                            { 
                                Name = r.Name, 
                                Type = r.Type, 
                                Pattern = r.Pattern, 
                                ExtractFirstOnly = r.ExtractFirstOnly 
                            });
                        }
                    }
                }
                catch {}
            }
            
            // Load LLM Settings
            if (!string.IsNullOrEmpty(settings.LlmSettingsJson))
            {
                try
                {
                    var llm = System.Text.Json.JsonSerializer.Deserialize<LlmSettings>(settings.LlmSettingsJson);
                    if (llm != null)
                    {
                        LlmProvider = llm.Provider;
                        LlmApiKey = llm.ApiKey;
                        LlmBaseUrl = llm.BaseUrl;
                        LlmModelName = llm.ModelName;
                    }
                }
                catch {}
            }
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        using var db = _contextFactory();
        var settings = db.AppSettings.FirstOrDefault();
        
        if (settings == null)
        {
            settings = new AppSettings();
            db.AppSettings.Add(settings);
        }

        settings.UserAgent = UserAgent;
        settings.DelayBetweenRequestsMs = DelayBetweenRequestsMs;
        settings.ProxyUrl = ProxyUrl;
        settings.ConcurrentRequests = ConcurrentRequests;
        settings.MaxPages = MaxPages;
        settings.MaxDepth = MaxDepth;
        settings.SaveImages = SaveImages;
        settings.TimeoutSeconds = TimeoutSeconds;

        settings.EnableJavaScript = EnableJavaScript;
        settings.AjaxTimeoutSeconds = AjaxTimeoutSeconds;
        settings.EnableHeadlessImages = EnableHeadlessImages;
        
        // Save Rules
        var rules = ExtractionRules.Select(r => new ExtractionRule 
        { 
            Name = r.Name, 
            Type = r.Type, 
            Pattern = r.Pattern, 
            ExtractFirstOnly = r.ExtractFirstOnly 
        }).ToList();
        
        settings.ExtractionRulesJson = System.Text.Json.JsonSerializer.Serialize(rules);
        
        // Save LLM Settings
        var llmSettings = new LlmSettings
        {
            Provider = LlmProvider,
            ApiKey = LlmApiKey,
            BaseUrl = LlmBaseUrl,
            ModelName = LlmModelName
        };
        settings.LlmSettingsJson = System.Text.Json.JsonSerializer.Serialize(llmSettings);

        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task InstallPlaywright()
    {
        SettingsStatus = "Installing Browsers... (This may take a minute)";
        try 
        {
            // Run the tool manually. 
            // In a published app this is tricky, but for dev:
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
            if (exitCode == 0)
            {
                SettingsStatus = "Browsers Installed Successfully.";
            }
            else
            {
                SettingsStatus = $"Installation Failed with code {exitCode}";
            }
        }
        catch (System.Exception ex)
        {
            SettingsStatus = $"Error installing: {ex.Message}";
        }
    }

    // --- Module 9: Custom Extraction Rules ---
    
    public ObservableCollection<ExtractionRuleViewModel> ExtractionRules { get; } = new();

    [RelayCommand]
    private void AddRule()
    {
        ExtractionRules.Add(new ExtractionRuleViewModel { Name = "New Rule", Pattern = "//div" });
    }

    [RelayCommand]
    private void RemoveRule(ExtractionRuleViewModel rule)
    {
        ExtractionRules.Remove(rule);
    }
}

public partial class ExtractionRuleViewModel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private ExtractionType _type = ExtractionType.XPath;
    [ObservableProperty] private string _pattern = string.Empty;
    [ObservableProperty] private bool _extractFirstOnly = true;

    public static ObservableCollection<ExtractionType> Types { get; } = new(System.Enum.GetValues(typeof(ExtractionType)).Cast<ExtractionType>());
}

    // --- Merged Ollama Properties and methods into main class ---

