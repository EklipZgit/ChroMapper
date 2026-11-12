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

### R13 — Addressed: Independent FFT oracle is now a test asset

Location: [GpuSpectrumPreparationTest.cs:136](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/GpuSpectrumPreparationTest.cs:136).

The old failure came from loading a missing shader under `Assets/Tests/Editor/Experiments`. Current source loads `Assets/Tests/Fixtures/OriginalEditorFFT.compute`; that shader and its metadata exist in the proposed working tree. The fixture retains its independent FFT implementation, and the test now asserts that the asset loaded before invoking it.

`SharedButterflyPreservesEditorPackedOutput` passed in `20261111-132346`. Retain the fixture and its metadata when preparing the PR; they are currently untracked. No further fix is requested. No production discovery, fallback loading, or guard is needed.

### R14 — Addressed: Triangle-frustum assertion passes in the current tree

Location: [KamikaziLightArrayTest.cs:313](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/KamikaziLightArrayTest.cs:313).

The original run failed because the lit `s934` renderer's bounds did not intersect the camera frustum at beat 20. `BackFacingTransparentTriangleRendersLitSurface` now passes in `20261111-132346`, retaining its frustum and rendering assertions. The earlier failure is not evidence of a remaining current-tree defect.

Retain that coverage. Do not add globally inflated bounds, disabled culling, or per-frame bounds repairs to close the old comment. This pass did not establish which intervening change resolved the earlier failure or reproduce native-player pixels independently.

The separate facing assertion originally grouped under R14 is addressed below.

## Validation candidates — no mandatory runtime fix

### R1 — Null-only color timeline and shared native parent anchors

Location: [DirectEnvironmentSharedTrackBinding.cs:77](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/DirectEnvironmentSharedTrackBinding.cs:77).

A null-only timeline can have a finite `StartTime` and no evaluated point definitions. A compiled generic `AnimateProperty` probe reproduced an `IndexOutOfRangeException` again on 2026-11-11 when its anchor was requested. Source tracing suggests that a directly tracked native parent/child pair sharing position and a null-only color track can reach that request through `ReadPreviousParentPose`.

What is missing: an actual map-level reproduction of that complete binding path in real-use content. The local map-data audit did not find the null-only color prerequisite. The generic probe alone is not evidence that an extra check belongs in every frame's anchor loop.

For investigation, a modified Sun fixture with `_color:null` at beat 110 and a seek to beat 122 is a useful constructed test, but it is not proof of real-world occurrence. If a real map confirms the failure, correct the existing property-presence/anchor contract at the appropriate binding or evaluation boundary. Do not introduce exception catching, repeated component recovery, or a general defensive layer. Any extra recurring work still needs the performance justification required by the workspace guide.

### R3 — Future time requests shadowing another track

Location: [TracksManager.cs:150](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Editor/Tracks/TracksManager.cs:150).

`TryGetTrackTime` reports success for a nonempty time timeline before its first request, when evaluation returns the `-1` default. The frozen-note and frozen-wall indexes stop after the first successful lookup. In a constructed A/B-track case, A's future request can therefore prevent checking B's currently active hold.

What is missing: real affected map content and an executed end-to-end reproduction. The note-oriented local inventory found no qualifying multiple-time-track note; it did not exhaust wall layouts.

If confirmed, make the existing lookup's success result agree with the established presence semantics. Prefer correcting that predicate over adding a fallback pass through the tracks, another object scan, or a new cache. Before accepting any added recurring cost, provide the real map, measured/structural cost, and the required approval. There is no requirement to add runtime handling solely for the constructed case.

### R4 — Conflicting time-track precedence in note-pose sampling

Location: [ObjectAnimator.cs:1944](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1944).

`SampleNotePose` overwrites `pathTime` for every track with a nonnegative time value, making the last applicable track win. Local Noodle source uses the first track with a value, and CM's frozen-note index explicitly follows that rule. Track names retain their input order through preparation and attachment.

What is missing: real content with conflicting active time tracks and a demonstrated spawn-pose mismatch. The local inventory found no qualifying note. The source discrepancy is worth investigating, but is not approval for a generalized track-resolution framework.

If a real reproduction confirms it, resolve the applicable existing value once while continuing to aggregate the other track properties. Avoid extra whole-map queries, repeated resolution passes, per-frame collection materialization, or new state systems. Reuse a shared rule only where that actually removes duplication and preserves cost.

### R5 — Enhancement-created Interscope car clones

Location: [NativeInterscopeCarBuilder.cs:113](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Editor/Environments/NativeInterscopeCarBuilder.cs:113).

The importer captures the original cars in fixed `VisualBodies`/`Cars` arrays and makes their bodies kinematic. Duplicating an individual car subtree later appears to copy those bodies and the data-only jump/suspension components without adding the clone to the timeline on `BasicEventEffectManager`.

What is missing: a real map depending on hydraulic motion in an individual car clone, and native-versus-CM verification of that behavior. The audited content did not provide a car-duplication candidate. A manually duplicated car alone is not sufficient justification for increasing simulation cost.

Do not add general clone discovery, extra physics scenes, duplicate simulation, or per-frame clone registration preemptively. If actual content establishes the need, determine the smallest lifecycle-time binding that preserves native behavior and scales only with the clones that really exist. Obtain approval before accepting increased runtime cost, memory, or complexity.

## Optional improvements

### R6 — Profile cold seeks only when investigating observed latency

Locations: [TimelineParticleSystem.cs:109](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Environments/Components/TimelineParticleSystem.cs:109) and [HydraulicCarTimeline.cs:111](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Environments/Effects/Basic/HydraulicCarTimeline.cs:111).

The source cost model is 18,000 synchronous `Simulate` calls per emitter for a cold five-minute seek. Car physics likewise replays elapsed frames and stores poses. These counts are not measured frame-time, allocation, or responsiveness evidence. Active renderers hidden with `forceRenderingOff` still participate; inactive emitters now release their clock callbacks through the enable/disable lifecycle.

The previous smoke-only scope is outdated: `EnvironmentDescriptor.InitializeTimelineParticles` now also registers looping, play-on-awake ambient systems while excluding event-controlled emitters. Britney's exported `DustBritney` is a concrete world-space, looping, play-on-awake system with a maximum of 1,000 particles, and the current camera-follow tests pass. The earlier observation that the inspected smoke exports used local space must not be generalized to all current timeline particles. These tests do not establish particle-history parity under an animated camera; that is a coverage limit, not a request to add emitter-history storage or additional replay machinery.

If a user-visible slowdown is observed, profile that exact map and seek before proposing an optimization. Preserve deterministic synchronous seeking and correct results after smoke is shown again. Do not automatically skip hidden smoke, change checkpoint policy, add asynchronous preparation, add caches, or require a new performance subsystem to close this comment. With no measured problem, no production change is required.

### R7 — Simplify animation responsibilities when touching an evidenced problem

Location: [ObjectAnimator.cs:14](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:14).

`ObjectAnimator` combines gameplay and sampled poses, material appearance, native hierarchy ordering, parent anchoring, pooling, and registration. Its size is a maintainability concern, not evidence of a runtime defect or performance regression.

Prefer a bounded extraction that removes an actual duplicated rule when resolving a confirmed issue. No broad rewrite, new component hierarchy, polymorphic dispatch layer, generic framework, or additional persistent state is required merely because the file is large.

### R8 — Consider serializing existing fixed structures

Locations: [GeometryContainer.cs:88](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Containers/GeometryContainer.cs:88) and [CustomEventLaneSections.cs:32](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Editor/Grid/Collections/CustomEventLaneSections.cs:32).

The material animation child and references are constructed imperatively, and category views copy multiple grid fields. Serializing the same fixed structure in a prefab/template may make configuration easier to maintain.

This is optional configuration cleanup, not a measured performance improvement. Preserve object counts, activation order, ownership, and current behavior. Do not add extra objects or per-frame dependency repair to make a template approach work.

### R9 — Reuse bound-search helpers only when the implementation stays efficient

Locations: [BasicEventChromaColorTimeline.cs:91](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Shared/BasicEventChromaColorTimeline.cs:91), `ColorBoostEffect`, and `HydraulicCarTimeline`.

Existing `LowerBoundBy`/`UpperBoundBy` helpers can replace some hand-written binary searches. Reuse is optional where contracts match. Preserve file-order/insertion-order ties, inclusive/exclusive endpoints, and allocation behavior. Do not force reuse through a freshly allocated capturing delegate/comparer on each query or change a logarithmic lookup into a scan. No new generic search framework is requested.

### R10 — Remove accidental duplicate code and warnings

Locations: [ObjectAnimator.cs:1244](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1244) and [GeometryContainer.cs:250](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Containers/GeometryContainer.cs:250).

`ApplyAnimation` repeats an identical `else if (colorRestorePending)` branch and a reset assignment. `GeometryContainer` repeats the same no-match warning block, causing duplicate log messages.

Delete the duplicate blocks and redundant comments. This removes unnecessary work. No replacement state, fallback, diagnostic subsystem, or cleanup-only test is needed.

### R11 — Make optional full-map test input configurable

Location: [BimboLimboSmokeParityTest.cs:37](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/BimboLimboSmokeParityTest.cs:37).

The optional full-map case hardcodes a personal Windows path. It could follow the environment-variable or conventional fixture-path approach already used by other parity tests. Preserve the ordinary reduced smoke coverage when full-map input is unavailable.

This suggestion concerns test setup only. Do not introduce production filesystem probing or runtime map discovery to support it.

### R15 — New optional question: Measure per-frame note-look history reconstruction

Location: [ObjectAnimator.cs:1505](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1505), called from [ApplyNoteLook](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:1477).

The new `SampleNoteLookWindow` reconstructs up to 33 history samples, calling `SampleNotePose` twice and sampling the player head in each iteration. That is up to 66 pose evaluations and 33 head-position queries per invocation. It runs during ordinary frame updates for eligible pre-hit notes without a time override, including the final pre-hit window of ordinary notes and earlier definite-position paths. It is not limited to a cold seek or a one-time post-hit reconstruction.

This is a concrete increase in repeated work, but no profiler capture in this review establishes a frame-time or allocation regression. If an actual map shows a note-animation bottleneck, profile this function with representative note counts and separate playback from stopped seeking. Preserve the native previous-up-vector behavior and the parity fixes if considering a cheaper implementation. No new cache, stateful replay system, extra component, algorithm rewrite, or mandatory optimization is requested without evidence and the required approval. Class size or the loop count alone is not a reason to change approved behavior.

## Addressed since the original review

### R12 — Recycled-note pose application

Location: [ObjectAnimator.cs:371](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/__Scripts/Beatmap/Animations/ObjectAnimator.cs:371).

The original full run reproduced four Sublimit failures after sampling recycled a note and pose application continued with cleared `NoteData`. Current source returns an explicit result from `UpdateAnimationState` and stops the caller when recycling has occurred. This addresses the lifecycle boundary instead of adding a note-data recovery check to the hot path.

The targeted result `20261111-133124` reports passes for `MopeMopePostHitTest.StoppedSeekCanFinishFakeBeforeApplyingItsPose` and all four previously failing `The15SublimitMapParityTest` cases. Retain that fix and coverage; no additional defensive handling is requested.

### R14 — Facing assertion used the viewing camera instead of the player head

Location: [AnimationReviewParityTest.cs:78](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/Assets/Tests/Editor/AnimationReviewParityTest.cs:78).

The original test measured against CM's viewing camera position, including its backward viewing offset. Current test source reconstructs the player-head position before that offset. `V2DisableNoteLookPreservesAuthoredFacing` passes again in targeted result `20261111-133124`. The disabled-look guarantee remains tested. No production fallback or extra head-position handling is requested by this review.

## Verification record and limits

- Original complete run: `pwsh ./Assets/Tests/Test-ChroMapper.ps1`, artifacts in [20261108-133545](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261108-133545). PlayMode: 1,674 passed, 7 failed, 31 skipped. EditMode: 306 passed, 0 failed, 0 skipped. Total: 1,980 passed, 7 failed, 31 skipped.
- Current focused run [20261111-132346](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261111-132346): PlayMode 37 passed; EditMode 15 passed; no failures or skips. It covers the FFT oracle, triangle-frustum case, lane index, ambient particles/spectrum, parent-assignment boundaries, pause pools, and rotation reset.
- Current focused run [20261111-133124](C:/src/BeatSaberStuff/ChroMapper-pr-animation-environment/TestResults/cli/20261111-133124): PlayMode 6 passed; EditMode 16 passed; no failures or skips. It covers the addressed recycle/facing/Sublimit cases and native texture imports.
- Total for this re-review: **74 targeted tests passed, 0 failed, 0 skipped**. These are not full regression coverage. The earlier full-suite counts describe an earlier compiled tree, not the current tree. No complete suite was rerun for this review/documentation update.
- Current compiled API probe evidence: [managed-probe-results.txt](C:/src/BeatSaberStuff/Scratch/animation-rereview-20261111/managed-probe-results.txt). These probes demonstrate that R2's ownership failures and R1's generic null-only anchor failure persist, not every proposed map-level scenario.
- Local map-data inventory: [map-case-audit.json](C:/src/BeatSaberStuff/Scratch/animation-vs-gls-review/map-case-audit.json). It is a limited prerequisite search, not a native gameplay reproduction or a proof of impossibility.
- Source inventory and incremental review artifacts are under [animation-rereview-20261111](C:/src/BeatSaberStuff/Scratch/animation-rereview-20261111). A transient deployment helper seen during the concurrent build was removed by that build and was excluded from PR findings.
- This review changed only this Markdown file and Scratch artifacts. It did not build/deploy the player, change production code, alter staging, or take a profiler capture. The independent deployment build using this worktree was allowed to finish before the targeted tests started.

Resolve R2's confirmed ownership defect and optionally remove R10's duplicate code. Preserve the addressed R12/R13/R14 fixes and coverage. Leave unproven edge-case handling and unmeasured performance redesign out of the implementation unless real-use evidence justifies them under the workspace rules.
