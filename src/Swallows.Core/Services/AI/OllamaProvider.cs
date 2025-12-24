using System.Net.Http;
using System.Text.Json;
using System.Text;
using Swallows.Core.Models;

namespace Swallows.Core.Services.AI;

public class OllamaProvider
{
    private readonly LlmSettings _settings;
    private readonly HttpClient _http;

    public OllamaProvider(LlmSettings settings, HttpClient http)
    {
        _settings = settings;
        _http = http;
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        var request = new
        {
            model = _settings.ModelName,
            prompt = prompt,
            stream = false
        };

        var content = new StringContent( JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{_settings.BaseUrl}/api/generate", content);
        
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaResponse>(json);
            return result?.response ?? "";
        }
        
        throw new HttpRequestException($"Ollama API Error: {response.StatusCode}");
    }
}

public class OllamaResponse
{
    public string response { get; set; } = "";
}
