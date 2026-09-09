# Unity Project Context

## September 9, 2026 rules decision

[Game design](../GAME_DESIGN.md) now records the agreed rules. [Implementation checklist](../RULES_IMPLEMENTATION_PLAN.md) tracks pending code and acceptance work. Undo is required and leaves move count unchanged; the current intent history/count coupling must change. Flags accept matching Normal sources only, require explicit successful capture when targeted, and level validation requires exactly one Flag. Reached-path atomicity, stopping Rocks, and Ghost starting-Sigil clearing are explicit. This update documents decisions; it does not certify runtime implementation or test results.


## September 8, 2026 baseline update

The focused source review at commit `d2319d3` is recorded in
[Project baseline](../PROJECT_BASELINE.md). Use that report for current feature
status, rule differences, and known issues; the generated section below is an
August snapshot and has not been fully revalidated.

- Current roots are `Assets/_Game/Production` and `Assets/_Game/Prototype`.
- Ghost/Sigil rules, presentation, and tests exist in production.
- Core and Application declare no engine references, but both now also reference
  `Blobs.Debugging`; their older dependency inventory below is incomplete.
- Serialized Build Settings retain old scene paths and prototype scene GUIDs.
  Production `Scenes/Game.unity` is not listed.
- Several asset-based tests retain pre-reorganization paths. No test pass or
  player-build success was established in this review.
- Current user edits to production `Level_Ghost.asset`, `ViewCatalog.asset`, and
  `Game.unity` were preserved. Unity MCP is unavailable on this tool surface;
  Editor/Console state and device behavior remain unverified.

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `/Users/caleb/Dev/blobs`
- Last analyzed: 2026-08-15
- Last analyzed commit: `b0d1980`
- Production code is under `Assets/_Game`; `Assets/Scripts` is the legacy prototype/feel reference.

## Confirmed Environment

- Unity version: 6000.3.6f1 (`bbb010bdb8a3`)
- Render pipeline: Universal Render Pipeline 17.0.3 with the 2D feature set
- Input system: Input System package 1.18.0; Player Settings uses the new input backend
- Target platforms: native mobile and Web are documented targets; current Build Settings contain the legacy `Assets/Scenes/Menu.unity` and `Assets/Scenes/Blobs.unity` scenes

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.0.3 and Unity 2D feature package 2.0.1 | Confirmed | `Packages/manifest.json`, `ProjectSettings/GraphicsSettings.asset` |
| Input | Input System 1.18.0 | Confirmed | `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset` |
| Animation | DOTween performs tweening; UniTask awaits presentation playback and cancellation | Confirmed | `Assets/_Game/Scripts/Presentation/Blobs.Presentation.asmdef`, Presentation code |
| Testing | Unity Test Framework 1.4.5 with a first-party EditMode assembly | Confirmed | `Packages/manifest.json`, `Assets/_Game/Tests/EditMode/Blobs.Tests.EditMode.asmdef` |
| Networking | No first-party multiplayer implementation found | Confirmed | production assemblies and representative code |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/_Game/Scripts/Core` | deterministic Unity-free rules and board state | Confirmed | asmdef and source |
| `Assets/_Game/Scripts/Application` | session, commands, snapshots, restart | Confirmed | asmdef and source |
| `Assets/_Game/Scripts/Presentation` | Unity board/blob/tile views and feedback | Confirmed | asmdef and source |
| `Assets/_Game/Scripts/Content` | authored assets mapped into Core definitions | Confirmed | asmdef and source |
| `Assets/_Game/Tests/EditMode` | production EditMode tests | Confirmed | test asmdef and test sources |
| `Assets/Scripts` | legacy prototype | Confirmed | `README.md`, `Docs/TECHNICAL_ARCHITECTURE.md` |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `Blobs.Core` | board state and deterministic rules | none | no engine references |
| `Blobs.Application` | playable session | Core | no engine references |
| `Blobs.Content` | Unity authoring adapters | Core | Unity-facing |
| `Blobs.Input` | input translation | Application, Core, Input System | Unity-facing |
| `Blobs.Presentation` | views, animation, feedback | Core, Application, Content, DOTween | correct home for board-surface rendering |
| `Blobs.UI` | HUD and screen UI | Application, UI/TMP | presentation-independent gameplay API |

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/Menu.unity`, then `Assets/Scenes/Blobs.unity` (legacy shell)
- Likely production startup scene: `Assets/_Game/Scenes/Game.unity` through `GameBootstrapper`, but it is not currently enabled in Build Settings
- Scene loading flow: legacy build flow is authoritative for builds; the production scene composes `GameSession`, input, board Presentation, and UI through `GameBootstrapper`

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Layered deterministic core | Core owns rules; Unity layers consume outcomes | Confirmed | `README.md`, `Docs/TECHNICAL_ARCHITECTURE.md`, asmdefs |
| Composition root | `GameBootstrapper` wires the production scene | Confirmed | `GameBootstrapper.cs` |
| Composed MVP-style Presentation | `BoardPresenter` observes snapshots and delegates effect/step composition to registered handlers and explicit timelines; `BlobPresenter` owns blob view lifecycle while focused collaborators own transitions and interaction choreography | Confirmed | Presentation presenter scripts |
| Composed impact feedback | `MergeAnimationOrchestrator` emits a generic impact context to independent audio, VFX, haptics, camera, or future feedback components | Confirmed | Presentation feedback scripts |
| ScriptableObject authoring | levels, themes, and view catalogs are authored assets | Confirmed | Content scripts/assets |

## Coding Conventions

- Namespace style: `Blobs.<Layer>` for production code
- Serialized fields: private `[SerializeField]` fields; some files place the attribute on a separate line
- Async: UniTask is installed; Presentation uses DOTween sequences with UniTask playback orchestration
- Comments/docs: XML summaries on public contracts and comments for non-obvious presentation ordering

## Testing And Validation

- EditMode tests: present under `Assets/_Game/Tests/EditMode`
- PlayMode presentation tests: present under `Assets/_Game/Tests/PlayMode`; cover move-step
  sequencing, interruption cleanup, runtime prefab composition, and scene-composition startup
- CI/build validation: no repository CI workflow found
- Local Unity executable/editor connection: unavailable from the current tool surface; use batchmode if an executable becomes available

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity MCP/editor inspection | unavailable | no Unity MCP tools exposed in this task |
| Repository/static inspection | available | shared workspace and shell |
| Unity batchmode executable | unverified | expected 6000.3.6f1 executable was not discoverable from the sandbox |

## Important Constraints

- Preserve the dependency direction documented in `Docs/TECHNICAL_ARCHITECTURE.md`.
- Do not grow production behavior out of legacy `Assets/Scripts` types.
- Presentation timing and rendering must not decide or mutate gameplay rules.
- The current worktree contains user edits to the production Game scene, blob catalog, and a UI font asset; preserve them.

## Unknowns And Confidence

- The active Editor scene, Console baseline, and active build target are unknown because no connected Unity tooling is available.
- The production scene is not in serialized Build Settings, so production runtime validation requires an explicit Editor scene launch or later build-settings decision.

## Source Files Inspected

- `README.md`
- `Docs/TECHNICAL_ARCHITECTURE.md`
- `Docs/PRODUCTION_PLAN.md`
- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- production asmdefs, `GameBootstrapper.cs`, `BoardPresenter.cs`, `TileView.cs`, Core board/level types, representative EditMode tests and authored level assets

<!-- unity-onboarding:generated:end -->
