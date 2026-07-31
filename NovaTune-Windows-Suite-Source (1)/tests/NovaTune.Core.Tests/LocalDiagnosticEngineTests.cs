using NovaTune.Core.Models;
using NovaTune.Core.Services;

namespace NovaTune.Core.Tests;

public sealed class LocalDiagnosticEngineTests
{
    [Fact]
    public void HighCpuAndMemory_ProduceExplainableInsights()
    {
        var snapshot = new SystemSnapshot(DateTimeOffset.UtcNow, 92, 90, 1, 1, 50, 1, 2, 0, 0, TimeSpan.FromHours(1));
        var insights = new LocalDiagnosticEngine().Analyze(snapshot, null);
        Assert.Contains(insights, x => x.Headline.Contains("CPU", StringComparison.Ordinal));
        Assert.Contains(insights, x => x.Headline.Contains("Memory", StringComparison.Ordinal));
    }
}
