using NovaTune.Core.Models;

namespace NovaTune.Core.Abstractions;

public interface ISystemMonitor
{
    Task<SystemSnapshot> CaptureAsync(CancellationToken cancellationToken);
}

public interface IScanProvider
{
    string Name { get; }
    Task<IReadOnlyList<DiagnosticIssue>> ScanAsync(CancellationToken cancellationToken);
}

public interface IHealthScoreCalculator
{
    HealthScores Calculate(IEnumerable<DiagnosticIssue> issues);
}

public interface ISmartScanService
{
    Task<ScanReport> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}

public interface IDiagnosticEngine
{
    IReadOnlyList<DiagnosticInsight> Analyze(SystemSnapshot snapshot, ScanReport? report);
}

public interface ICleanupPreviewService
{
    Task<CleanupPreview> PreviewAsync(CancellationToken cancellationToken);
}

public interface IRestorePointService
{
    Task<OperationResult> CreateAsync(string description, CancellationToken cancellationToken);
}

public interface ICleanupService
{
    Task<OperationResult> ExecuteAsync(CleanupPreview approvedPreview, CancellationToken cancellationToken);
    Task<OperationResult> UndoAsync(string transactionId, CancellationToken cancellationToken);
}

public interface IRepairService
{
    IReadOnlyList<OperationPlan> GetPlans();
    Task<OperationResult> ExecuteAsync(string planId, CancellationToken cancellationToken);
}
