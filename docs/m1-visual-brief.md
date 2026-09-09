# M1 visual facility brief

Status: approved scene-content scope, 2026-09-09; implemented in the first visual/bootstrap scene. The operator approved the visual/bootstrap result. See bootstrap-status.md for historical verification and flight-verification.md for the subsequent physical-flight pass.

The scene should read as a professional autonomous-drone industrial test facility. Use a coherent industrial palette, believable dimensions, finished surface materials, restrained lighting, readable signage, road/floor markings and a recognizable professional quadcopter. Visual polish is a required deliverable alongside tested physical flight.

## Required scene content

| Area / prop | Visual acceptance intent |
| --- | --- |
| MicroHawk home base | Branded base area with a clearly marked primary landing pad and clear approach corridor. |
| Warehouse | Recognizable industrial building with facade detail, doors and believable scale. |
| Multiple loading bays | At least three distinct dock fronts with bay numbers, bumpers and marked approach areas. |
| Restricted Zone A | Legible name, high-contrast perimeter/floor hatching and visible warning signage. It is not automatically a drone no-fly zone. |
| Security gate | Recognizable gate/barrier and security entrance signage. |
| Structural inspection wall | A dedicated visibly labeled inspection surface with several distinguishable damage locations. |
| Crack/damage markers | At least three visible locally authored crack/surface-damage markers, using geometry or materials; no detector or decal plugin required. |
| Static workers/mannequins | Multiple recognizable static human-shaped props, with industrial clothing/PPE cues. |
| Static vehicles/forklifts | Multiple identifiable parked vehicle/forklift props near the relevant facility areas. |
| Pipes/tanks/equipment | A grouped utility area with visible pipe runs, tanks and industrial equipment. |
| Obstacle/navigation section | Clearly separated, labeled obstacle course with varied static shapes and physically usable clearance. |
| Secondary landing/inspection pads | At least two distinct marked pads with IDs and clear approaches. Only explicitly configured landing sites authorize landing. |
| Numbered inspection markers | Several readable IDs distributed across the facility, including wall/equipment locations. |

Counts above are implementation targets to make “multiple/several” reviewable. They do not expand functional mission scope.

## Layout and visual verification

Keep the home pad and initial flight corridor open; place industrial areas around a readable circulation route. Separate decorative detail from simplified collision geometry, but ensure collision geometry represents physical obstacles. Keep signs/damage legible from useful overview, follow and close inspection camera positions. Do not use prop names as flight instructions.

Review the actual rendered Play-mode scene for every required item, material consistency, readable signs, visible damage, drone silhouette, lighting and camera usability. Record representative views when implementation exists. Passing a prefab-count test alone does not satisfy visual acceptance. Avoid an unmeasured frame-rate promise; profile the actual target machine before selecting budgets.

## Data provenance

World metadata may describe authored entities, collision geometry, allowed landing sites and deterministic scenario configuration. Label it as simulator ground truth. Future sensor observations live in separate types/channels and retain sensor/time/provenance information. Evaluation may compare observations against isolated ground-truth labels; perception must not consume those labels to fabricate detections.

No computer vision, object detection, tracking, temporal dwell detection, LLMs, voice or autonomous semantic reasoning is part of M1. Workers and vehicles do not move. Damage markers do not demonstrate crack detection. Ground markings do not demonstrate restricted-zone monitoring.

## Asset policy

Create meshes, surface materials, signage and damage markers locally within the project. No Asset Store pack, third-party texture/font library, Blender installation, ProBuilder, Cinemachine or decal extension is required for this scope. If a later visual need warrants a dependency, disclose its identity, purpose, license and download before adding it.


