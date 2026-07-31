using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Core.Services;

public sealed class LocalDiagnosticEngine : IDiagnosticEngine
{
    public IReadOnlyList<DiagnosticInsight> Analyze(SystemSnapshot snapshot, ScanReport? report)
    {
        var insights = new List<DiagnosticInsight>();
        if (snapshot.CpuPercent >= 85)
            insights.Add(new("CPU load is very high", "Sustained CPU pressure delays foreground work and can cause game stutter.", ["Open Task Manager and sort by CPU.", "Check cooling if clock speed falls while temperature rises."], IssueSeverity.High));
        if (snapshot.MemoryPercent >= 85)
            insights.Add(new("Memory pressure detected", "Windows may compress memory or page to disk when available RAM is low.", ["Close the largest unused apps.", "Review startup apps before considering more RAM."], IssueSeverity.Medium));
        if (snapshot.SystemDiskPercent >= 90)
            insights.Add(new("System drive needs free space", "Low free space can slow updates, paging, browser caches, and application installs.", ["Preview temporary files.", "Move large personal files only after reviewing them."], IssueSeverity.High));
        if (report?.Issues.Any(i => i.Category == ScanCategory.Startup && i.Severity >= IssueSeverity.Medium) == true)
            insights.Add(new("Startup load can be reduced", "Several auto-start entries may compete for CPU and disk during sign-in.", ["Disable only apps you recognize and do not need immediately."], IssueSeverity.Medium));
        if (insights.Count == 0)
            insights.Add(new("No immediate bottleneck detected", "Current CPU, memory, and storage pressure are within normal limits.", ["Run Smart Scan after reproducing a slowdown for a more useful report."], IssueSeverity.Information));
        return insights;
    }
}
