# Test review — pr-animation worktree

Scope: every test under `Assets/Tests` and `Assets/TestsEditMode` in this worktree —
124 new `.cs` files (untracked), 23 modified test files, and the unchanged tracked
suite for cross-file overlap. Findings are based on reading the new/modified tests
and tracing shared fixtures and helpers; no test run was performed for this review.

Recommendations:

- **Drop** — the test does not lock in a production behavior worth keeping.
- **Combine** — merge assertions/tests that exercise the same path; keeps coverage,
  removes duplication.
- **Parameterize** — same body, different inputs; use `[Values]`/`[TestCase]`.
- **Keep** — distinct behavior; listed only where a naive reading suggests overlap.

---

## 1. Drop candidates

### `Assets/Tests/Editor/NativeSpectrumCalibrationTest.cs` — drop (whole class)

Class-level `[Explicit]`; both tests (`CaptureUnitySpectrum`,
`CaptureResampledUnitySpectrum`) exist to *produce* calibration data, not verify
behavior. They write `unity-calibration-{48000,44100}.json` into
`../../Scratch/NativeSpectrumParity/` and the only assertions are
`AudioRenderer.Start()` succeeded and some spectral peak exceeded a threshold —
i.e. "audio rendered something". Nothing in the repo reads that output
(`NativeSpectrumParityTest` uses its own `NativeSpectrumMusicFixture.json` /
`NativeSpectrumToneFixture.json` fixtures instead). This is a manual DSP capture
utility committed as a test. Move it under `DiagnosticTools/` or delete it.

### `Assets/Tests/Editor/VoidwalkersSpectrumCaptureTest.cs` — drop (whole class)

Class-level `[Explicit]`; requires a hard-coded local map+song path
(`C:\Users\tdrak\BSManager\...\50033 (Voidwalkers...)`). `CaptureExpertPlusStandardAtBeat225890`
loads the real map, captures PCM/spectrum/screen values and PNGs into Scratch for
manual native comparison. Its asserts only validate the capture inputs (screen
count, nonzero spectrum) — they do not pin ChroMapper behavior. Same category as
above: a capture harness, not a regression test.

### `Assets/Tests/Editor/DeleteItAllEndingNoteFacingTest.cs` — drop (whole class)

Class-level `[Explicit]` (~850 lines). It is a manual investigation harness: hard
coded local map path, `[BenjaminDiag]`/`[PauseDiag]`/`[ScrubParity]` `Debug.Log`
probes, and broad camera/seek/playback diagnostics. The behaviors it explores
(note facing toward the playing camera, pause/resume note-set stability, scrub
parity) are covered by the non-explicit
`DeleteItAllNoteLookParityTest` (`EndingDotsFacePlayingCameraAcrossSeeksAndPreserveRoll`,
`PreviewDotsFacePlayerCameraWithoutDrift`, `PausedPreviewKeepsGameplayNoteSet`,
`SpawnFacingUsesTimeWhenDefinitePositionDoesNotTravel`), which runs in CI.
If any assertion here is still unique, port it into that file; otherwise delete.

### `Assets/Tests/Editor/NoteSpawnEffectTest.cs` → `CaptureOriginalVoidwalkersAtBeat29812` — drop

`[Explicit]`, requires the local Voidwalkers map, writes three PNGs + a JSON to
`TestResults/NoteSpawnEffect/`, and its only assertion is `changedPixels > 20` —
"the spawn effect changed some pixels", which is a smoke check, not a pinned
behavior. The reduced-fixture equivalents in the same file
(`VoidwalkersSpawnLightRendersAboveTheNotesAtBeat29812`,
`SpawnFogProducesASurroundingGlowInThePlayerCamera`,
`NativeFogLightProducesPrepassPixels`) already lock the behavior in CI.

### `Assets/Tests/Editor/AngstBeat99ParityTest.cs` → `FullMapPauseDoesNotPopulateInactiveEventGrid` — drop

`[Explicit]` full-map reproduction of "pause must not populate the inactive event
grid". The same behavior is locked in CI by
`PlaybackPausePoolTest.PauseDoesNotPopulateInactiveBasicEventGrid` (and the
complement `PausedEventNodesAreAvailableWhenTheirGridIsVisible`). The full-map
variant adds no assertion the reduced test lacks.

### `Assets/Tests/Editor/AngstBeat99ParityTest.cs` → `FullMapAtReportedBeat` — drop or downgrade

`[Explicit]`, requires the local Angst map. Captures a PNG and then asserts a
single-beat snapshot (`_Cutout == 0.5` on 100 notes, two directional-light color
floors). It is a one-off "reported beat" verification rather than a behavior
contract; the surface-lighting and spawn-angle coverage elsewhere in the same
file already pins the underlying mechanisms. Low value even as a manual repro.

---

## 2. Combine candidates — same assertions, multiple tests

### `Assets/Tests/Editor/ChromaMaterialRoutingParityTest.cs` — combine 16 → 8

Eight test pairs are identical except for the map version:

| V3 ("Authored…") | V2 ("Legacy…") | Shared body |
|---|---|---|
| `AuthoredSimpleLitEmissionAngleAliasReachesCompiledBranch` | `LegacyAuthoredSimpleLitEmissionAngleAliasReachesCompiledBranch` | `CheckEmissionAngleAlias(version)` |
| `AuthoredLocalAliasesReachTheirNativeShaderFamily` | `LegacyAuthoredLocalAliasesReachTheirNativeShaderFamily` | `CheckLocalAliases(version)` |
| `AuthoredGlobalKeywordsReachSupportedShaderGates` | `LegacyAuthoredGlobalKeywordsReachSupportedShaderGates` | `CheckGlobalKeywords(version)` |
| `NamedStandardUsesAuthoredMaterialDefinition` | `LegacyNamedStandardUsesAuthoredMaterialDefinition` | `CheckNamedStandard(version)` |
| `NamedAndInlinePresetsUseNativeMaterialRoutes` | `LegacyNamedAndInlinePresetsUseNativeMaterialRoutes` | `CheckMaterialRoutes(version)` |
| `PresetKeywordOverridesReplaceDefaultsWithoutChangingUnrelatedRoutes` | `LegacyPresetKeywordOverridesReplaceDefaultsWithoutChangingUnrelatedRoutes` | `CheckKeywordOverrides(version)` |
| `GeneratedPresetsUseNativeOmittedColorAndGlowingDistanceDefaults` | `LegacyGeneratedPresetsUseNativeOmittedColorAndGlowingDistanceDefaults` | `CheckGeneratedDefaults(version)` |
| `PrimitiveTypesHonorNativeMeshesAndCollisionRequests` | `LegacyPrimitiveTypesHonorNativeMeshesAndCollisionRequests` | `CheckPrimitiveTypes(version)` |
| `LightPresetsCreatePhysicalLightAndFogWithoutComponentPayload` | `LegacyLightPresetsCreatePhysicalLightAndFogWithoutComponentPayload` | `CheckAutomaticLights(version)` |

Each pair is a one-line wrapper `=> CheckX(3)` / `=> CheckX(2)`. Merge each pair
into a single `[UnityTest]` taking `[Values(2, 3)] int version` (or loop both
versions inside one test, tagging failures with the version). Identical coverage,
half the test entries, and the per-version failure attribution is preserved by
the `failures` list each `CheckX` already builds.

### `Assets/Tests/Editor/ChromaCustomEventEnumAliasTest.cs` — parameterize 4 → 1–2

`QuotedNumericComponentEasingUsesTheNativeEnum`,
`QuotedWhitespaceComponentEasingUsesTheNativeEnum`,
`QuotedCombinedComponentEasingUsesTheNativeEnum`, and
`NumericJsonComponentEasingKeepsItsExistingLinearFallback` are four wrappers over
`CheckEasing(JSONNode easing, float expectedHeight)` differing only in the easing
payload (`"2"`, `" easeInQuad "`, `"easeInQuad, easeOutQuad"`, `2`) and expected
height (6/6/10/8). Parameterize on the string form (parse `"2"` as a number inside
the helper or add a `bool numeric` arg). Each case still needs its own
`ReloadMap`, so this mostly reduces entry count — worth doing since the file is
otherwise four near-identical 15-line wrappers.

### `Assets/Tests/Editor/MopeMopeStretchedNoteTest.cs` — parameterize 2 → 1

`StretchedSideNoteUsesTheGamesBaseLayerNearBeat235` and
`ExplicitCustomHeightRetainsTheHighNoodleLayer` are both `=> CheckHeight(bool)`.
`CheckHeight` already contains the full seek/playback/pose assertion body; the
only differences are the injected `_position` custom data, the expected apex
(0.858333 vs 5.058333), and one extra bottom-bound assert in the default case.
Make it `[UnityTest] CheckHeight([Values(false, true)] bool explicitHeight)` —
one test, two cases, identical coverage.

### `Assets/Tests/Editor/ChangeOfSceneDockFogParityTest.cs` — merge the two Sun-track band tests

`SunTrackKeepsNativeLowerBloomBandAfterInstantTransform` and
`SunTrackKeepsNativeBloomBandsDuringPlaybackWithoutNativeAudio` load the same
`ChangeOfSceneSunTrackFixture.json`, find the same four `Sun`-parented tubes, and
assert the *identical* expected position per beat
(`beat < 107 → parent.position`; `NeonTube (2) → (0,0,464.9172)`; else
`(332.2705,-419.1781,464.9172)`) at the same 0.01 tolerance. They differ only in
time advancement (`MoveToJsonTime` seeks vs deterministic playback) and the
playback variant's extra `CurrentJsonTime`/`isPlaying == false` asserts.
Merge into one test that runs the seek phase then the playback phase, extracting
the duplicated expected-position expression into a local function. Saves a map
load and removes the duplicated expected-value table. (The other five Sun-track
variants — world rotation at instant cuts, localPosition space, deeper children,
later color write, constant repeat — each mutate the fixture differently and
should stay separate.)

### `Assets/Tests/Editor/NulctrlBeat43ParityTest.cs` — drop `CapturedPrismColors` (or dedupe)

`CapturedPrismColors([Values(0,1)])` asserts each prism wall's `_Color` against
`expectations[capture]["prisms"]` at 0.001 tolerance. `CapturedFrameAlignsStretchedDotsAndPrismColors`
(lines ~133–139) asserts *the same* prism `_Color` values against the *same*
expectations in every one of its four cases (2 captures × 2 editor-size settings).
The standalone test is fully subsumed. Either delete it, or — if the render-free
data check is wanted as a cheaper fallback when the camera-render assertion is
fragile — remove the prism-color loop from the frame test instead. Do not keep
both.

Related consolidation in the same file: `CapturedNotePositions`,
`CapturedPrismDepths`, and `CapturedRingColors` are all non-render data checks
sharing `Load(capture)`. Merging them into one "captured data parity" test would
save four map loads (2 captures × 2 merged-away tests) while keeping every
assert. Optional — the per-test split is also reasonable.

### `Assets/Tests/Editor/MopeMopeNative241Test.cs` — merge the two `Crossed` tests

`CrossedEyesStartOnTheOppositeSideWithVerticalAvoidance` and
`EyeScaleUsesThePreviousCompletedTrackFrame` both call `Load("Crossed")` (same
fixture, same expectations file, same beat) and assert different properties of
the same note (position vs `localScale.y == 12`). Merge into one test — saves a
map load. The other fixtures (`Depth`, `Stack`, `Dots`, `Head`, `Facing`,
`V3Head`) are each used once and stay.

### `Assets/Tests/Editor/TheyAreManyColorsGeometryParityTest.cs` — parameterize the `CheckEye` pair

`EyePixelsRemainSquareAfterParentFlattensTheCubes` (`CheckEye("Eye", 337)`) and
`SingleCubeKeepsItsRotationWhileItsParentHasZeroScale` (`CheckEye("SingleCube", 1)`)
differ only in fixture name and expected cube count. `[TestCase("Eye", 337)]` /
`[TestCase("SingleCube", 1)]` on one method. Cosmetic — same runtime cost either
way.

### `Assets/Tests/Editor/EnvironmentTimelineEffectsTest.cs` — parameterize/merge three clusters

- `BillieSmokeAndCloudsReturnAfterSmallScrubs`,
  `Panic2SmokeAndCloudsReturnAfterSmallScrubs`,
  `BritneySmokeAndCloudsReturnAfterSmallScrubs` are three one-line wrappers over
  `CheckEnvironment(string)`. Parameterize `[TestCase("BillieEnvironment")]` etc.
- `CameraFollowingDustUsesTheSelectedRigDuringCameraDisable` and
  `CameraFollowingDustSamplesTheCameraBeforeSimulation` both reload
  `BritneyEnvironment` and assert `FollowCamera` dust tracks the selected camera.
  The first adds a disabled-camera no-throw check; the second asserts sampling
  order. Merge into one test (disable camera, assert no throw + position, re-enable
  path already covered by the second's assertion).
- `SmokeDoesNotDriftWhileTheTimelineIsPaused` and
  `SmokeReturnsToTheSameParticlesAfterReverseSeek` both capture `BigSmokePS`
  particles on the shared map and assert determinism (paused frames vs
  forward-then-reverse seek). Merge into one "smoke determinism" test with both
  phases.

### `Assets/Tests/Editor/The15SublimitScrubParityTest.cs` — extract shared scrub-check helper

(Pre-existing file, not modified on this branch — included because it is part of
the same scrub-parity family as the new tests.) `MatchedTimeScrubParityBeats316To372`,
`RepeatedCrossStepSeeksKeepBeat372Notes`, `RapidCrossStepScrollKeepsBeat372Notes`,
and `SnappedScrollKeepsBeat372Notes` each repeat the same ~40-line preamble
(`AudioTimeSyncController`/`NoteGridContainer`/`TracksManager` lookups,
`CurrentSeconds` property reflection, `InitializeVirtualInput`, the
`expectedParentX = 580197.6f` / `staleParentX = 4165241.4f` constants, and the
beat-372 note assertions). The *input patterns* are genuinely different (matched
time scrub, repeated cross-step seeks across a mode switch, rapid scroll,
snapped scroll) so keep all four tests, but extract the shared preamble and
assert block into one helper — the file is already 1011 lines.

### `Assets/Tests/Editor/Issue628RotationReproTest.cs` — parameterize the fixture pair

`ReducedPairRetains360TurnWithOffsetWorldRotation` and
`LegacyPairRetains360TurnWithOffsetWorldRotation` run the same
`Retains360Turn` check against `Issue628RotationFixture.json` (V3) and
`Issue628LegacyRotationFixture.json` (legacy) respectively. Parameterize the
fixture path; `OriginalMapRetains360TurnWithOffsetWorldRotation` stays `[Explicit]`
(local map). Minor.

### `Assets/Tests/Editor/SaltyBeat532CameraRewindParityTest.cs` + `SaltyBeat537HeadCameraParityTest.cs` — share the harness, not the tests

These two files are near-identical in setup: same
`SaltyBeat537HeadCameraFixture.json`, same `OnMapLoaded` (Animations, FOV 90,
offsetZ 0, CameraFOV 90, PanicEnvironment, 215 BPM, 250 s), same
`EnterPlayingFromBasicEventWorkspace`, `RestoreEditingMode`,
`RestoreEmptySharedMap`, and the same private `cameraAnimator` reflection. ~120
lines of duplicated harness. **Do not merge the test methods** — they cover
distinct behaviors (pre-b537 pristine pose + mode-exit unbind vs the authored
easeOutSine curve in seeks and deterministic playback), and the 532 file's
comment documents why it keeps its own fixture class (isolation from the ease
test's leftover `asdkm` bind). Instead extract the shared load/teardown into a
common base class (note they currently derive from `TestBase` vs `InputTestBase`
— the base must accommodate both, or keep two thin subclasses). Also note
`Beat537HeadCameraSeeksToAuthoredEaseWithoutLag` and
`Beat537HeadCameraEasesAcrossDeterministicPlaybackFrames` re-derive the same
`expectedZ = -3·sin(clamp01((beat-537)/8)·π/2)` formula — worth a shared const/
helper if the classes are merged.

### `Assets/TestsEditMode/CustomEventModelOptionsTest.cs` + `CustomEventOptionsTest.cs` — share the scene-scan helper only

Not duplicates (different settings: `ApplyEventModelToCustomEvents` vs
`SplitCustomEventsByTrack`, different placement/tooltip/locale assertions), but
they repeat the same "open `04_Options.unity` preview scene, find `SettingsBinder`
by `BindedSetting`, scan `Assets/Locales` string tables" boilerplate. Extract a
small shared helper in the test assembly. Optional.

---

## 3. Explicit / local-only reproduction inventory (informational)

These never run in CI. Most follow the repo's established "reduced fixture in CI +
`[Explicit]` full-map repro locally" convention and are reasonable to keep *as
repros* — but they should not be counted as regression coverage:

- Full-map variants sharing a `CheckX` helper with a reduced-fixture CI test:
  `BimboLimboCloneParityTest` (2 pairs), `BimboLimboObstacleFlightParityTest`,
  `ChangeOfSceneDockFogParityTest` (2), `ChangeOfSceneMountainsTest`,
  `ChangeOfSceneScaffoldingTest`, `DeleteItAllGraySurfaceParityTest`,
  `DeleteItAllOverheadLightParityTest`, `MegamixNoteJumpParityTest`,
  `MegamixRunwayParityTest` (3), `MopeMopePostHitTest`,
  `SuperluminalEditModeMaterialTest`, `SuperluminalMoleculeParityTest`,
  `SuperluminalRocketMaterialParityTest`, `SuperluminalSurfaceLightingParityTest`,
  `SuperluminalTunnelParityTest`, `SuperluminalWallSeekTest` (2),
  `The15SublimitPrismParityTest`, `AngstBeat99ParityTest` (3 remaining:
  `FullMapFrozenNoteAnglesMatchNativeCapture`,
  `FullMapFrozenNotesPreserveNativeFacing`, `FullMapAtReportedBeat` — see §1),
  `SaltyNoteColorParityTest.FullMapPreservesTrackAlphaAndNativeMaterialAlpha`,
  `Issue628RotationReproTest.OriginalMapRetains360TurnWithOffsetWorldRotation`.
- Pure capture/diagnostic utilities (see §1 for the drop recommendations):
  `NativeSpectrumCalibrationTest`, `VoidwalkersSpectrumCaptureTest`,
  `DeleteItAllEndingNoteFacingTest`, `NoteSpawnEffectTest.CaptureOriginalVoidwalkersAtBeat29812`,
  `BimboLimboSmokeParityTest.OriginalMapSmokeUsesTheNativeSeedAndReturnsAfterScrubs`
  (borderline — it does assert particle state after scrubs, so it is a real
  repro, just local-only).

Where a full-map `[Explicit]` test and its reduced fixture test share the same
`CheckX` helper, the pair is fine — the reduced test is the CI gate, the full-map
test is the local repro. No action needed beyond what §2 already covers.

---

## 4. Cross-file overlap checked and cleared

- `NulctrlBeat43ParityTest` vs `NulctrlDotGlowRenderingTest` /
  `NulctrlPrismFrameRenderingTest` / `NulctrlRingPhysicsParityTest`: different
  fixtures and different assertions (map-level captured parity vs isolated
  shader/glow profile vs ring-state propagation). Keep all.
- `MopeMopeNoteSpawnTest` (spawn *origin* paths: off-lane, raised/angled,
  fly-bomb) vs `NoteSpawnEffectTest` (spawn-effect *rendering*: compositing,
  fog glow, visibility modes, per-note flags): different aspects, keep both.
- `PlaybackPausePoolTest.PauseDoesNotPopulateInactiveBasicEventGrid` vs
  `AngstBeat99ParityTest.FullMapPauseDoesNotPopulateInactiveEventGrid`: same
  behavior; the Angst one is `[Explicit]` → drop per §1.
- `DeleteItAllNoteLookParityTest` (CI) vs `DeleteItAllEndingNoteFacingTest`
  (class `[Explicit]` diagnostic): overlap confirmed → drop the latter per §1.
- `LaneRotationMapLoadTest` vs `Issue628RotationReproTest`: same
  `Issue628RotationFixture.json` but different subsystems (lane-rotation
  smoothing/motion clearing vs AnimateTrack world rotation). Keep both.
- `SaltyBeat532CameraRewindParityTest` vs `SaltyBeat537HeadCameraParityTest`:
  shared fixture/harness, distinct behaviors → share harness only (§2).
- `SuperluminalFullMapFixture.json` is shared by six `Superluminal*` files, but
  each tests a different subsystem (wall seek pose, tunnel water/probes, rocket
  reflection, surface lighting, molecules, edit-mode wall opacity). Keep.
- `WorldCavesInEnvironmentEssence.json` is shared by `WorldCavesInEnvironmentTest`,
  `RingRotationWaveParityTest`, and `SavePreservesAuthoredFileOrderTest` —
  different assertions per file. Keep.
- Pre-existing fixture-sharing clusters (`Censored*`, `Spells*`, `Salty*`,
  `The15Sublimit*`, `Heliov*`, `Megamix*`, `Aurora*`, `Paradigm*`, `Kamikazi*`)
  follow the same one-file-per-behavior pattern; no new redundancy found.
- `ChromaEnvironmentRoutingTest`, `ChromaObjectColorRoutingTest`,
  `ChromaBasicEventRoutingAuditTest`, `BloomFogChromaParityAuditTest`: dense but
  each test/audit case pins a distinct routing, easing, gradient, lightID,
  lookup-method, or undo/redo behavior. Keep.
- `EnvironmentEnhancementEditingTest` / `EnvironmentEnhancementRefreshTest`:
  many small model/pool-refresh tests; names map to distinct payload, ordinal,
  and refresh behaviors. Keep (the `Clone…`/`Apply…`/`CompleteClone…` trio is the
  only borderline group — they assert different ownership/copy semantics).
- `CustomEventLaneTest`, `CustomEventModelTest`, `AnimationHotReloadTest`,
  `DenseWallLoadingTest`, `DenseCustomEventLoadingTest`, `InterscopeCarTimelineTest`,
  `InterscopeLightingParityTest`, `WeekndLampTranslationTest`,
  `WorldCavesParentBoundaryTest`, `IncomingObjectMovementParityTest`,
  `VoidwalkersStackJumpParityTest`, `BurstSliderLocalRotationTest`,
  `ParticleLightTimelineTest`, `PlaybackPausePoolTest`, `GpuSpectrumPreparationTest`,
  `SpectrumPreparationTest`, `NativeSpectrumParityTest`,
  `NativeEnvironmentLightmapParityTest`, `NativeEnvironmentProbeParityTest`,
  `LegacyAnimationClockTest`, `LegacyAnimationTimelineTest`, `AnimationAggregatorCompositionTest`,
  `GLSInitialStateTest`, `GeometrySelectionInputTest`, `ChainPreviewOverlayTest`,
  `BoxSelectionGroundLaneTest`, `MirrorEditorOverlayTest`, `GlassDesertTrackDefinitionTest`,
  `MultiSettingsSerializationTest`, `ArcPlacementInitializationTest`,
  `BloomFogEnvironmentEnhancementTest`, `BloomFogLightMeshParityTest`,
  `NativeRendererBoundsTest`, `NativeShaderVariantsTest`,
  `NativeSignedAlphaRenderingTest`, `NoteReflectionMipTest`,
  `NoteJumpRotationTest`, `LightRotationAxisParityTest`,
  `RainHeightFadeParityTest`, `SkyGradientOrientationTest`,
  `SmokeDepthIntersectionTest`, `SpriteLightColorParityTest`,
  `ChromaBaseWaterRenderingParityTest`, `ChromaComponentRoutingTest`,
  `ChromaEmptyLightIdEditingTest`, `ChromaEnumAliasRibbonTest`,
  `ChromaGeometryPreviewParityTest`, `ChromaGlowingKeywordRenderingParityTest`,
  `ChromaGradientSourceEditingTest`, `ChromaLegacyEnvironmentRemovalTest`,
  `ChromaObjectRequestParsingTest`, `ChromaOverdrawRenderingParityTest`,
  `TestRenderSettingsTest`, `SuperluminalNoteParityTest`,
  `SuperluminalPlayerAndFlightParityTest`, `SuperluminalTunnelTimelineTest`,
  `The15SublimitLaserRenderingTest`, `The15SublimitMapParityTest`,
  `HeliovDistantNoteTest`, `HeliovPlayerCameraTest`, `HeliovMapParityTest`,
  `MopeMopeFrozenNoteTest`, `MopeMopeNoteSpawnTest`, `MopeMopePostHitTest`,
  `NulctrlDotGlowRenderingTest`, `NulctrlPrismFrameRenderingTest`,
  `NulctrlRingPhysicsParityTest`, `BimboLimboSmokeParityTest`,
  `AngstOpeningColorParityTest`, `BimboLimboCloneParityTest`,
  `BimboLimboObstacleFlightParityTest`, `ChangeOfSceneMountainsTest`,
  `ChangeOfSceneScaffoldingTest`, `DeleteItAllGraySurfaceParityTest`,
  `DeleteItAllOverheadLightParityTest`, `DeleteItAllMapParityTest`,
  `DeleteItAllNoteLookParityTest`, `DeleteItAllPlaybackParityTest` (pre-existing),
  `AngstBeat99ParityTest` (remainder), `TheyAreManyColorsGeometryParityTest`
  (remainder): reviewed — each test pins a distinct behavior, fixture, or
  lifecycle phase. Keep.

---

## 5. Modified-file diff review

The 23 modified test files were diffed. Largest changes:

- `DeleteItAllNoteLookParityTest.cs` (+394): added
  `SpawnFacingUsesTimeWhenDefinitePositionDoesNotTravel`,
  `PreviewDotsFacePlayerCameraWithoutDrift`, and expanded facing/scrub asserts —
  all distinct from the pre-existing tests in the file. Keep. (See §1 re:
  `DeleteItAllEndingNoteFacingTest` overlap.)
- `SaltyNoteColorParityTest.cs` (+225): added
  `CapturedFlashMatchesNativeNotesAndChain([Values(0,1)])` (native-capture parity,
  distinct from the behavior tests `Beat176FlashCycles…`,
  `Beat189FadeReturns…`, `ReverseScrubThrough176…`) and
  `FullMapPreservesTrackAlphaAndNativeMaterialAlpha` (`[Explicit]` local repro —
  consistent with convention). Keep.
- Remaining modified files (`AdditionalAnimationParityTest`,
  `AnimationPropertyVersionParityTest`, `AnimationReviewParityTest`,
  `AuroraMapParityTest`, `BasicEventAppearanceTest`, `CustomEventLogoRenderingTest`,
  `MapReloadLifetimeTest`, `MaterialTrackAnimationTest`,
  `ParametricBoxEnhancementTransformTest`, `SaltyBeat219PlacementParityTest`,
  `SaltyBeat532CameraRewindParityTest`, `SaltyBeat537HeadCameraParityTest`,
  `SaltyPlaybackParityTest`, `SelectionControllerTest`,
  `The15SublimitMapParityTest`, `VaguenessJourneyEnvironmentScaleTest`,
  `WorldCavesInEnvironmentTest`, `NotesContainerTest`, `GraphicsOptionsTest`,
  `TestBase`, `TestUtils`, `README.md`, `Test-ChroMapper.ps1`): small diffs
  (2–73 lines) adding or adjusting asserts in existing tests; no redundant
  additions found. `TestBase`/`TestUtils` changes are shared-harness
  improvements, not tests.

---

## 6. Summary table

| File / test | Recommendation |
|---|---|
| `NativeSpectrumCalibrationTest` (class) | **Drop** — capture utility, output unused, trivial asserts |
| `VoidwalkersSpectrumCaptureTest` (class) | **Drop** — local-path capture utility |
| `DeleteItAllEndingNoteFacingTest` (class) | **Drop** — class-Explicit diagnostic; coverage exists in `DeleteItAllNoteLookParityTest` |
| `NoteSpawnEffectTest.CaptureOriginalVoidwalkersAtBeat29812` | **Drop** — Explicit capture, assert is only `changedPixels > 20` |
| `AngstBeat99ParityTest.FullMapPauseDoesNotPopulateInactiveEventGrid` | **Drop** — redundant with `PlaybackPausePoolTest` (CI) |
| `AngstBeat99ParityTest.FullMapAtReportedBeat` | **Drop** — one-off reported-beat snapshot, weak asserts |
| `ChromaMaterialRoutingParityTest` (8 Authored/Legacy pairs) | **Combine** — `[Values(2,3)]` per pair, 16 → 8 |
| `ChromaCustomEventEnumAliasTest` (4 tests) | **Parameterize** — easing payload + expected height |
| `MopeMopeStretchedNoteTest` (2 tests) | **Parameterize** — `CheckHeight([Values(false,true)])` |
| `ChangeOfSceneDockFogParityTest` (2 Sun-track band tests) | **Combine** — same fixture/expected table, seek + playback phases |
| `NulctrlBeat43ParityTest.CapturedPrismColors` | **Drop** — subsumed by prism asserts in `CapturedFrameAligns…` |
| `NulctrlBeat43ParityTest` (3 data tests) | Optional **combine** — merge non-render checks, save 4 loads |
| `MopeMopeNative241Test` (2 `Crossed` tests) | **Combine** — same fixture, two asserts |
| `TheyAreManyColorsGeometryParityTest` (`CheckEye` pair) | **Parameterize** — `[TestCase]` fixture + count |
| `EnvironmentTimelineEffectsTest` (3 smoke-scrub, 2 dust, 2 smoke-determinism) | **Parameterize/merge** — shared helpers already exist |
| `The15SublimitScrubParityTest` (4 scrub tests) | **Combine setup** — extract shared preamble/assert helper |
| `Issue628RotationReproTest` (reduced/legacy pair) | **Parameterize** — fixture path |
| `SaltyBeat532…` + `SaltyBeat537…` | **Share harness** — extract common base; keep tests separate |
| `CustomEventModelOptionsTest` + `CustomEventOptionsTest` | Optional shared scene-scan helper |
| All other new/modified/unchanged tests | **Keep** — distinct behaviors (§4) |

## Limitations

- Review was static: no Unity test run was executed, so "redundant" means
  *assertion-level* redundancy verified by reading, not proven by a failing
  suite after removal.
- For the ~90 new files reviewed by test-name/fixture inventory rather than
  full line-by-line reads, recommendations in §4 ("keep") rest on distinct test
  names, distinct fixtures, and distinct assertion helpers — a second pass on
  any single file could still surface intra-file duplication.
- The explicit full-map/reduced-fixture pairs were evaluated against the repo's
  established convention; if the project wants to shrink local-only repro
  surface area, every `[Explicit]` full-map test is a candidate for deletion
  without losing CI coverage — that is a policy decision, not a redundancy one.
