# ChroMapper environment generation

The JSON files in `Assets/__Scenes/Environments/Data/` are UGEcko exports of Beat Saber environment data. Keep these source files unchanged.

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

## Mesh library

An exported mesh hash is a SHA-256 hash of GPU-buffer data. It is not a hash of the FBX file.

Do not match a mesh by name alone. Compare its source scene, known names, bounds, and geometry channels before assigning it to `EnvironmentMeshSO`.

Place a mesh used by one environment in its folder under `Assets/_Graphics/Models/Environments/`. A related environment family can share its family folder.

Place a mesh used across unrelated environments in the shared environment-model root. These locations make ownership and reuse visible.

Preserve custom bounds on native mesh assets. A safe vertex-space conversion can recalculate normals when the source normals are unsuitable.

Generated renderers assign imported meshes directly to `MeshFilter.sharedMesh` and particle renderers. Thus, an FBX node transform cannot correct mesh coordinates.

FBX mesh data must contain the correct units and handedness before direct assignment. See [Mesh format and coordinates](../../_Graphics/GUIDES.md#mesh-format-and-coordinates) for conversion details.
