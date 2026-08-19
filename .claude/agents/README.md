# Trackify agent library

Ten role agents modelling a full development team for this repository. They are grounded in
[`docs/arc42/`](../../docs/arc42/) (architecture, as built) and apply the
[req42](https://req42.de/) method for requirements work — there is deliberately **no `docs/req42/`**;
requirements are derived on demand from code + arc42 rather than kept as a separate artifact.

## Routing — which agent to reach for

| Agent | Reach for it when | Writes code? |
|---|---|---|
| `tech-lead` | A task spans layers or roles and you want it decomposed and routed | no |
| `requirements-engineer` | "Should we build X?", scope disputes, turning a wish into testable requirements | no |
| `architect` | Layer placement, a new ADR, anything touching `docs/arc42/` | docs only |
| `core-developer` | Domain / Application / Infrastructure: entities, ports, use-cases, EF Core | yes |
| `uno-ui-developer` | `Source/Trackify/Presentation/**` — XAML, MVVM, navigation, layout | yes |
| `ble-specialist` | LWP, SharpBrick, BlueZ, Plugin.BLE, discovery/connect/GATT behaviour | yes |
| `test-engineer` | New tests, arch-test rules, reproducing a bug as a failing test | yes |
| `devops-engineer` | CI workflows, Docker, Pi publish/systemd, packaging | yes |
| `code-reviewer` | Pre-PR review of a working diff against this repo's enforced conventions | no |
| `ux-copy-reviewer` | German UI wording, actionable error text, safe-area/a11y checks | no |

`tech-lead` is the only agent that spawns others. The rest are leaves — call them directly when you
already know the role.

## Shared invariants every agent is told

- **Dependencies point inward only:** `Domain ← Application ← Infrastructure ← front-ends`, enforced by
  [`Test/Trackify.Tests/Architecture/LayerTrainDependencyTests.cs`](../../Test/Trackify.Tests/Architecture/LayerTrainDependencyTests.cs)
  (six NetArchTest facts, run in `ci.yml`).
- **The build fails on any warning and on code-style violations** — `TreatWarningsAsErrors` +
  `EnforceCodeStyleInBuild` in `Directory.Build.props`; `IDE0130`/`IDE0161` are `error` in
  `.editorconfig` (lines 162–163), **not** in `Directory.Build.props`.
- **Package versions live only in `Directory.Packages.props`** (Central Package Management); Uno's
  version lives in `global.json`.
- **Language split:** app UI German; CLI output, code, comments and docs English.
- **Never fail silently** — log and return a failure, or rethrow. Exactly three deliberate swallow
  sites exist, each commented at the call site (arc42 §8.6); do not add a fourth.
- **Verification reality:** BLE needs a Raspberry Pi, iOS needs macOS, and the Uno Skia surface
  cannot be screenshotted in an agent environment. The dev-machine gate is arc42 §7.6.
- **ADRs are settled.** `docs/arc42/09-architecture-decisions.md` records ADR-01…ADR-16 so they are
  not re-opened by accident. ADR-16 (MCP) is the only one still *proposed*.

## Known documentation drift (as of 2026-08-19)

Agents are told to trust the code first, then arc42, then `CLAUDE.md` — because these conflict:

| Topic | Reality | Which doc is wrong |
|---|---|---|
| Does the Uno app reference `Trackify.Infrastructure`? | **Yes** — `Source/Trackify/Trackify.csproj` references Domain, Application *and* Infrastructure | `CLAUDE.md` ("only by the CLI", "must not reference it") |
| Is there a server/backend? | **Yes** — `Source/Trackify.Cli/Server/` (REST + SignalR), ADR-08/09 | `CLAUDE.md` ("there is no server/backend") and `README.md` |
| Arch test filename | `LayerTrainDependencyTests.cs` | `CLAUDE.md` (`LayerDependencyTests.cs`) |
| Per-project log class path | `Log.cs` at project root | `CLAUDE.md` (`Logging/Log.cs`) |
| SDK pin | `10.0.0` in `global.json` | arc42 TE-1 and §7.6, root `README.md`, a `ci.yml` comment (all say `9.0.100`) |
| Allowed code-behind files | **Two**: `MainPage.xaml.cs` *and* `SecondPage.xaml.cs` | arc42 SC-7 / §8.9 (say one) |
| Pure LWP addressing file | `Application/Lego/LwpAddressingMapping.cs` | arc42 §3.2 + ADR-02 and `CLAUDE.md` (say `LwpAddressing.cs`) |
| Workflow count | **Three**: `ci.yml`, `android-apk.yml`, `cli-arm64.yml` | `CLAUDE.md` (documents two); arc42 §7.6 also wrongly claims all provision .NET 8/9/10 — `android-apk.yml` provisions 9 + 10 only |

None of this drift is fixed by adding these agents. Fixing it is tracked separately.
