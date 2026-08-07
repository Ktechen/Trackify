# 11. Risks and Technical Debt

Known and accepted, in priority order. "Accepted" means a decision was made, not that it was
overlooked — each entry names what would trigger a revisit.

## 11.1 Risks

### R-1: The LAN backend has no authentication (medium impact, accepted & scoped)

Anyone on the network segment can list trains, discover hubs, connect, and drive at full speed. There
is no auth, no TLS, and CORS accepts any origin.

- **Accepted because** the design target is a trusted home LAN with toy trains, no credentials and no
  personal data ([ADR-15](09-architecture-decisions.md#adr-15-no-authentication-or-tls-on-the-lan-backend)).
- **Hard boundary:** never port-forward or expose the backend to the internet. Do not run it on an
  untrusted network (a shared flat, a conference, a hackspace).
- **Revisit if** the backend ever needs to be reachable off-LAN, or if the store starts holding
  anything more sensitive than train names. A bearer token read from `appsettings.json` would be the
  minimum viable step.

### R-2: Transitive `Tmds.DBus` advisory (low impact, accepted)

`Linux.Bluetooth` pins `Tmds.DBus 0.15.0`, which carries `GHSA-xrw6-gwf8-vvr9`. `NU1903` is
suppressed in `Trackify.Infrastructure`, `Trackify.Cli` **and** the Uno app (the app takes the
Infrastructure reference to share the SQLite store, so it inherits the transitive dependency even
though it never runs BlueZ).

- **Accepted because** the D-Bus path is only ever exercised against a trusted local Pi's
  `bluetoothd`, and the app never executes it at all.
- **Revisit when** `Linux.Bluetooth` ships a build against a patched `Tmds.DBus`. The suppression is
  broad — a *different* advisory in another package would also be silenced.

### R-3: Upstream version coupling to SharpBrick (medium impact, mitigated by pinning)

`Plugin.BLE` must stay at exactly `3.0.0` ([ADR-10](09-architecture-decisions.md#adr-10-pin-pluginble-to-300)).
A bump produces a **runtime** `MissingMethodException` on connect — invisible to the compiler and
impossible to catch in CI, since BLE never runs there.

- **Mitigation:** the pin is commented in `Directory.Packages.props`, in CLAUDE.md and here.
- **Revisit only** together with a `SharpBrick.PoweredUp.Mobile` upgrade, and verify on a real phone.

### R-4: Uno heads are not gated by CI (medium likelihood, accepted with a manual mitigation)

`ci.yml` builds the shared core + CLI + tests only. A change that breaks the **desktop, WASM or iOS**
head can merge ([ADR-13](09-architecture-decisions.md#adr-13-do-not-build-the-uno-app-in-ci)). The
Android head is covered only on tags, by `android-apk.yml`. The SonarCloud scan has the same scope,
so app code is not analysed either.

- **Mitigation:** the documented local check — build the four buildable heads plus a desktop launch
  smoke test — before pushing app changes.
- **Revisit if** app changes become frequent enough for the gap to bite; the fix is a per-OS +
  workload CI matrix.

### R-5: The `EnsureCreated()` schema trap (low impact, high annoyance)

There are no EF migrations. Adding or renaming a property on `Train` will **not** update an existing
`trackify.db` — the app reads an outdated schema and fails at query time, on developer machines and
on every deployed Pi.

- **Mitigation today:** delete the dev database after an entity change.
- **Revisit when** the first schema change has to reach a Pi that holds trains worth keeping. That is
  the point to adopt EF migrations ([ADR-06](09-architecture-decisions.md#adr-06-sqlite--ef-core-with-ensurecreated-instead-of-a-json-file)).

### R-6: No automated verification of the top quality goal (accepted)

Control latency, BLE discovery, connect behaviour and UI rendering are all verified **by hand** on a
phone and a Pi. Nothing in CI would catch a regression that, say, doubled the debounce or broke the
BlueZ LE discovery filter.

- **Structural cause:** BLE cannot run on a hosted runner, and the Uno Skia surface cannot be
  screenshotted in an automated environment.
- **Mitigation:** the BlueZ hard-won facts are documented as constraints
  ([§2.1 TC-4](02-architecture-constraints.md#21-technical-constraints-not-negotiable)) precisely
  because a regression would otherwise be silent.

### R-7: Single-connection hubs make the two modes mutually exclusive (inherent)

A hub accepts one connection. If the Pi is driving a train, a phone in Direct mode cannot connect to
it — and the failure surfaces as a generic connect error, not "someone else has it".

- **Inherent to the hardware** (TC-3); Server mode is the intended answer.
- Could be improved by detecting the condition and explaining it in the UI.

### R-8: `RemoteTrainSync` falls back to matching by name (low impact)

Sync de-duplicates by `HubId` → `BleAddress` → **`Name`**. Two distinct trains that share a name and
have no hub address recorded yet will be merged into one local row.

- **Realistic trigger:** trains created in the app before discovery has run, then synced from a Pi.
- **Fix if it bites:** drop the name fallback and require a hub identity for matching.

## 11.2 Technical debt

| # | Debt | Where | Cost of leaving it |
|---|---|---|---|
| D-1 | **`SECURITY.md` is still the unedited GitHub template** — placeholder version numbers (`5.1.x`, `4.0.x`) that match nothing in this project, and "tell them where to go" boilerplate instead of a reporting route | [`SECURITY.md`](../../SECURITY.md) | A reporter has no actual way to report; the fake version table is misleading |
| D-2 | **Duplicated types across layers**: `ConnectedHub` and `UnsupportedLegoService` exist in both `Application/Services/` and `Infrastructure/Ble/` | both projects | Two copies drift; a fix applied to one is silently missing from the other |
| D-3 | **Stale `trains.json` references** — the store has been SQLite since [ADR-06](09-architecture-decisions.md#adr-06-sqlite--ef-core-with-ensurecreated-instead-of-a-json-file), but `docker-compose.yml` still mounts `./data` for `trains.json` and sets `TRACKIFY_STORE=/data/trains.json`; CLAUDE.md repeats it | [`docker-compose.yml`](../../docker-compose.yml), `CLAUDE.md` | The documented Docker deployment points the store at a path with a JSON name — confusing at best |
| D-4 | **Command name drift**: the registered command is `trackify server`, but XML doc-comments on `TrackifyServer` and `ServerCommand` say `trackify serve` | [`Server/TrackifyServer.cs`](../../Source/Trackify.Cli/Server/TrackifyServer.cs), [`Commands/ServerCommand.cs`](../../Source/Trackify.Cli/Commands/ServerCommand.cs) | Readers (and future docs) copy the wrong command name |
| D-5 | **`SecondPage`/`SecondViewModel`** are leftover Uno template scaffolding still wired into the route table | `Presentation/Pages`, `App.xaml.cs` | Dead surface area in the navigation graph |
| D-6 | **Sensor actions are modelled but not executed** — `TrackSegment.Sensor`/`Action` are persisted and editable, but nothing acts on them against hardware | `Domain/Trains/TrackSegment.cs` | The UI implies a capability the system does not have |
| D-7 | **`TrainStateStore` is in-memory only** — speeds are lost on a server restart, so a client reconnecting after one sees an empty state until the next change | `Cli/Server/TrainStateStore.cs` | Minor; deliberate, but worth revisiting if state ever needs to survive |
| D-8 | **The `ILegoService` seam is minimal** — no sensor feedback, no port modes, no hub battery/status | `Application/Lego/ILegoService.cs` | Any richer LWP feature requires widening the interface across four implementations |
| D-9 | **Hand-written DTO mapping** — `Train` and `TrainDto` carry near-identical property lists that must be kept in sync manually | `Application/Trains/TrainMapping.cs` | A forgotten property silently fails to round-trip |
| D-10 | **MCP server is documented as planned but does not exist** — no `Mcp/` folder, no packages | CLAUDE.md, [issue #1](https://github.com/Ktechen/Trackify/issues/1) | Documentation describes a shape that is not code; see [ADR-16](09-architecture-decisions.md#adr-16-mcp-server-to-live-in-infrastructure), including its layering caveat |

## 11.3 Deliberate non-debt

Things that look like debt in review but are decisions, so they should not be "fixed" casually:

- **`AddTrackifyDomain()` registers nothing.** It exists so every composition root calls every layer
  ([ADR-01](09-architecture-decisions.md#adr-01-clean-architecture-with-per-layer-di-entry-points)).
- **Two different transport-selection mechanisms.** Neither can replace the other
  ([ADR-04](09-architecture-decisions.md#adr-04-two-transport-selection-styles-runtime-vs-compile-time)).
- **`catch (NullReferenceException)` in the connect path.** A bounded workaround for a specific
  upstream bug, not sloppy error handling
  ([ADR-14](09-architecture-decisions.md#adr-14-bounded-connect-retry-around-a-sharpbrick-null-deref)).
- **`Trackify.Application` builds different TFMs on different machines.** Required, with a RID guard
  that is load-bearing ([ADR-05](09-architecture-decisions.md#adr-05-host-conditioned-multi-targeting-of-trackifyapplication)).
- **Three swallowed-exception sites.** Each is narrow, commented, and justified
  ([§8.6](08-crosscutting-concepts.md#86-error-handling)).
- **German user-facing strings inside `SwitchingLegoService`.** A deliberate exception to
  "services are UI-neutral" for the two states only the user can fix
  ([ADR-09](09-architecture-decisions.md#adr-09-runtime-directserver-switch-instead-of-a-restart)).
- **The Console sink assembly passed explicitly to Serilog.** Required for single-file publishes;
  removing it silently disables logging on the Pi
  ([ADR-12](09-architecture-decisions.md#adr-12-source-generated-loggermessage-over-serilog)).
