# MicroHawk development rules

## Status and approval gate

The operator approved the architecture and implementation plan for Milestone 1 on 2026-09-09, with expanded visual scene content. See `docs/architecture-plan.md`. All safety, physics, coordinate, testing and abstraction requirements remain in force. Later milestones are not authorized. Do not commit or push until explicitly authorized; architecture approval alone is not a request to commit or push.

Current approved pass: complete physical flight, typed commands, deterministic safety/state transitions, battery/failsafes, route validation, telemetry, operations panel, reset and tests. The visual/bootstrap result is approved. Preserve the existing IndustrialTest scene and prefab; do not regenerate the environment. Battery work is explicitly authorized now despite its earlier M3 placement. Use installed/pinned Unity 6000.6.0f1. No Python, AI, perception, voice, external flight integration or new milestone work. Stop after verification and report; no commit/push.

The first implementation milestone is **Interactive 3D MicroHawk simulation foundation**. Its scope is a Unity industrial test environment, visible quadcopter, takeoff, target-position movement, landing, return-to-home, basic state, telemetry, deterministic reset, and a typed simulator interface for a later Python connection.

## Approved visual scope and observation boundary

- M1 must look like a polished autonomous-drone industrial test facility. Visual quality and functional engineering are both acceptance requirements.
- Include the MicroHawk home base/pad, warehouse, multiple loading bays, clearly marked/signed Restricted Zone A, security gate, structural inspection wall, several authored crack/damage markers, static workers/mannequins, static vehicles or forklifts, pipes/tanks/equipment, obstacle/navigation section, secondary landing/inspection pads and numbered inspection markers.
- These are scene content only. Do not claim perception, detection, tracking, temporal dwell detection or autonomous semantic understanding. Keep workers and vehicles static in M1.
- Keep simulator ground-truth/world metadata separate from future perception observation types and data paths. Metadata may support deterministic geometry/safety, configured landing sites and isolated evaluation labels; it must never be relabeled or secretly consumed as a sensor detection.
- Future perception must infer relevant facts from sensor inputs and preserve provenance. M1 labels and UI must describe props/world configuration honestly, never as detected entities.
- Author scene content locally using meshes, materials and signage. No third-party assets are required by default. Damage can use authored surface geometry/material markers; a decal plugin is not required.

## Non-negotiable control boundary

Operator request -> Intent Agent -> Mission Planner -> structured typed mission/actions -> deterministic Safety Engine -> Flight Controller -> Simulator Adapter -> Drone.

- No LLM, VLM, AI agent, prompt, model tool, or generated script may directly access drone physics, transforms, motors, actuator APIs, or the flight controller. AI emits data, never executable control code.
- Validate every AI output against a versioned schema before use. Schema validity is necessary but does not imply safety. Reject unknown action types, extra fields, invalid units/frames, nonfinite numbers, and out-of-range values.
- All command sources, including local UI, tests exercising production entry points, and future network ingress, enter through the same typed validation and deterministic safety boundary. Do not add debug movement shortcuts that bypass it.
- The safety engine owns acceptance, rejection, runtime constraints, and deterministic override decisions. Revalidate against current state at dispatch and monitor execution. An accepted command is not perpetual permission.
- The flight controller consumes only internally authorized actions. Only the concrete simulator adapter may apply flight forces/torques to the drone. Presentation scripts may animate cosmetic child meshes but never drive the physical body.
- Reset is a separate local simulation lifecycle operation, not a mission action or AI capability. Quiesce execution before the reset service restores state through the adapter. Never expose teleport/reset to autonomous mission tools.
- Future PX4/FlytBase integrations must preserve this boundary through capability-aware adapters. Do not imply that simulation control is certified or directly suitable for hardware.

## Scope and engineering

- Milestone 1 is Unity 6/C# only. Do not implement or install LLM agents, LangGraph, voice, computer vision, RAG, FlytBase, or cloud services. Python/network runtime work starts in later milestones.
- Keep Unity simulation and Python autonomy in separate monorepo roots. Transport and simulator types must not leak into AI domain logic.
- Prefer small cohesive modules, immutable command/state snapshots, explicit ownership, assembly boundaries, and constructor injection for pure services. Use a small composition root rather than a dependency-injection framework.
- No premature microservices, generic plugin frameworks, speculative skeletons, giant manager files, or fake feature implementations. Add interfaces when an actual boundary needs them.
- Mission plans, policy results, telemetry, and events must carry identifiers, schema versions, explicit SI units, coordinate frames, and simulation time where applicable. Distinguish observed evidence, inference, and simulator ground truth.
- Preserve facility-local ENU: x=east, y=north, z=up, meters; m/s velocities; radians; yaw zero=east, positive toward north. Unity positions map to (east, up, north); convert orientations by explicit basis transformation with tests.
- Drive control and policy from a fixed tick and injected simulation clock. Seed randomness; fix ordering. Never use render delta time or wall-clock time for mission dwell timers/control.
- Deterministic control/reset does not guarantee bitwise PhysX reproducibility across machines or versions. Declare the supported replay environment and numeric tolerances; test the claim actually made.
- Bound queues and execution times. Define cancellation, duplicate handling, rejection reasons, and command terminal states. Failure or missing state must not silently grant permission.
- Obstacle checks must consider swept drone volume and stopping clearance. Return-to-home must validate each segment; never assume a straight route is safe.

## Verification and repository hygiene

- Use Unity EditMode tests for pure contracts/policy/control calculations and PlayMode tests for physics, scene integration, lifecycle, and reset. Later use pytest and shared contract fixtures for Python/C# interoperability.
- Test meaningful safety failures and end-to-end behavior as well as success. Run the checks appropriate to the change; state exactly what ran and what could not run. Never report visual verification, a Unity build, or tests as passed without executing them.
- Pin the Unity editor patch and package versions. Retain `.meta` files, text-serialized scenes/prefabs, project settings, and package locks. Exclude generated caches, builds, local environments, secrets, and runtime captures. Avoid paid or unlicensed assets; document asset provenance.
- Introduce Python 3.11+ with one pinned development interpreter, Pydantic, pytest, and a dependency lock only when the backend milestone begins. Add FastAPI, LangGraph, Ollama, OpenCV, and speech dependencies only when their approved milestone needs them.
- Keep setup/run/test instructions and architectural decisions accurate. Label proposed, implemented, and verified behavior separately. Complete one approved milestone and demonstrate its acceptance criteria before expanding scope.
- Follow the operator's latest explicit instructions. Surface any requested change that conflicts with a safety invariant before changing that invariant.


