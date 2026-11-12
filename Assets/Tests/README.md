# Tests

## Running tests

From the repository root, using PowerShell 7 or newer (works on linux):

```powershell
# run all tests
pwsh ./Assets/Tests/Test-ChroMapper.ps1
# run specific test
pwsh ./Assets/Tests/Test-ChroMapper.ps1 -TestFilter 'Tests.Infrastructure.InputTestPolicyTest'
# or filter to specific tests using anything from https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-command-line.html#testfilter
# EG run all tests with "GLS" in their class/test name
pwsh ./Assets/Tests/Test-ChroMapper.ps1 -TestFilter '(?i)GLS'
```

The runner executes PlayMode and EditMode tests and saves the full logs and results under `TestResults/cli/<timestamp>`. Add `-IncludeManual` to include manual tests. Close Unity if it has this checkout open. An editor using another worktree can stay open.

## Megamix note jump rotation

`MegamixNoteJumpParityTest` uses the five original notes at b414.500122–415.5 from Megamix's ExpertPlus Lawless difficulty. `MegamixNoteJumpFixture.json` retains the 14 preceding BPM events, including the ramp from 160 to 180 BPM, with base BPM 160, NJS 18 and offset -.75. The four affected notes have no transform animations or tracks, and the source map has no VNJS events. The test checks their jump rotations against Beat Saber 1.44.1 `NoteJump.Init`/`ManualUpdate` and Noodle's `DoNoteLook`, through paused seeks and deterministic playback. Source rotation-table indices are 1, 2, 5 and 6. The fixture is 1,925 bytes, SHA-256 `2f44f1b2a67c339975582a58940686c2756bbb7c741bfb9dcf123dec4a0ee224`.

Both source-map regressions failed before the fix. They include the native same-swing final angle (153.43495 degrees for the blue pair), finalize LateUpdate before facing assertions, and exercise despawn/respawn. A separate case verifies cached floor-layer seeds after stack edits. `NoteJumpRotationTest` checks stage landmarks and a chain-head control. `FullMapJumpRotationMatchesNativeStages` is explicit and requires the ignored original `TestResults/Megamix/FullMap.json`. The targeted 17-case run, including that full-map check and `DeleteItAllNoteLookParityTest`, passed in `TestResults/cli/20261015-170546`.

## Input tests

For mapper tests that send input, inherit [InputTestBase](Infrastructure/InputTestBase.cs). `PreviewWorkflowTestBase` and `BasicEventChunkingTestBase` already inherit it. The base isolates the Input System from editor focus and physical devices. Creating a separate `CMInput` asset alone still uses the shared runtime.

1. Call `InitializeVirtualInput(includeKeyboard: true)` before creating or enabling test actions. Use `false` for mouse-only tests.
2. Bind the production callbacks to your test actions. Send input through `VirtualMouse`, `VirtualKeyboard`, and the helpers on the base class.
3. Dispose the test actions before calling `TearDownVirtualInput`. Put cleanup in teardown or a `finally` block so it also runs after a failed assertion.

For several input reports in one Unity frame, use `queueEventOnly: true` and call `InputSystem.Update()` while the isolated runtime is active. Process modifier presses before sending the mouse or key event that uses them. Assert the effect of the production callback, such as a changed selection or a new undoable action.

If you manage `InputTestFixture` directly, create dedicated virtual devices and disable shared action maps before `Setup()`. Dispose your actions before `TearDown()`, then restore the shared maps afterward.

Use the virtual device references instead of `Mouse.current` or `Keyboard.current`. `InputTestPolicyTest` checks for those accesses. See the [input architecture guide](../Input/README.md) for action maps, bindings, and composites.

## Playback tests

Jenkins runs Linux Unity under Xvfb without a dependable native audio clock. A test that waits for `AudioSource.time` to advance can pass on Windows and hang on Jenkins.

Use `TestUtils.StartDeterministicPlaybackAtSongBpmTime` and `TestUtils.PauseDeterministicPlayback` to enter and leave the production playing state with native audio stopped. Those helpers do not freeze the clock: `AudioTimeSyncController.Update` still advances it each frame. When stepping time explicitly, keep that update from advancing the clock between your steps and assertions, while letting the playback callbacks and animation updates run. [AuroraMapParityTest](Editor/AuroraMapParityTest.cs) shows this setup and restores the controller afterward.

## Map fixtures

- Pass the map's BPM, environment, clip length, and `difficultyInfo` to `TestUtils.ReloadMap`. The default reload reuses the mapper scene and replaces the environment. Use `forceSceneReload: true` when the test needs a fresh scene.
- If a fixture restores the map by reloading it, override `CleanupTestObjects` with an empty body. Ordinary object cleanup can destroy enhancement controllers before the reload releases their targets.
- Restore `UIModeType.Normal`, the editing camera, and saved settings in `[UnityTearDown]`. For fixtures that replace the shared map, reload an empty map with `forceSceneReload: true` in `[UnityOneTimeTearDown]`.
- Clone fixture JSON before loading it if you also use it to calculate expected results. The loader mutates custom-event data.
- Capture rendered results after animation `LateUpdate` calls. `AuroraRenderStageProbe` in [AuroraMapParityTest](Editor/AuroraMapParityTest.cs) is an example.

## Parity fixtures

Reduced fixtures retain the source objects, event order, and referenced custom data, including named points, materials, environment enhancements, and track parents. Keep these dependencies when reducing another map. Native behavior references are the selected Beat Saber 1.44.1 assets and managed decompilation plus Heck/Noodle/Chroma sources. Render comparisons use linear HDR readback and explicit sRGB PNG encoding. A controlled render comparison does not establish exact gameplay exposure or recording-camera parity.

- `MegamixRunwayParityTest` covers BigMirror's oblique side-light rotation at beat 163.109 and nearby forward/reverse seeks. `LightRotationAxisParityTest` compares accumulated rotation with native `Transform.Rotate(axis, angle, Space.Self)`. The paired-light effect retains the native paired effect's Euler semantics. The optional full map is `TestResults/Megamix/FullMap.json`.
- Superluminal (358a7, modus/nasafrasa) fixtures use Standard ExpertPlus V3.2.0, GagaEnvironment, 150 BPM, NJS 16, and jump offset 0. Reduced cases cover note decoration/dissolve/facing, floor travel and jump continuity, surface lighting, tunnel material and probe filtering, rocket materials, held molecule transforms, edit-mode wall color/opacity, and synchronous wall seeks. Full-source cases use the ignored `TestResults/Superluminal/SuperluminalFullMapFixture.json`; note and floor-travel cases also accept `CHROMAPPER_SUPERLUMINAL_FULL_MAP_PATH`. Full cases skip when their source is unavailable, while reduced cases always run. Do not rely on `Explicit` alone to exclude full cases from Unity's regular runner. Optional `CHROMAPPER_SUPERLUMINAL_CAPTURE_DIRECTORY` saves captures for supported cases.
- `SuperluminalWallSeekTest` covers immediate and subsequent wall matrices in Playing, Preview, and after leaving Normal. Its fine-scrub cases drive production input callbacks through isolated virtual devices for 1/64-beat forward/backward nudges, asserting one timeline notification and stable wall matrices. Local profiler captures and their capture-specific reports are under `ProfilerCaptures`; `TestResults/Superluminal/tiny-scrub-profile-comparison.json` records the compared inputs and measured hierarchy timings.
- `DeleteItAllOverheadLightParityTest` covers reflective NeonTop glass at beat 405.484 in Interscope. `DeleteItAllGraySurfaceParityTest` separately covers the PillarL/PillarR surrounds using native lightmap atlas coordinates, with the full-map camera facing 90 degrees right at level pitch. Optional full cases use `TestResults/DeleteItAll/FullMap.json`.

For Superluminal floor travel, native `VariableMovementDataProvider` moves notes 100 units over 0.5 seconds before jumping. Chroma's `spawnEffect` controls the flash, and Noodle's `disableNoteGravity` selects the authored floor/start layer while the jump arc still runs. The tests retain CM's one-unit hit-plane convention and approximate jump arc. Held-transform molecule tests distinguish constant track values, which are written once, from parent motion and live-provider clocks.

## Arc geometry

`ArcGeometryParityTest` compares spline samples with Beat Saber 1.44.1's `SliderMeshController.CreateBezierPathAndMesh` and the shipped ShortSlider, MediumSlider and LongSlider prefabs. All three serialize tangent length and middle-anchor offset as 2.5 metres, with middle-control modifiers X/Y 0.25 and Z 0.15. These values come from `BeatSaberDecompilation/1.44.1/SerializedAssets/AssetRipperOutput`, exported from the configured 1.44.1 installation with AssetRipper 1.3.14 (see `asset-export-manifest.json`). The cases cover middle-anchor offsets, directionless arcs, attached-note angle offsets, and the 0.225-metre note-edge offset, reduced to 0.0225 metres for unattached endpoints. They also check curve refresh after attaching and deleting a note. CM's note jump and arc depth animation remain approximations.

## Note spawn effects

`MopeMopePostHitTest` covers b266.796875 (80.0390625 seconds) with deterministic playback and native audio stopped. `MopeMopePostHitFixture.json` retains three original `FlyBomb` notes and their two b200 events, all using inline curves. It checks the descending jump parabola, the cubic final-quarter travel, the local facing rotation held after the hit, and facing independent of CM's viewing distance. Pause, resume, forced pool refresh, Normal/Playing switches and tiny or distant seeks must reconstruct the same pose and dissolve. The playback and pause checks use 1/64 snapping so they compare the same beat.

`MopeMopeNoteRowFixture.json` retains the three original b272 notes. The fake `eyes` notes use raw layer 8, which the game converts to Base before same-swing angles are calculated. The test checks native cut directions despite different jump durations and loading order. The unfixed code added a 26.565-degree turn using raw layer 8 and whichever partners happened to be loaded. Same-swing lookup now uses the complete data row, including Noodle's fake color notes.

`MopeMopeSeekRecycleFixture.json` reduces a stopped-seek failure to one fake note and one time curve. The curve finishes while the raw jump window still contains the note. Retained notes crossing their hit beat must be marked for animation-owned recycling, and a seek that recycles one must stop before applying its pose. The regression reproduced the `ApplyAnimation` null reference before the caller-order fix.

`FullMapKeepsTheSameObjectsAndPosesAcrossPreviewRoutes` is explicit and requires the original difficulty at `TestResults/MopeMope/FullMap.json`. It compares every active fake note across the same pause, view-switch and seek routes, and writes render captures under `TestResults/MopeMope/<timestamp>/`. The unfixed full map dropped active fake notes from 363 to 164 on pause. An additional frozen-note regression checks that a time curve holding exactly 1 finishes the fake note during playback, matching `NoteJump.ManualUpdate` rather than leaving it in the pool until a seek.

`MopeMopeStretchedNoteTest` retains one original b236 `eyes` note, the b229 `AnimateTrack` event and its complete named `Laugh` point definition. The raw `_lineLayer:8` is converted to Base by Beat Saber 1.44.1's `BeatmapTypeConverters.ConvertNoteLineLayer` before Noodle's `SpawnDataManager` calculates gravity. CM must therefore use the 0.858333 jump apex while preserving the raw layer for editing/export. An explicit custom Y of 8 retains Noodle's 5.058333 apex. Forward/backward seeks and deterministic playback near b235 verify the jump height and the stretched renderer's bottom below foot level. The raw-layer case failed before the fix with a 5.058333 apex.

`MopeMopeFrozenNoteTest` covers the b200 (60-second) pause and seek regression from the same map. `MopeMopeFrozenNoteFixture.json` retains the original b191.5 fake `FaceTrack` note and its b185 `AnimateTrack` event, including all inline time, rotation and dissolve curves. Playback holds it at approximately 0.476 of its jump with body visibility 0.3 and arrow visibility 0.6. The tests compare that pose after pause, forced pool refresh, resume, and repeated seeks from b0 and b400. They also verify removal when the time curve finishes and restoration on a backward seek. Both tests failed before the fix because the paused pool discarded notes outside its hit-time window.

`MopeMopeNoteSpawnTest` reduces 129bf (Mope Mope, waika), Standard ExpertPlus V2.0.0, to one original note and one `AssignPathAnimation` event per fixture. Both use TriangleEnvironment, 200 BPM, NJS 16 and jump offset 2. `MopeMopeNoteSpawnFixture.json` keeps the b141 `BlueRight` note (NJS 14) and its 20-lane side-entry path. `MopeMopeAngledNoteSpawnFixture.json` keeps the b179 `ATrack` note whose jump starts at b175, with a four-lane upward offset and 45-degree local rotation. All retained curves are inline and reference no point definitions or track parents.

The tests verify the world position and rotation throughout the flash during paused seeks, reverse seeks and deterministic playback. Noodle's `BeatEffectUseWorldPosition` anchors the flash at the note's jump-start world pose, even when its body starts dissolved. Before the fix, inherited path keys were ignored, placing the first flash 12 world units too close to the grid and leaving the second upright.

## Native environment lighting

`NativeEnvironmentProbeParityTest` covers all 47 baked environments, shader-global probe bindings, scene regeneration, and native face/mip GPU samples. `NativeEnvironmentLightmapParityTest` covers the eight legacy lightmaps in Billie, Interscope, Linkin Park, and Skrillex, including renderer atlas rectangles, import/build paths, and inactive clones. Four native SaberBurnMarksArea renderers are intentionally absent from CM scenes. Loading all environments requires a fresh mapper reload in teardown to restore shared lightshow references.

The independent expected samples are in `Fixtures/NativeEnvironmentProbeSamples.json` and `Fixtures/NativeEnvironmentLightmapSamples.json`. Production provenance is recorded in `NativeProbeSources.json` and `NativeLightmapSources.json` under `Assets/_Graphics/Textures/Environments/BakedLighting`. `BakedLightmapAtlas` preserves renderer properties Unity omits without a LightingDataAsset.

Regenerate native probes and lightmaps with `Assets/Editor/Environments/ImportNativeReflectionProbes.py` using Python 3.12+, UnityPy 1.25.2, numpy, and texture2ddecoder. Preserve GUIDs, shipped mip chains, sRGB decoding, and trilinear/clamp sampling. The `.cmprobe` format is GZip-compressed: `CMR1`, little-endian width UInt16, mip-count byte, sRGB Boolean, then RGB24 texels in face/mip order. `Fixtures/GagaWaterProbeMipReference.bytes.gz` is an independent compact reference containing probe/face/mip-ordered native mip levels 2 and 3 for both Gaga probes. The original PNG exports retain base-image evidence but do not supply the native mip chains.

The compiled Chroma request inventory and coverage qualifications are documented in [ChromaEnvironmentRequestRouting.md](ChromaEnvironmentRequestRouting.md), [ChromaMaterialGeometryCoverage.md](ChromaMaterialGeometryCoverage.md), and [ChromaLightRequestRouting.md](ChromaLightRequestRouting.md).

## Voidwalkers arc initialization

`VoidwalkersArcLifecycleTest` loads Monstercat2 with a saved cursor at beat 83, enters Preview and Playing with animations enabled and disabled, then seeks and forces arc-pool reuse. It checks jump parameters and all loaded spline samples for finite positions and leaves Unity's unexpected-error detection enabled. A separate case checks a newly initialized movement provider before any timeline update. The fixture preserves all 67 arcs, 185 notes at their endpoint beats, BPM and rotation events, and the complete `customData` section (whose only key is `time`) from Standard ExpertPlus of the local `50033 (Voidwalkers - Nallak, Kinz, Uzi, Sonoluminn)` map. Source difficulty SHA-256: `FD3356A850DFC66358727588573EBAA4B09032BDCE91C598849C7F402EE44A15`. Metadata is 142 BPM, NJS 18, jump offset -0.5. No explicit test seek or manual spline recomputation precedes the first preview attachment.
