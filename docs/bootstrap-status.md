# MicroHawk visual/bootstrap status

Historical bootstrap report: this result was subsequently approved by the operator. Physical flight is now implemented and verified; see [current verification](flight-verification.md). The sections below describe the earlier visual-only checkpoint.

## Meaningful files and directories

| Path | Contents |
| --- | --- |
| `simulator/unity/MicroHawkSim/Assets/MicroHawk/Scenes/IndustrialTest.unity` | Actual saved industrial scene, authored geometry, lights, cameras, home/spawn and registry references. |
| `Assets/MicroHawk/Prefabs/MicroHawkDrone.prefab` (inside project) | Reusable four-rotor drone with composite geometry, compound colliders and dynamic Rigidbody. |
| `Assets/MicroHawk/Materials/` | Locally authored industrial palette and depth-tested signage material. |
| `Assets/MicroHawk/Shaders/` | Small URP signage shader; text respects scene depth instead of drawing through objects. |
| `Assets/MicroHawk/Settings/` | URP pipeline, renderer and generated render-pipeline settings. |
| `Assets/MicroHawk/Scripts/Contracts/` | Finite ENU position value type; no Unity dependency. |
| `Assets/MicroHawk/Scripts/World/` | Validated stable IDs and scene-scoped registry, explicitly simulator ground truth. |
| `Assets/MicroHawk/Scripts/Simulation/` | Coordinate conversion and read-only home/drone/configuration references; no physical actuation. |
| `Assets/MicroHawk/Scripts/Presentation/` | Overview/follow camera switching, preview overlay and dynamic signage-atlas binding. |
| `Assets/MicroHawk/Scripts/Safety/`, `Flight/` | Requested assembly boundaries with descriptive assembly metadata only. No safety/controller behavior is claimed. |
| `Assets/MicroHawk/Editor/` | Five cohesive editor-only authoring files for geometry/material helpers, facility areas, static props, drone prefab and scene setup/capture. Authoring is explicit, never automatic on import/Play. |
| `Assets/MicroHawk/Tests/EditMode/`, `PlayMode/` | Coordinate, registry, saved-scene/prefab tests and a real dynamic settling/camera-switch test. |
| `Packages/`, `ProjectSettings/` | Pinned manifest and resolved lock, exact editor revision, text serialization, visible metadata, scene build list, linear URP rendering and fixed timestep settings. |
| `tools/run-unity.ps1` | Pinned-editor authoring/test/capture helper with exit-code and result checking. |
| `README.md`, `AGENTS.md`, `docs/` | Open/Play/camera instructions, preserved invariants, current authorization, version clarification and verification report. |

All imported assets and folders retain Unity `.meta` files. Generated caches, logs and captures remain ignored under `.gitignore`.

## Verification record

Initial Unity import and scene authoring ran successfully. The first EditMode suite passed 11/11. The first PlayMode run failed because the primitive cylinder pad used a rounded capsule collider: the drone slid on it. Static cylinders now use mesh collision geometry with genuinely flat pad tops; the following PlayMode run passed 1/1. No constraints, teleportation or hidden stabilizer were added to hide that failure.

The first rendered views revealed depth-ignoring font material. A locally authored depth-tested signage shader fixed text appearing through the wall/drone. GPU-enabled PlayMode also passed. Headless PlayMode did not produce requested screen captures, so that optional capture path and its unused built-in module were removed. Supplied images are editor camera renders, not proof of manual button/keyboard interaction or the Play-mode overlay. Final run counts and results are appended below after execution completes.

## Versions and limitations

- Unity 6000.6.0f1 (`f7f8ed4d1e24`), Hub 3.21.1. Installed version was disclosed and pinned; earlier documentation proposed 6000.3.23f1.
- URP/Core/Shader Graph/URP Config 17.6.0; Test Framework 1.8.0. Full dependencies and sources are in the lockfile.
- No third-party assets, Python runtime, AI, detection, tracking, dwell logic, mission execution, flight controls, telemetry API, reset controller or network endpoint.
- Workers/vehicles are static visual props. Crack lines are authored visual markers. Camera housings/status lights are geometry, not functioning sensors or status telemetry.
- Normal Unity fixed-step gravity is used only for passive settling. The later flight host must implement the approved explicit fixed-step control/adapter boundary.
- No standalone player build or cross-machine performance/determinism validation is claimed in this pass.
- Unit counts do not prove visual acceptance; representative rendered views are supplied for operator review.

## Open the result

Add `simulator/unity/MicroHawkSim` to Unity Hub and use 6000.6.0f1. Open `Assets/MicroHawk/Scenes/IndustrialTest.unity`, press Play, then click the Game view. Use buttons, 1/2 or Tab for the cameras. Stop Play to return to the saved authoring state. Do not use scene rebuild to preserve manual scene edits; rebuild deliberately replaces the generated facility/prefab/palette.

Next recommended pass, after explicit approval: typed command and authorization contracts, deterministic safety/state transitions, controller/adapter ownership, fixed-step host and takeoff/hold tests. No such work starts automatically.


## Final executed results

- Final EditMode: **11 total, 11 passed, 0 failed**, Unity exit 0; `artifacts/EditMode.xml` and `.log`.
- Final GPU-enabled PlayMode: **1 total, 1 passed, 0 failed**, Unity exit 0; `artifacts/PlayModeVisual.xml` and `.log`. This covers passive physical settling and camera switching without moving the drone.
- Headless PlayMode after the collider correction also passed 1/1; its result remains in `artifacts/PlayMode.xml`.
- Scene authoring and camera rendering exited 0. Inspected `artifacts/visuals/overview.png`, `drone.png` and `inspection.png`. These are editor camera renders; manual keyboard/button/overlay review remains for the operator.
- Final source compilation has no C# warnings or errors in the final test logs. Deprecated Unity 6.6 object lookups were replaced. All authored assets/folders have `.meta` companions with no duplicate GUIDs.
- Unity logs a non-blocking licensing access-token/signature diagnostic, then successfully resolves the installed Personal entitlement and runs. Headless import also reported an unsupported conservative-rasterization shader. No licensing credentials are stored in the repository; raw logs stay ignored.
- The package resolver selected **Searcher 4.9.5**, newer than the initial dependency minimum 4.9.3 disclosed before import. The actual full graph is pinned in `packages-lock.json`. No unrequested art/feature package was installed.
- No standalone executable was built. No performance, hardware-flight or cross-platform determinism claim is made.
- Git: unborn `main`; foundation, docs, simulator and tooling are untracked. No staging, commit or push performed. Unity caches and local artifacts are ignored.

