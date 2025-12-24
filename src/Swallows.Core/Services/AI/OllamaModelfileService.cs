namespace Swallows.Core.Services.AI;

public class OllamaModelfileService
{
    public Task CreateModelAsync(string modelName, string baseModelPath, string systemPrompt)
    {
        return Task.CompletedTask;
    }

    public Task<string> CreateCustomModelAsync(string baseName, string newName)
    {
        return Task.FromResult("Success");
    }
}
