using System;
using System.Net.Http;
using System.Threading.Tasks;
using Swallows.Core.Services.AI;
using Xunit;
using Xunit.Abstractions;

namespace Swallows.Tests.Integration;

/// <summary>
/// End-to-End test for Ollama integration
/// Tests the complete flow: install → start → download model → run inference
/// </summary>
public class OllamaE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _httpClient;
    private readonly OllamaInstallerService _installerService;
    private readonly OllamaProcessService _processService;
    private bool _serviceStarted = false;

    public OllamaE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient();
        _installerService = new OllamaInstallerService(_httpClient);
        _processService = new OllamaProcessService(_httpClient);
    }

    [Fact]
    public async Task FullOllamaWorkflow_ShouldSucceed()
    {
        // STEP 1: Ensure Ollama is installed
        _output.WriteLine("Step 1: Checking Ollama installation...");
        
        if (!_installerService.IsOllamaInstalled())
        {
            _output.WriteLine("Ollama not found, installing...");
            
            var installProgress = new Progress<string>(message => _output.WriteLine($"  {message}"));
            var installSuccess = await _installerService.DownloadAndInstallAsync(installProgress);
            
            Assert.True(installSuccess, "Ollama installation should succeed");
            _output.WriteLine("✓ Ollama installed successfully");
        }
        else
        {
            _output.WriteLine("✓ Ollama already installed");
        }

        var ollamaPath = _installerService.GetLocalOllamaPath();
        _output.WriteLine($"  Ollama path: {ollamaPath}");

        // STEP 2: Start Ollama service
        _output.WriteLine("\nStep 2: Starting Ollama service...");
        
        var isRunning = await _processService.IsRunningAsync();
        if (!isRunning)
        {
            var startSuccess = await _processService.StartServiceAsync();
            Assert.True(startSuccess, "Ollama service should start successfully");
            _serviceStarted = true;
            _output.WriteLine("✓ Ollama service started");
        }
        else
        {
            _output.WriteLine("✓ Ollama service already running");
        }

        // Verify service is responding
        _output.WriteLine("Waiting for service to be fully ready...");
        await Task.Delay(5000); // Give it more time to fully start
        
        // Retry connection check
        for (int i = 0; i < 10; i++)
        {
            isRunning = await _processService.IsRunningAsync();
            if (isRunning)
            {
                _output.WriteLine($"✓ Service responding after {(i + 1) * 2 + 5} seconds");
                break;
            }
            _output.WriteLine($"  Retry {i + 1}/10...");
            await Task.Delay(2000);
        }
        
        Assert.True(isRunning, "Ollama service should be running and responding");

        // STEP 3: Download a small model (tinyllama ~637MB Q2_K)
        _output.WriteLine("\nStep 3: Downloading small model (tinyllama:latest)...");
        
        var downloadProgress = new Progress<string>(message => 
        {
            if (!message.Contains("pulling") || message.Contains("100%"))
            {
                _output.WriteLine($"  {message}");
            }
        });

        var downloadResult = await _processService.PullModelAsync("tinyllama:latest", downloadProgress);
        
        Assert.Contains("Success", downloadResult, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine("✓ Model downloaded successfully");

        // STEP 4: Run inference with a simple prompt
        _output.WriteLine("\nStep 4: Running inference test...");
        
        var testPrompt = "What is 2+2? Answer in one word.";
        _output.WriteLine($"  Prompt: '{testPrompt}'");

        var response = await RunInferenceAsync("tinyllama:latest", testPrompt);
        
        Assert.NotNull(response);
        Assert.NotEmpty(response);
        _output.WriteLine($"  Response: '{response}'");
        _output.WriteLine("✓ Inference completed successfully");

        // STEP 5: Verify response quality
        _output.WriteLine("\nStep 5: Validating response...");
        
        Assert.True(response.Length > 0, "Response should not be empty");
        Assert.True(response.Length < 1000, "Response should be reasonably sized for this simple prompt");
        
        _output.WriteLine("✓ Response validation passed");
        _output.WriteLine("\n✅ Full E2E workflow completed successfully!");
    }

    private async Task<string> RunInferenceAsync(string modelName, string prompt)
    {
        try
        {
            // Use Ollama API to generate response
            var requestBody = new
            {
                model = modelName,
                prompt = prompt,
                stream = false
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = System.Text.Json.JsonDocument.Parse(responseJson);

            if (responseObj.RootElement.TryGetProperty("response", out var responseText))
            {
                return responseText.GetString() ?? "";
            }

            return "";
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Inference error: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        
        // Note: We don't stop the service here as it might be used by other tests
        // User can manually stop via the UI or Activity Monitor
    }
}
