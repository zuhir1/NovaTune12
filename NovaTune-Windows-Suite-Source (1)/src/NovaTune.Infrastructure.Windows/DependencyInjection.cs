using Microsoft.Extensions.DependencyInjection;
using NovaTune.Core.Abstractions;
using NovaTune.Infrastructure.Windows.Cleaning;
using NovaTune.Infrastructure.Windows.Monitoring;
using NovaTune.Infrastructure.Windows.Repair;
using NovaTune.Infrastructure.Windows.Safety;
using NovaTune.Infrastructure.Windows.Scanning;

namespace NovaTune.Infrastructure.Windows;

public static class DependencyInjection
{
    public static IServiceCollection AddNovaTuneWindows(this IServiceCollection services) => services
        .AddSingleton<ISystemMonitor, WindowsSystemMonitor>()
        .AddSingleton<ICleanupPreviewService, CleanupPreviewService>()
        .AddSingleton<IRestorePointService, RestorePointService>()
        .AddSingleton<ICleanupService, SafeCleanupService>()
        .AddSingleton<IRepairService, WindowsRepairService>()
        .AddSingleton<IScanProvider, StorageScanProvider>()
        .AddSingleton<IScanProvider, StartupScanProvider>()
        .AddSingleton<IScanProvider, WindowsHealthScanProvider>();
}
