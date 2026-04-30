---
name: state-machine-change
description: "Use when changing SeatTooLong sitting monitor state transitions, overlay timer semantics, absence grace behavior, rest handling, or statistics produced by state changes. Enforces TDD workflow."
argument-hint: "Describe the desired state-machine or overlay behavior change"
---

# SeatTooLong State Machine Change

Use this skill for changes involving `SittingMonitor`, `MonitoringService`, overlay state/timers, absence grace, rest transitions, notification timing, or statistics emitted from state transitions.

## TDD Procedure

1. Read [AGENTS.md](../../../AGENTS.md) and the relevant sections of [README.md](../../../README.md) for current behavior.
2. Write or update a focused failing test before changing implementation.
3. Prefer these test entry points:
   - `SeatTooLong.Tests/SittingMonitorTests.cs` for `Idle`, `Sitting`, `Alerting`, `Resting`, absence grace, and timer semantics.
   - `SeatTooLong.Tests/StatisticsServiceTests.cs` for sessions emitted from state changes or active-session flushing.
   - `SeatTooLong.Tests/JsonSettingsServiceTests.cs` for settings defaults and persistence.
   - `SeatTooLong.Tests/SeatedFaceRuleTests.cs` and `SeatTooLong.Tests/HaarPersonDetectorTests.cs` for detection heuristics.
4. Implement the smallest code change that passes the new test.
5. Refactor only if it removes real duplication or clarifies existing behavior.
6. Run `dotnet test SeatTooLong.Tests --verbosity minimal`.
7. Build the app. If the running tray app locks Debug output, use `dotnet build SeatTooLong.App\SeatTooLong.App.csproj -p:BaseOutputPath=obj\verify-output\`.

## Behavior Notes

- `SittingMonitor` owns the business states: `Idle`, `Sitting`, `Alerting`, `Resting`.
- Overlay timer semantics are intentional: `Idle` and `Resting` use current state duration, `Sitting` and `Alerting` use current sitting duration, and temporary away display uses absence duration.
- Absence grace comes from `AppSettings.AbsenceGracePeriodSeconds` and maps to `SittingMonitorOptions.AbsenceGracePeriod`.
- Keep runtime detection thresholds in sync with `SeatTooLong.Core/Vision/SeatedFaceRule.cs` and sample-image tests.
- Statistics should upsert active sessions via `StatisticsService.FlushActiveSessions()` rather than creating duplicates.