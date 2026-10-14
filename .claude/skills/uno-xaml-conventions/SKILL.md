---
name: uno-xaml-conventions
description: Trackify's Uno Platform / XAML / MVVM house rules for the app head (Source/Trackify) — code-behind ban and its two exceptions, attached behaviors instead of event handlers, bound values instead of imperative control APIs, Components vs Widgets, namespace/suffix rules, CommunityToolkit.Mvvm style, global converters and design tokens, design-time DataContext, safe-area insets, German UI, and how UI work is verified here. Use when creating or editing a Page, Component, Widget, Converter, Behavior, ViewModel or any .xaml/.xaml.cs file under Source/Trackify, or when reviewing app-head changes. ECC has no Uno/XAML/MAUI/WinUI skill, so this is the only coverage for the app side.
---

# Uno / XAML conventions (Trackify app head)

Scope: `Source/Trackify/` only. The CLI and the shared core have their own rules in `CLAUDE.md`.
Deeper structure: `docs/arc42/05-building-block-view.md` §5.2.4.

## Code-behind

- **`.xaml.cs` contains `InitializeComponent()` and nothing else** — for Pages, Components *and*
  Widgets alike. Logic in a view can't be tested and can't be reused; it belongs in the ViewModel.
- **Two deliberate exceptions, both responsive master–detail:** `Pages/MainPage.xaml.cs` and
  `Pages/SecondPage.xaml.cs`. Both react to width **and** to a selection change
  (`MainViewModel.SelectedTrain` / `SecondViewModel.SelectedSegment`) — an
  `AdaptiveTrigger`/`VisualStateManager` can only express the width half, so the combination is
  hand-rolled in `SizeChanged`/`Loaded`/`DataContextChanged` plus one `ApplyResponsiveLayout()`.
  Nothing else lives in either file. **Don't add a third exception** — and don't grow these two.
- Not logic, therefore fine: a Widget's `DependencyProperty` declarations (that is what a Widget *is*
  — see `Widgets/SpeedProfileWidget.xaml.cs`) and `Shell.xaml.cs`'s
  `IContentControlProvider.ContentControl` (Uno.Extensions navigation contract).

## View-side interactivity

- Prefer a plain `Command` binding. When an event must reach a command that isn't natively
  `Command`-bindable (a tapped `Grid`/`Path`/`StackPanel`), write or reuse an **attached behavior** in
  `Presentation/Behaviors/` — never a `*_Click`/`*_Tapped` handler in a `.xaml.cs`.
- Existing behaviors: `TappedCommandBehavior`, `ColumnSplitterBehavior`, `RowSplitterBehavior`.
  `TappedCommandBehavior` passes the element's `DataContext` as the command parameter, so item
  templates can reach the page VM:

  ```xml
  local:TappedCommandBehavior.Command="{Binding ElementName=Root, Path=DataContext.SelectSegmentCommand}"
  ```

  (hence the `x:Name="Root"` on the `UserControl` root — inside a template the `DataContext` is the item).

## No imperative control APIs

- Avoid `ScrollViewer.ChangeView` and friends entirely: programmatic zoom isn't reliably supported on
  every Uno target, and it hides state from the ViewModel. Express the state as a **bound value the
  ViewModel owns**.
- The worked example (Streckenplaner zoom): `SecondViewModel.ZoomFactor` (double, clamped 0.4–3 by
  `ZoomIn`/`ZoomOut`/`ZoomFit`) → computed `CanvasWidth`/`CanvasHeight` (900×600 × factor, re-raised
  from `OnZoomFactorChanged`) → bound onto `Viewbox.Width`/`Height` around a fixed 900×600 `Canvas`,
  inside a plain, always-supported `ScrollViewer` (`Components/TrackCanvas.xaml`).
- Related trap in the same file: a `{Binding}` inside a `RenderTransform` has **no DataContext to
  inherit**. Pre-fit coordinates in `Helpers/TrackGeometry.cs` instead of binding a transform.

## Components vs Widgets

- **`Presentation/Components/`** — page-specific composed sections that **inherit the page's
  `DataContext`** (`TrainListPanel`, `TrainEditor`, `TrackCanvas`, `SegmentInspector`, `AddHubDialog`).
  They bind straight to the hosting ViewModel and are not reusable elsewhere.
- **`Presentation/Widgets/`** — reusable atoms that **expose `DependencyProperty`s** and know nothing
  about any ViewModel (`SpeedProfileWidget.Graph`, `BlueprintFrame`). If it needs a VM, it's a Component.

## Naming, namespaces, files

- **Namespace must match the folder** (`IDE0130`) and be **file-scoped** (`IDE0161`). Both are
  **errors** here — `EnforceCodeStyleInBuild` + `TreatWarningsAsErrors` in `Directory.Build.props` —
  so the build fails on a violation. `Platforms/**` is exempt (Uno convention namespaces).
- Type-name suffix per folder for new types: `Services/` → `*Service`, `Presentation/ViewModels/` →
  `*ViewModel`, `Presentation/Behaviors/` → `*Behavior`, `Presentation/Widgets/` → `*Widget`,
  `Presentation/Converters/` → `*Converter`. This one is a convention, **not** analyzer-checked (a few
  older types sit outside it) — follow it for new code; don't rename existing types in passing.
- **One top-level type per file**, named after the type — every converter, behavior and item VM gets
  its own file.
- The folder is **`Pages`**, never "Screens".
- The app's `GlobalUsings.cs` already carries `CommunityToolkit.Mvvm.ComponentModel`/`.Input`,
  `Trackify.Presentation.ViewModels`, `Trackify.Helpers`, `Trackify.Models` and the Domain /
  Application namespaces. Don't re-import those per file.

## MVVM

- CommunityToolkit.Mvvm with **field-based** `[ObservableProperty]` and `[RelayCommand]`:
  `[ObservableProperty] private double zoomFactor = 1;`. `MVVMTK0045` (partial-property advice) is
  suppressed on purpose — field-based keeps default values readable and we don't target WinRT AOT.
- Side effects belong in the generated `partial void On<Name>Changed(...)` hook, not in a setter.
- **No business logic in a page or view** — it lives in the ViewModel, or in an Application use-case
  (`ITrainControlService`/`ITrainService`) reached through DI.
- Routes and ViewMaps are registered in `App.xaml.cs` → `RegisterRoutes` (Uno.Extensions Navigation).

## Resources

- **Converters are declared once globally** in `Styles/Converters.xaml` (merged in `App.xaml`) and
  used via `{StaticResource HexToBrushConverter}`. Never re-declare one in a page's `Resources`; a new
  converter means a new file in `Presentation/Converters/` plus one line in `Converters.xaml`.
- Design tokens and shared styles live in `Styles/DesignTokens.xaml` (`RadiusMedium`,
  `SectionCardStyle`, `MutedTextBrush`, `MonoFontFamily`, …) and `Styles/ColorPaletteOverride.xaml`.
  Reach for `{ThemeResource …}`/`{StaticResource …}`, not a hard-coded brush or corner radius.

## Design-time binding

Every classic `{Binding}` view carries a design-time DataContext so paths resolve in the IDE — add one
to each new view, **and to each new `DataTemplate`** whose item type differs:

```xml
xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
mc:Ignorable="d"
d:DataContext="{d:DesignInstance Type=vm:SecondViewModel}"
```

It is design-time only and ships nothing.

## Mobile safe area

Uno `Toolkit` insets, so the status bar / notch / gesture nav never overlap content. Apply the same
pair on every new mobile-reachable page: `utu:SafeArea.Insets="Top,Left,Right"` on the page header and
`utu:SafeArea.Insets="Bottom,Left,Right"` on the content `Grid` (see `MainPage.xaml`, `SecondPage.xaml`).

## Language

**App UI labels are German** ("Train-Konfiguration", "Verbindung", "Streckenplaner"). CLI output is
English; code, comments and commit messages stay English.

## Verification reality — no screenshots here

The Skia-rendered UI **cannot be pixel-verified in this environment** (the GPU surface comes back
blank, and the WASM canvas render-loop times out the browser tool). So proof of a green app change is:

```bash
dotnet build Source/Trackify/Trackify.csproj -f net10.0-android
dotnet build Source/Trackify/Trackify.csproj -f net10.0-desktop
dotnet build Source/Trackify/Trackify.csproj -f net10.0-browserwasm
dotnet build Source/Trackify/Trackify.csproj -f net10.0-windows10.0.19041.0
dotnet test  Test/Trackify.Tests/Trackify.Tests.csproj
dotnet run --project Source/Trackify/Trackify.csproj -f net10.0-desktop   # launch smoke test
```

Those four heads are the ones that build here (iOS needs a Mac). Build **every** head you could have
broken — a XAML or `#if` mistake often compiles on desktop and fails on wasm or Android. The user
confirms visuals on a real device; never claim a layout "looks right".
