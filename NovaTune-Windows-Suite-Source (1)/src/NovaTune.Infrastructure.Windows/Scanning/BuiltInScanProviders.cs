using Microsoft.Win32;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Infrastructure.Windows.Scanning;

public sealed class StorageScanProvider : IScanProvider
{
    public string Name => "Storage";

    public Task<IReadOnlyList<DiagnosticIssue>> ScanAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var issues = new List<DiagnosticIssue>();
        var root = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        var drive = new DriveInfo(root);
        var freePercent = drive.TotalSize == 0 ? 100 : drive.AvailableFreeSpace * 100d / drive.TotalSize;
        if (freePercent < 10)
            issues.Add(new("storage.system-critical", ScanCategory.Storage, IssueSeverity.High, "System drive is almost full", $"Only {freePercent:F1}% is free on {root}.", "Windows Update, paging, and applications may slow down or fail.", "Review the cleanup preview and large personal files.", true, "cleanup.preview"));
        else if (freePercent < 20)
            issues.Add(new("storage.system-low", ScanCategory.Storage, IssueSeverity.Medium, "System drive free space is low", $"{freePercent:F1}% remains free on {root}.", "Updates and caches have limited working space.", "Keep at least 15–20% free where practical.", true, "cleanup.preview"));
        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(issues);
    }
}

public sealed class StartupScanProvider : IScanProvider
{
    private static readonly (RegistryHive Hive, string Path)[] Locations =
    [
        (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
        (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run")
    ];
    public string Name => "Startup";

    public Task<IReadOnlyList<DiagnosticIssue>> ScanAsync(CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var location in Locations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var hive = RegistryKey.OpenBaseKey(location.Hive, RegistryView.Registry64);
            using var key = hive.OpenSubKey(location.Path, writable: false);
            count += key?.GetValueNames().Length ?? 0;
        }
        IReadOnlyList<DiagnosticIssue> issues = count >= 15
            ? [new("startup.crowded", ScanCategory.Startup, IssueSeverity.Medium, "Many startup applications", $"{count} Run-key entries start with Windows.", "Sign-in may take longer and background memory use may rise.", "Review startup entries individually; do not disable security or driver components automatically.")]
            : [];
        return Task.FromResult(issues);
    }
}

public sealed class WindowsHealthScanProvider : IScanProvider
{
    public string Name => "Windows health";
    public Task<IReadOnlyList<DiagnosticIssue>> ScanAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var issues = new List<DiagnosticIssue>();
        if (Environment.OSVersion.Version.Build < 17763)
            issues.Add(new("windows.unsupported-build", ScanCategory.Windows, IssueSeverity.Critical, "Unsupported Windows build", "NovaTune requires Windows 10 build 17763 or later.", "Platform APIs may be unavailable.", "Upgrade Windows before using optimization actions."));
        if (!Environment.Is64BitOperatingSystem)
            issues.Add(new("windows.32bit", ScanCategory.Windows, IssueSeverity.High, "32-bit Windows is not supported", "This release targets 64-bit Windows.", "Some diagnostics and native APIs will not function.", "Use a supported 64-bit Windows installation."));
        return Task.FromResult<IReadOnlyList<DiagnosticIssue>>(issues);
    }
}
