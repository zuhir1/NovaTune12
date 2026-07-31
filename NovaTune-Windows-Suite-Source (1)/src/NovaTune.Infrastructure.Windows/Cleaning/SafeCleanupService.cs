using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Infrastructure.Windows.Cleaning;

public sealed class SafeCleanupService(IRestorePointService restorePoints) : ICleanupService
{
    private readonly string _quarantineRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaTune", "Quarantine");

    public async Task<OperationResult> ExecuteAsync(CleanupPreview approvedPreview, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approvedPreview);
        if (DateTimeOffset.UtcNow - approvedPreview.CreatedAt > TimeSpan.FromMinutes(30))
            return new OperationResult(false, "The preview expired. Scan again before cleaning.");
        var selected = approvedPreview.Candidates.Where(x => x.Selected).ToList();
        if (selected.Count == 0) return new OperationResult(false, "No cleanup items are selected.");

        var restore = await restorePoints.CreateAsync("NovaTune before cleanup", cancellationToken).ConfigureAwait(false);
        if (!restore.Succeeded) return restore;

        var transactionId = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var transactionRoot = Path.Combine(_quarantineRoot, transactionId);
        Directory.CreateDirectory(transactionRoot);
        var entries = new List<QuarantineEntry>();
        var warnings = new List<string>();

        foreach (var candidate in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!IsAllowed(candidate.Path) || !File.Exists(candidate.Path)) continue;
                var info = new FileInfo(candidate.Path);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                var destination = Path.Combine(transactionRoot, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(info.FullName))) + info.Extension);
                File.Move(info.FullName, destination, overwrite: false);
                entries.Add(new QuarantineEntry(info.FullName, destination, info.Length));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                warnings.Add($"Skipped {candidate.Path}: {ex.Message}");
            }
        }

        var manifest = new QuarantineManifest(transactionId, DateTimeOffset.UtcNow, entries);
        await WriteManifestAsync(transactionRoot, manifest, cancellationToken).ConfigureAwait(false);
        return new OperationResult(true, $"Moved {entries.Count} files to quarantine. You can undo this operation.", transactionId, warnings);
    }

    public async Task<OperationResult> UndoAsync(string transactionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId) || transactionId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return new OperationResult(false, "Invalid transaction identifier.");
        var root = Path.Combine(_quarantineRoot, transactionId);
        var manifestPath = Path.Combine(root, "manifest.json");
        if (!File.Exists(manifestPath)) return new OperationResult(false, "The undo manifest was not found.");
        var manifest = JsonSerializer.Deserialize<QuarantineManifest>(await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false));
        if (manifest is null || !string.Equals(manifest.TransactionId, transactionId, StringComparison.Ordinal))
            return new OperationResult(false, "The undo manifest is invalid.");
        var warnings = new List<string>();
        var restored = 0;
        foreach (var entry in manifest.Entries.Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!File.Exists(entry.QuarantinedPath)) continue;
                if (File.Exists(entry.OriginalPath)) { warnings.Add($"Not restored because a file already exists: {entry.OriginalPath}"); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(entry.OriginalPath)!);
                File.Move(entry.QuarantinedPath, entry.OriginalPath);
                restored++;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { warnings.Add(ex.Message); }
        }
        return new OperationResult(true, $"Restored {restored} files.", transactionId, warnings);
    }

    private static bool IsAllowed(string path)
    {
        var full = Path.GetFullPath(path);
        foreach (var root in CleanupPreviewService.GetRoots())
        {
            if (!IsChildOf(full, root.Path)) continue;
            var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root.Path));
            var current = Path.GetDirectoryName(full);
            while (current is not null)
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                if (string.Equals(Path.TrimEndingDirectorySeparator(current), normalizedRoot, StringComparison.OrdinalIgnoreCase)) return true;
                current = Path.GetDirectoryName(current);
            }
        }
        return false;
    }

    private static bool IsChildOf(string path, string root)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static Task WriteManifestAsync(string transactionRoot, QuarantineManifest manifest, CancellationToken token) =>
        File.WriteAllTextAsync(Path.Combine(transactionRoot, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), token);
}
