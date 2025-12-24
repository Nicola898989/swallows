using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Swallows.Core.Models;
using Newtonsoft.Json.Linq;

namespace Swallows.Core.Services.AI;

public class HuggingFaceService
{
    private readonly HttpClient _http;

    public HuggingFaceService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<HFModel>> SearchGGUFModelsAsync(string query)
    {
        // Minimal stub implementation provided since source was lost.
        // In real app this would query HF API.
        await Task.Delay(100); 
        
        if (string.IsNullOrEmpty(query)) return new List<HFModel>();

        return new List<HFModel>
        {
            new HFModel { Id = $"TheBloke/{query}-GGUF", Author = "TheBloke", Downloads = 1000, Likes = 50, Parameters = "7B", LastModified = DateTime.Now },
            new HFModel { Id = $"MaziyarPan/{query}-GGUF", Author = "MaziyarPan", Downloads = 500, Likes = 20, Parameters = "7B", LastModified = DateTime.Now }
        };
    }
    
    public async Task<List<QuantizationFile>> GetModelFilesAsync(string modelId)
    {
         await Task.Delay(100);
         return new List<QuantizationFile>
         {
             new QuantizationFile { Filename = "model.q4_k_m.gguf", QuantizationLevel = "Q4_K_M", Size = 4000000000, SizeFormatted = "4.0 GB" },
             new QuantizationFile { Filename = "model.q5_k_m.gguf", QuantizationLevel = "Q5_K_M", Size = 5000000000, SizeFormatted = "5.0 GB" }
         };
    }
    
    public async Task<string> DownloadModelAsync(string modelId, string filename, IProgress<double> progress)
    {
        // Simulation
        for(int i=0; i<=100; i+=10)
        {
            progress.Report(i);
            await Task.Delay(50);
        }
        return $"/tmp/{filename}";
    }
}
