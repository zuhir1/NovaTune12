using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ISystemMonitor _monitor;
    private readonly ISmartScanService _scanner;
    private readonly IDiagnosticEngine _diagnostics;
    private readonly ICleanupPreviewService _cleanupPreview;
    private readonly ICleanupService _cleanup;
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly CancellationTokenSource _lifetime = new();
    private ScanReport? _lastReport;
    private string? _lastTransactionId;

    [ObservableProperty] private double _cpuPercent;
    [ObservableProperty] private double _memoryPercent;
    [ObservableProperty] private double _diskPercent;
    [ObservableProperty] private string _networkText = "0 KB/s";
    [ObservableProperty] private int _healthScore = 100;
    [ObservableProperty] private int _performanceScore = 100;
    [ObservableProperty] private int _securityScore = 100;
    [ObservableProperty] private int _storageScore = 100;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private double _scanProgress;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private long _cleanupBytes;
    [ObservableProperty] private bool _canUndo;

    public ObservableCollection<DiagnosticIssue> Issues { get; } = [];
    public ObservableCollection<DiagnosticInsight> Insights { get; } = [];
    public ObservableCollection<CleanupCandidate> CleanupCandidates { get; } = [];

    public DashboardViewModel(ISystemMonitor monitor, ISmartScanService scanner, IDiagnosticEngine diagnostics,
        ICleanupPreviewService cleanupPreview, ICleanupService cleanup, ILogger<DashboardViewModel> logger)
    {
        _monitor = monitor;
        _scanner = scanner;
        _diagnostics = diagnostics;
        _cleanupPreview = cleanupPreview;
        _cleanup = cleanup;
        _logger = logger;
        _ = MonitorLoopAsync(_lifetime.Token);
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task SmartScanAsync()
    {
        IsBusy = true;
        StatusText = "Smart Scan is running…";
        ScanProgress = 0;
        SmartScanCommand.NotifyCanExecuteChanged();
        PreviewCleanupCommand.NotifyCanExecuteChanged();
        try
        {
            var progress = new Progress<ScanProgress>(p => { ScanProgress = p.Percentage; StatusText = p.Message; });
            _lastReport = await _scanner.ScanAsync(progress, _lifetime.Token);
            Issues.Clear();
            foreach (var issue in _lastReport.Issues) Issues.Add(issue);
            HealthScore = _lastReport.Scores.Overall;
            PerformanceScore = _lastReport.Scores.Performance;
            SecurityScore = _lastReport.Scores.Security;
            StorageScore = _lastReport.Scores.Storage;
            StatusText = $"Scan complete — {Issues.Count} issue(s) found";
        }
        catch (OperationCanceledException) { StatusText = "Scan cancelled"; }
        catch (Exception ex) { _logger.LogError(ex, "Smart Scan failed"); StatusText = "Scan failed safely — no changes were made"; }
        finally
        {
            IsBusy = false;
            SmartScanCommand.NotifyCanExecuteChanged();
            PreviewCleanupCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartOperation))]
    private async Task PreviewCleanupAsync()
    {
        IsBusy = true;
        StatusText = "Building a safe cleanup preview…";
        try
        {
            var preview = await _cleanupPreview.PreviewAsync(_lifetime.Token);
            CleanupCandidates.Clear();
            foreach (var candidate in preview.Candidates.Take(500)) CleanupCandidates.Add(candidate);
            CleanupBytes = preview.TotalBytes;
            StatusText = $"Preview ready — {preview.Candidates.Count} files, {FormatBytes(preview.TotalBytes)}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogError(ex, "Cleanup preview failed"); StatusText = "Preview failed — no changes were made"; }
        finally { IsBusy = false; SmartScanCommand.NotifyCanExecuteChanged(); PreviewCleanupCommand.NotifyCanExecuteChanged(); }
    }

    public async Task<OperationResult> ExecuteCleanupAsync(CleanupPreview approvedPreview)
    {
        IsBusy = true;
        try
        {
            var result = await _cleanup.ExecuteAsync(approvedPreview, _lifetime.Token);
            StatusText = result.Message;
            _lastTransactionId = result.TransactionId;
            CanUndo = result.Succeeded && result.TransactionId is not null;
            return result;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanUndoOperation))]
    private async Task UndoAsync()
    {
        if (_lastTransactionId is null) return;
        var result = await _cleanup.UndoAsync(_lastTransactionId, _lifetime.Token);
        StatusText = result.Message;
        CanUndo = !result.Succeeded;
        UndoCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanUndoChanged(bool value) => UndoCommand.NotifyCanExecuteChanged();
    private bool CanStartOperation() => !IsBusy;
    private bool CanUndoOperation() => CanUndo && !IsBusy;

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    var snapshot = await _monitor.CaptureAsync(token);
                    CpuPercent = snapshot.CpuPercent;
                    MemoryPercent = snapshot.MemoryPercent;
                    DiskPercent = snapshot.SystemDiskPercent;
                    NetworkText = $"↓ {FormatBytes(snapshot.NetworkReceiveBytesPerSecond)}/s  ↑ {FormatBytes(snapshot.NetworkSendBytesPerSecond)}/s";
                    Insights.Clear();
                    foreach (var insight in _diagnostics.Analyze(snapshot, _lastReport)) Insights.Add(insight);
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogDebug(ex, "Telemetry sample failed"); }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public CleanupPreview CreateApprovedPreview() => new(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, CleanupCandidates.ToList());

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.##} {units[unit]}";
    }

    public void Dispose() { _lifetime.Cancel(); _lifetime.Dispose(); }
}
