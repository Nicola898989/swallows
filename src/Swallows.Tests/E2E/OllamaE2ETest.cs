using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Swallows.Core.Models;
using Swallows.Core.Services.AI;

namespace Swallows.Tests.E2E;

public class OllamaE2ETest
{
    private readonly HttpClient _client;

    public OllamaE2ETest()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Verify_Ollama_Is_Running_Locally()
    {
        // 1. Check basic connectivity to default port 11434
        string url = "http://localhost:11434"; // Default Ollama port
        try
        {
            var response = await _client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode, $"Ollama is not reachable at {url}. Is it running?");
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Ollama is running", content); // Ollama usually returns this on root
        }
        catch (HttpRequestException)
        {
            Assert.Fail($"Could not connect to {url}. Please ensure 'ollama serve' is running.");
        }
    }

    [Fact]
    public async Task Verify_Ollama_Generation_With_Default_Model()
    {
        // This test assumes 'phi3:mini' or 'llama3' or similar is installed.
        // We will try a very common one or read from settings if possible, but for E2E unit test we might need a fixed one.
        // Let's rely on 'phi3:mini' as suggested in the project.
        
        var settings = new LlmSettings
        {
            Provider = LlmProviderType.Ollama,
            BaseUrl = "http://localhost:11434",
            ModelName = "phi3:mini" // Common small model
        };
        
        var provider = new OllamaProvider(settings, _client);
        
        try 
        {
            // Simple ping prompt
            string result = await provider.GenerateAsync("Say 'Hello Swallows' in one word.");
            
            // Check if we got a response (not empty)
            Assert.False(string.IsNullOrWhiteSpace(result), "Ollama returned empty response.");
            Assert.Contains("Hello", result, StringComparison.OrdinalIgnoreCase); 
        }
        catch (HttpRequestException)
        {
             // If connection fails, ignore test or fail depending on strictness. 
             // For E2E local dev, failing is good to alert user.
             Assert.Fail("Ollama generation failed. Is it running?");
        }
        catch (Exception ex)
        {
             // If model missing, Ollama returns 404 or specific error.
             if (ex.Message.Contains("404"))
             {
                 // Model not found maybe?
                 Assert.Fail($"Ollama reachable but model '{settings.ModelName}' not found. Run 'ollama pull {settings.ModelName}'");
             }
             throw;
        }
    }
}
