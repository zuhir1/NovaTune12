using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Core.Services;

public sealed class HealthScoreCalculator : IHealthScoreCalculator
{
    private static readonly IReadOnlyDictionary<IssueSeverity, int> Penalties =
        new Dictionary<IssueSeverity, int>
        {
            [IssueSeverity.Information] = 0,
            [IssueSeverity.Low] = 2,
            [IssueSeverity.Medium] = 7,
            [IssueSeverity.High] = 15,
            [IssueSeverity.Critical] = 28
        };

    public HealthScores Calculate(IEnumerable<DiagnosticIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        var list = issues.ToList();
        var performance = Score(list.Where(i => i.Category is ScanCategory.Performance or ScanCategory.Startup));
        var security = Score(list.Where(i => i.Category == ScanCategory.Security));
        var storage = Score(list.Where(i => i.Category == ScanCategory.Storage));
        var hardware = Score(list.Where(i => i.Category is ScanCategory.Hardware or ScanCategory.Drivers));
        var windows = Score(list.Where(i => i.Category is ScanCategory.Windows or ScanCategory.Network or ScanCategory.Applications));
        var overall = (int)Math.Round(performance * .25 + security * .25 + storage * .2 + hardware * .15 + windows * .15);
        return new HealthScores(overall, performance, security, storage, hardware);
    }

    private static int Score(IEnumerable<DiagnosticIssue> issues) =>
        Math.Clamp(100 - issues.Sum(i => Penalties[i.Severity]), 0, 100);
}
