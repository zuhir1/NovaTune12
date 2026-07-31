namespace NovaTune.Core.Models;

public sealed record CleanupCandidate(
    string Path,
    string Category,
    long SizeBytes,
    DateTimeOffset LastModified,
    bool Selected = true);

public sealed record CleanupPreview(
    string Id,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CleanupCandidate> Candidates)
{
    public long TotalBytes => Candidates.Where(x => x.Selected).Sum(x => x.SizeBytes);
}

public sealed record OperationStep(string Description, string? Command = null, bool RequiresRestart = false);

public sealed record OperationPlan(
    string Id,
    string Title,
    string Explanation,
    bool RequiresAdministrator,
    bool RequiresRestorePoint,
    IReadOnlyList<OperationStep> Steps);

public sealed record OperationResult(
    bool Succeeded,
    string Message,
    string? TransactionId = null,
    IReadOnlyList<string>? Warnings = null);

public sealed record QuarantineEntry(string OriginalPath, string QuarantinedPath, long SizeBytes);

public sealed record QuarantineManifest(
    string TransactionId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<QuarantineEntry> Entries);
