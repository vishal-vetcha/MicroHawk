# Physical flight foundation — verification report

Verified locally on 2026-09-09 with Unity 6000.6.0f1 (f7f8ed4d1e24), Windows 11, URP 17.6.0 and Test Framework 1.8.0. This completes the requested physical-flight implementation pass. Python, AI, voice, perception and external flight integrations remain deferred.

## Executed checks

| Check | Actual result |
|---|---|
| Unity EditMode | 37 passed, 0 failed (`artifacts/EditMode.xml`) |
| Unity GPU-enabled PlayMode | 10 passed, 0 failed (`artifacts/PlayModeVisual.xml`) |
| Windows x64 player build | Succeeded, Unity exit 0 (`artifacts/build/MicroHawk.exe`) |
| Built player startup | Remained running for a 10-second hidden headless smoke check; owned test process then stopped |
| Scene and prefab preservation | SHA-256 unchanged from the pre-pass baseline |
| Git | Unborn main; repository files remain untracked; no commit or push |

EditMode covers contracts, ENU conversion, scene configuration, state transitions, invalid/nonfinite commands, altitude/geofence/speed rejection, battery thresholds, blocked routes, authorized control inputs and bounded/deterministic control calculations. PlayMode covers the existing passive scene check plus nine physical-flight tests: full flight demonstration, ordinary landing and illegal commands, blocked target/stale session, low-battery RTH, critical emergency landing, ten reset repetitions, telemetry cadence, runtime obstacle braking, and reset during landing.

Physical assertions include hover altitude within 0.2 m and speed below 0.2 m/s, MoveTo arrival within 0.25 m, home touchdown within 0.2 m, ten reset/takeoff positions within 0.05 m, contact-confirmed landing, and a 25-second limit for direct descent from 4 m. Runtime obstacle testing checks braking travel against the configured stopping/reaction envelope. These are controlled-scenario guarantees on this editor/platform, not universal collision avoidance or bitwise PhysX determinism.

## Physically executed demonstration

The GPU PlayMode run loaded the preserved IndustrialTest scene and executed Reset → Arm → Takeoff 4 m → Hover → MoveTo inspection-wall stand-off → timed Hold → ReturnHome → automatic Land. Commands entered the production typed gateway and safety engine; tests advanced the same fixed-step runtime that Play mode uses.

The 10 Hz CSV ends at simulation tick 3775 / 75.50 seconds in Landed, with zero reported speed, ENU position (-20, -15, 0.525) m and 91.663% battery. The recorded states include TakingOff, Hovering, Flying, Holding, ReturningHome and Landing. The home spawn is 0.535 m high; its deliberate 1 cm settling gap explains the final -0.01 m relative altitude. RTH completed with a Touchdown outcome and armed=false. Landing occupied approximately 17 seconds of the demonstration trace.

Actual camera renders were generated and inspected:

- `artifacts/visuals/flight-hover.png`
- `artifacts/visuals/flight-inspection.png`
- `artifacts/visuals/flight-return-descent.png`
- `artifacts/visuals/flight-landed.png`
- Recorded telemetry: `artifacts/visuals/flight-telemetry.csv`

The screenshots show real simulated body positions, not authored flight poses. Camera-only renders exclude the OnGUI panel. Manual button clicking and a visible standalone-player UI walkthrough were not performed; panel code compiles and submits the same typed commands tested above. The headless player smoke check verifies startup only, not rendering.

## Modules and files

All Unity source paths below are relative to `simulator/unity/MicroHawkSim/Assets/MicroHawk`.

| Area | Created or updated |
|---|---|
| Contracts | `Scripts/Contracts/FlightVector.cs`, `FlightCommands.cs`, `FlightSnapshots.cs`: typed actions/outcomes, SI/ENU values, immutable telemetry, operations/reset/adapter interfaces |
| Safety | `Scripts/Safety/FlightStateMachine.cs`, `SafetyEngine.cs`: explicit transitions, configurable limits, internally authorized actions, route validation and runtime overrides |
| Flight | `Scripts/Flight/FlightTuning.cs`, `PositionController.cs`, `FlightExecution.cs`: deterministic battery, bounded force/attitude control, completion/timeout and RTH sequencing |
| Simulation | `Scripts/Simulation/UnitySimulatorAdapter.cs`, `DroneContacts.cs`, `TelemetryPublisher.cs`, `FlightRuntime.cs`; updated `SceneConfiguration.cs`: composition, physical actuation, fixed stepping, command queue, reset and telemetry |
| Presentation | `Scripts/Presentation/OperationsPanel.cs`, `RotorFeedback.cs`; updated `FacilityCameras.cs`: telemetry-driven panel/cosmetics and follow view |
| Tests | `Tests/EditMode/FlightPolicyTests.cs`, `Tests/PlayMode/PhysicalFlightTests.cs`, test assembly references |
| Build | `Editor/FlightDemoBuild.cs`, updated `tools/run-unity.ps1` with Build action |
| Documentation | Updated AGENTS.md, README, architecture plan, visual brief and bootstrap status; new flight-foundation.md, this report and ADR 0002 |

Assembly boundaries/references and Unity .meta files accompany source changes. No packages, third-party assets or services were installed. The World module retains its ground-truth role and supplies no perception observations.

Preserved SHA-256 values:

- IndustrialTest.unity: `D6A433D50422985B0F247728874746238D556F8E0B742BC48319C0EB369C3A18`
- MicroHawkDrone.prefab: `28938952B4D98A103968C1A3A11D02063E0DAA9602BAA5A1822A45BEFB6A0947`

## Unity diagnostics

Final test/build logs contain no C# compiler warnings or test failures. Unity logs Licensing Client signature validation/code 10 and an unavailable access token, then successfully resolves the Unity Personal entitlement and proceeds. Build shutdown logs Curl error 42 (callback aborted) and lingering worker sessions, while returning exit code 0. The standalone headless smoke log reports disabled hardware video decoding with the Null graphics device and an internal allocation older than four frames (age 8); no managed exception was logged. This engine diagnostic remains a limitation of the smoke result, not a claimed clean interactive-player validation.

## Remaining limits and next pass

Flight uses body-axis thrust/attitude torque, not per-motor aerodynamics. Battery is an explainable percentage model; zero percent does not cut electrical power. Safety rejects blocked direct routes rather than finding alternate paths. Emergency landing requires a physically clear flat footprint; an obstructed route may force a reported hold. No safe-landing or collision-avoidance guarantee is made for arbitrary disturbances/geometry. Reset repeatability is tested locally within tolerances.

Workers, vehicles, cracks and restricted zones are still scene content. Destination buttons are configured positions, not semantic inspection. No detections, tracking, dwell events, autonomous reasoning or captured inspection evidence are claimed by this pass's flight renders.

The exact next pass, after review, is Python autonomous-mission integration without an LLM: freeze versioned JSON action/telemetry/outcome schemas; add Pydantic validation and shared C#/Python positive/negative fixtures; implement a loopback transport through IFlightOperations; stream telemetry and terminal outcomes; execute a deterministic multi-action mission client. Cover malformed payloads, stale/reconnected sessions, duplicates, timeouts and safety rejections before introducing agent planning. Reset must stay a local administrative capability. Stop here; do not begin that pass or commit/push automatically.
