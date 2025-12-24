using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Swallows.Core.Models;
using Swallows.Core.Services.AI;

namespace Swallows.Desktop.ViewModels;

public class AiResult
{
    public string Url { get; set; } = "";
    public string Status { get; set; } = "Pending"; // Pending, Processing, Done, Error
    public string ResultJson { get; set; } = "";
}

public partial class AiAnalysisViewModel : ObservableObject
{
    private readonly LlmService _llmService;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _selectedAnalysisType = "Audit";
    
    public ObservableCollection<string> AnalysisTypes { get; } = new ObservableCollection<string> { "Audit", "GenerateMeta" };
    public ObservableCollection<AiResult> Results { get; } = new();

    // We pass page IDs or fully loaded pages? Page IDs is safer if we need to fetch.
    // But currently we passing loaded items from DataGrid.
    private readonly System.Collections.Generic.List<Page> _pagesToAnalyze;

    public AiAnalysisViewModel(LlmService llmService, System.Collections.Generic.List<Page> pages)
    {
        _llmService = llmService;
        _pagesToAnalyze = pages;
        
        foreach(var p in pages)
        {
            Results.Add(new AiResult { Url = p.Url, Status = "Pending" });
        }
    }

    [RelayCommand]
    private async Task RunAnalysis()
    {
        if (IsBusy) return;
        IsBusy = true;
        
        try 
        {
            StatusMessage = "Initializing AI Provider...";
            await _llmService.InitializeAsync();
            
            int total = Results.Count;
            int current = 0;

            foreach (var item in Results.ToList()) // ToList to avoid modification issues
            {
                if (item.Status == "Done") continue; // Skip already done

                item.Status = "Processing...";
                // Force UI update implies we might need to modify Observables or use PropertyChanged
                // Since AiResult is simple POCO, UI might not update unless we replace it or implementation INPC.
                // For simplicity, let's just let DataGrid unbind/rebind or assume it doesn't update live per row perfectly unless INPC.
                // Actually DataGrid usually updates if property changes? No, only if INPC.
                // Let's implement INPC on AiResult or just simple replacement.
                // Replacing object in ObservableCollection triggers update.
                int index = Results.IndexOf(item);
                
                var page = _pagesToAnalyze.FirstOrDefault(p => p.Url == item.Url);
                if (page != null)
                {
                    StatusMessage = $"Analyzing {item.Url}...";
                    try 
                    {
                        var json = await _llmService.AnalyzePageAsync(page, SelectedAnalysisType);
                        item.ResultJson = json;
                        item.Status = "Done";
                    }
                    catch (Exception ex)
                    {
                        item.Status = "Error: " + ex.Message;
                    }
                }
                
                // Trigger update
                Results[index] = item;
                Results[index] = new AiResult { Url = item.Url, Status = item.Status, ResultJson = item.ResultJson }; // Hack force refresh

                current++;
                Progress = (double)current / total * 100;
            }
            
            StatusMessage = "Analysis Complete.";
        }
        catch(Exception ex)
        {
            StatusMessage = "Error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
