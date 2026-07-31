using System.Diagnostics;
using System.Security.Principal;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Infrastructure.Windows.Repair;

public sealed class WindowsRepairService(IRestorePointService restorePoints) : IRepairService
{
    private static readonly IReadOnlyList<OperationPlan> Plans =
    [
        new("sfc.verify", "Verify Windows system files", "SFC verifies protected system files and repairs incorrect versions.", true, true, [new("Run System File Checker", "sfc.exe /scannow")]),
        new("dism.health", "Repair Windows component store", "DISM checks and repairs the Windows image used by SFC and Windows Update.", true, true, [new("Restore component health", "dism.exe /Online /Cleanup-Image /RestoreHealth")]),
        new("network.dns", "Flush DNS cache", "Clears cached DNS answers. It does not change your DNS provider.", false, false, [new("Flush DNS resolver cache", "ipconfig.exe /flushdns")]),
        new("network.winsock", "Reset Winsock", "Resets the Windows socket catalog. VPN or security software may require repair afterward.", true, true, [new("Reset Winsock", "netsh.exe winsock reset", true)])
    ];

    public IReadOnlyList<OperationPlan> GetPlans() => Plans;

    public async Task<OperationResult> ExecuteAsync(string planId, CancellationToken cancellationToken)
    {
        var plan = Plans.SingleOrDefault(p => string.Equals(p.Id, planId, StringComparison.Ordinal));
        if (plan is null) return new OperationResult(false, "Unknown repair plan.");
        if (plan.RequiresRestorePoint)
        {
            var restore = await restorePoints.CreateAsync($"NovaTune before {plan.Title}", cancellationToken).ConfigureAwait(false);
            if (!restore.Succeeded) return restore;
        }
        var warnings = new List<string>();
        foreach (var step in plan.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Command)) continue;
            var split = step.Command.Split(' ', 2);
            var elevate = plan.RequiresAdministrator && !IsAdministrator();
            var start = new ProcessStartInfo(split[0])
            {
                UseShellExecute = elevate,
                Verb = elevate ? "runas" : string.Empty,
                RedirectStandardError = !elevate,
                RedirectStandardOutput = !elevate,
                CreateNoWindow = !elevate,
                WindowStyle = elevate ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
            };
            if (split.Length == 2) start.Arguments = split[1];
            try
            {
                using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {split[0]}.");
                var errorTask = elevate ? Task.FromResult(string.Empty) : process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                var error = await errorTask.ConfigureAwait(false);
                if (process.ExitCode != 0) return new OperationResult(false, $"{step.Description} failed with exit code {process.ExitCode}. {error.Trim()}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                return new OperationResult(false, $"{step.Description} was not started. No further steps were run. {ex.Message}");
            }
            if (step.RequiresRestart) warnings.Add("Restart Windows to complete this repair.");
        }
        return new OperationResult(true, $"{plan.Title} completed.", Warnings: warnings);
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
