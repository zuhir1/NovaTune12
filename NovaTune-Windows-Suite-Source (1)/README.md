# NovaTune

NovaTune is a safety-first Windows 10/11 diagnostics and optimization suite built with C#, .NET 9, WinUI 3, MVVM, and dependency injection.

## Implemented foundation

- Fluent dark/light WinUI shell with English, German, and Arabic-ready localization architecture.
- Live CPU, memory, disk, and network telemetry.
- Concurrent Smart Scan providers with progress, cancellation, severity, impact, and recommendations.
- Local, privacy-preserving diagnostic explanation engine (no computer data is uploaded).
- Preview-only cleanup discovery for allow-listed cache and temporary locations.
- Mandatory restore-point gate before cleanup.
- Quarantine-based cleanup with a transaction manifest and undo; no permanent deletion in the default workflow.
- Read-only startup inspection and disk health checks.
- Explicit repair plans for SFC, DISM, DNS, Winsock, and Windows Update operations.
- Core unit tests and architecture/safety documentation.

## Build on Windows

Requirements:

- Windows 10 1809+ or Windows 11 (a currently supported Windows release is recommended)
- Visual Studio 2022/2026 with **.NET Desktop Development** and **Windows App SDK C# Templates**
- .NET 9 SDK
- Windows 10/11 SDK 10.0.19041 or newer

```powershell
dotnet restore .\NovaTune.sln
dotnet build .\NovaTune.sln -c Release
dotnet test .\NovaTune.sln -c Release
dotnet run --project .\src\NovaTune.App\NovaTune.App.csproj
```

Run normally for diagnostics. NovaTune requests elevation only when a selected repair or protected cleanup actually needs it. Do not disable antivirus protection or System Restore to run the app.

## Important scope note

This repository is a production-oriented first release foundation, not a claim that every item in the long-term product brief is finished. Driver distribution, kernel-level sensors, online AI, malware classification, a residual uninstaller, full browser support, MSIX signing, and hardware-vendor APIs require separate signed components, licensed data, Windows hardware testing, and security review. See `docs/ROADMAP.md`.

## License

Proprietary-ready source scaffold. Add your chosen license before distribution.
