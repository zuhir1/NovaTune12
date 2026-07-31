using System.Collections.Concurrent;
using System.Diagnostics;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Models;

namespace NovaTune.Core.Services;

public sealed class SmartScanService(IEnumerable<IScanProvider> providers, IHealthScoreCalculator scoreCalculator)
    : ISmartScanService
{
    private readonly IReadOnlyList<IScanProvider> _providers = providers.ToList();

    public async Task<ScanReport> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var results = new ConcurrentBag<ScanProviderResult>();
        var completed = 0;

        await Parallel.ForEachAsync(_providers, cancellationToken, async (provider, token) =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                progress?.Report(new ScanProgress(provider.Name, Percentage(completed), $"Scanning {provider.Name}…"));
                var issues = await provider.ScanAsync(token).ConfigureAwait(false);
                results.Add(new ScanProviderResult(provider.Name, issues, stopwatch.Elapsed));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                results.Add(new ScanProviderResult(provider.Name, [], stopwatch.Elapsed, exception.Message));
            }
            finally
            {
                var count = Interlocked.Increment(ref completed);
                progress?.Report(new ScanProgress(provider.Name, Percentage(count), $"Completed {provider.Name}"));
            }
        }).ConfigureAwait(false);

        var ordered = results.OrderBy(r => r.Provider, StringComparer.OrdinalIgnoreCase).ToList();
        var issues = ordered.SelectMany(r => r.Issues)
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new ScanReport(started, DateTimeOffset.UtcNow, ordered, issues, scoreCalculator.Calculate(issues));
    }

    private double Percentage(int count) => _providers.Count == 0 ? 100 : Math.Clamp(count * 100d / _providers.Count, 0, 100);
}
