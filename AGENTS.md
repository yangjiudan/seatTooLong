# SeatTooLong Agent Guide

Use this guide to work productively in this repository. For product context, link to [README.md](README.md) and [doc/PRD.md](doc/PRD.md) instead of duplicating them.

## Project Shape

- `SeatTooLong.Core/` contains business logic with no WPF dependency: state machine, settings, statistics, localization, notification message building, and shared vision rules.
- `SeatTooLong.App/` is the Windows WPF host: camera capture, Haar detector, tray, toast notifications, overlay, settings, reports, and app composition in `App.xaml.cs`.
- `SeatTooLong.Tests/` contains xUnit/Moq tests and should be updated alongside behavior changes. This project has been developed in a TDD style.
- `SeatTooLong.IntegrationTest/` is a manual live-camera console harness; do not rely on it for normal automated verification.

## Build And Test

- Build the solution: `dotnet build SeatTooLong.sln`
- Run tests: `dotnet test SeatTooLong.Tests --verbosity minimal`
- Run the app: `dotnet run --project SeatTooLong.App`
- If `dotnet build` fails with MSB3021/MSB3027 because `SeatTooLong.App` is locking files, either ask before stopping the user’s running tray app or build into an isolated output path: `dotnet build SeatTooLong.App\SeatTooLong.App.csproj -p:BaseOutputPath=obj\verify-output\`
- Existing NU1701 warnings from WPF/chart/OpenTK-related packages are known; do not treat them as regressions unless package versions change.

## TDD Requirement

- Use TDD for behavior changes: add or update a focused failing test first, implement the smallest change to pass, then refactor if needed.
- Do not skip tests for core behavior, settings, statistics, detection heuristics, localization, or UI-facing state transitions.
- If a behavior is hard to test directly in WPF, test the underlying Core/App service state or mapping logic and mention the residual UI verification needed.
- Keep tests close to the affected area and prefer existing test classes over creating broad new test fixtures.

## Core Behavior Conventions

- `SittingMonitor` owns the state machine: `Idle`, `Sitting`, `Alerting`, `Resting`.
- `Idle` and `Resting` overlay timers use current state duration. `Sitting` and `Alerting` use current sitting duration. The temporary away display uses absence duration.
- Absence grace is configurable via `AppSettings.AbsenceGracePeriodSeconds` and maps to `SittingMonitorOptions.AbsenceGracePeriod`.
- Runtime person detection and sample-image tests should share `SeatTooLong.Core/Vision/SeatedFaceRule.cs`; avoid diverging detector thresholds between app and tests.
- Statistics should persist active sitting/rest sessions periodically through `StatisticsService.FlushActiveSessions()` and update existing rows rather than creating duplicates.

## Persistence And Local State

- Runtime settings are stored at `%LOCALAPPDATA%\SeatTooLong\settings.json`.
- SQLite statistics are stored at `%LOCALAPPDATA%\SeatTooLong\stats.db`.
- `App.xaml.cs` must ensure the app data directory exists before opening settings or SQLite files.
- Camera frames are processed in memory only; do not add image/video persistence unless the user explicitly asks and privacy implications are addressed.

## UI And Localization

- Keep WPF UI strings in `SeatTooLong.Core/Localization/LocalizationService.cs` and update both Chinese and English entries together.
- Settings changes should flow through `AppSettings`, `SettingsWindow`, and `App.OnSettingsSaved` so runtime behavior updates without restart when possible.
- Keep the settings window compact and row-aligned; avoid putting a control row in a `*` height grid row unless the row is intended as spacer content.

## Testing Expectations

- For state-machine changes, update `SittingMonitorTests` before implementation.
- For settings changes, update `JsonSettingsServiceTests` first and verify mapping in app composition when relevant.
- For SQLite/statistics changes, update `SqliteStatisticsRepositoryTests` and `StatisticsServiceTests` first.
- For detection heuristics, update `SeatedFaceRuleTests` and `HaarPersonDetectorTests` first; ensure sample image expectations stay aligned with runtime code.

## Editing Notes

- Keep changes focused and avoid unrelated cleanup.
- Prefer preserving existing simple patterns over adding abstractions.
- Do not commit, branch, or stop a running user app unless explicitly requested.
