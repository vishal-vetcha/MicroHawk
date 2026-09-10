# Execution adapter boundary

`microhawk.transport.endpoint.FlightEndpoint` is the mission executor's structural interface. `SimulatorLink` implements it over the current versioned Unity WebSocket endpoint. The model never receives this interface; it only emits validated intent data. The deterministic executor owns calls.

| Domain action | Current Unity execution | Future FlytBase/PX4 endpoint responsibility |
|---|---|---|
| Arm / Disarm | Typed state admission | Check actual platform capabilities and arming semantics |
| Takeoff | Altitude above recorded home body reference | Convert the documented frame/reference; supervise autopilot takeoff |
| MoveTo | ENU absolute position, validated direct route | Convert facility ENU to the hardware navigation frame and enforce route/geofence policies |
| Hold | Brake/hold through Unity safety | Use supported autopilot hold with equivalent terminal semantics |
| ReturnHome | Validated climb, transit and contact-confirmed landing | Map supported RTH behavior; verify landing from authoritative telemetry |
| Land | Registered pad / critical emergency policy | Apply hardware-specific landing suitability and emergency policy |
| Telemetry/outcomes | 10 Hz snapshots and measured terminal events | Publish measured state, battery, session, safety overrides and actual completion |
| Camera | Gimballed Unity RGB rendering | Real sensor source with timestamp, calibration and provenance |

No FlytBase SDK, credentials, endpoint URL or real-aircraft API is configured or tested. There is no functioning FlytBaseAdapter. Replacing Unity requires a real execution endpoint with hardware-specific supervisory safety and capability negotiation; it is not a mapping from force calls to invented API methods. Mission/intent/perception layers stay independent of those platform details. Sensor calibration must be replaced and perception reevaluated for real footage.
