# Tooling inspection and prerequisite proposal

Inspected 2026-09-09. This is a read-only inspection plus repository documentation; no software was downloaded, installed or launched.

## Findings

| Item | Observed result |
| --- | --- |
| Unity Editor | No executable/version found in Program Files, Program Files (x86), standard Hub editor locations, local Programs or checked personal install locations. No Unity uninstall registration, PATH command or running process found. |
| Unity Hub | No installation/version or standard roaming Hub editor configuration found. |
| Unity 6 available? | Not found by these checks; treat it as unavailable for project creation. An unregistered portable installation in an unrelated custom folder is not ruled out. |
| Git | `2.49.0.windows.1`, at `C:\Program Files\Git\cmd\git.exe`. |
| Repository | Unborn `main`, no commits. Before this checkpoint: only untracked `AGENTS.md` and `docs/architecture-plan.md`. No implementation or existing Unity project. |
| OS/process | Windows build `10.0.26200.0`, AMD64 process; registry reports display version `25H2`. Legacy registry product-name text is not used to infer the marketed OS edition. |
| Free disk | Approximately 253 GiB on C: at inspection time. |
| Python | Uninstall registry records Python 3.11.9 and 3.13.3 components; `python`/`py` were not resolved on this task's PATH. Usable standalone interpreters were not verified. Python is unnecessary for M1. |
| .NET / IDE | `dotnet` not resolved on PATH; no Visual Studio installation found in inspected application registrations. Neither is being installed in this checkpoint. |
| GPU / RAM | CIM inspection was access-denied; hardware capability is unverified. No performance claim follows from this inspection. |
| Unity licensing | Not verified. No license/credential contents were inspected. |

Inspection checked standard installation roots, user/machine uninstall registrations, PATH, running Unity processes, standard Hub configuration paths and selected per-user installation roots. It was not an exhaustive scan of every user/project directory on disk.

## Exact editor proposal

Use **Unity 6.3 LTS, 6000.3.23f1, Windows x64**. This is a verified published release, dated 2026-08-26; it is a reproducible proposed pin, not a claim to be the newest available patch. Unity identifies 6.3 as an LTS line. Sources: [release and installers](https://unity.com/releases/editor/whats-new/6000.3.23f1), [release support](https://unity.com/releases/unity-6/support).

After installation, record that exact editor version and revision in the project. Do not create `ProjectVersion.txt` now or claim a tested package configuration before the editor exists.

## Required software and package disclosure before any download

| Dependency | Purpose / proposed scope |
| --- | --- |
| Unity Hub for Windows | Proposed installation/license management route; absent locally. Use the official stable Hub installer, disclose its actual version when obtained. It is not part of the simulation runtime. |
| Unity Editor 6000.3.23f1, Windows x64 | Required to create/import the project, run physics/visual tests and build. Use Windows standalone **Mono** for the initial desktop build. |
| Unity editor prerequisites | The official installer may supply Microsoft runtime components; review/disclose the selected installer components before installation. No optional platforms, dedicated-server modules or IL2CPP toolchain are requested. |
| `com.unity.render-pipelines.universal` | Required URP rendering package, editor-compatible **17.3** line. Resolve the exact patch from the selected editor's supported package metadata, then pin it. |
| `com.unity.test-framework` | Required EditMode/PlayMode test runner. Select the stable version supported by the installed editor, then pin it; no experimental version. |
| Transitive Unity dependencies | URP may resolve Core RP, Shader Graph and supporting packages; Test Framework resolves its test dependencies such as Unity's NUnit integration. Inspect the actual dependency graph and disclose exact additions before registry resolution/download. Preserve the complete generated lock afterwards. |
| Built-in editor modules | Unity 3D physics, UI Toolkit, camera/rendering and text capabilities. Use editor-provided modules and local authoring; these are not separate Asset Store extensions. |

Exact package patches/transitive versions are deliberately **unresolved**, since no editor/package manifest exists locally to verify them. Before the first package restore, inspect the selected editor's bundled package/template manifests and present the concrete versioned dependency list; do not silently let a broad template install unrelated packages. A minimal project with explicitly selected URP and test dependencies is preferred over a sample-heavy template.

Reference: [URP package documentation and supported editor lines](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/manual/index.html), [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/manual/index.html). These reference lines do not constitute a tested local dependency lock.

**No third-party assets or packages are required.** Author the entire static facility with local meshes/materials/signage, including damage geometry or material markers. No Asset Store packs, paid fonts/textures, external decal system, Blender, ProBuilder, Cinemachine, AI Navigation, networking framework, Python packages, Ollama or cloud service is required for this step.

Visual Studio/Rider and editor-integration packages are optional conveniences, not proposed prerequisites for initial Mono editor/build work. IL2CPP/C++ tools and additional platform support are deferred. See [Unity Windows requirements](https://docs.unity3d.com/6000.3/Documentation/Manual/windows-requirements-and-compatibility.html) for backend-specific prerequisites.

## Manual Hub action and next work

With no Hub/editor detected, the proposed Hub route requires installation, user sign-in/license activation and installation of the pinned editor before Unity verification can occur. User interaction is expected for account/license dialogs and any operating-system installer consent. Do not create a project in Hub yet. If an existing custom editor installation is supplied later, inspect its version and register it instead of duplicating it.

This checkpoint stops here as requested. Next, resolve the Unity prerequisite, inspect/disclose exact package versions, then create `simulator/unity/MicroHawkSim` with pinned URP settings, assembly boundaries and test harness. Follow with the authored visual facility and typed safety/flight implementation. No downloaded asset or package, generated Unity project, executable code, build or Unity test run is part of this checkpoint.

## Bootstrap pass: superseding tooling findings

Unity Hub 3.21.1 and Unity Editor 6000.6.0f1, revision f7f8ed4d1e24, are now installed at C:\Program Files\Unity Hub and C:\Program Files\Unity\Hub\Editor\6000.6.0f1 respectively. The editor contains Windows standalone support. The bootstrap pins this installed version, explicitly disclosed as different from the previous proposal. No Unity editor or Hub installer was run by this implementation pass.

The actual package graph is recorded in simulator/unity/MicroHawkSim/Packages/packages-lock.json. URP/Core/Shader Graph/URP Config are 17.6.0; Test Framework 1.8.0; NUnit 2.1.0; Burst 2.0.0; Collections and Performance Test Framework 6.6.0; Graph Authoring 1.0.0. Registry dependencies disclosed before import: Profiling Core 1.0.3, Searcher 4.9.3 and Mono Cecil 1.11.6. Standard editor modules use version 1.0.0. No external assets or fonts were downloaded; signage uses Unity's built-in LegacyRuntime font.

The initial sandboxed import could not write Unity caches. Its stalled process was terminated and import retried with execution approval, succeeding. Current run/test status is maintained in bootstrap-status.md; the historical foundation-only findings above are retained for context.

Resolver note: the final lock selected Searcher 4.9.5 rather than its initially observed minimum 4.9.3. This was reported and is pinned in the resolved lock. Screen Capture 1.0.0 was briefly tried for headless Play-mode images, then removed because that optional path did not yield images. Camera renders remain supported via the editor authoring tool. No additional external asset was introduced.
