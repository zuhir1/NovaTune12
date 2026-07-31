using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using NovaTune.Core.Abstractions;
using NovaTune.Core.Services;
using NovaTune.Infrastructure.Windows;

namespace NovaTune.App;

public partial class App : Application
{
    private readonly IHost _host;
    private Window? _window;

    public App()
    {
        InitializeComponent();
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => builder.AddDebug());
                services.AddSingleton<IHealthScoreCalculator, HealthScoreCalculator>();
                services.AddSingleton<ISmartScanService, SmartScanService>();
                services.AddSingleton<IDiagnosticEngine, LocalDiagnosticEngine>();
                services.AddNovaTuneWindows();
                services.AddSingleton<ViewModels.DashboardViewModel>();
                services.AddSingleton<MainWindow>();
            }).Build();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await _host.StartAsync();
        _window = _host.Services.GetRequiredService<MainWindow>();
        _window.Closed += async (_, _) =>
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await _host.StopAsync(timeout.Token);
        };
        _window.Activate();
    }
}
