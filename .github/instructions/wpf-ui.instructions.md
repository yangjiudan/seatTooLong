---
description: "Use when editing WPF XAML, settings pages, overlay UI, report windows, or localization-backed UI in SeatTooLong. Covers compact layout, row alignment, and runtime setting flow."
applyTo: ["SeatTooLong.App/**/*.xaml", "SeatTooLong.App/Views/**/*.cs", "SeatTooLong.Core/Localization/LocalizationService.cs"]
---

# SeatTooLong WPF UI Guidelines

- Keep UI changes compact and row-aligned; in settings pages, each control row should normally use `Auto` height.
- Use `*` grid rows only for intentional spacer/content areas, not for individual control rows such as sliders, labels, or checkboxes.
- Put value labels beside sliders in the same row so the label, control, and current value scan as one setting.
- Keep user-facing strings in `SeatTooLong.Core/Localization/LocalizationService.cs` and update Chinese and English entries together.
- Settings must flow through `AppSettings`, `SettingsWindow`, and `App.OnSettingsSaved` so changes apply at runtime where possible.
- For overlay behavior, keep display duration semantics aligned with `SittingMonitor`: `Idle`/`Resting` use current state duration, `Sitting`/`Alerting` use current sitting duration, and the temporary away display uses absence duration.
- When changing UI behavior, use TDD against the underlying state, settings, localization, or mapping logic first; then verify WPF build/diagnostics.
