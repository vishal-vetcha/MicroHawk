# MicroHawk

### Autonomous Agentic AI Drone System

**Natural-Language Missions · Vision-Grounded Autonomy · Deterministic Safety · Physical Flight Simulation · Voice Interaction · Evidence & Reporting**

MicroHawk is a simulation-first autonomous drone platform that allows an operator to command a physically simulated drone using natural language or voice.

Instead of treating an LLM as the flight controller, MicroHawk separates **AI reasoning from deterministic execution and safety**.

An operator can say:

> **"Patrol Restricted Zone A. If anyone remains inside for more than 10 seconds, investigate, capture evidence, report what happened, and return home."**

MicroHawk converts that request into a validated structured mission, physically flies the drone through a Unity industrial environment, analyzes imagery from the drone camera, reasons over temporal events, autonomously executes conditional actions, captures evidence, returns home, lands, generates a mission report, and can read the report aloud.

The system is designed around one fundamental rule:

> **AI may propose what the drone should do. Deterministic software decides what the drone is allowed to do.**

---

# Demo

## Demo Videos

**End-to-End Autonomous Mission**

`[DEMO VIDEO URL]`

**Restricted Zone Autonomous Security Mission**

`[DEMO VIDEO URL]`

**Autonomous Structural Inspection**

`[DEMO VIDEO URL]`

**Voice-Controlled Mission + Spoken Report**

`[DEMO VIDEO URL]`

**Safety Override Demonstration**

`[DEMO VIDEO URL]`

---

# What MicroHawk Can Do

MicroHawk currently demonstrates an end-to-end autonomous drone stack including:

* natural-language drone commands
* voice-controlled mission requests
* local LLM-based intent understanding
* structured mission generation
* strict mission-schema validation
* bounded autonomous replanning
* deterministic safety enforcement
* physical Rigidbody-based drone flight
* autonomous takeoff, navigation, hover, landing and Return-to-Home
* geofence enforcement
* altitude and speed limits
* battery-aware mission safety
* emergency behavior
* real-time telemetry
* WebSocket communication between Python and Unity
* drone-mounted RGB camera streaming
* image-derived person detection
* restricted-zone monitoring
* temporal dwell reasoning
* conditional autonomous investigation
* structural damage/crack inspection
* evidence capture
* mission event logging
* decision journal
* mission reports
* spoken reports
* operator authorization
* failure injection
* dashboard-based mission control
* simulation-first validation
* future external flight-controller / FlytBase adapter boundary

---

# Example Missions

### Autonomous Structural Inspection

```text
Inspect the structural inspection wall for cracks and return home.
```

MicroHawk:

1. interprets the request
2. generates a structured inspection mission
3. validates the mission
4. arms the drone
5. physically takes off
6. navigates to the inspection location
7. positions itself at the inspection stand-off
8. captures RGB imagery from the simulated drone camera
9. performs image-based damage analysis
10. stores evidence
11. returns home
12. lands
13. produces a mission report

---

### Restricted-Zone Security Patrol

```text
Patrol Restricted Zone A. If anyone remains inside for more than 10 seconds, investigate, capture evidence, report what happened, and return home.
```

The system can:

1. parse the temporal condition
2. patrol the requested zone
3. observe rendered camera frames
4. detect a visible person from image pixels
5. track the observation over simulation time
6. determine whether the person remains inside the zone
7. trigger the conditional branch only after the dwell threshold is exceeded
8. execute a bounded investigation
9. capture evidence
10. return home
11. land
12. generate and speak the final report

A validated demonstration produced a restricted-zone dwell event after approximately **10.2 simulation seconds**, triggering exactly one investigation branch before Return-to-Home.

---

### Deterministic Safety Demonstration

Try:

```text
Take off to 100 meters and inspect the warehouse.
```

The AI layer may understand the requested objective, but it cannot override the flight safety system.

The Unity safety engine evaluates the command before physical execution.

An unsafe altitude request is rejected with a safety reason such as:

```text
AltitudeLimit
```

The aircraft does not execute the prohibited movement.

This separation between probabilistic AI reasoning and deterministic flight authority is a core architectural principle of MicroHawk.

---

# System Architecture

```text
                         MICROHAWK
                             │
                  ┌──────────▼──────────┐
                  │      Operator       │
                  │    Voice / Text     │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │   Intent / Agent    │
                  │ Local Qwen via      │
                  │      Ollama         │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │  Mission Planner    │
                  │ Structured Actions  │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │ Schema Validation   │
                  │ + Policy Validation │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │ Mission Executor    │
                  │ / Agent Runtime     │
                  └──────────┬──────────┘
                             │
                    Versioned WebSocket
                             │
                  ┌──────────▼──────────┐
                  │       Unity         │
                  │ IFlightOperations   │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │ Deterministic       │
                  │   Safety Engine     │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │ Flight Controller   │
                  └──────────┬──────────┘
                             │
                  ┌──────────▼──────────┐
                  │ Rigidbody Physics   │
                  │  Simulated Drone    │
                  └─────────────────────┘
```

The reverse data path provides:

```text
Drone
  │
  ├── Telemetry
  ├── Flight State
  ├── Battery
  ├── Position / Altitude
  ├── Command Outcomes
  └── RGB Camera Frames
           │
           ▼
     Python Backend
           │
           ├── Perception
           ├── Temporal Events
           ├── Evidence
           ├── Memory
           ├── Decision Journal
           └── Mission Reports
                    │
                    ▼
             Web Dashboard
```

---

# The Most Important Architectural Rule

The LLM **does not directly fly the drone**.

It cannot directly:

* manipulate the Unity `Transform`
* manipulate the `Rigidbody`
* apply arbitrary forces
* apply arbitrary torques
* change motor outputs
* teleport the aircraft
* bypass safety checks

Instead:

```text
Natural Language
       ↓
AI Intent
       ↓
Typed Mission
       ↓
Validated Command
       ↓
Deterministic Safety
       ↓
Flight Controller
       ↓
Physics
```

This keeps high-level reasoning flexible while maintaining deterministic authority over physical execution.

---

# Agentic Autonomy

MicroHawk is more than a natural-language wrapper around fixed drone commands.

The mission layer supports conditional autonomous behavior.

For example:

```text
IF a person remains inside Restricted Zone A for > 10 seconds
THEN investigate
THEN capture evidence
THEN report
THEN return home
```

The system maintains mission state and evaluates observations over simulation time.

The autonomy layer can perform bounded decision-making while the deterministic flight layer remains authoritative.

This creates a hierarchy:

```text
Operator Goal
     ↓
AI Interpretation
     ↓
Mission Policy
     ↓
Observation
     ↓
Event Detection
     ↓
Conditional Decision
     ↓
Safe Flight Command
```

---

# Vision-Grounded Perception

MicroHawk deliberately distinguishes between:

### Navigation knowledge

The simulator may expose known world geometry, target positions and zone definitions for navigation and deterministic simulation.

### Perception evidence

Operational claims such as:

```text
"Person detected"
```

or

```text
"Damage detected"
```

must originate from the drone's rendered camera imagery.

The drone RGB sensor captures frames from the simulated environment and transfers them to the Python perception stack.

This prevents the autonomy system from simply reading hidden semantic truth from the Unity scene and pretending that it visually detected something.

---

# Person Detection and Temporal Reasoning

The restricted-zone pipeline performs:

```text
RGB Frame
    ↓
Image-Based Candidate Detection
    ↓
Person Observation
    ↓
Track Association
    ↓
Zone Membership
    ↓
Simulation-Time Dwell Tracking
    ↓
Threshold Evaluation
    ↓
Conditional Mission Event
```

The temporal layer allows MicroHawk to reason about conditions that require observations over time rather than reacting to a single frame.

Example:

```text
Person observed in restricted_zone_a
Dwell = 4.8 s
→ continue monitoring

Dwell = 8.7 s
→ continue monitoring

Dwell = 10.2 s
→ policy condition satisfied
→ investigate
```

---

# Structural Damage Inspection

MicroHawk also includes a visual inspection workflow.

Example request:

```text
Inspect the structural inspection wall for cracks and return home.
```

The drone navigates to the inspection target and captures actual rendered RGB frames.

The operational inspection pipeline uses deterministic image processing to identify visible damage candidates from image pixels.

Evidence can then be displayed in the mission dashboard and included in the final mission record.

The system intentionally does **not** rely exclusively on free-form VLM descriptions for operational safety-critical detections.

---

# Why the VLM Does Not Control Operational Perception

MicroHawk supports a local vision-language model as part of the AI experimentation layer, but operational perception remains constrained.

During development, unrestricted VLM interpretation could produce semantically plausible but visually unsupported descriptions.

Therefore, the architecture treats model-generated interpretation as advisory unless validated by a constrained perception path.

For demonstrated missions:

* language models handle natural-language understanding and structured planning
* deterministic CV handles operational visual detections
* schema validation constrains plans
* deterministic safety controls flight authority

This is intentional.

---

# Local AI

MicroHawk is designed to run its primary AI stack locally.

The current configuration uses:

```text
Ollama
+
qwen2.5vl:3b
```

No cloud LLM API key is required for the demonstrated local workflow.

The local model is used for constrained natural-language intent understanding and mission planning.

Operational detections remain validated by the perception subsystem rather than blindly trusting free-form model output.

---

# Flight Capabilities

The simulator implements typed flight operations including:

```text
ARM
DISARM
TAKEOFF
MOVE_TO
HOLD
LAND
RETURN_HOME
```

The drone is physically simulated rather than teleported between mission points.

Flight behavior includes:

* physical thrust
* attitude stabilization
* translational control
* altitude control
* waypoint navigation
* hover
* landing
* Return-to-Home
* rotor animation
* battery consumption
* flight-state transitions

---

# Deterministic Safety Layer

Safety remains independent of the LLM.

Current safety mechanisms include:

* maximum-altitude enforcement
* invalid-altitude rejection
* geofence checks
* route validation
* speed limits
* battery reserve checks
* low-battery Return-to-Home behavior
* critical-battery emergency behavior
* command validation
* mission sequencing constraints
* deterministic rejection reasons

The important authority chain is:

```text
AI requests action
        ↓
Safety evaluates action
        ↓
APPROVE / REJECT
        ↓
Only approved actions reach flight control
```

---

# Mission Control Dashboard

MicroHawk includes a local browser-based mission-control interface.

Default dashboard:

```text
http://127.0.0.1:8000
```

The dashboard provides:

* simulator connection state
* mission status
* flight state
* battery
* altitude
* safety state
* operator mission input
* voice control
* mission presets
* Return-to-Home
* abort/hold control
* live RGB sensor imagery
* perception evidence
* decision journal
* mission events
* command outcomes
* validated intent
* structured plan
* mission report
* spoken-report controls
* unsafe-altitude safety demonstration

---

# Voice Interaction

MicroHawk supports push-to-speak interaction on compatible Windows systems.

The operator can speak a mission instead of typing it.

Example:

```text
Patrol Restricted Zone A.
If anyone remains inside for more than ten seconds,
investigate, capture evidence, report what happened,
and return home.
```

After mission completion, MicroHawk can also read the generated report aloud.

This creates a conversational operator loop:

```text
Operator speaks
      ↓
Speech recognition
      ↓
Mission interpretation
      ↓
Autonomous execution
      ↓
Mission report
      ↓
Text-to-speech
      ↓
Drone speaks report
```

---

# Evidence and Mission Reporting

Mission execution generates structured operational information including:

* detected events
* command outcomes
* safety decisions
* captured evidence frames
* mission state
* temporal events
* final flight state
* mission explanation
* final report

Example result:

```text
Mission Completed.

Final flight state: Landed.

Observed a person in restricted_zone_a for
10.2 simulation seconds.

Conditional policy triggered investigation.

Evidence captured.

Aircraft returned home successfully.
```

---

# Operator Authorization

Mission-control actions are protected by a local operator token.

The token is generated/stored locally under:

```text
runtime-data/operator.token
```

This file is intentionally excluded from Git.

**Never commit this token.**

The dashboard requires the local operator token before privileged mission controls are unlocked.

---

# Failure Injection

The simulator contains failure-injection controls for testing safety behavior.

Examples include:

```text
Battery to 24%
Battery to 11%
```

These controls allow safety and recovery behavior to be demonstrated without waiting for the simulated battery to naturally discharge.

---

# Technology Stack

## Simulation

* Unity 6
* C#
* Universal Render Pipeline
* Unity Rigidbody physics
* Unity Test Framework

Development version used:

```text
Unity 6000.6.0f1
```

---

## Backend

* Python 3.12
* FastAPI
* Uvicorn
* WebSockets
* Pydantic
* OpenCV
* NumPy
* pytest

Development environment used:

```text
Python 3.12.14
```

---

## AI

* Ollama
* Qwen2.5-VL 3B

Model:

```text
qwen2.5vl:3b
```

---

## Communication

```text
Python Backend
      ⇅
Versioned WebSocket Protocol
      ⇅
Unity Simulator
```

The transport includes:

* structured messages
* strict parsing
* command identifiers
* session handling
* command expiry
* idempotency protection
* bounded queues
* telemetry
* outcomes
* sensor frames

---

# Repository Structure

A simplified repository layout:

```text
MicroHawk/
│
├── backend/
│   ├── src/
│   │   └── microhawk/
│   │       ├── api/
│   │       ├── config/
│   │       ├── contracts/
│   │       ├── memory/
│   │       ├── missions/
│   │       ├── perception/
│   │       ├── transport/
│   │       └── world/
│   │
│   ├── tests/
│   │   ├── integration/
│   │   └── unit/
│   │
│   └── pyproject.toml
│
├── simulator/
│   └── unity/
│       └── MicroHawkSim/
│           ├── Assets/
│           ├── Packages/
│           └── ProjectSettings/
│
├── docs/
│   ├── decisions/
│   ├── flight-foundation.md
│   ├── flight-verification.md
│   └── external-flight-adapters.md
│
├── tools/
│   ├── setup.ps1
│   ├── run-backend.ps1
│   ├── run-simulator.ps1
│   ├── run-demo.ps1
│   ├── run-tests.ps1
│   ├── voice.ps1
│   ├── verify-live.py
│   ├── verify-reconnect.py
│   └── collect-demo-evidence.py
│
├── runtime-data/             # local runtime state - ignored
├── artifacts/                # generated builds/artifacts - ignored
├── .gitignore
├── AGENTS.md
└── README.md
```

---

# Prerequisites

The demonstrated setup was developed and validated primarily on Windows.

Recommended prerequisites:

### Required

* Windows 10/11
* Git
* Python 3.12
* Ollama
* `qwen2.5vl:3b`
* Unity Hub
* Unity `6000.6.0f1` for Editor development/rebuilding

### Recommended

* 16 GB RAM
* modern multi-core CPU
* dedicated or capable integrated GPU for Unity
* microphone for voice interaction
* speakers/headphones for spoken reports

A dedicated CUDA GPU is **not required** for the demonstrated 3B local-model workflow, although stronger hardware can improve inference speed.

---

# Installation

## 1. Clone the Repository

Open **Command Prompt**:

```cmd
cd /d C:\
mkdir Projects
cd Projects
git clone YOUR_GITHUB_REPOSITORY_URL
cd MicroHawk
```

For example:

```cmd
git clone https://github.com/YOUR_USERNAME/MicroHawk.git
cd MicroHawk
```

---

# 2. Install Python

Install Python 3.12.

Verify:

```cmd
python --version
```

Expected:

```text
Python 3.12.x
```

If the Python launcher is installed:

```cmd
py -3.12 --version
```

---

# 3. Create the Virtual Environment

From the MicroHawk repository root:

```cmd
python -m venv .venv
```

Activate it:

```cmd
.venv\Scripts\activate
```

You should now see something similar to:

```text
(.venv) C:\Projects\MicroHawk>
```

Upgrade packaging tools:

```cmd
python -m pip install --upgrade pip setuptools wheel
```

---

# 4. Install the Python Backend

From the repository root:

```cmd
pip install -e .\backend
```

If development/test dependencies are defined as an extra in the current project configuration, install the corresponding development extra as well.

You can verify the installation with:

```cmd
python -c "import microhawk; print('MicroHawk backend import successful')"
```

---

# 5. Install Ollama

Install Ollama for your operating system.

Official site:

https://ollama.com/

After installation, verify:

```cmd
ollama --version
```

MicroHawk was developed with a modern Ollama release and requires a release compatible with Qwen2.5-VL.

---

# 6. Download the Required Qwen Model

MicroHawk's current local model is:

```text
qwen2.5vl:3b
```

Pull it:

```cmd
ollama pull qwen2.5vl:3b
```

Verify:

```cmd
ollama list
```

You should see:

```text
qwen2.5vl:3b
```

Test it:

```cmd
ollama run qwen2.5vl:3b
```

Then enter:

```text
Hello
```

Exit the interactive session when finished.

The model is several gigabytes, so allow enough disk space for Ollama's model storage.

---

# 7. Install Unity

Install Unity Hub and install:

```text
Unity 6000.6.0f1
```

The Unity project is located at:

```text
simulator/unity/MicroHawkSim
```

In Unity Hub:

```text
Add/Open Project
→ select simulator/unity/MicroHawkSim
```

Allow Unity to import the project and restore packages.

The main industrial test scene is:

```text
Assets/MicroHawk/Scenes/IndustrialTest.unity
```

---

# 8. Initial Project Setup

The repository contains:

```text
tools/setup.ps1
```

On Windows, from the repository root:

```cmd
powershell -ExecutionPolicy Bypass -File tools\setup.ps1
```

Use the repository scripts wherever possible because they configure the environment expected by MicroHawk.

---

# Running MicroHawk

The safest startup order is:

```text
1. Ollama
2. Backend
3. Simulator
4. Dashboard
5. Operator authorization
6. Mission
```

---

# Step 1 — Make Sure Ollama Is Running

Verify:

```cmd
ollama list
```

Confirm:

```text
qwen2.5vl:3b
```

is available.

If necessary:

```cmd
ollama serve
```

Do not start a second Ollama server if the desktop/background service is already running.

---

# Step 2 — Start the MicroHawk Backend

Open a new Command Prompt:

```cmd
cd /d C:\Projects\MicroHawk
powershell -ExecutionPolicy Bypass -File tools\run-backend.ps1
```

Keep this terminal open.

The backend provides the mission-control API and Unity communication endpoint.

Default local services use:

```text
Dashboard/API: 127.0.0.1:8000
Unity transport: 127.0.0.1:8765
```

---

# Step 3 — Start the Simulator

Open another Command Prompt:

```cmd
cd /d C:\Projects\MicroHawk
powershell -ExecutionPolicy Bypass -File tools\run-simulator.ps1
```

The MicroHawk industrial facility should open.

The simulator should establish a connection with the Python backend.

---

# Step 4 — Open Mission Control

Open:

```text
http://127.0.0.1:8000
```

Or from Command Prompt:

```cmd
start http://127.0.0.1:8000
```

The dashboard should eventually report:

```text
SIMULATOR CONNECTED
```

---

# Step 5 — Unlock Operator Controls

The local operator token is intentionally not stored in Git.

After initial setup/runtime initialization, retrieve the local token with:

```cmd
type runtime-data\operator.token
```

Copy the exact value.

In the dashboard:

```text
Local operator token
→ paste token
→ Unlock controls
```

Do not share this token publicly.

Do not add it to Git.

---

# Step 6 — Run Your First Mission

Start with:

```text
Inspect the structural inspection wall for cracks and return home.
```

Click:

```text
Send mission
```

Observe both:

### Unity

Watch:

```text
Landed
→ Armed
→ Takeoff
→ Navigation
→ Inspection
→ Return Home
→ Landing
```

### Dashboard

Watch:

```text
Intent
→ Validated Plan
→ Command Outcomes
→ RGB Evidence
→ Perception
→ Decision Journal
→ Mission Report
```

---

# Recommended Demo Mission 1 — Structural Inspection

```text
Inspect the structural inspection wall for cracks and return home.
```

Expected high-level flow:

```text
Natural Language
→ Plan
→ Safety Validation
→ Takeoff
→ Navigate
→ Camera Inspection
→ Damage Evidence
→ Return Home
→ Land
→ Report
```

---

# Recommended Demo Mission 2 — Restricted Zone

```text
Patrol Restricted Zone A. If anyone remains inside for more than 10 seconds, investigate, capture evidence, report what happened, and return home.
```

Expected high-level flow:

```text
Mission Request
→ Patrol
→ Camera Observation
→ Person Track
→ Zone Membership
→ Dwell Timer
→ Threshold > 10 s
→ Investigation
→ Evidence
→ Return Home
→ Land
→ Report
```

---

# Recommended Demo Mission 3 — Safety

```text
Take off to 100 meters and inspect the warehouse.
```

Expected result:

```text
Mission interpreted
→ unsafe flight request reaches deterministic validation
→ AltitudeLimit
→ command rejected
→ prohibited movement does not execute
```

This is an important demonstration because it proves the AI does not possess unrestricted control over the simulated aircraft.

---

# Voice-Controlled Missions

The dashboard provides:

```text
Push to speak
```

You can also launch the voice workflow from Command Prompt:

```cmd
cd /d C:\Projects\MicroHawk
powershell -ExecutionPolicy Bypass -File tools\voice.ps1
```

Example spoken command:

```text
Inspect the structural inspection wall for cracks and return home.
```

or:

```text
Patrol Restricted Zone A.
If anyone remains inside for more than ten seconds,
investigate, capture evidence, report what happened,
and return home.
```

After the mission, use the report-reading functionality to hear the mission report spoken aloud.

---

# Simulator Camera Controls

Useful simulator views include:

```text
1      Overview
2      Drone Follow
TAB    Switch Camera
```

A good demonstration sequence is:

```text
Overview
→ issue mission
→ watch takeoff
→ switch to drone follow
→ watch physical navigation
→ observe dashboard evidence
→ watch Return-to-Home
→ watch landing
```

---

# Failure-Injection Testing

The simulator includes battery failure controls such as:

```text
Battery to 24%
Battery to 11%
```

These are useful for demonstrating deterministic recovery/safety behavior.

Run a normal mission first before demonstrating injected failures.

---

# Running Tests

MicroHawk includes automated tests across the Python autonomy layer and Unity simulation layer.

Use:

```cmd
cd /d C:\Projects\MicroHawk
powershell -ExecutionPolicy Bypass -File tools\run-tests.ps1
```

During final integrated development, the project reached:

```text
44 Unity EditMode tests
12 Unity PlayMode tests
20 Python tests
----------------------
76 automated tests
```

The test suite covers areas including:

* command contracts
* mission validation
* flight logic
* safety behavior
* transport behavior
* perception
* temporal events
* recovery
* memory
* integration behavior

---

# Live Verification Utilities

Additional repository utilities include:

```cmd
python tools\verify-live.py
```

and:

```cmd
python tools\verify-reconnect.py
```

These are intended for validating live system behavior and transport/reconnection behavior.

---

# Troubleshooting

## `WinError 10048`

Example:

```text
OSError: [WinError 10048]
Only one usage of each socket address is normally permitted
```

This usually means another MicroHawk backend is already listening on the required port.

Check:

```cmd
netstat -ano | findstr :8765
```

Example:

```text
TCP 127.0.0.1:8765 0.0.0.0:0 LISTENING 4272
```

The final number is the PID.

Terminate the stale process only if you have confirmed it is the unwanted MicroHawk backend:

```cmd
taskkill /PID 4272 /F
```

Replace `4272` with the actual PID.

Check the dashboard port too:

```cmd
netstat -ano | findstr :8000
```

`TIME_WAIT` connections are normal after sockets close.

The important thing to look for is an unexpected:

```text
LISTENING
```

process.

---

# Simulator Does Not Connect

Check that:

1. backend is running
2. port `8765` is listening
3. simulator was started from the expected project
4. the local operator/runtime configuration exists
5. Windows Firewall has not blocked local communication

Then restart in this order:

```text
Backend
→ Simulator
→ Dashboard
```

---

# Dashboard Does Not Open

Verify the backend is listening:

```cmd
netstat -ano | findstr :8000
```

Then open:

```cmd
start http://127.0.0.1:8000
```

---

# Qwen Model Missing

Check:

```cmd
ollama list
```

If `qwen2.5vl:3b` is absent:

```cmd
ollama pull qwen2.5vl:3b
```

Then verify again:

```cmd
ollama list
```

---

# Operator Token Missing

Run the repository setup/runtime initialization first:

```cmd
powershell -ExecutionPolicy Bypass -File tools\setup.ps1
```

Then inspect:

```cmd
type runtime-data\operator.token
```

The token must remain local and must not be committed.

---

# Clean Shutdown

When finished:

1. allow the current mission to finish or safely Return-to-Home
2. close the simulator
3. stop the backend with `Ctrl+C`

If a backend remains running:

```cmd
netstat -ano | findstr :8765
```

Identify the correct MicroHawk process and terminate it if necessary:

```cmd
taskkill /PID YOUR_PID /F
```

Then check:

```cmd
netstat -ano | findstr :8765
netstat -ano | findstr :8000
```

No unexpected `LISTENING` process should remain.

---

# Security

Never commit:

```text
runtime-data/operator.token
.env
credentials
API keys
local secrets
.venv
Unity Library
Unity Temp
generated build artifacts
```

These paths should remain excluded through `.gitignore`.

Before every public push, it is good practice to run:

```cmd
git status
```

and inspect the staged files.

You can verify that the operator token is ignored with:

```cmd
git check-ignore -v runtime-data\operator.token
```

---

# Simulation vs. Real Aircraft

MicroHawk currently operates against a simulated aircraft.

It does **not** claim that the current repository directly controls a production drone.

This is intentional.

The architecture introduces an abstraction boundary between mission autonomy and flight execution:

```text
Agentic Mission System
        ↓
IFlightOperations
        ↓
Current:
Unity Simulation Adapter

Future:
External Drone / FlytBase Adapter
```

This allows the autonomy layer to remain largely independent of the underlying aircraft provider.

---

# FlytBase Integration Direction

MicroHawk was designed with a future external flight-operations adapter in mind.

The current Unity implementation can conceptually be replaced by an adapter mapping MicroHawk operations to capabilities such as:

```text
Navigation
Command & Control
Telemetry
Video / Payload
Mission Planning
Geofence / Safety
```

The goal is:

```text
Today
Natural Language
→ MicroHawk Agent
→ Safety
→ Unity Drone

Future
Natural Language
→ MicroHawk Agent
→ Safety / Policy
→ FlytBase Adapter
→ Supported Drone / Dock
```

The current repository is therefore best understood as a **simulation-first autonomous-agent research and engineering platform**, not a finished real-aircraft integration.

---

# Design Principles

MicroHawk was built around several principles.

### 1. Safety before autonomy

The AI cannot bypass deterministic flight rules.

### 2. Perception must be grounded

Operational detections originate from sensor imagery rather than hidden simulator labels.

### 3. Structured interfaces between probabilistic and deterministic systems

LLM output is validated before execution.

### 4. Simulation before real hardware

Complex autonomy should be tested extensively before connection to physical aircraft.

### 5. Explainability matters

Operators should be able to understand why an autonomous branch executed.

### 6. Evidence matters

Security and inspection missions should produce evidence, not merely text claims.

### 7. Failure is expected

The architecture includes validation, bounded replanning, recovery and failure injection.

---

# Demonstrated End-to-End Loop

MicroHawk has demonstrated the following complete loop:

```text
Human Voice / Text
        ↓
Local AI Intent
        ↓
Validated Structured Mission
        ↓
Deterministic Safety
        ↓
Physical Simulated Flight
        ↓
Drone RGB Camera
        ↓
Image-Derived Perception
        ↓
Temporal / Conditional Reasoning
        ↓
Autonomous Investigation
        ↓
Evidence Capture
        ↓
Return-to-Home
        ↓
Physical Landing
        ↓
Mission Report
        ↓
Spoken Response
```

That closed loop is the central capability of the project.

---

# Current Limitations

MicroHawk is a research/demo platform and currently has several intentional limitations:

* aircraft execution is simulated
* environment-specific CV is optimized for the controlled simulation
* local LLM performance depends on host hardware
* speech recognition availability depends on the host OS
* the current system is not certified for safety-critical real-world flight
* the FlytBase/external aircraft adapter is an architectural extension point, not a completed production integration
* operational perception should be retrained/replaced with robust production perception models before deployment in uncontrolled real environments

---

# Future Work

Planned extensions include:

* FlytBase Drone API integration
* real drone/dock adapter
* production-grade object detection
* stronger multi-object tracking
* multimodal VLM verification
* thermal-camera support
* multi-drone mission allocation
* dock-based autonomous dispatch
* persistent site maps
* richer spatial memory
* anomaly detection
* human-in-the-loop escalation
* mission replay
* remote operator handoff
* production authentication
* encrypted transport
* distributed telemetry
* edge deployment
* ROS / MAVLink / PX4 experimentation
* multi-site fleet orchestration

---

# Why MicroHawk

The project explores a central question:

> **How can modern AI agents interact with physical autonomous systems without giving probabilistic models unrestricted control over safety-critical execution?**

MicroHawk's answer is a layered architecture combining:

```text
LLM reasoning
+
structured contracts
+
deterministic validation
+
physical simulation
+
vision-grounded perception
+
temporal reasoning
+
bounded autonomy
+
human-readable evidence
```

The result is not simply a chatbot controlling a drone.

It is an experimental **Physical AI architecture** where language, perception, autonomy, safety and physical execution operate through explicit boundaries.

---

# Author

**Vishal Vetcha**

B.Tech Computer Science & Engineering
GITAM (Deemed to be University)

Research interests:

* Agentic AI
* Autonomous Systems
* Computer Vision
* Multimodal AI
* Physical AI
* Deep Learning
* Resource-Efficient AI

**GitHub:** `[YOUR GITHUB PROFILE]`

**LinkedIn:** `[YOUR LINKEDIN PROFILE]`

**Email:** `[YOUR EMAIL]`

---

# Repository

```text
[YOUR FINAL MICROHAWK GITHUB URL]
```

---

# Disclaimer

MicroHawk is an experimental simulation and research project.

The current implementation is intended for simulation, development, demonstration and research. It should not be connected to or used to control real aircraft without appropriate engineering validation, hardware integration, operational safeguards, regulatory compliance and safety review.

---

## MicroHawk

**Talk to the drone. Give it a goal. Let the agent reason. Keep safety deterministic.**

```text
VOICE → INTENT → PLAN → SAFETY → FLIGHT → VISION → REASON → ACT → EVIDENCE → REPORT
```
