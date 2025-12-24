using Swallows.Core.Models;

namespace Swallows.Core.Services;

public class SystemResourceService
{
    public RamStatus GetRamStatus(long modelSizeBytes)
    {
        // Simple mock logic
        if (modelSizeBytes > 16_000_000_000L) return RamStatus.Unsupported;
        if (modelSizeBytes > 8_000_000_000L) return RamStatus.Risky;
        return RamStatus.Safe;
    }
}
