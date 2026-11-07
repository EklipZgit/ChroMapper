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

## Native environment lighting

`NativeEnvironmentProbeParityTest` covers all 47 baked environments, shader-global probe bindings, scene regeneration, and native face/mip GPU samples. `NativeEnvironmentLightmapParityTest` covers the eight legacy lightmaps in Billie, Interscope, Linkin Park, and Skrillex, including renderer atlas rectangles, import/build paths, and inactive clones. Four native SaberBurnMarksArea renderers are intentionally absent from CM scenes. Loading all environments requires a fresh mapper reload in teardown to restore shared lightshow references.

The independent expected samples are in `Fixtures/NativeEnvironmentProbeSamples.json` and `Fixtures/NativeEnvironmentLightmapSamples.json`. Production provenance is recorded in `NativeProbeSources.json` and `NativeLightmapSources.json` under `Assets/_Graphics/Textures/Environments/BakedLighting`. `BakedLightmapAtlas` preserves renderer properties Unity omits without a LightingDataAsset.

Regenerate native probes and lightmaps with `Assets/Editor/Environments/ImportNativeReflectionProbes.py` using Python 3.12+, UnityPy 1.25.2, numpy, and texture2ddecoder. Preserve GUIDs, shipped mip chains, sRGB decoding, and trilinear/clamp sampling. The `.cmprobe` format is GZip-compressed: `CMR1`, little-endian width UInt16, mip-count byte, sRGB Boolean, then RGB24 texels in face/mip order. `Fixtures/GagaWaterProbeMipReference.bytes.gz` is an independent compact reference containing probe/face/mip-ordered native mip levels 2 and 3 for both Gaga probes. The original PNG exports retain base-image evidence but do not supply the native mip chains.

The compiled Chroma request inventory and coverage qualifications are documented in [ChromaEnvironmentRequestRouting.md](ChromaEnvironmentRequestRouting.md), [ChromaMaterialGeometryCoverage.md](ChromaMaterialGeometryCoverage.md), and [ChromaLightRequestRouting.md](ChromaLightRequestRouting.md).
