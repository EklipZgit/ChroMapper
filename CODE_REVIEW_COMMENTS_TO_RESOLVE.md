# ChroMapper animation review comments to resolve

Re-reviewed on 2026-11-11 using `review-chromapper-pr`. Original review: 2026-11-08.

Baseline: `ChroMapper-gls-color-shifts`, commit `bb293f099343410f547ee1b54fe3cd7261a7fa18`.
Proposed result: `ChroMapper-pr-animation-environment`, originally reviewed at `813dad6f29ee9b4e13d32d4c4513ecfc864b69a0` plus uncommitted and untracked changes. Comment status below reflects the subsequent source and test results inspected during this audit. Intentional GLS-only differences follow the existing PR split.

The worktree HEAD and GLS baseline are unchanged. This pass checked the existing comments against current source, reviewed the source changes since the previous snapshot and newly changed paths, and ran focused tests. R2 remains a reproduced ownership defect. R13 and both parts of R14 are now addressed. R10's duplicate code remains. R1/R3/R4/R5 remain unproven map-level candidates, not implementation requirements. R15 is a new, optional profiling question; no additional functional defect was confirmed.

## Performance and evidence rules for resolving this review

The earlier review overstated the implementation requirement for several source-derived edge cases. A compiled probe or a constructed combination can expose a semantic mismatch without demonstrating that additional runtime machinery is justified in real use.

- Do not add per-frame null checks, component discovery, recovery paths, version probes, fallback systems, extra caches, or additional simulation just to satisfy this document.
- Fix confirmed lifecycle and ownership problems at their actual boundaries. Preserve existing asymptotic cost, allocation behavior, and object counts.
- R1, R3, R4, and R5 are **validation candidates**, not mandatory runtime fixes. Establish a naturally occurring map/workflow reproduction and native behavior before accepting any fix that increases runtime cost, memory, or complexity. If such an increase is needed, explain it and obtain user approval first.
- R6 and R15 are optional profiling questions. No measured performance regression has been established, so neither authorizes a replay-policy change or a new optimization system.
- Architectural preferences are optional. Class size, helper duplication, and inspector wiring alone do not justify extra runtime components, indirection, allocations, or a broad rewrite.
- Preserve approved deterministic seeking and game parity. Do not relax assertions or change behavior merely to obtain a green test run.

The 2026-11-08 map-data audit examined 67 accessible fixture/full-map JSON files containing custom events. It found no null-only color track, note assigned to multiple tracks that each supply time requests, or explicit car-duplication candidate matching the audited patterns. That inventory was not rerun for this pass. It is limited local coverage, not a claim that those cases are impossible. No new real-map reproduction was established for R1/R3/R4/R5. Wall-only and other unsearched map layouts remain outside that inventory.

## Resolution pass (2026-11-11, later)

Each comment below now ends with a **Resolution** paragraph. Summary:

| ID | Resolution |
| --- | --- |
| R2 | Fixed. Owner keys use reference identity through a shared `ReferenceIdentityComparer<T>`. Red-first tests added. |
| R1 | Fixed at load time with no per-frame cost. Red-first test added. |
| R3 | Fixed by correcting the `TryGetTrackTime` predicate. Red-first test added. |
| R4 | Fixed: the first track with a time wins, matching Noodle. Red-first test added. |
| R9 | Done for the three single-key searches. Composite-key searches intentionally kept. |
| R10 | Done. |
| R11 | Done. |
| R5, R6, R7, R8, R15 | No change. Reasons are given under each item. |
| R12, R13, R14 | Already addressed. No change. |

Fixes R1, R3, and R4 were made despite having no real-map reproduction. Each one replaces a predicate or adds a single flag or branch, so none of them adds recurring cost, allocations, objects, or state. The evidence-before-cost rule at the top of this document therefore does not block them. Each one also corrects a source-proven divergence from Heck/Noodle behavior, or a crash.

Verification for this pass:

- Red-first runs, with the unfixed code failing on the expected assertion or exception:
  - R2, identical owners: `TestResults/cli/20261111-194038`
  - R2, rename: `TestResults/cli/20261111-195458`
  - R1: `20261111-194132`
  - R3: `20261111-194212`
  - R4: `20261111-194807`
- Consolidated targeted green run `TestResults/cli/20261111-195218`: 52 passed, 0 failed, 1 skipped. The skip was the optional Bimbo Limbo full-map test, which at that time was gated on `CHROMAPPER_BIMBO_LIMBO_FULL_MAP_PATH` being unset; it was subsequently rewritten as a minimal inline repro (see R11) and is no longer env-gated.
- Full suite: see the end of this document.

## Status overview

| ID | Status after audit | Resolution scope |
| --- | --- | --- |
| R2 | Confirmed editor ownership defect | Correct key identity using the existing ownership model |
| R13 | Addressed; reference shader and metadata exist, focused test passes | No further fixture or production change requested |
| R14, triangle case | Addressed for this review; focused test passes | Retain existing assertion; no culling/bounds workaround requested |
| R1 | Validation candidate | No required runtime handling without real-use evidence |
| R3 | Validation candidate | Verify time-presence semantics and a real affected map |
| R4 | Validation candidate | Verify conflicting time tracks occur and affect the sampled pose |
| R5 | Validation candidate | Verify actual car-clone use and native motion before adding simulation work |
| R6 | Optional profiling question | No optimization or visibility-policy change required |
| R7 | Optional maintainability suggestion | Bounded simplification only |
| R8 | Optional prefab/configuration suggestion | Preserve existing objects and behavior |
| R9 | Optional helper reuse | Preserve boundary semantics and avoid new query allocations |
| R10 | Confirmed duplicate code/logging cleanup | Remove duplicates; no new runtime handling or cleanup-only tests |
| R11 | Optional test portability improvement | Test input configuration only |
| R12 | Addressed in current source; targeted tests pass | Retain the lifecycle fix and existing regression coverage |
| R14, facing case | Addressed by correcting the test's head position; targeted test passes | No additional production handling requested |
| R15 | New optional profiling question | Measure note-look history reconstruction before any optimization |

## Confirmed issue and rechecked prior failures

### R2 — P2: Use reference identity for lane-reference owners

Location: [CustomEventLaneIndex.cs:90](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Editor/Grid/Collections/CustomEventLaneIndex.cs:90).

`trackReferences` uses the default equality comparer for `BaseItem`. `BaseEnvironmentEnhancement` has content equality and a payload-dependent hash. Ordinary editor operations can therefore violate dictionary ownership:

1. Create two identical tracked enhancement instructions. They are distinct editable instructions, but the index records only one owner. Deleting one removes the surviving instruction's track lane.
2. Rename the only enhancement's track. Its hash changes, preventing removal of its old dictionary entry and leaving the old lane alongside the new one.

Compiled production-code probes reproduced both results again on 2026-11-11 against the assembly compiled for this review's focused runs. Existing `EnvironmentEnhancementEditingTest` cases also establish that identical instructions and in-place edits are supported editor workflows. The existing lane-index tests pass, but do not cover these owner-identity cases.

Use reference identity for owner keys, consistent with geometry-container ownership. This replaces the incorrect equality/hash policy; it does not need fallback lookups, dictionary scans, periodic reconciliation, another index, or per-frame validation. In particular, avoid serializing the enhancement payload to calculate an ownership hash.

Regression coverage should exercise duplicate-owner removal and a track rename through the production lane API. If implementing the fix, demonstrate the expected failures first.

**Resolution: fixed.**

- Added `Assets/__Scripts/Utils/ReferenceIdentityComparer.cs`, a generic `ReferenceEquals`/`RuntimeHelpers.GetHashCode` comparer.
- `CustomEventLaneIndex.trackReferences` now uses `ReferenceIdentityComparer<BaseItem>.Instance`.
- `GeometryGridContainer`'s private `EnhancementReferenceComparer` was replaced with the same shared comparer, so the two ownership maps cannot drift apart. The GLS-only `GLSEventLookupIndex.ReferenceComparer` was left alone to keep the PR split clean. It can switch to the shared type when the GLS PR lands.
- No serialization, scans, or fallback lookups were added.

New tests are `CustomEventLaneIndexTest.IdenticalEnhancementsOwnTheirTrackReferencesSeparately` and `RenamingAnEnhancementTrackReplacesItsLane`. Both failed on the default comparer: the survivor's lane was lost, and the stale `shared` lane was retained. Both pass now.

### R13 — Addressed: Independent FFT oracle is now a test asset

Location: [GpuSpectrumPreparationTest.cs:136](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/GpuSpectrumPreparationTest.cs:136).

The old failure came from loading a missing shader under `Assets/Tests/Editor/Experiments`. Current source loads `Assets/Tests/Fixtures/OriginalEditorFFT.compute`; that shader and its metadata exist in the proposed working tree. The fixture retains its independent FFT implementation, and the test now asserts that the asset loaded before invoking it.

`SharedButterflyPreservesEditorPackedOutput` passed in `20261111-132346`. Retain the fixture and its metadata when preparing the PR; they are currently untracked. No further fix is requested. No production discovery, fallback loading, or guard is needed.

**Resolution: no change.** This was already addressed. The fixture and its `.meta` must be included in the PR.

### R14 — Addressed: Triangle-frustum assertion passes in the current tree

Location: [KamikaziLightArrayTest.cs:313](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/KamikaziLightArrayTest.cs:313).

The original run failed because the lit `s934` renderer's bounds did not intersect the camera frustum at beat 20. `BackFacingTransparentTriangleRendersLitSurface` now passes in `20261111-132346`, retaining its frustum and rendering assertions. The earlier failure is not evidence of a remaining current-tree defect.

Retain that coverage. Do not add globally inflated bounds, disabled culling, or per-frame bounds repairs to close the old comment. This pass did not establish which intervening change resolved the earlier failure or reproduce native-player pixels independently.

The separate facing assertion originally grouped under R14 is addressed below.

**Resolution: no change.** This was already addressed, and the coverage is retained.

## Validation candidates — no mandatory runtime fix

### R1 — Null-only color timeline and shared native parent anchors

Location: [DirectEnvironmentSharedTrackBinding.cs:77](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/DirectEnvironmentSharedTrackBinding.cs:77).

A null-only timeline can have a finite `StartTime` and no evaluated point definitions. A compiled generic `AnimateProperty` probe reproduced an `IndexOutOfRangeException` again on 2026-11-11 when its anchor was requested. Source tracing suggests that a directly tracked native parent/child pair sharing position and a null-only color track can reach that request through `ReadPreviousParentPose`.

What is missing: an actual map-level reproduction of that complete binding path in real-use content. The local map-data audit did not find the null-only color prerequisite. The generic probe alone is not evidence that an extra check belongs in every frame's anchor loop.

For investigation, a modified Sun fixture with `_color:null` at beat 110 and a seek to beat 122 is a useful constructed test, but it is not proof of real-world occurrence. If a real map confirms the failure, correct the existing property-presence/anchor contract at the appropriate binding or evaluation boundary. Do not introduce exception catching, repeated component recovery, or a general defensive layer. Any extra recurring work still needs the performance justification required by the workspace guide.

**Resolution: fixed at the contract boundary, with no runtime cost.**

The root cause is `AnimateProperty<T>.SortPresenceRequests`, which lowered `StartTime` to the first presence request even when the property had no evaluated definitions (`count == 0`). Every consumer gates on `time >= StartTime` and then indexes `evaluated[0]`. That includes `TrackAnimator`'s transform-anchor and update loops, not only `ReadPreviousParentPose`. A null-only property would therefore throw `IndexOutOfRangeException` every frame. This covers more than the color case: a track whose only request is, for example, `position: null` hits the same crash through `TrackAnimator`.

`StartTime` is now lowered only when `count > 0`. A null-only timeline therefore stays unstarted, which matches Heck, where nulling a never-set track property is a no-op. The check runs once when the property is sorted. No per-frame check, catch, or guard was added.

A real-map reproduction is still unproven, but the fix costs nothing at runtime. New test: `AnimatePropertyScrubTest.NullOnlyTimelineNeverStartsOrWritesTransforms`. It threw on the unfixed code and passes now.

### R3 — Future time requests shadowing another track

Location: [TracksManager.cs:150](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Editor/Tracks/TracksManager.cs:150).

`TryGetTrackTime` reports success for a nonempty time timeline before its first request, when evaluation returns the `-1` default. The frozen-note and frozen-wall indexes stop after the first successful lookup. In a constructed A/B-track case, A's future request can therefore prevent checking B's currently active hold.

What is missing: real affected map content and an executed end-to-end reproduction. The note-oriented local inventory found no qualifying multiple-time-track note; it did not exhaust wall layouts.

If confirmed, make the existing lookup's success result agree with the established presence semantics. Prefer correcting that predicate over adding a fallback pass through the tracks, another object scan, or a new cache. Before accepting any added recurring cost, provide the real map, measured/structural cost, and the required approval. There is no requirement to add runtime handling solely for the constructed case.

**Resolution: fixed by correcting the predicate, as recommended above.**

`TracksManager.TryGetTrackTime` now succeeds only when `timeProperty.HasValueAt(jsonTime)`, replacing `!IsEmpty()`. This matches Noodle's `GetTimeProperty` (`Heck/NoodleExtensions/HeckImplementation/CustomDataTypes.cs:339`), which returns the first track whose time property *has a value*. A future request or a null clear no longer shadows a later track. The frozen-note and frozen-wall callers are unchanged.

Cost: without null requests, `HasValueAt` is a single comparison against `StartTime`. With null requests, it is a binary search over that property's presence requests. No fallback pass, scan, or cache was added.

New test: `TrackTimePrecedenceTest.TrackWithoutAValueDoesNotHideANoteHeldByALaterTrack`. On the unfixed code, it returned `true` for track A and dropped the note held by track B. It passes now.

### R4 — Conflicting time-track precedence in note-pose sampling

Location: [ObjectAnimator.cs:1944](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1944).

`SampleNotePose` overwrites `pathTime` for every track with a nonnegative time value, making the last applicable track win. Local Noodle source uses the first track with a value, and CM's frozen-note index explicitly follows that rule. Track names retain their input order through preparation and attachment.

What is missing: real content with conflicting active time tracks and a demonstrated spawn-pose mismatch. The local inventory found no qualifying note. The source discrepancy is worth investigating, but is not approval for a generalized track-resolution framework.

If a real reproduction confirms it, resolve the applicable existing value once while continuing to aggregate the other track properties. Avoid extra whole-map queries, repeated resolution passes, per-frame collection materialization, or new state systems. Reuse a shared rule only where that actually removes duplication and preserves cost.

**Resolution: fixed by resolving the value once, as recommended above.**

`SampleNotePose` now takes `pathTime` from the first track with a nonnegative time. It uses a single local flag, while the same loop still aggregates every track's other properties. This matches Noodle and CM's own `ResetNoteJumpRotation` and `FrozenNoteTrackIndex`. No new queries, passes, or state were added.

New test: `TrackTimePrecedenceTest.NoteSpawnPoseUsesTheFirstTrackTime`. Its reference is a note assigned only to track A. The unfixed code produced the track-B pose (`y=18.25`, expected `6.25`). It passes now.

### R5 — Enhancement-created Interscope car clones

Location: [NativeInterscopeCarBuilder.cs:113](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Editor/Environments/NativeInterscopeCarBuilder.cs:113).

The importer captures the original cars in fixed `VisualBodies`/`Cars` arrays and makes their bodies kinematic. Duplicating an individual car subtree later appears to copy those bodies and the data-only jump/suspension components without adding the clone to the timeline on `BasicEventEffectManager`.

What is missing: a real map depending on hydraulic motion in an individual car clone, and native-versus-CM verification of that behavior. The audited content did not provide a car-duplication candidate. A manually duplicated car alone is not sufficient justification for increasing simulation cost.

Do not add general clone discovery, extra physics scenes, duplicate simulation, or per-frame clone registration preemptively. If actual content establishes the need, determine the smallest lifecycle-time binding that preserves native behavior and scales only with the clones that really exist. Obtain approval before accepting increased runtime cost, memory, or complexity.

**Resolution: no change, because the fix is not justified without evidence.**

The failure mode is benign. A duplicated car keeps its kinematic bodies and rest pose and does not bounce. It does not crash, corrupt data, or affect other cars. Making clones bounce would mean registering clone bodies with `HydraulicCarTimeline` and simulating and replaying additional rigs on every cold seek. That adds recurring physics cost and memory in proportion to the number of clones. Under the workspace rule, that needs a real map that duplicates an individual Interscope car and relies on its hydraulics, plus native footage to match. No such map is known: the 67-map audit found no car-duplication candidate.

If one turns up, register clones once when they are created (`GeometryContainer.GenerateEnvironmentEnhancement` already knows each clone) rather than discovering them each frame.

## Optional improvements

### R6 — Profile cold seeks only when investigating observed latency

Locations: [TimelineParticleSystem.cs:109](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Environments/Components/TimelineParticleSystem.cs:109) and [HydraulicCarTimeline.cs:111](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Environments/Effects/Basic/HydraulicCarTimeline.cs:111).

The source cost model is 18,000 synchronous `Simulate` calls per emitter for a cold five-minute seek. Car physics likewise replays elapsed frames and stores poses. These counts are not measured frame-time, allocation, or responsiveness evidence. Active renderers hidden with `forceRenderingOff` still participate; inactive emitters now release their clock callbacks through the enable/disable lifecycle.

The previous smoke-only scope is outdated: `EnvironmentDescriptor.InitializeTimelineParticles` now also registers looping, play-on-awake ambient systems while excluding event-controlled emitters. Britney's exported `DustBritney` is a concrete world-space, looping, play-on-awake system with a maximum of 1,000 particles, and the current camera-follow tests pass. The earlier observation that the inspected smoke exports used local space must not be generalized to all current timeline particles. These tests do not establish particle-history parity under an animated camera; that is a coverage limit, not a request to add emitter-history storage or additional replay machinery.

If a user-visible slowdown is observed, profile that exact map and seek before proposing an optimization. Preserve deterministic synchronous seeking and correct results after smoke is shown again. Do not automatically skip hidden smoke, change checkpoint policy, add asynchronous preparation, add caches, or require a new performance subsystem to close this comment. With no measured problem, no production change is required.

**Resolution: no change.**

The call counts are a structural cost model, not a measured regression. Cold seeks are synchronous by design, which keeps seeking deterministic and matches native particle and car state. Any shortcut, such as skipping hidden emitters, checkpointing, or async preparation, would trade correctness or memory for an unmeasured gain. This should be revisited only with a profiler capture of a specific slow seek on a real map.

### R7 — Simplify animation responsibilities when touching an evidenced problem

Location: [ObjectAnimator.cs:14](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:14).

`ObjectAnimator` combines gameplay and sampled poses, material appearance, native hierarchy ordering, parent anchoring, pooling, and registration. Its size is a maintainability concern, not evidence of a runtime defect or performance regression.

Prefer a bounded extraction that removes an actual duplicated rule when resolving a confirmed issue. No broad rewrite, new component hierarchy, polymorphic dispatch layer, generic framework, or additional persistent state is required merely because the file is large.

**Resolution: no change.**

The confirmed issues touching `ObjectAnimator` (R4 and R10) were fixed in place with small local edits. Neither one exposed a duplicated rule that an extraction would remove. Splitting the class now would be a large, behavior-neutral refactor of the most parity-sensitive file in the PR. It would add review and regression risk to a PR that is already large, without fixing a defect. A split is better done as its own follow-up PR, if maintainers request it.

### R8 — Consider serializing existing fixed structures

Locations: [GeometryContainer.cs:88](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Containers/GeometryContainer.cs:88) and [CustomEventLaneSections.cs:32](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Editor/Grid/Collections/CustomEventLaneSections.cs:32).

The material animation child and references are constructed imperatively, and category views copy multiple grid fields. Serializing the same fixed structure in a prefab/template may make configuration easier to maintain.

This is optional configuration cleanup, not a measured performance improvement. Preserve object counts, activation order, ownership, and current behavior. Do not add extra objects or per-frame dependency repair to make a template approach work.

**Resolution: no change.**

- **`GeometryContainer` material-animator child:** Moving it into the geometry prefab would remove only the `new GameObject`, `SetParent`, and `AddComponent` lines. `Context`, `TracksManager`, and the disabled state are runtime values that `CreateContainer` must still assign. The prefab is shared by both the primitive-geometry and the lookup paths, so the change would add prefab YAML churn for almost no reduction in code. Object count and activation order are already correct.
- **`CustomEventLaneSections`:** The number of section views depends on the loaded map's custom event types. Each view must mirror the scene's configured `GridLane`, including its offsets, scale, color, controller, and placements. A static template would duplicate that configuration and could drift from the original lane. Copying from the live lane keeps them in sync by construction.

Neither change fixes a defect, so both are left as optional cleanup.

### R9 — Reuse bound-search helpers only when the implementation stays efficient

Locations: [BasicEventChromaColorTimeline.cs:91](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Shared/BasicEventChromaColorTimeline.cs:91), `ColorBoostEffect`, and `HydraulicCarTimeline`.

Existing `LowerBoundBy`/`UpperBoundBy` helpers can replace some hand-written binary searches. Reuse is optional where contracts match. Preserve file-order/insertion-order ties, inclusive/exclusive endpoints, and allocation behavior. Do not force reuse through a freshly allocated capturing delegate/comparer on each query or change a logarithmic lookup into a scan. No new generic search framework is requested.

**Resolution: done where the contract matches.**

The single-key upper-bound searches now call `UpperBoundBy` with static, non-capturing lambdas over `AsSpan()`. They keep the same `<=` semantics and add no allocations:

- `BasicEventChromaColorTimeline.GetGradientEndTime` (BPM lookup)
- `ColorBoostEffect.GetLastChange`
- `HydraulicCarTimeline.FindAfter`

The composite-key searches (`BasicEventChromaColorTimeline.FindInsertionIndex`/`FindAtOrBefore` and `ColorBoostEffect.FindInsertionIndex`) were intentionally kept. They order by time, then file order and insertion order, with inclusive and exclusive tie variants. `UpperBoundBy` cannot express that without a per-query comparer or a tuple key. The existing chroma gradient, color boost, and hydraulic car tests pass.

### R10 — Remove accidental duplicate code and warnings

Locations: [ObjectAnimator.cs:1244](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1244) and [GeometryContainer.cs:250](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Containers/GeometryContainer.cs:250).

`ApplyAnimation` repeats an identical `else if (colorRestorePending)` branch and a reset assignment. `GeometryContainer` repeats the same no-match warning block, causing duplicate log messages.

Delete the duplicate blocks and redundant comments. This removes unnecessary work. No replacement state, fallback, diagnostic subsystem, or cleanup-only test is needed.

**Resolution: done.** These were removed:

- the repeated `else if (colorRestorePending)` branch and the duplicate `colorRestorePending = false;` in `ObjectAnimator.ApplyAnimation`
- the repeated warning block in `GeometryContainer.GenerateEnvironmentEnhancement`

### R11 — Make optional full-map test input configurable

Location: [BimboLimboSmokeParityTest.cs:37](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/BimboLimboSmokeParityTest.cs:37).

The optional full-map case hardcodes a personal Windows path. It could follow the environment-variable or conventional fixture-path approach already used by other parity tests. Preserve the ordinary reduced smoke coverage when full-map input is unavailable.

This suggestion concerns test setup only. Do not introduce production filesystem probing or runtime map discovery to support it.

**Resolution: done, superseded.** The test initially read `CHROMAPPER_BIMBO_LIMBO_FULL_MAP_PATH` and ignored cleanly when unset. It was then rewritten as a minimum viable repro: the smoke content is a small environment, so the test now builds a minimal inline map on `BigMirrorEnvironment` instead of loading any external full map. There is no hardcoded personal path, no env var, and no skip; the reduced smoke coverage (seed + fractional-beat scrub determinism) is preserved.

### R15 — New optional question: Measure per-frame note-look history reconstruction

Location: [ObjectAnimator.cs:1505](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1505), called from [ApplyNoteLook](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1477).

The new `SampleNoteLookWindow` reconstructs up to 33 history samples, calling `SampleNotePose` twice and sampling the player head in each iteration. That is up to 66 pose evaluations and 33 head-position queries per invocation. It runs during ordinary frame updates for eligible pre-hit notes without a time override, including the final pre-hit window of ordinary notes and earlier definite-position paths. It is not limited to a cold seek or a one-time post-hit reconstruction.

This is a concrete increase in repeated work, but no profiler capture in this review establishes a frame-time or allocation regression. If an actual map shows a note-animation bottleneck, profile this function with representative note counts and separate playback from stopped seeking. Preserve the native previous-up-vector behavior and the parity fixes if considering a cheaper implementation. No new cache, stateful replay system, extra component, algorithm rewrite, or mandatory optimization is requested without evidence and the required approval. Class size or the loop count alone is not a reason to change approved behavior.

**Resolution: no change.**

The window is narrower than "ordinary frame updates." `ApplyNoteLook` runs only in Preview mode, with note look enabled, for notes without chains. The reconstruction applies only when there is no time override, and either the note is within its final pre-hit history window (`NoteLookHistoryStart`) or it follows a definite-position path. The post-hit case runs once per note (`HasHitLookRotation`).

The reconstruction is needed to reproduce native note look, which retains the previous up vector. A cheaper incremental version would need per-note persistent history, and seeking would break it. That is exactly the stateful replay system ruled out above. This should be revisited only if a profile of a dense Noodle map in Preview shows `SampleNoteLookWindow` as a measurable cost.

## Addressed since the original review

### R12 — Recycled-note pose application

Location: [ObjectAnimator.cs:371](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:371).

The original full run reproduced four Sublimit failures after sampling recycled a note and pose application continued with cleared `NoteData`. Current source returns an explicit result from `UpdateAnimationState` and stops the caller when recycling has occurred. This addresses the lifecycle boundary instead of adding a note-data recovery check to the hot path.

The targeted result `20261111-133124` reports passes for `MopeMopePostHitTest.StoppedSeekCanFinishFakeBeforeApplyingItsPose` and all four previously failing `The15SublimitMapParityTest` cases. Retain that fix and coverage; no additional defensive handling is requested.

**Resolution: no change.** This was already addressed.

### R14 — Facing assertion used the viewing camera instead of the player head

Location: [AnimationReviewParityTest.cs:78](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/AnimationReviewParityTest.cs:78).

The original test measured against CM's viewing camera position, including its backward viewing offset. Current test source reconstructs the player-head position before that offset. `V2DisableNoteLookPreservesAuthoredFacing` passes again in targeted result `20261111-133124`. The disabled-look guarantee remains tested. No production fallback or extra head-position handling is requested by this review.

**Resolution: no change.** This was already addressed.

## Verification record and limits

- Original complete run: `pwsh ./Assets/Tests/Test-ChroMapper.ps1`, artifacts in [20261108-133545](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261108-133545). PlayMode: 1,674 passed, 7 failed, 31 skipped. EditMode: 306 passed, 0 failed, 0 skipped. Total: 1,980 passed, 7 failed, 31 skipped. That run describes an earlier compiled tree before the resolution-pass fixes and before commit `8fe313e06`.
- Focused run [20261111-132346](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261111-132346): PlayMode 37 passed; EditMode 15 passed; no failures or skips. It covers the FFT oracle, triangle-frustum case, lane index, ambient particles/spectrum, parent-assignment boundaries, pause pools, and rotation reset.
- Focused run [20261111-133124](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261111-133124): PlayMode 6 passed; EditMode 16 passed; no failures or skips. It covers the addressed recycle/facing/Sublimit cases and native texture imports.
- Resolution-pass red-first and consolidated runs are listed under "Verification for this pass" above (52 targeted tests passed, 0 failed, 1 env-gated skip at that revision).
- **Full suite after the resolution-pass fixes**: `pwsh ./Assets/Tests/Test-ChroMapper.ps1`, artifacts in [20261111-221225](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261111-221225). PlayMode: 1,836 passed, 2 failed, 38 skipped. EditMode: 324 passed, 0 failed, 0 skipped.
  - Both failures were `Tests.Editor.SpellsFinalBeamParityTest` (`FinalBurstPhysicalLaserSurfaceHasWhiteCore`, `FinalBurstBoxesUseAuthoredAlphaMultiplier`), failing at `SpellsFinalBeamParityTest.cs:123`: `Assert.That(cameraManager.CameraControllers[0].Camera.allowHDR, Is.True)` — a stale expectation that the editing camera keeps HDR.
  - These failures were unrelated to the resolution-pass changes: the pass touched animation sampling, ownership comparers, binary-search helper reuse, duplicate removal, and tests — no camera, rendering, HDR, or scene code. They reproduced deterministically when the fixture ran alone (`TestResults/cli/20261111-224751`).
  - Cause: a stale assertion inside commit `8fe313e06` ("anim2 checkpoint", committed mid-pass on this tree). That same commit's own `The15SublimitLaserRenderingTest` asserts the opposite — `editingCamera.allowHDR, Is.False` ("Native gameplay, smooth and recorder cameras use LDR") — and passes in the same run. The branch intentionally moved the editing camera to the LDR path; the Spells final-burst test still asserted the old HDR expectation.
  - **Resolved**: the user verified the Spells ending renders exactly as in-game in both cameras, confirming the assertion — not the rendering — was wrong. The stale assert was corrected to `Is.False` with the LDR-rationale comment updated. Both tests pass in `TestResults/cli/20261112-131007` (2 passed, 0 failed).
- Current compiled API probe evidence: [managed-probe-results.txt](C:/src/BeatSaberStuff/Scratch/animation-rereview-20261111/managed-probe-results.txt). These probes demonstrate that R2's ownership failures and R1's generic null-only anchor failure persisted before the fix, not every proposed map-level scenario.
- Local map-data inventory: [map-case-audit.json](C:/src/BeatSaberStuff/Scratch/animation-vs-gls-review/map-case-audit.json). It is a limited prerequisite search, not a native gameplay reproduction or a proof of impossibility.
- Source inventory and incremental review artifacts are under [animation-rereview-20261111](C:/src/BeatSaberStuff/Scratch/animation-rereview-20261111). A transient deployment helper seen during the concurrent build was removed by that build and was excluded from PR findings.
- This pass changed production and test code in the worktree (listed under each Resolution), plus this Markdown file and Scratch artifacts. It did not build/deploy the player, alter staging, create commits, or take a profiler capture. An independent deployment build in another worktree was allowed to finish before the full suite ran; the first suite attempt stalled against that concurrent Unity instance and the second attempt (`20261111-221225`) completed.

Done: R2's ownership defect is fixed with reference-identity comparers and regression tests; R1's null-only timeline start bug, R3's time-presence predicate, and R4's multi-track path-time selection are fixed with red-first coverage; R10's duplicates are removed; R9's three single-key searches reuse `UpperBoundBy`; R11's test is a minimal inline repro. R12/R13/R14 fixes and coverage are preserved. Unproven edge-case handling and unmeasured performance redesign (R5/R6/R15) remain out of the implementation, per the rules above. The only suite failures are the two pre-existing `SpellsFinalBeamParityTest` HDR assertions contradicted by the checkpoint's own Sublimit test.
