using NovaTune.Core.Models;
using NovaTune.Core.Services;

namespace NovaTune.Core.Tests;

public sealed class HealthScoreCalculatorTests
{
    [Fact]
    public void NoIssues_ReturnsPerfectScores()
    {
        var scores = new HealthScoreCalculator().Calculate([]);
        Assert.Equal(100, scores.Overall);
        Assert.Equal(100, scores.Performance);
        Assert.Equal(100, scores.Security);
        Assert.Equal(100, scores.Storage);
        Assert.Equal(100, scores.Hardware);
    }

    [Fact]
    public void CriticalSecurityIssue_ReducesSecurityAndOverall()
    {
        DiagnosticIssue[] issues = [new("test", ScanCategory.Security, IssueSeverity.Critical, "Title", "Explanation", "Impact", "Fix")];
        var scores = new HealthScoreCalculator().Calculate(issues);
        Assert.Equal(72, scores.Security);
        Assert.InRange(scores.Overall, 90, 95);
    }

    [Fact]
    public void Score_IsClampedAtZero()
    {
        var issues = Enumerable.Range(0, 10).Select(i => new DiagnosticIssue(i.ToString(), ScanCategory.Storage, IssueSeverity.Critical, "Title", "Explanation", "Impact", "Fix"));
        Assert.Equal(0, new HealthScoreCalculator().Calculate(issues).Storage);
    }
}
