# 1. Introduction and Goals

Trackify configures and controls **LEGO Powered Up** train hubs over **Bluetooth Low Energy**. It is
a hobby/model-railway control system: you describe your trains once (hub type, motor ports, LED
colour, speed, acceleration and braking curves), and then drive them — from a phone, from a desktop,
from a browser, or hands-free from a Raspberry Pi that keeps a whole layout running unattended.

There is **no vendor cloud and no mandatory backend**. In the default mode the device that shows the
UI is also the device that owns the radio.

## 1.1 Requirements Overview

### Core capabilities

| # | Requirement | Where it lives |
|---|---|---|
| R1 | Discover nearby Powered Up hubs and capture their address | `ILegoService.DiscoverAsync`, `trackify discover` |
| R2 | Persist a train's configuration and reuse it from any front-end | `Train` entity + `ITrainRepository` (SQLite) |
| R3 | Connect to a train's hub and drive its motor at a signed percentage speed | `ITrainControlService.SetSpeedAsync` |
| R4 | Set the hub's built-in RGB LED to the train's configured colour | `ITrainControlService.SetLedAsync` |
| R5 | Model acceleration/braking as a function `f(x)` over `x ∈ [0,1]`, including user-entered formulas | `Domain/SpeedFunction.cs`, `Domain/ExpressionParser.cs` |
| R6 | Plan a track layout from segments with per-segment speed, direction and sensor action | `Domain/Trains/TrackSegment.cs`, `TrackCanvas` |
| R7 | Run the whole fleet unattended, reconnecting hubs that drop | `trackify auto` |
| R8 | Drive hubs attached to *another* machine over the LAN | `trackify server` + the app's Server mode |
| R9 | Stop every motor cleanly on shutdown (Ctrl+C / SIGINT / systemd stop) | `ConsoleCancellation`, `stop_signal: SIGINT` |

### Out of scope (deliberately)

- No user accounts, no multi-tenancy, no cloud sync, no telemetry.
- No authentication or transport security on the LAN backend — see [ADR-15](09-architecture-decisions.md#adr-15-no-authentication-or-tls-on-the-lan-backend).
- No support for non-LEGO train hardware, and no LEGO hub firmware flashing.
- No control of hub peripherals beyond motors and the built-in RGB LED (sensor *actions* are
  configured in the domain model but not yet executed against hardware).

## 1.2 Quality Goals

The top five, in priority order. Each maps to concrete scenarios in
[§10 Quality Requirements](10-quality-requirements.md).

| Priority | Quality goal | Why it matters here | Architectural consequence |
|---|---|---|---|
| 1 | **Control responsiveness** — a speed change reaches the motor with no perceptible lag | A train is a physical object in motion; laggy control feels broken and can derail stock | Debounced-but-immediate send path (`SetSpeedDebounced`, 200 ms); SignalR (not REST polling) for real-time remote control; no request/response round trip for the local path |
| 2 | **Portability across front-ends** — one behaviour set, five app heads plus a CLI | Same trains must be drivable from a phone at the layout and from a Pi under it | `ILegoService` as the single BLE seam; per-platform transports selected in DI; shared `TrainControlService` |
| 3 | **Changeability / architectural integrity** — the layering cannot silently rot | Solo-maintained hobby project; drift is only caught if the build catches it | Clean Architecture with per-layer DI, NetArchTest rules in CI, `TreatWarningsAsErrors` |
| 4 | **Unattended reliability** — a Pi keeps the layout running for days | The Pi deployment is headless; nobody is watching the console | `trackify auto` re-reads config and reconnects dropped hubs each sweep; systemd `Restart=on-failure`; bounded connect retry |
| 5 | **Operational safety** — nothing keeps moving after you stop it | A runaway train with no UI attached is the worst failure mode | SIGINT-driven clean shutdown that stops motors *before* disconnecting; global exception handlers so no failure is swallowed |

**Explicit non-goal:** *hardening against a hostile network*. The LAN backend trusts its network
segment ([ADR-15](09-architecture-decisions.md#adr-15-no-authentication-or-tls-on-the-lan-backend)).

## 1.3 Stakeholders

| Role | Expectations of the architecture |
|---|---|
| **Maintainer** (repository owner) | Can add a feature in one place and have it appear in both front-ends; the build tells them when they break a layer rule |
| **Hobbyist / end user** | Configure a train once on the phone, then drive it from anywhere; German UI labels; no account, no setup server |
| **Pi operator** (often the same person, different hat) | A single self-contained binary, a systemd unit or a `docker compose up`, and readable logs when a hub misbehaves |
| **Contributor** | Can tell from the folder where code belongs; conventions are documented *and* enforced, so review is about substance |
| **CI / quality gates** (GitHub Actions, SonarCloud, CodeQL) | The shared core + CLI build and test on a plain Linux runner without mobile workloads |
| **Upstream projects** (SharpBrick.PoweredUp, Uno Platform, BlueZ) | Consumed at documented, pinned versions; workarounds for upstream defects are isolated and commented ([§11](11-risks-and-technical-debt.md)) |
