# ChroMapper environment generation

The JSON files under `Assets/__Scenes/Environments/Data/` are the UGEcko game-export source files consumed by the current environment generator. In this document, "export JSON" means one of those checked-in files, such as `Assets/__Scenes/Environments/Data/TheSecondEnvironment.json`; it does not mean a beatmap JSON file. Keep these exports faithful to the game dump. ChroMapper-specific corrections belong in the import path, not in those source files.

## Editor commands and generated data

Two similarly named editor commands have different responsibilities:

- `Environment/Populate Build Data` is implemented by `EnvironmentBuildPopulate.cs`. It rebuilds shared mesh, material, sprite, shader, and layer libraries. It does not import track names or lane order.
- `Environment/Update Environment List` is implemented by `EnvironmentListUpdate.cs`. It reads each raw environment JSON file and calls `LightTracksDefinition.CopyTo` in `Structures/EnvDataInfo.cs`. That call generates or refreshes `TracksDefinitionSO` assets under `Assets/__Scripts/Environments/TracksDefinitions/`.

`EnvironmentSceneCreator` and the files under `Create/` consume the environment data and populated libraries to build environment scenes. They do not decide the editor-facing basic-event lane names or ordering.

## Correcting exported track names

The raw `lightTracks.eventTracks[].trackName` value is copied by `LightTracksDefinition.CopyTo`. Environment-specific ChroMapper aliases are applied there by `GetBasicTrackName(environmentId, eventType, exportedName)`.

For the Skrillex mixed ring lanes, the raw export remains unchanged while the importer produces:

| Event type | Raw export | ChroMapper label |
| --- | --- | --- |
| 8 | `Ring Rotation` | `Ring 2 Rotation / Zoom` |
| 9 | `Laser Mode` | `Ring 1 Rotation / Zoom` |

Changing an import alias requires updates in both places below:

1. Update the import rule in `Structures/EnvDataInfo.cs` so a future `Environment/Update Environment List` run reproduces the correction.
2. If the update command will not be run, make the same change in the already-generated `Assets/__Scripts/Environments/TracksDefinitions/<EnvironmentId>TracksDefinition.asset` file.

Do not edit the raw UGEcko JSON just to make a ChroMapper label friendlier. `EnvironmentBuildPopulate.cs` is also not the right location because it never creates track definitions.

`Assets/Tests/Editor/SkrillexRingLaneTest.cs` covers both the import-time result and the checked-in generated asset. The `Tests` assembly references the editor-only `EnvironmentInfo` assembly so the import path can be exercised directly.

## Basic-event lane order

Lane-order fixes are runtime presentation changes. They belong in `Assets/__Scripts/Editor/Grid/CreateEventTypeLabels.cs`, not in the environment builder, export JSON, importer, or generated track-definition asset. In particular, the Skrillex lane-order correction must be implemented in `CreateEventTypeLabels` while retaining each lane's original event type.

The runtime presentation starts from the generated definitions and flows through these collections:

1. The raw `lightTracks.eventTracks` array establishes the source order.
2. `LightTracksDefinition.CopyTo` currently maps that list without sorting it.
3. `TracksDefinitionSO.Register` appends each definition to serialized `basicEntries`.
4. `TracksDefinitionSO.Initialize` builds `Basic` in the same insertion order.
5. `CreateEventTypeLabels.AddBasicLabels` enumerates `Basic` to create lanes from left to right.

The label builder performs two passes: tracks whose `Kind` is `Lights` are emitted first, then every non-light kind. Source order is preserved within each pass. Consequently, the displayed lane order can differ from serialized `basicEntries` without changing any event type or persisted lane identity.

The Second is the precedent for a presentation-only lane reorder. During the July 2026 work, an attempted reorder of its export JSON and generated `.asset` was explicitly reverted so event types and serialized track identities remained untouched. `CreateEventTypeLabels` was changed instead to emit all `Lights` definitions before non-light control definitions. The Second therefore displays `Logo, Runway, Left Flags, Right Flags, Buildings, Boost Colors, Ring Zoom`, while its unchanged serialized order remains `1, 4, 2, 3, 5, 9, 0`.

Every checked-in revision of The Second's export has that same `1, 4, 2, 3, 5, 9, 0` sequence, starting with the UGEcko-authored import. The recent ChroMapper change did not create that export order. Kaleidoscope currently uses the numeric export sequence and has no current-system override.

For the Skrillex visual reorder, preserve the export JSON, the generated track-definition asset, and every event type. Add the Skrillex-specific presentation rule where `CreateEventTypeLabels` enumerates definitions, and retain the `(visible lane, event type)` mapping in `laneObjs` so placement, selection, mirroring, and authored beatmap data continue using the original event type. Because this code runs when ChroMapper creates or refreshes the event-grid labels, no environment regeneration is required.

### Unrelated historical prefab ordering

Commit `7c22ff71a` from 2022 also changed a `LightingManagers` array in the retired `Assets/_Prefabs/MapEditor/Platforms/The Second.prefab`. That old prefab mechanism is not the recent lane-order change and is not part of the current generated-environment pipeline.
