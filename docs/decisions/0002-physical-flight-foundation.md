# ADR 0002: Physical flight foundation on the approved scene

Status: accepted scope from the operator's physical-flight pass request; implemented incrementally.

The approved scene and drone prefab are preserved byte-for-byte. SceneConfiguration composes flight services on Play, using existing references. No environment regeneration or scene replacement is performed. The existing authoring tool remains available but is not invoked by this pass.

Battery consumption and runtime battery failsafes are explicitly brought forward from the original M3 roadmap into this pass. Python, networking, LLMs, perception, voice and external flight integrations remain deferred.

The command path is IFlightOperations -> bounded FIFO -> deterministic SafetyEngine -> internally constructed AuthorizedFlightAction -> FlightExecution/PositionController -> ISimulatorAdapter -> UnitySimulatorAdapter -> Rigidbody forces and torques. Presentation references Contracts only. Contracts, Safety and Flight have no UnityEngine references. Simulation is the composition root and owns the physics clock. World remains ground truth, separate from future observations.

The adapter applies collective acceleration along the drone's actual body-up axis, and bounded angular acceleration. This is equivalent to mass-scaled thrust and inertial attitude actuation; it is not a per-motor aerodynamic/electrical model. Position and attitude feedback remain pure C#. ENU uses right-handed vector math. Unity's axis exchange has negative determinant, so axial angular vectors require a sign inversion in addition to swapping north/up. Heading is ENU radians, east-zero/positive-to-north.

Normal flight never sets position, rotation or velocity. Local reset restores those through the adapter only. ISimulationReset is not an autonomous command capability. Cosmetic propeller children and observer cameras may change their own transforms.

The existing MonoBehaviour fixed tick drives one explicit Physics.Simulate(0.02) call with automatic physics disabled. Test stepping calls that identical tick. Telemetry publishes every five ticks (10 Hz); command outcomes publish at their transition tick. Runtime routes are checked before actuation and include a configured attitude-response allowance in braking clearance.

Normal Land requires proximity to a registered pad and a flat clear footprint. Emergency landing may use another flat physical surface in place; if no safe vertical landing footprint exists, Emergency holds with an explicit failure reason. This bounded foundation does not promise general path planning, collision avoidance in arbitrary dynamic scenes, or actual-battery power-loss physics.

Future hardware adapters should implement a capability-aware execution endpoint exposing the same typed operations. PX4 owns stabilization in that endpoint; do not pretend its API maps directly to Unity forces. No hardware implementation is included or certified here.
