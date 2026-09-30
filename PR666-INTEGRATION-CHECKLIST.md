# PR 666 integration checklist for `gls-color-dist-shifts-final`

This branch is currently based on dev commit `05186ebd3d0989af9e6e4390161030dacd02b0fb`. PR 666 (`a2607659f6919b7a891aa560378214337cf9e7c7`) already merged that exact dev commit, so the work below is the GLS work that must survive when this branch is updated after PR 666 lands.

Do not resolve the listed files wholesale with “ours” or “theirs.” PR 666 renames environment APIs and moves rendering code while this branch adds GLS behavior on the old paths. Reapply the GLS behavior to the PR 666 destination and retain PR 666’s API/path changes.

## Highest-risk manual ports

### `Assets/__Scripts/Environments/Effects/Components/LightObjects/LightColorTween.cs`

PR 666 moves this file to `Assets/__Scripts/Utils/Tween/LightColorTween.cs` without changing the dev implementation. Reapply the GLS branch’s changes at the new path:

- independent normal-color, strobe-color, and strobe-pulse easing;
- the terminal-tail elapsed-time handling;
- zero-to-strobe phase correction;
- RGB, HSV, and TrueHSV strobe interpolation;
- the strobe timing behavior consumed by `LightColorGroupEffect`.

Do not recreate the old file after the rebase. The final implementation belongs at `Assets/__Scripts/Utils/Tween/LightColorTween.cs`.

### `Assets/_Graphics/Shaders/ShaderLibrary/Easings.hlsl`

PR 666 moves this library to `Assets/_Graphics/Shaders/ShaderLibrary/Core/Easings.hlsl`. Reapply the GLS branch’s shader easing changes at the new path:

- Beat Saber 1.44.1 Elastic equations;
- static Back constants so they are not compiled as unbound uniforms;
- Beat Saber InOut Back, Elastic, and Bounce functions used by GLS preview shaders.

Update includes to the PR 666 destination and do not restore the old path.

### `Assets/__Scripts/Environments/Effects/GLS/LightColorGroupEffect.cs`

Retain PR 666’s environment/color-scheme wiring while porting all GLS endpoint preparation:

- resolve normal and strobe colors independently;
- apply box/event color-shift arrays with both dense chunk progress and affected-light progress;
- share tween configuration with `GLSColorTimeline`;
- normalize no-strobe endpoints to their primary color and brightness;
- keep strobe easing separate from color easing;
- select RGB/HSV/TrueHSV from authored easing type;
- scale authored cycles-per-beat strobe frequency using the map BPM domain;
- retain strict next-group interruption boundaries;
- cache the owning color box and both spatial progress values in `LightColorEventStateData`.

This port depends on the branch’s `GLSColorTimeline`, `GLSColorDistribution`, `GLSEventCommon`, easing-data, and color-model changes. Compile errors here should be treated as a missing companion port, not removed behavior.

### `Assets/_Graphics/Shaders/Object/BasicGradient.shader`

PR 666 substantially changes rendering/shader infrastructure, while this branch substantially changes the transition-ribbon shader. Manually combine both implementations. Preserve these GLS behaviors:

- separate color and strobe easing dispatch;
- Beat Saber InOut easing IDs;
- RGB/HSV/TrueHSV interpolation;
- per-light distribution texture rows and clocks;
- independent normal/strobe shift output;
- distributed timing and per-light ribbon strips;
- strobe phase/fade parity with `LightColorTween`;
- correct sRGB/linear conversion and display compensation for texture-sourced HDR colors.

Validate pixels and material bindings after the merge; a compiling shader is not sufficient.

### `Assets/__Scenes/03_Mapper.unity`

Both branches heavily modify this scene. PR 666 changes rendering, cameras, and environment infrastructure. The GLS branch adds editor UI and serialized references. Reapply the GLS scene changes onto the PR 666 scene rather than choosing either complete scene:

- GLS shift picker/row UI and its controller references;
- event and group icon material/sprite references;
- incoming/outgoing ribbon renderer references;
- controller fields added by the GLS input/view changes;
- any GLS prefab overrides introduced by the branch.

Open the merged scene in Unity and verify every new serialized field is assigned. Do not insert comments into the Unity YAML.

## Direct C# overlaps

The following files exist at the same path in both branches and need a semantic merge.

### Beatmap model and appearance

- `Assets/__Scripts/Beatmap/Appearances/EventAppearanceSO.cs`: retain PR 666’s environment definition rename and add the GLS branch’s red desync-risk presentation for ring/zoom/laser events.
- `Assets/__Scripts/Beatmap/Base/BaseDifficulty.cs`: retain `RuntimeTrackDefinitions`/`TrackDefinitionsSO`; also retain the GLS branch’s public nullable `SongBpm`, which is used to convert authored strobe cycles per beat into `SongBpmTime`.
- `Assets/__Scripts/Beatmap/Containers/EventContainer.cs`: retain PR 666’s `TrackDefinitions` API and port the pooled hover-only desync warning label/reset behavior.
- `Assets/__Scripts/Beatmap/Containers/GLSEventContainer.cs`: retain PR 666’s renamed track-definition API and port `GLSEventIconView`, the dedicated incoming ribbon, icon refresh, and color-hover labels.
- `Assets/__Scripts/Beatmap/Containers/GLSGroupContainer.cs`: retain PR 666’s renamed track-definition API and port icon display, incoming ribbons, physical light-count propagation to preview ghosts, and hover-label cleanup.
- `Assets/__Scripts/Beatmap/Requirements/ChromaGLSReq.cs`: combine PR 666’s current requirement infrastructure with the GLS branch’s required-versus-suggested scan for extended distribution/node easings across color, rotation, translation, and FloatFX boxes.

### Runtime context, grids, placement, and input

- `Assets/__Scripts/Editor/BeatmapRuntimeContext.cs`: retain PR 666’s `TrackDefinitions`/color-scheme changes and port reset/population of authoritative GLS physical light counts plus `GetGlsLightCount`.
- `Assets/__Scripts/Editor/Grid/Collections/EventGridContainer.cs`: retain every PR 666 `TrackDefinitions` rename and port the 20 ms ring/zoom/laser desync-risk index, localized relinking, and appearance refresh behavior.
- `Assets/__Scripts/Editor/Grid/Collections/GLSEventGridContainer.cs`: retain PR 666’s API names and port physical-light-count caching plus retention of incoming and outgoing ribbons across viewport refreshes.
- `Assets/__Scripts/Editor/Grid/Collections/GLSGroupGridContainer.cs`: retain PR 666’s API names and port active-environment light counts into primary containers and pooled preview ghosts.
- `Assets/__Scripts/Editor/Grid/Placements/GLSGroupPlacement.cs`: retain PR 666’s API names and port ribbon-hit passthrough plus queued-preview generation using physical light counts and indexed color-boost state.
- `Assets/__Scripts/Editor/Grid/SelectionController.cs`: retain every PR 666 `TrackDefinitions` rename and port whole-selection paste/shift clamping, slider and GLS child extents, BPM-aware song-end projection, inner-node song-end bounds, and precision-safe snapping.
- `Assets/__Scripts/Editor/Input/BeatmapEventInputController.cs`: retain PR 666’s renamed environment API and port desync-warning hover routing plus three-state RGB/HSV/TrueHSV ribbon cycling.
- `Assets/__Scripts/Editor/Loading/MapLoader.cs`: retain PR 666’s loading changes and explicitly relink ring events after load so desync-risk flags are computed for loaded maps as well as edited maps.

### Settings and editor UI

- `Assets/__Scripts/Settings/Settings.cs`: the GLS branch changes `CameraAA` from `0` to `3` for world-space text, but PR 666 removes the old post-process-AA interpretation and maps the setting to camera MSAA. Do not blindly keep the numeric value. Re-evaluate the desired default against PR 666’s new AA pipeline and test icon/text readability.
- `Assets/__Scripts/UI/Editor/PlacementController/EventBoxViewController.cs`: retain PR 666’s `TrackDefinitions` rename and port both normal and strobe `GLSColorDistributionArrayViewController` fields, initialization, visibility, queued-data copy, command dispatch, and refresh.

`Assets/__Scripts/Editor/Grid/CommonBeatmapUtils.cs.meta` overlaps only because the GLS worktree currently has an untracked meta while PR 666 adds one. Keep PR 666’s meta/GUID; do not manufacture a second asset identity.

## Serialized prefab overlaps

- `Assets/_Prefabs/MapEditor/Beatmap/GLS Event.prefab`: reapply the event icon view, incoming ribbon instance, distribution texture bindings, and new serialized controller fields to the PR 666 prefab.
- `Assets/_Prefabs/MapEditor/Beatmap/GLS Group.prefab`: reapply the group icon view, incoming ribbon instance, distribution texture bindings, light-count state, and new serialized controller fields to the PR 666 prefab.

After resolving both prefabs, instantiate/pool each one in Unity and verify no missing-script or unassigned-reference warnings occur.

## Other shader overlaps

- `Assets/_Graphics/Shaders/Editor/Grid/XZ.shader`: combine PR 666’s shader-library/layout changes with derivative-filtered grid coverage, transparent blending, HJD/sub-beat/lane anti-aliasing, and the unused-spacing guard.
- `Assets/_Graphics/Shaders/Object/Note.shader`: retain PR 666 rendering changes and port the opt-in GLS distribution texture, per-renderer light count/depth center, and beveled event-block strip layout.

The branch’s new `Assets/_Graphics/Shaders/ShaderLibrary/GridCoverage.hlsl` and meta file are companion inputs for the XZ grid shader and must remain present.

## Test overlap and test migration

- `Assets/TestsEditMode/ModRequirementsTest.cs`: PR 666 already uses this renamed path. Keep the GLS branch’s extended easing requirement cases in that file and preserve all PR 666 tests.
- `Assets/TestsEditMode/IndexFilterHelperTest.cs.meta`: use the PR 666 GUID if the files differ only because this branch generated an untracked meta.

The GLS tests that cover the manual ports must remain present after the update, especially:

- `BeatSaberEasingParityTest`
- `EventDesyncRiskTest`
- `GLSColorEasingInputTest`
- `GLSColorOwnershipTest`
- `GLSColorTimelineBoundsTest`
- `GLSEmptyLaneOwnershipTest`
- `GLSEventIconResolverTest`
- `GLSRibbonHoverTest`
- `GLSRibbonParityTest`
- `GLSColorDistributionCopyPasteTest`
- `GLSSongBoundaryTest`
- `GLSStrobePhaseTest`
- `GLSTransformAutomaticLanePlaybackTest`
- `SongBoundaryTest`
- `InputEasingSceneTest`

Also retain the existing modified GLS tests for basic-event HSV interpolation/ribbons, GLS strobe fade, transition caching, appearance, translation, selection, ring zoom, V3/V4 serialization, index filters, and requirements.

## Generated or incidental Unity overlaps

These paths overlap PR 666 but should not be hand-merged as source code:

- `Assets/_Graphics/Materials/Font/NotoSansCJKjp.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `Packages/com.llealloo.audiolink/Runtime/RenderTextures/rt_AudioLink.asset`

Keep PR 666’s version first. Only reserialize/regenerate one of these assets if the GLS feature actually depends on a verified changed property. Review the Unity Inspector diff before committing regenerated YAML.

## Companion GLS files that must not be lost

The direct-overlap files above refer to new branch files that will not necessarily conflict. Ensure these survive the update:

- `Assets/__Scripts/Beatmap/Appearances/GLSEventIconType.cs`
- `Assets/__Scripts/Beatmap/Containers/GLSEventIconView.cs`
- `Assets/__Scripts/Environments/Effects/GLS/GLSColorTimeline.cs`
- `Assets/__Scripts/UI/Editor/PlacementController/GLSColorDistributionArrayViewController.cs`
- `Assets/__Scripts/UI/Editor/PlacementController/GLSColorDistributionRowView.cs`
- `Assets/__Scripts/UI/Editor/PlacementController/GLSColorDistributionPicker.cs`
- `Assets/_Graphics/Shaders/GLSIconSprite.shader`
- `Assets/_Graphics/Shaders/ShaderLibrary/GridCoverage.hlsl`
- `Assets/_Graphics/Materials/Editor/GLS Icon Sprite.mat`
- `Assets/_Graphics/Textures/GLS Event Icons/`
- `Assets/_Prefabs/UI/GLS Shift Row.prefab`

Keep each corresponding `.meta` file with its asset so scene and prefab references retain their GUIDs.

## Verification after updating from post-666 dev

1. Search for stale old paths and names: `LightColorTween.cs` under `LightObjects`, `ShaderLibrary/Easings.hlsl`, and the singular `TracksDefinition` runtime API.
2. Open `03_Mapper`, the GLS Event prefab, and the GLS Group prefab in Unity; verify all serialized icon, shift-array, and incoming-ribbon references.
3. Run the full ChroMapper test suite from this exact worktree with `pwsh ./Assets/Tests/Test-ChroMapper.ps1`.
4. Exercise color, rotation, translation, and FloatFX GLS nodes; do not validate only color nodes.
5. Manually inspect RGB/HSV/TrueHSV transitions, normal/strobe shift arrays, zero-to-strobe transitions, BPM-scaled strobe timing, filtered per-light ribbons, icon hover labels, copy/paste at song boundaries, and desync-risk warnings.
6. Inspect ribbon and node pixels in the editor. Managed tests do not prove shader/material/serialized-reference correctness.











Notes:

- [ParametricBoxLight.cs (line 72)](C:/src/BeatSaberStuff/ChroMapper-pr666-assets-review/Assets/__Scripts/Environments/Components/Lightings/LightControllers/ParametricBoxLight.cs:72) fixes a dev bug by applying the calculated newCol containing AlphaMultiplier/MinAlpha.
- ParametricSpriteLight.cs and SpriteLightController.cs only have syntax/whitespace changes.
- [RectangleFakeGlowLightController.cs (line 18)](C:/src/BeatSaberStuff/ChroMapper-pr666-assets-review/Assets/__Scripts/Environments/Components/Lightings/LightControllers/RectangleFakeGlowLightController.cs:18) adds initialization-time component discovery; it does not remove dev behavior.
- CombinedLightsController.cs changes LightIntensityController to the renamed LightIntensityData, stores the resulting color, and stops throwing for an invalid MixType. That last part is a small behavior change—invalid serialized enum values become a silent/default result—but no valid dev behavior is lost.
- CombinedLightsGroupController.cs is the same data-type rename plus formatting.
- LightIntensityData.cs is effectively the renamed LightIntensityController.
- The three untracked ring classes are byte-identical moves from dev.
- SDF, spectrogram, particle, lightmap, color-array, and tint controllers/data are genuinely new.