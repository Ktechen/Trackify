# Trackify — Architecture Documentation (arc42)

Architecture documentation for **Trackify** following the [arc42](https://arc42.de/overview/) template
(© Dr. Peter Hruschka & Dr. Gernot Starke, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)).

> **Scope & language.** This describes the architecture *as built* — every statement is traceable to
> code, csproj, workflow or config in this repository. Written in English to match the code comments,
> the CLI and the primary README (the *app UI* is German; see [§2](02-architecture-constraints.md)).

## Sections

| # | Section | What you'll find |
|---|---|---|
| 1 | [Introduction and Goals](01-introduction-and-goals.md) | What Trackify does, the top-5 quality goals, stakeholders |
| 2 | [Architecture Constraints](02-architecture-constraints.md) | Hardware, protocol, platform, and self-imposed rules |
| 3 | [Context and Scope](03-context-and-scope.md) | System boundary, external partners, technical interfaces |
| 4 | [Solution Strategy](04-solution-strategy.md) | The five decisions that shape everything else |
| 5 | [Building Block View](05-building-block-view.md) | Level 1 (layers) → Level 2 (per-project) → Level 3 (transports) |
| 6 | [Runtime View](06-runtime-view.md) | Discover, drive local, drive over the network, auto-pilot, shutdown |
| 7 | [Deployment View](07-deployment-view.md) | Phone/desktop, Raspberry Pi (bare + Docker), CI/CD |
| 8 | [Crosscutting Concepts](08-crosscutting-concepts.md) | Domain model, DI, DTO boundary, logging, errors, persistence, config |
| 9 | [Architecture Decisions](09-architecture-decisions.md) | ADR-01 … ADR-15 with context and consequences |
| 10 | [Quality Requirements](10-quality-requirements.md) | Quality tree + evaluation scenarios |
| 11 | [Risks and Technical Debt](11-risks-and-technical-debt.md) | Known risks, accepted advisories, debt with pointers |
| 12 | [Glossary](12-glossary.md) | Domain and technical terms |

## Diagram conventions

Diagrams are [Mermaid](https://mermaid.js.org) so they render on GitHub and stay diffable.

- **Solid arrow** = compile-time dependency or synchronous call.
- **Dashed arrow** = runtime-selected binding (DI) or network hop.
- Colours are not semantic; the label carries the meaning.

## Keeping this current

The architecture is *enforced*, not just described — so most drift breaks the build first:

- Layer dependency rules → [`Test/Trackify.Tests/Architecture/LayerTrainDependencyTests.cs`](../../Test/Trackify.Tests/Architecture/LayerTrainDependencyTests.cs)
  (NetArchTest, runs in CI).
- Namespace/folder + file-scoped namespaces → `IDE0130`/`IDE0161` as **errors** in
  [`Directory.Build.props`](../../Directory.Build.props).
- Package versions → [`Directory.Packages.props`](../../Directory.Packages.props) (Central Package Management).

When one of those changes, update the affected section here in the same PR. Sections most likely to
go stale: [§5](05-building-block-view.md), [§7](07-deployment-view.md), [§9](09-architecture-decisions.md),
[§11](11-risks-and-technical-debt.md).
