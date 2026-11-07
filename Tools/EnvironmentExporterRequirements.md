# Environment exporter additions for UGEcko

CM consumes one `Assets/__Scenes/Environments/Data/<environment>.json` per environment, using the existing `environmentData` and `objects[].components` layout described by [Chroodle's environment data documentation](https://ugecko.github.io/chroodleWeb/data/envdata/). Native component values belong in that file. CM no longer merges `SpriteColors`, `AnimationPlayback` or `Physics` sidecars from its Editor directory.

These additions were verified against Beat Saber **1.44.1**. They are present in CM's current environment JSON, but the external exporter has not been modified. A new export must include them to retain the fixes. No request has been sent to UGEcko automatically.

## Required fields

| JSON location | Value to export | Why CM needs it |
| --- | --- | --- |
| `objects[].components.SpriteRenderer[].color` | `SpriteRenderer.color` as `[r,g,b,a]`, including alpha | White defaults turn black floor decals into glowing white logos. |
| `objects[].components.Animation[].clipName` | Name of the active state's original AnimationClip | Length/frame rate alone cannot identify or reconstruct a clip. |
| `objects[].components.Animation[].playbackSpeed` | `AnimationState.speed` | Evaluate the clip at the correct rate. |
| `objects[].components.Animation[].phaseSeconds` | `state.time - songSeconds * state.speed`, measured after native startup | Preserve loading/countdown advancement and RandomAnimationStartTime's offset while making CM seeking deterministic. |
| `objects[].components.Animation[].wrapMode` | Effective state wrap mode, resolving Default through the clip | Retain looping behavior. Keep existing clipLength, clipFrameRate and isEnabled fields. |
| `environmentData.physics.gravity` | `Physics.gravity`, `[x,y,z]` | Recreate native car physics settings. |
| `environmentData.physics.fixedDeltaTime` | `Time.fixedDeltaTime`, seconds | Step the car simulation on the same fixed interval. |
| `environmentData.physics.startupSongSeconds` | Song clock of the shared body startup sample | Identify which startup state the snapshot represents. |
| `objects[].components.RigidBodyWrapper[].startupState.position` | `Rigidbody.position`, world-space `[x,y,z]` | Native bodies move during loading/countdown. Static Transform data is not the song-start state. |
| `objects[].components.RigidBodyWrapper[].startupState.rotation` | `Rigidbody.rotation`, world quaternion `[x,y,z,w]` | Preserve startup orientation without an Euler conversion. |
| `objects[].components.RigidBodyWrapper[].startupState.linearVelocity` | World linear velocity `[x,y,z]` | Continue the native motion from its initial state. |
| `objects[].components.RigidBodyWrapper[].startupState.angularVelocity` | World angular velocity, radians/second `[x,y,z]` | Continue native rotation from its initial state. |
| `objects[].components.HydraulicCarSuspensionEffect[].expandEventValues` | Native `_expandEventValues`, separately from `_contractEventValues` | The two native filters differ. Reusing contract values changes hydraulic events. |
| `environmentData.fogParams.noteSpawnIntensity` | Native `BloomFogEnvironment._fogParams.noteSpawnIntensity` | Preserve the camera-relative note-spawn light intensity. Already restored in all 47 current CM exports. |

The current animation support covers one enabled, fully weighted state per component: GagaLogoIdle, Crow, ZombieHand and SkeletonHand. Capture the state after Start, not just immediately after sceneLoaded. The current snapshots deliberately retain one verified set of random starting phases. They do not imply that every native session picks the same offsets. If an exporter encounters blended states, report them rather than collapsing them silently to one clip.

The clip curves remain original `.anim` assets under `Assets/_Graphics/Animations/Environments`, with hashes in NativeClips.json. Clip files are assets, like meshes and textures; do not replace their curves with a dense sampled JSON pose trace.

## Existing car data to preserve

The export already contains the rigidbody settings, spring joints, colliders and most hydraulic fields. Preserve those records and their reference IDs. `RigidBodyWrapper` is the existing exporter name; CM now accepts it as a Rigidbody record. Renaming it or introducing ConfigurableJoint is unnecessary.

SpringJoint.connectedBody must reference the exported Rigidbody instance ID. HydraulicCarSuspensionEffect.springJoint must reference the exported spring joint, and its rigidbody must reference the exported car body. Keep separate contract/expand event types, filters and distances. Preserve HydraulicCarJumpEffect's event type/filter, impulse, position, randomness, cooldown and rigidbody reference.

Capture all 24 car bodies together at the first positive song-time sample, including wheels and their velocities. Use the Rigidbody's physics pose, not an interpolated renderer Transform. Do not copy transient runtime instance IDs into another export: remap by the complete hierarchy and component occurrence. Repeated sibling names require the existing sibling indices in ChromaIDs. The same rule applies to sprite matching.

The 0.2.0 car diagnostic omitted SphereCollider center/radius. CM retains the pre-existing exported center/radius (0 and 0.35). A fresh native exporter should read them directly; do not describe the retained values as newly measured by that diagnostic.

## How these values were obtained

- Sprite tint: offline UnityPy inspection of installed 1.44.1 bundles, reading SpriteRenderer.m_Color and joining exact scene/CAB and complete ChromaID. `DiagnosticTools/NativeLightingSnapshot/Extract-SpriteColors.py` contains the extraction. It recovered 1,694 records across 46 installed environments. Halloween2's unavailable bundle was not guessed.
- Animation playback: read-only `DiagnosticTools/NativeEnvironmentAnimationSnapshot` in the native process. Gaga capture `20261107T003507735`, Halloween2 `20261107T003529415`, and Halloween/Spooky Beat `20261107T010223028` provide 20 active animation states. Startup and later transform samples are retained as regression fixtures.
- Cars: read-only `DiagnosticTools/NativeInterscopeSnapshot` 0.2.0, Passenger Princess capture `native-20261106T183355871`. It recorded native bodies/joints/forces and startup motion. Existing car configuration in the main JSON already matched the retained snapshot before consolidation.
- Note-spawn intensity: native BloomFogEnvironment references and FogParams assets, read by `Tools/Import-NoteSpawnIntensity.py`. The tool documents and reports the source scene/asset for every value.

Raw captures, source bundle hashes and the pre-consolidation files are preserved outside the product repository under workspace `Scratch/EnvironmentExportConsolidation/`. They are diagnostics, not additional inputs loaded by CM's importer.

## Other asset-reference omissions

Native lightmap data and packed reflection-probe texture pairs also need stable resource references in an environment export. The current exporter supplies renderer lightmap indices/scale-offsets and probe bounds but does not supply those asset references. CM's imported LightmapDataSO/ReflectionProbeDataSO and texture payloads belong in graphics asset libraries, alongside other textures. An exporter extension should identify the native resource and its atlas/probe entries. Do not inline binary `.cmprobe`/texture payloads into component JSON.

CustomBoundingBox and RectangleFakeGlowLightWithId already exist in the exports. Their recent fixes were CM importer omissions/spelling, not reasons to add parallel metadata files or duplicate export fields.

## Acceptance checks

`EnvironmentExportMetadataTest` checks the fields in the actual main JSON and verifies an independent export cannot acquire a tint from another saved export. `NativeSpriteColorImportTest` checks native decals and explicit tint values. `LegacyAnimationTimelineTest` compares imported playback with native recorded poses. `InterscopeCarImportTest` regenerates twice from the main JSON; `InterscopeCarTimelineTest` checks native motion, pause, seek and edit behavior. These tests must pass with all three former sidecar folders absent.
