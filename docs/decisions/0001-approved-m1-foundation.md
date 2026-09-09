# ADR 0001: Approved M1 foundation and visual scope

Status: accepted by operator on 2026-09-09.

## Decision

- Preserve Operator -> Intent Agent -> Mission Planner -> typed/schema-validated actions -> deterministic Safety Engine -> Flight Controller -> Simulator Adapter -> Drone. AI never manipulates the drone or controller. M1 uses a local typed UI in place of future intent/planning.
- Keep physics dynamic and plausibly quadcopter-like, with bounded thrust/torque and fixed-step deterministic supervisory/control logic. The adapter owns actuation. Reset is a separate local lifecycle operation. Test logical determinism and physical replay/reset within declared tolerances, without cross-platform bitwise claims.
- Retain ENU coordinates, explicit units and orientation basis conversion, pure safety/control boundaries, capability-aware simulator abstraction and EditMode/PlayMode verification from the architecture plan.
- Expand M1 to the entire polished static industrial facility in `docs/m1-visual-brief.md`. Visual and engineering acceptance both apply.
- Separate simulator metadata from future sensor-based perception. No scene prop implies detection, tracking or semantic autonomy.
- Keep Python communication and every AI/perception capability deferred to later milestones. The versioned loopback WebSocket direction is documented for M2, not implemented in M1.

## Execution checkpoint

The operator requested tooling inspection and repository foundation/documentation, then a stop before Unity project creation. No software/package/asset installation or download occurs in this checkpoint. Disclose prerequisites first. Commits and pushes remain unauthorized.

## Consequences

The initial scene requires meaningful authored visual work without adding detection behavior. The simulator stays independently runnable once built. Visual review cannot replace dynamics tests; dynamics tests cannot replace visual review. External flight integration will preserve high-level contracts but still needs hardware-specific execution and safety validation.

## Subsequent visual/bootstrap authorization

The operator closed the tooling checkpoint and authorized the first runnable visual/bootstrap scene, prefab, cameras, registry and initial tests. Flight control requires explicit approval of that result. The installed editor is 6000.6.0f1 (f7f8ed4d1e24), differing from the original 6000.3.23f1 proposal; this was disclosed before project implementation and pinned to follow the instruction to use the installed editor.

During this pass the dynamic drone settles on the pad through Unity's normal fixed-step physics. There is no active flight host yet. Explicit manually stepped physics and authorized controller actuation remain requirements for the subsequent flight pass. Editor-only geometry/scene authoring may position the initial prefab; runtime camera scripts only move cameras.
