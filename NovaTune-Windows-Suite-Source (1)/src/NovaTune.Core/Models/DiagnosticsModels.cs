namespace NovaTune.Core.Models;

public enum IssueSeverity { Information, Low, Medium, High, Critical }

public enum ScanCategory
{
    Performance, Storage, Startup, Hardware, Windows, Network, Security, Drivers, Applications
}

public sealed record DiagnosticIssue(
    string Id,
    ScanCategory Category,
    IssueSeverity Severity,
    string Title,
    string Explanation,
    string EstimatedImpact,
    string Recommendation,
    bool CanAutoFix = false,
    string? FixActionId = null);

public sealed record ScanProgress(string Provider, double Percentage, string Message);

public sealed record ScanProviderResult(
    string Provider,
    IReadOnlyList<DiagnosticIssue> Issues,
    TimeSpan Duration,
    string? Error = null);

public sealed record HealthScores(
    int Overall,
    int Performance,
    int Security,
    int Storage,
    int Hardware);

public sealed record ScanReport(
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyList<ScanProviderResult> Providers,
    IReadOnlyList<DiagnosticIssue> Issues,
    HealthScores Scores);

public sealed record SystemSnapshot(
    DateTimeOffset CapturedAt,
    double CpuPercent,
    double MemoryPercent,
    ulong MemoryUsedBytes,
    ulong MemoryTotalBytes,
    double SystemDiskPercent,
    long SystemDiskFreeBytes,
    long SystemDiskTotalBytes,
    long NetworkReceiveBytesPerSecond,
    long NetworkSendBytesPerSecond,
    TimeSpan Uptime);

public sealed record DiagnosticInsight(
    string Headline,
    string Explanation,
    IReadOnlyList<string> Recommendations,
    IssueSeverity Severity);
