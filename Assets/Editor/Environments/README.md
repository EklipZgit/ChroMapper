# ChroMapper environment generation

The JSON files in `Assets/__Scenes/Environments/Data/` are UGEcko exports of Beat Saber environment data. Native component properties belong in these files. Verified 1.44.1 additions and the external exporter changes required to preserve them are documented in [EnvironmentExporterRequirements.md](../../../Tools/EnvironmentExporterRequirements.md). The external exporter itself has not been changed.

`EnvironmentDataObject` defines each exported object and its component arrays. `EnvironmentDataInfo` defines environment metadata, including the color scheme and light tracks.

`TrackDefinitionsSO` is the generated Unity asset type. Generated track-definition assets belong in `Assets/__Scripts/Environments/TrackDefinitions/`.

## Generation workflow

The editor commands have separate responsibilities. Run them in this order:

1. Run `Environment/Populate Build Data`.
2. Assign unresolved Unity assets in `EnvironmentLibrarySO.asset`.
3. If an environment has no scene, select its JSON export. Run `Environment/Create from Data (No Script)` to create the scene.
4. Run `Environment/Update Environment List` to create or update color schemes and track definitions.
5. Run `Environment/Create All from Data`, or run `Environment/Create from Data` for one export.

The scene must exist before the list command accepts an environment. Script-enabled scene construction then assigns the generated color-scheme asset.

`Populate Build Data` reads all exports and updates the mesh, material, texture, sprite, shader, and layer libraries. It does not change source JSON.

`Create from Data` reads the selected JSON export. A selected or active scene resolves its matching JSON export from the `Data/` folder.

`Create All from Data` processes all exports.

These commands construct scenes from the source data and populated libraries. The optional `No Script` command omits component construction and cleanup.

`Update Environment List` updates `EnvironmentListSO`, color-scheme assets, and `TrackDefinitionsSO` assets. It skips an environment when its generated scene is absent.

`EnvironmentBuildPopulate.cs` owns the build libraries. `EnvironmentListUpdate.cs` owns the environment list, color schemes, and track definitions.

`EnvironmentSceneCreator` and the files in `Create/` own scene construction. Generated assets can be overwritten by the next applicable command.

## Scene construction

Scene construction uses these passes:

1. `StripObjects` retains exported objects and their ancestor paths, then resets existing component values without replacing the components.
2. `SpawnObjects` reuses matching components, removes obsolete components, adds missing components, and recursively creates absent ancestors.
3. `BuildComponents` fills component values and references when scripts are enabled.
4. `Cleanup` removes unused empty objects and collects the final ChromaID markers.

A newly created ancestor has an identity local transform and a `ChromaIDMarker`. Its full ChromaID retains the scene prefix, which does not become another GameObject.

Later export data reuses and populates the same ancestor object. Existing marked objects from the old scene are also reused.

An ancestor can have no corresponding exported object. `Cleanup` accepts this state and retains the ancestor when its descendants require the path.

The builder design uses `CreateContainer` and `FillComponents`. The container supplies source data, library lookups, object indexes, and component indexes.

Each component-data record first binds to a Unity component. Its `FillComponents` implementation then copies values and resolves references through the container.

Component reuse matches the exact runtime type and occurrence order on each marked object. Retained components keep their scene-local fileIDs, not their previous values.

Unmarked helper objects, including generated event managers, still undergo replacement. Component reuse does not guarantee an empty scene diff after regeneration.

## Track-definition import

`Structures/EnvironmentDataInfo.cs` converts exported light-track metadata into Basic and GLS definitions. It also derives Basic Event capabilities from exported object components.

Environment-specific metadata corrections belong in this conversion. This keeps regenerated assets correct without changing the UGEcko exports.

## Native sprite colors

The main exports retain native `SpriteRenderer.color` as RGBA on each component. Defaulting it to white turns black floor decals into white logos, including Interscope's `Logo` and Linkin Park's soldier and text decals. `SpriteRendererData` imports the explicit color and uses Unity's white default only when the field is absent.

The verified 1.44.1 colors came from native scene bundles, matched by complete ChromaID including sibling indices. Match the exact scene using the bundle's `m_SceneHashes`: some bundles contain multiple environments. Object names alone are insufficient because repeated sibling names are common.

The main JSON now includes 1,694 verified sprite tints across 46 environments. Halloween2 retains its existing values where verified sprite data was unavailable. There is no SpriteColors overlay or second source of defaults in the importer. Keep the updated environment JSON and generated scenes together. A new external export must retain the added fields.

`NativeSpriteColorImportTest` covers native and explicit colors. `EnvironmentExportMetadataTest` covers source completeness and independent-export isolation. `InterscopeLightingParityTest` checks the resulting Interscope and Linkin Park scene decals.

## Interscope cars

The Interscope car investigation and retained lane corrections originate with **ziqangao**, [PR #693](https://github.com/Caeden117/ChroMapper/pull/693), commit `a06c3d5b242ba95236a46858b63344fa1b6acf5e` (November 5, 2026). The retained changes name the left/right laser, car-bounce, laser-spread and hydraulic lanes and use integer controls for bounce/hydraulics. They are applied during track-definition import so regeneration preserves the contribution. The car simulation retains native SpringJoints rather than the PR's ConfigurableJoint replacement.

The main Interscope JSON contains the native settings, separate expand filters, component references and each Rigidbody's startupState. environmentData.physics contains gravity, fixedDeltaTime and the startup sample's song time. Regenerate it with the ordinary `Environment/Create from Data` command. CreateUtils deserializes this export directly, without applying a retained physics capture afterward.

The original export already contains the car bodies under `RigidBodyWrapper`. The component-data alias imports them into `RigidBody` so the jump and joint references resolve. Missing connected bodies otherwise attach the joints to the world origin.

`NativeInterscopeCarBuilder` creates an inactive physics template and one shared `HydraulicCarTimeline` for events 8, 16 and 17. The template retains the native rigidbodies, springs and floor/wheel colliders. The timeline steps a separate local physics scene at the captured fixed interval and caches local body poses. Visual bodies are kinematic with Unity interpolation disabled, so paused time remains static and scrubbing uses the same poses as playback. Edits and reinitialization invalidate the simulation cache. Parent track animation remains separate from the local car motion.

The retained startup state comes from the first positive song-time sample because native physics advances during loading/countdown. Bounce randomness is stable per event/car in CM and does not consume Unity's shared random stream. It does not reproduce the native game's unrelated global random draws.

The verified capture used recorder 0.2.0, which identified wheel spheres but omitted their center/radius. Those two fields retain the existing export's values; they were not freshly measured by that capture. Recorder 0.2.1 includes them for future captures.

Regression coverage lives in `InterscopeCarImportTest` and `InterscopeCarTimelineTest`. The compact native hydraulic fixture retains only the basic events and sampled Car1 poses used by the comparison.

## Native clips, particle colors and renderer bounds

`Animation` records now include clipName, playbackSpeed, phaseSeconds and wrapMode alongside length/frame rate. The four original 1.44.1 clips are retained unmodified under `Assets/_Graphics/Animations/Environments/`; `NativeClips.json` records their source hashes. They drive the Gaga logo, Halloween crows/zombie hands and Halloween2 crow/skeleton hands.

The read-only recorder in workspace `DiagnosticTools/NativeEnvironmentAnimationSnapshot/` supplied the missing playback fields now stored on the 20 Animation records in the main exports. Raw recordings remain diagnostic artifacts outside the product repository. LegacyAnimationData resolves the clip asset and applies those fields through the normal component importer. There is no separate playback JSON lookup or capture-import menu.

`TimelineLegacyAnimation` samples the original clip at absolute song seconds plus the exported startup phase. Halloween's hands use native random starting times, so the export deliberately freezes one captured arrangement. It does not claim that every new native session chooses the same random phases. Pausing, rewinding and reloading in CM reuse those phases. Sampling remains local to the animation object and its children.

`ParticleLightTimeline` imports Imagine Dragons' two `ParticleSystemEventEffect` records into the basic-event timeline. It retains the native callback ordering and highlight decay at CM's deterministic 90 Hz preview cadence rather than advancing colors with wall-clock frames. Palette edits invalidate its color snapshots.

The exported `RectangleFakeGlowLightWithId` key now binds to the platform glow controller. Its companion `RectangleFakeGlow` record describes geometry on the same controller. Both records must share that component, including on regeneration, or they compete over the same material property block.

`CustomBoundingBoxData` imports native renderer bounds through `NativeRendererBounds`. Native `customBoundSize` contains extents, so the Unity Bounds size is twice that value. The helper restores the renderer override in Awake because Unity does not serialize renderer bounds overrides into a scene. Keep this separate from mesh bounds and shader displacement settings.

`EnvironmentAnimationImportTest` checks actual export records and generated scene bindings. `LegacyAnimationTimelineTest` compares the imported clips with recorded native transforms and checks seeks/loops. `LegacyAnimationClockTest` checks the generated logo's paused, playing and rebound clock callbacks. `ParticleLightTimelineTest` covers pause, rewind, native decay and palette invalidation. `NativeRendererBoundsTest` checks cloned renderer bounds and culling.

## Mesh library

An exported mesh hash is a SHA-256 hash of GPU-buffer data. It is not a hash of the FBX file.

Do not match a mesh by name alone. Compare its source scene, known names, bounds, and geometry channels before assigning it to `EnvironmentMeshSO`.

Place a mesh used by one environment in its folder under `Assets/_Graphics/Models/Environments/`. A related environment family can share its family folder.

Place a mesh used across unrelated environments in the shared environment-model root. These locations make ownership and reuse visible.

Preserve custom bounds on native mesh assets. A safe vertex-space conversion can recalculate normals when the source normals are unsuitable.

Generated renderers assign imported meshes directly to `MeshFilter.sharedMesh` and particle renderers. Thus, an FBX node transform cannot correct mesh coordinates.

FBX mesh data must contain the correct units and handedness before direct assignment. See [Mesh format and coordinates](../../_Graphics/GUIDES.md#mesh-format-and-coordinates) for conversion details.
