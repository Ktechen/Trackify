# 9. Architecture Decisions

Decisions that were expensive, risky, or non-obvious — recorded so the next reader does not re-open
them by accident. Format: **Context → Decision → Consequences**, with the code that embodies it.

All listed decisions are **accepted** and reflected in the current codebase unless marked otherwise.

| ID | Decision | Status |
|---|---|---|
| [ADR-01](#adr-01-clean-architecture-with-per-layer-di-entry-points) | Clean Architecture with per-layer DI entry points | accepted |
| [ADR-02](#adr-02-use-sharpbrickpoweredup-instead-of-implementing-lwp) | Use SharpBrick.PoweredUp instead of implementing LWP | accepted |
| [ADR-03](#adr-03-ilegoservice-as-the-single-ble-seam) | `ILegoService` as the single BLE seam | accepted |
| [ADR-04](#adr-04-two-transport-selection-styles-runtime-vs-compile-time) | Two transport-selection styles: runtime vs. compile-time | accepted |
| [ADR-05](#adr-05-host-conditioned-multi-targeting-of-trackifyapplication) | Host-conditioned multi-targeting of `Trackify.Application` | accepted |
| [ADR-06](#adr-06-sqlite--ef-core-with-ensurecreated-instead-of-a-json-file) | SQLite + EF Core with `EnsureCreated()` instead of a JSON file | accepted |
| [ADR-07](#adr-07-front-ends-see-traindto-never-domain-entities) | Front-ends see `TrainDto`, never Domain entities | accepted |
| [ADR-08](#adr-08-rest-for-one-shot-actions-signalr-for-real-time-control) | REST for one-shot actions, SignalR for real-time control | accepted |
| [ADR-09](#adr-09-runtime-directserver-switch-instead-of-a-restart) | Runtime Direct/Server switch instead of a restart | accepted |
| [ADR-10](#adr-10-pin-pluginble-to-300) | Pin `Plugin.BLE` to 3.0.0 | accepted |
| [ADR-11](#adr-11-spectreconsolecli-bridged-onto-microsoftextensionsdependencyinjection) | Spectre.Console.Cli bridged onto MS DI | accepted |
| [ADR-12](#adr-12-source-generated-loggermessage-over-serilog) | Source-generated `[LoggerMessage]` over Serilog | accepted |
| [ADR-13](#adr-13-do-not-build-the-uno-app-in-ci) | Do not build the Uno app in CI | accepted, with a known gap |
| [ADR-14](#adr-14-bounded-connect-retry-around-a-sharpbrick-null-deref) | Bounded connect retry around a SharpBrick null-deref | accepted (workaround) |
| [ADR-15](#adr-15-no-authentication-or-tls-on-the-lan-backend) | No authentication or TLS on the LAN backend | accepted, scoped |
| [ADR-16](#adr-16-mcp-server-to-live-in-infrastructure) | MCP server to live in Infrastructure | **proposed** (not implemented) |

---

### ADR-01: Clean Architecture with per-layer DI entry points

**Context.** Two front-ends with nothing in common (a five-head Uno app, a Spectre CLI) must behave
identically, and later a third host appeared (the ASP.NET backend). A single maintainer cannot police
layering by memory.

**Decision.** Dependencies point inward — `Domain ← Application ← Infrastructure ← front-ends` — and
each layer owns its registration: `AddTrackifyDomain()`, `AddTrackifyApplication()`,
`AddTrackifyInfrastructure(storePath?)`. Composition roots chain them and never reach inside a layer.
The rules are asserted by NetArchTest in CI.

**Consequences.** Adding a use-case makes it available to all three hosts at once. The third host
(`TrackifyServer`) cost almost nothing to add — it repeats the same three calls. Cost: a small amount
of ceremony (`AddTrackifyDomain()` registers nothing) and the DTO mapping of ADR-07. Front-ends do
reference Infrastructure at the composition root, which is intentional and does not violate the rule
the tests check (no *type* dependency inward-out).

---

### ADR-02: Use SharpBrick.PoweredUp instead of implementing LWP

**Context.** LWP v3 is a substantial binary protocol (hub attach/detach, port modes, feedback). The
vendored LEGO spec is in `docs/lego-ble-wireless-protocol-docs`.

**Decision.** Depend on **SharpBrick.PoweredUp 5.0.2** for protocol encoding and hub abstraction, and
supply only the platform BLE adapter beneath it.

**Consequences.** Weeks of protocol work avoided, and hub-type handling comes for free. The cost is a
hard coupling to SharpBrick's abstractions and its defects — ADR-10 (a transitive version pin) and
ADR-14 (a null-deref workaround) both exist solely because of this dependency. Because command
building is *not pure*, `LwpCommands` lives in Infrastructure; only the genuinely pure bits (RGB-LED
port table, MAC format/parse) live in `Application/Lego/LwpAddressing.cs`.

---

### ADR-03: `ILegoService` as the single BLE seam

**Context.** Four incompatible BLE stacks (Android, CoreBluetooth, WinRT, BlueZ), one product.

**Decision.** One port with six members — `IsSupported`, `DiscoverAsync`, `Connect/DisconnectAsync`,
`SetSpeedAsync(hubId, port, power)`, `SetLedAsync(hubId, r, g, b)`. Hubs are addressed by an opaque
string key; power is a signed percentage. Everything above the seam depends only on the interface.

**Consequences.** The remote transport ([ADR-08](#adr-08-rest-for-one-shot-actions-signalr-for-real-time-control))
became *just another implementation* — the UI needed no changes to gain network control, and the
server implements its endpoints by calling the same interface. `IsSupported` lets a head without a
radio be honest instead of throwing. Cost: the seam is intentionally narrow, so richer LWP features
(sensor feedback, port modes) will need it widened.

---

### ADR-04: Two transport-selection styles: runtime vs. compile-time

**Context.** `AddLinuxLego` uses `if (OperatingSystem.IsLinux())`; `AddAndroidLego`/`AddIosLego`/
`AddWindowsLego` use `#if`. The inconsistency looks like an oversight and has to be explained once.

**Decision.** Keep both, chosen by what the code *can* do:

- **Runtime** for Linux — the BlueZ types (`Linux.Bluetooth`, D-Bus) compile on every TFM, so one
  artifact can ship to the Pi and degrade to a no-op elsewhere. This is what makes
  `dotnet publish -r linux-arm64` work from a Windows box with no build flags.
- **Compile-time** for Android/iOS/Windows — `DirectLegoService` and `WindowsLegoService` reference
  packages (`Plugin.BLE`, SharpBrick `.WinRT`) that **only exist on those TFMs**. They cannot be
  compiled into a general build, so a runtime check is impossible.

**Consequences.** The Pi deployment stays flag-free (a real benefit: no "did you build with LINUX?"
class of bug). The price is that the two mechanisms must be explained wherever they appear — hence
this ADR, and comments at both call sites.

---

### ADR-05: Host-conditioned multi-targeting of `Trackify.Application`

**Context.** The mobile/WinRT transports live in Application (they are use-case-adjacent and shared
across heads), but their packages only restore on their own TFM — and iOS cannot be built off macOS.

**Decision.** Condition the TFM list on the **build host**: Windows → `net10.0;net9.0;net10.0-android;net10.0-windows…`,
macOS → `…;net10.0-android;net10.0-ios`, Linux → plain `net10.0;net9.0`. Additionally, detect a
desktop/server RID publish (a RID set that is neither android nor ios) and force the transport-free
TFM set for it.

**Consequences.** Each host builds exactly the heads it can. The RID guard is not optional: without
it, `dotnet publish -r linux-arm64` drags in the mobile TFMs and NuGet fails trying to restore a
nonexistent `Microsoft.NETCore.App.Runtime.Mono.<rid>` pack (`NU1102`). Cost: the same project
produces different assets on different machines, which surprises newcomers and rules out a naive
"build everything on one runner" CI ([ADR-13](#adr-13-do-not-build-the-uno-app-in-ci)).

---

### ADR-06: SQLite + EF Core with `EnsureCreated()` instead of a JSON file

**Context.** The original store was `trains.json`. It needed concurrent access from the app and the
CLI on the same machine, queries, and stable identity.

**Decision.** EF Core + SQLite (`trackify.db`), a generic `BaseRepository<T>` over an
`IDbContextFactory`, GUID v7 keys, enums stored as **names**, schema via `EnsureCreated()` — **no
migrations**. Domain entities carry no EF attributes; mapping is fluent in `TrackifyDbContext`.

**Consequences.** Robust shared access and a hand-inspectable file. Migration-free means **changing an
entity's shape requires deleting the dev `trackify.db`** (or adopting EF migrations) — acceptable for
a single-user hobby store, tracked as debt in [§11](11-risks-and-technical-debt.md). Some artefacts
still reference the old `trains.json` path (`docker-compose.yml`); those are stale.

---

### ADR-07: Front-ends see `TrainDto`, never Domain entities

**Context.** Without a boundary, persistence concerns (audit timestamps, key strategy) leak into two
front-ends and a network contract.

**Decision.** `TrainDto` + `TrainMapping` (`ToDto`/`ToEntity`) form the Application boundary. The
arch test `Cli_never_touches_the_domain_entity_namespace` enforces it; the test project references
the CLI *solely* to make that checkable.

**Consequences.** `GET /api/trains` returns `TrainDto` directly — the boundary and the wire format are
the same shape, which is why the network layer needed no separate contract types. Cost: hand-written
mapping and near-duplicate property lists that must be kept in sync.

---

### ADR-08: REST for one-shot actions, SignalR for real-time control

**Context.** Server mode needs both "give me the train list" and "the slider moved, now".

**Decision.** Split by interaction shape. **REST (Refit)** for one-shot actions — `GET /api/trains`,
`POST /api/discover`, `POST /api/hubs/{hubId}/connect|disconnect`, `GET /api/state`. **SignalR** for
real-time speed/LED, plus a `SpeedChanged` broadcast to all clients. Route templates and hub method
names live in **`Trackify.Application`** (`ApiRoutes`, `TrainHubMethods`) so both sides compile
against the same constants.

**Consequences.** Slider movements avoid HTTP round-trip overhead, and every connected client sees
live speed without polling. A renamed route or hub method is a **compile error**, not a runtime 404 —
this is the main reason the contract lives in Application rather than being duplicated. Cost: two
protocols to reason about, and `GET /api/state` exists purely so a late-joining client can catch up.
`TrainStateStore` is in-memory and per-run, deliberately.

---

### ADR-09: Runtime Direct/Server switch instead of a restart

**Context.** A hub accepts one connection (TC-3), and users move between rooms. Requiring an app
restart to change transport is a poor experience — and on desktop/WASM there is no local option at
all.

**Decision.** `SwitchingLegoService` implements `ILegoService` and consults a live `ConnectionState`
on **every call**, routing to the local transport or a `RemoteLegoService`. The remote transport is
re-created when the URL changes; the previous one is disposed in the background. `App.xaml.cs`
rebuilds the platform-registered transport as the "local" one and registers the switcher in its place.

**Consequences.** Toggling takes effect immediately, no restart, no UI changes anywhere. Mode + URL
persist in local app settings. Cost: the DI trick (re-resolving the last `ILegoService` descriptor via
`ImplementationInstance`/`ImplementationFactory`/`ImplementationType`) is subtle, and errors are
surfaced as **German** user-facing messages from a service — a deliberate exception to
"services are UI-neutral", because these are the two states only the user can fix.

---

### ADR-10: Pin `Plugin.BLE` to 3.0.0

**Context.** `SharpBrick.PoweredUp.Mobile 5.0.2` was compiled against `Plugin.BLE 3.0.0`. Newer
versions change signatures SharpBrick calls.

**Decision.** Pin exactly `3.0.0` in `Directory.Packages.props`, with the reason in a comment.

**Consequences.** Connect works. Upgrading Plugin.BLE produces a **runtime `MissingMethodException`
on connect** — invisible at compile time, and impossible to hit in CI since BLE never runs there. This
pin may only be lifted together with a SharpBrick.Mobile upgrade. Its `net7.0-android33.0` asset is
consumable by `net10.0-android`.

---

### ADR-11: Spectre.Console.Cli bridged onto Microsoft.Extensions.DependencyInjection

**Context.** Spectre has its own `ITypeRegistrar` abstraction; the layers register into
`IServiceCollection`.

**Decision.** Use the `Spectre.Console.Cli.Extensions.DependencyInjection` package's
`DependencyInjectionRegistrar` rather than hand-writing an adapter.

**Consequences.** Commands take constructor-injected use-cases like any other class, and there is no
bespoke registrar to maintain. Cost: one more third-party dependency on the CLI's critical path.

---

### ADR-12: Source-generated `[LoggerMessage]` over Serilog

**Context.** Logging on a control path that fires on every slider tick must not allocate, but the
output still needs to be readable on a Pi.

**Decision.** Per-project `internal static partial class Log` with `[LoggerMessage]` declarations and
explicit `EventId` ranges; Serilog as the backend via `AddSerilog`, configured from `appsettings.json`.
`ILogger<T>` is a **required** constructor dependency; tests pass `NullLogger<T>.Instance`. The Domain
does not log at all.

**Consequences.** Allocation-free hot paths and structured, greppable output. Two non-obvious costs:
the Console sink assembly must be passed **explicitly** to `ReadFrom.Configuration` or a single-file
publish silently logs nothing; and every new message needs a declaration rather than an inline string.

---

### ADR-13: Do not build the Uno app in CI

**Context.** `Source/Trackify` declares five heads. Restoring the project on any single runner
triggers workload imports (android, wasm-tools) and OS-locked TFMs (iOS needs macOS, Windows needs
Windows) — **even with `-f <head>`**, because restore imports workloads for all TFMs.

**Decision.** `ci.yml` gates the shared core + CLI + tests on ubuntu-latest. The Android head is built
by `android-apk.yml` on windows-latest for tags. The other heads are verified locally by developers.
The same limitation scopes the SonarCloud analysis.

**Consequences.** The gate is fast and reliable on a single runner. The gap is real and accepted: a
change that breaks only the desktop, WASM or iOS head **can merge**. Mitigation is the documented
local check (build the four buildable heads + a desktop launch smoke test). Closing this properly
needs a per-OS + workload CI matrix. → [§11](11-risks-and-technical-debt.md)

---

### ADR-14: Bounded connect retry around a SharpBrick null-deref

**Context.** SharpBrick's `BluetoothKernel.ConnectAsync` has an unfixed null-dereference
([sharpbrick/powered-up#188](https://github.com/sharpbrick/powered-up/issues/188)) that makes connects
fail intermittently.

**Decision.** Wrap connect in a **bounded** retry that catches `NullReferenceException` and
`ArgumentNullException` only, in `Infrastructure/Ble/LwpCommands.cs`.

**Consequences.** Connects succeed reliably in practice. This is knowingly a workaround for an
upstream bug: catching `NullReferenceException` is normally wrong, the retry is bounded so a genuine
null bug still surfaces, and it must be revisited when SharpBrick fixes the issue.

---

### ADR-15: No authentication or TLS on the LAN backend

**Context.** The backend runs on a Raspberry Pi on a home network, driving toy trains. It stores no
credentials and no personal data. A Pi has no certificate.

**Decision.** No authentication, no TLS, and permissive CORS (`SetIsOriginAllowed(_ => true)`) so the
WASM head — which sends an `Origin` — can reach it. Credentials are deliberately **not** enabled,
avoiding the unsafe any-origin + `AllowCredentials` combination (CWE-942). The example `http://` bind
address carries a documented `S5332` suppression.

**Consequences.** Zero-configuration on a trusted LAN. **Anyone on that network can drive the trains,
and the backend must never be exposed to the internet or port-forwarded.** This is a scoped
acceptance, not an oversight — the mitigations that *are* present target what still matters at this
trust level: generic error bodies (CWE-209) and log-forging protection (CWE-117). → [§11](11-risks-and-technical-debt.md)

---

### ADR-16: MCP server to live in Infrastructure

**Status: proposed — not implemented.** Tracked in
[issue #1](https://github.com/Ktechen/Trackify/issues/1).

**Context.** Exposing train control over the **Model Context Protocol** would let an AI client drive
trains using the existing use-cases.

**Decision (planned).** The official `ModelContextProtocol` C# SDK + `ModelContextProtocol.AspNetCore`
over HTTP/SSE; MCP tool classes under `Trackify.Infrastructure/Mcp/` wrapping `TrainControlService`
and `ITrainRepository` (`list_trains`, `discover_hubs`, `drive_train`, `stop_train`, `set_color`),
plus an `AddTrackifyMcp()` extension. A thin host chains the three `AddTrackify*` calls and maps the
SSE endpoint.

**Consequences (anticipated).** Note the layering tension recorded up front: an **inbound** MCP server
is normally a front-end, not Infrastructure — the issue places it in Infrastructure, with the network
host as the composition root. Whoever implements this should either accept that deviation explicitly
or move the MCP tools to a front-end project. Runtime verification will need an MCP client plus a Pi.
The existing backend ([ADR-08](#adr-08-rest-for-one-shot-actions-signalr-for-real-time-control))
already demonstrates the "same use-cases over the network" pattern this would follow.
