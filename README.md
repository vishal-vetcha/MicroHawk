# MicroHawk

MicroHawk now has a physical drone-flight, deterministic-safety and telemetry foundation in the approved industrial facility. No AI or perception is implemented.

## Run the flight demonstration

Use **Unity 6000.6.0f1** (`f7f8ed4d1e24`). Add `simulator/unity/MicroHawkSim` in Unity Hub, open `Assets/MicroHawk/Scenes/IndustrialTest.unity`, and press Play.

Use the right-hand operations panel: **RESET → ARM → TAKEOFF 4m → wait for Hovering → Inspection wall stand-off → wait for Hovering → HOLD → RETURN HOME**. Return Home climbs, travels home and lands automatically. Wait for **Landed / Armed false**. After Reset, allow brief physical settling before Arm.

Use **1 / 2 / Tab** or the camera buttons for overview/follow. Land performs a direct descent only over a configured landing pad. Destination buttons submit ordinary typed MoveTo commands and may reject if their route is blocked. Propeller meshes rotate while armed. Tune flight, safety and battery settings on the existing SimulationConfiguration object; reset to apply changes.

The Windows build, when generated, is `artifacts/build/MicroHawk.exe`. It runs locally without Python or an LLM. See [final verification](docs/flight-verification.md) for actual build/test results and limitations.

## Architecture and functionality

Typed Arm, Disarm, Takeoff, MoveTo, Hold, Land and ReturnHome commands enter one bounded queue and deterministic Safety Engine. Only an internally authorized action reaches flight execution/control. The Unity adapter alone applies physical forces/torques or restores the body on local reset. Presentation uses immutable telemetry. The scene and prefab were preserved, not regenerated.

Implemented: explicit flight states; bounded position/attitude control; validated routes/geofence/altitude/speed; simulated battery consumption; low-battery RTH and critical emergency landing; 10 Hz telemetry and execution events; operations panel; deterministic reset and flight tests. The industrial props, authored cracks, workers and vehicles remain scene content, not detections.

Versions remain URP **17.6.0**, Unity Test Framework **1.8.0**, with all resolved dependencies locked. This pass adds no packages or external assets.

## Tests, captures and build

Close other editor instances using this project. From the repository root in PowerShell:

```powershell
.\tools\run-unity.ps1 -Action EditMode
.\tools\run-unity.ps1 -Action PlayMode
.\tools\run-unity.ps1 -Action PlayModeVisual
.\tools\run-unity.ps1 -Action Build
```

The GPU PlayMode suite captures the physically executed demonstration to `artifacts/visuals/flight-*.png` and writes `flight-telemetry.csv`. Tests use the same fixed-step host and typed command gateway as UI. These are camera renders of real simulated states, not generated illustrations. Logs/XML/builds/captures are ignored local artifacts. Do not run the old Author/rebuild command during incremental development: it replaces authored scene content.

## Documentation

- [Flight architecture, states, safety, battery, telemetry and limitations](docs/flight-foundation.md)
- [Final verification and module inventory](docs/flight-verification.md)
- [Development invariants](AGENTS.md)
- [Original architecture and roadmap](docs/architecture-plan.md)
- [Physical-flight decision](docs/decisions/0002-physical-flight-foundation.md)
- [Visual brief](docs/m1-visual-brief.md) and [historical bootstrap report](docs/bootstrap-status.md)

This pass stops after the flight-foundation report. Next, following review, add schema-validated Python transport and deterministic mission sequencing. No Python/LLM/voice/perception work starts automatically. No commit or push is authorized.
