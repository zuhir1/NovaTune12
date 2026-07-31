using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Infrastructure.Windows.Cleaning;

public sealed class CleanupPreviewService : ICleanupPreviewService
{
    private static readonly TimeSpan MinimumAge = TimeSpan.FromHours(24);

    public async Task<CleanupPreview> PreviewAsync(CancellationToken cancellationToken)
    {
        var roots = GetRoots().DistinctBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();
        var candidates = await Task.Run(() =>
        {
            var result = new List<CleanupCandidate>();
            foreach (var root in roots) EnumerateRoot(root.Path, root.Category, result, cancellationToken);
            return result.OrderByDescending(x => x.SizeBytes).ToList();
        }, cancellationToken).ConfigureAwait(false);
        return new CleanupPreview(Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow, candidates);
    }

    internal static IReadOnlyList<(string Path, string Category)> GetRoots()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roots = new List<(string, string)>
        {
            (Path.GetTempPath(), "Temporary files"),
            (Path.Combine(local, "D3DSCache"), "DirectX shader cache"),
            (Path.Combine(local, "CrashDumps"), "Crash dumps"),
            (Path.Combine(local, "Microsoft", "Windows", "INetCache"), "Internet cache")
        };
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windows)) roots.Add((Path.Combine(windows, "Temp"), "Windows temporary files"));
        return roots.Where(x => Directory.Exists(x.Item1)).Select(x => (Path.GetFullPath(x.Item1), x.Item2)).ToList();
    }

    private static void EnumerateRoot(string root, string category, ICollection<CleanupCandidate> result, CancellationToken token)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            try
            {
                var directoryInfo = new DirectoryInfo(directory);
                if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                foreach (var file in directoryInfo.EnumerateFiles())
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        if ((file.Attributes & FileAttributes.ReparsePoint) != 0 || DateTime.UtcNow - file.LastWriteTimeUtc < MinimumAge) continue;
                        result.Add(new CleanupCandidate(file.FullName, category, file.Length, file.LastWriteTimeUtc));
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or FileNotFoundException) { }
                }
                foreach (var child in directoryInfo.EnumerateDirectories())
                    if ((child.Attributes & FileAttributes.ReparsePoint) == 0) pending.Push(child.FullName);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException) { }
        }
    }
}
