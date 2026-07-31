# Architecture

NovaTune uses clean boundaries so Windows-specific code never leaks into scoring, policy, or presentation logic.

```text
NovaTune.App (WinUI 3, MVVM)
    -> NovaTune.Core (models, policies, orchestration)
    -> NovaTune.Infrastructure.Windows (Windows APIs and guarded operations)
```

## Main flows

1. `DashboardViewModel` periodically requests an immutable `SystemSnapshot`.
2. `SmartScanOrchestrator` runs independent providers concurrently and isolates provider failures.
3. `HealthScoreCalculator` converts issues into explainable component scores.
4. Cleanup is two-phase: `PreviewAsync` produces immutable candidates, then a fresh validated plan is approved by the user.
5. `SafeCleanupService` requires a restore point and moves candidates to quarantine. The manifest supports undo.

## Extension points

- Add scan categories through `IScanProvider`.
- Add telemetry implementations through `ISystemMonitor`.
- Add operations through `IRepairService` only when they have preview, cancellation, logs, and explicit confirmation.
- Replace the local diagnostic engine with an opt-in AI provider behind `IDiagnosticEngine`; redact data before any network call.

## Security boundaries

- The UI process runs as the interactive user.
- Protected actions are explicit and separately elevated.
- Scan providers are read-only.
- Paths are canonicalized and checked against allow-listed roots immediately before execution.
- Reparse points are skipped to prevent traversal outside approved roots.
- Cleanup defaults to quarantine rather than deletion.
