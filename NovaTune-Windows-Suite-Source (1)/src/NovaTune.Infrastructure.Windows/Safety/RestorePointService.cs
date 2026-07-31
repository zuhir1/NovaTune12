using System.Diagnostics;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Infrastructure.Windows.Safety;

public sealed class RestorePointService : IRestorePointService
{
    public async Task<OperationResult> CreateAsync(string description, CancellationToken cancellationToken)
    {
        var safeDescription = description.Replace("'", "''", StringComparison.Ordinal);
        var script = $"Checkpoint-Computer -Description '{safeDescription}' -RestorePointType MODIFY_SETTINGS -ErrorAction Stop";
        var start = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-NonInteractive");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(script);
        try
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException("PowerShell could not be started.");
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0
                ? new OperationResult(true, "Restore point created.")
                : new OperationResult(false, $"Restore point creation failed with exit code {process.ExitCode}. No changes were made.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new OperationResult(false, $"Restore point creation failed. No changes were made. {ex.Message}");
        }
    }
}
