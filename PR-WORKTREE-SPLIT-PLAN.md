P08 — C:\src\BeatSaberStuff\ChroMapper-pr-preview-runtime-resources  -- PR'd
P05 — C:\src\BeatSaberStuff\ChroMapper-pr-grid-text-rendering
P06 — C:\src\BeatSaberStuff\ChroMapper-pr-cjk-font-rendering   -- 

# ChroMapper PR/worktree split plan

## Current execution status — 2026-10-07

This section supersedes the October 6 status and its broad full-map ownership classification. The user authorized one local P02 amend including all outstanding animation-worktree changes. Main's index remains user-managed; no push or PR merge was performed.

- **P02:** `ChroMapper-pr-animation-environment`, branch `envEnhAndAnimAndRenderParity`, amended HEAD `c8957653c`. Original author and commit message preserved. Full default gate **1141/0/0** (944 PlayMode + 197 EditMode), `TestResults/cli/20261006-224547`. Clean immediately after the amend. The original 13-path addendum and all changes present at the amend are committed; no unstaged addendum remains.
- **Main:** `ChroMapper-gls-color-shifts`, branch `glsColorDistShiftsRibbonsEasingsIcons`, HEAD `d9853c8b` after attribution cleanup and `git pull Caeden117 dev` (dev `32f89b586`). Post-cleanup full default gate **2867/0/0** (2345 PlayMode + 522 EditMode), `TestResults/cli/20261006-230716`. No Main commit. User-staged `Assets/Tests/README.md` and `Assets/Tests/Test-ChroMapper.ps1` remain staged unchanged.
- Other worktree gate records below were not re-run by this amendment task.

### Additional validated extraction

- [x] Moved eight independently passing classes and their metas into P02: `CensoredLaserResetParityTest`, `DeleteItAllMapParityTest`, `InfluxEnvironmentParityTest`, `ParadigmMapParityTest`, `SaltyOpeningSphereParityTest`, `SpellsLaserWallTest`, `RingRotationWaveParityTest`, and `VaguenessJourneyEnvironmentScaleTest`.
- [x] Included their referenced fixtures/metas and verified mapper names and map IDs from local `Info.dat` and folder prefixes. Preserved the user's existing credits and fixture-generator edits.
- [x] Included the existing generic root-clone ChromaID rewrite guard in P02. It prevents rewriting the already-renamed root marker twice; it does not import rebuilt scene data or static remap tables.
- [x] Removed the eight exclusive Main test pairs and eight exclusive fixture pairs after P02 passed. Removed the two pure `WorldCavesInMemDiagTest`/`WorldCavesInRunwayDiagTest` pairs. All **36 paths** are byte-preserved in recovery quarantine.
- [x] Removed six obsolete logging/capture-only methods without deleting their assertion-based replacement coverage: Spells' beat-288 dump, Kamikazi's triangle probe, the two Spells beat-275 isolation probes, and Salty's sphere/fake-wall and opening-ring probes.

### What remains in Main and why

Maps referencing light IDs are not automatically Main-owned. Static remap tables stay with the environment rebuild they describe. The first broad P02 transport compiled but returned **45/24/0**; the root-clone correction separately passed two of three targeted cases. Per the user's instruction, failing groups were moved back rather than weakening assertions or importing the protected rebuild/runtime bundle. Eight failed groups and seven unvalidated/manual groups were excluded from the amend; their complete functional cases and fixtures remain in Main. This is an isolation result, not a claim that every group is inherently impossible to extract.

Examples of the remaining import/calibration coupling are Billie's material compositing queue, BTS ring spacing, and the old versus rebuilt Timbaland ring identities. Other observed P02 failures involved native-alpha endpoints and render-camera behavior. These were not solved by pulling more Main dependencies into P02.

`CensoredFullMapFixture`, `SpellsLaserWallFixture`, and `WorldCavesInEnvironmentEssence` remain shared because retained Main tests and P02 tests still consume them. Unvalidated full-map experiments remain Main-owned for now; they were not merged into P02 as failing coverage. Pure diagnostics were quarantined rather than shipped or converted into `Assert.Pass` regressions.

Publishing follow-up: push the amended P02 branch when desired. If P02 or Main merges second, resolve their documented shared-code overlap and test the combined tree; neither local gate establishes a merged or published PR.

Recovery: `C:/src/BeatSaberStuff/Recovery/ChroMapper-amend-extraction-prep-20261007T020903Z/` contains verified credits, pre-edit/index snapshots, excluded P02 copies, and `main/quarantine-36/MANIFEST.psv`. The retained history-cleanup stash is `2dffa0fe`; original history and pre-amend P02 backup refs were not deleted.

## Historical execution status — 2026-10-06

**Authoritative — final current status.** This section supersedes the older historical narratives below wherever they conflict; their text, markings, and timestamps are retained unchanged as history. Gate counts below are passed/failed/skipped. User decision in effect: preserve every index — the earlier candidate-staging exception is suspended. No commits or pushes were made in this continuation.

| ID | Worktree | Branch | HEAD | Latest full gate (passed/failed/skipped) |
| --- | --- | --- | --- | --- |
| P02 | `C:\src\BeatSaberStuff\ChroMapper-pr-animation-environment` | `envEnhAndAnimAndRenderParity` | `653d29891` (user committed the original PR: `9702572e0` implementation + `653d29891` fixture credits atop dev `32f89b586`) | **1120/0/0** (923 PlayMode + 197 EditMode), `TestResults\cli\20261006-171646` — plus the unstaged validated addendum listed below |
| P05 | `C:\src\BeatSaberStuff\ChroMapper-pr-grid-text-rendering` | `gridAndTextRendering` | `971500cdb` (user committed, clean) on the new `32f89b586` base | **999/0/0** (802 PlayMode + 197 EditMode), `TestResults\cli\20261006-174459` — the earlier 949 count was on the old `4e9c8b001` baseline only |
| P06 | `C:\src\BeatSaberStuff\ChroMapper-pr-cjk-font-rendering` | `cjkAndUnicodeFallbackSongList` | `d167f23d0` (user-amended new revision) | **999/0/0** (801 PlayMode + 198 EditMode), `TestResults\cli\20261006-173502` — pre-dirty Noto + AudioLink mat/rt preserved (3 paths); not a clean claim |
| P08 | `C:\src\BeatSaberStuff\ChroMapper-pr-preview-runtime-resources` | `renderPerfAndTestPerfCleanup` | `121746a1f` (user committed) | **935/0/0**, `TestResults\cli\20261006-152045` — unchanged |
| P07 | `C:\src\BeatSaberStuff\ChroMapper-pr-difficulty-set-ownership` | `fixDifficultyDuplicationInInfoDat` | `20f42bfb4` | **935/0/0**, `TestResults\cli\20261006-152642` |
| P03 | `C:\src\BeatSaberStuff\ChroMapper-pr-json-color-precision` | `losslessJsonPrecision` | `c57cdf156` | **983/0/0**, `TestResults\cli\20261006-152829` |
| P09 | `C:\src\BeatSaberStuff\ChroMapper-pr-platform-compatibility` | `osSpecificFixes` | `543969b9a` | **940/0/0**, `TestResults\cli\20261006-153011` |
| P04 | `C:\src\BeatSaberStuff\ChroMapper-pr-preview-editor-workflow` | `pr/preview-editor-workflow` | `4e9c8b001` | **978/0/0**, `TestResults\cli\20261006-153148` — 24 staged paths preserved; no standalone PR needed (see containment note) |

Main: `C:\src\BeatSaberStuff\ChroMapper-gls-color-shifts`, branch `glsColorDistShiftsRibbonsEasingsIcons`, HEAD `1e9f576f`. Full gate **2846/0/0** (2343 PlayMode + 503 EditMode), `TestResults\cli\20261006-172549`. The reviewed retry moved the 52 main-exclusive files plus the earlier 4 NoteLook paths — **56 main-exclusive files** moved out after the candidate was green, including matching `.meta` files. The old 52-file manifest is unchanged except the `MapReloadLifetimeTest.cs` row, where the lead added a new case; the version-2 manifest/byte backups and the old v1 are preserved externally. Exactly 4 typed edit guards in 3 input `.cs` files reverted `HasSameContent` to typed `CompareTo == 0`; the other GLS/content-comparison seams were kept. No source feature loss, no assertions weakened. Diagnostic logging kept in main per user guidance.

P02 addendum (remains UNSTAGED because the user chose preserve-every-index — a validated addendum the user can stage, NOT a clean/staged claim). Exactly 13 owned paths — 10 untracked (3 portable test classes + 2 fixtures, with metas) plus 3 modified existing files:

- `Assets/Tests/Editor/DeleteItAllNoteLookParityTest.cs` + `.meta` (untracked)
- `Assets/Tests/Editor/SaltyBeat531TimingWindowParityTest.cs` + `.meta` (untracked)
- `Assets/Tests/Editor/SaltyBeat532CameraRewindParityTest.cs` + `.meta` (untracked)
- `Assets/Tests/Fixtures/DeleteItAllEndingDotFacingFixture.json` + `.meta` (untracked)
- `Assets/Tests/Fixtures/SaltyBeat531TimingWindowFixture.json` + `.meta` (untracked)
- `Assets/Tests/Editor/MapReloadLifetimeTest.cs` (modified)
- `Assets/Tests/Infrastructure/TestUtils.cs` (modified)
- `Assets/__Scripts/Editor/Loading/MapLoader.cs` (modified)

The 6 existing dirty P02 material paths (runtime/import values) were present before our runs and are preserved — they are NOT automatically PR content.

P04 containment: the complete 24-path P04 staged payload was verified inside P02 — 20 canonical-equal plus 4 P02 animation additions/comments (`workflow-overlap-evidence\manifest.psv` + 4 full diffs). The P04 test metas and the `04_Options`/VisualFeedback/pause-hover payload are identical; the differences are cameraHome capture, seek-sync subscriptions, the audio pending-flush event, the scene camera reference, and test-credit comments — no P04 workflow content is missing. The P04 worktree and index are retained as the validated backup; do not delete.

Resolved failure history: the first retry of the 52-file P02-exclusive cleanup plus the 4 typed comparison-guard reversions failed main gate `TestResults\cli\20261006-160748` (2848/1/0): `SaltyFullMapParityTest.Beat2625LaserRoutingTrial` — `SingleOrDefault` saw two type-0 ID-6 controllers (GameCorePairLaserTrackLaneRing and PanicConstructionGlowLine). All 52 + 3 input edits were exactly restored. It was NOT a production light-ID regression: a fresh `SaltyTrial` alone passes 1/0/0, and the `RingRotationWave`→`Salty` paired run was 1/1 because Timbaland targets leaked when the normal `CleanupAfterTest` deleted the controller before the `UnityTearDown` reload. Main's RingRotation fixture keeps an empty `CleanupTestObjects` override that preserves the map until its unchanged `UnityTearDown` reload destroys the targets; the same paired run on `20261006-170849` went 2/0/0, clearing leftovers, and the full post-retry gate is green. A separate cleanup-ownership bug is proven but is NOT the Salty carrier: the new P02 case failed 0/1/0 (`20261006-165224`), then MapLoader's existing `manager.DestroyTrackBoundEnvironmentObjects` call was moved BEFORE `geometry.ClearSpawnedGeometry` in both main and P02 (no other method changes) — class green 3/0/0 (`20261006-165303`). All source edits (3 guard reversions + the cleanup-call move) were reviewed; no indexes touched. Per the final validation report, all candidate full gates and main are green with 0 Unity processes.

Completed:
- [x] All eight candidate worktrees exist and hold full-suite gates (counts in the table above).
- [x] Earlier P07, P03, P09, P08, P05, P06 extractions executed (candidate gates green, source removals applied, main re-gated).
- [x] P02 independent combined PR + shared workflow payload validated; original PR user-committed at `653d29891`, latest full gate 1120/0/0 plus the validated unstaged 13-path addendum.
- [x] Main scoped removals complete at the documented boundary: 56 main-exclusive files (52 + 4 NoteLook) moved out after candidate green, including matching `.meta` files.
- [x] Two small stabilizations applied and verified: the 4 typed `CompareTo == 0` guard reversions in 3 input files, and the MapLoader cleanup-order move in main + P02.
- [x] P04 containment verified — complete 24-path payload present in P02 (20 canonical-equal + 4 documented P02 additions); P04 retained as validated backup.
- [x] Updated-base P05/P06 validation re-run and green on the current bases.
- [x] Every index preserved — user-managed; candidate-staging exception suspended.

Pending — user-owned follow-up: stage/commit/push the P02 validated 13-path addendum if desired; exclude the 6 pre-dirty materials unless intentional; whichever of P02/main merges second, resolve the intentional shared-code conflicts and run the combined suite.

Main-retained ownership (approved scope, review complete at these boundaries): P01 GLS/import/light-ID/native/generated/cloned-ID work and assets; the user-authorized shared workflow overlap; the required shared animation/runtime integration and the full-map/ID-dependent tests. This does NOT claim all P02 production was removed — the boundary is a dependency overlap: `BasicLightEffect`'s `HeckNamed` is used by 4 main calls; GLS actions/grids need the content-comparison seams; `GeometryContainer` native IDs/remapping are interwoven with track/hierarchy/lifetime cleanup; `LoadedDifficultySelectController` uses the MapLoader cleanup; and the WorldCaves/Spells/Paradigm/The15Sublimit/Heliov/DeleteItAll/BloomFog refreshed-asset integration tests stay main. The shared `SpellsLaserWallFixture`/`WorldCavesInEnvironmentEssence` fixtures and the 5 workflow test pairs remain in main. Major feature-ownership review is complete at these boundaries; the granular legacy Phase-1 all-path/GUID manifest items stay unchecked for lack of complete evidence and are deferred to a future fine-grained PR audit — they do not block the validated extractions or gates.

Recovery artifacts (all outside the repo): recovery root `C:\src\BeatSaberStuff\Recovery\ChroMapper-gls-color-shifts-20261006T025404Z\` (existing); `P02-reconcile-transport\` — including the requested manifests `P02-removal-manifest.psv` (v1) and `P02-removal-manifest-v2.psv`, incoming transports, byte backups, diffs, integrity files, and the tracker snapshots; `P02-reconcile-main-snapshot\` and `P02-reconcile-quarantine\` (`MOVED-MANIFEST.psv`, 56 entries) — the two pre-op snapshot/quarantine dirs; `artifacts\lifetime-fix-pretest\`; `workflow-overlap-evidence\` (P04-vs-P02 manifest + 4 full diffs + staged forward patch); `artifacts\gates\GATE-MANIFEST.txt`; `artifacts\final-proofs\states\`; `C:\src\BeatSaberStuff\Recovery\P02-NoteLookParity-20261006T154633Z`.

Updated: 2026-10-05.

## Status and scope (historical — superseded by "Current execution status — 2026-10-07" above)

This document began as a planning and tracking document; execution is now underway. As of 2026-10-05 the environment-asset history consolidation and the pre-rewrite recovery snapshots are complete. The initial full default-suite gate on the amended main history failed once (3023 passed, 1 failed — the Pyro fire-flash parity case; details below), then passed after the approved asset-only Pyro stabilization: **3024 passed, 0 failed, 0 skipped** on 2026-10-05 (`TestResults\cli\20261005-223138`), so the gate is now green and per-item extraction gates proceed under their own review. All eight extraction worktrees exist (P02 seeded with the verified 24-file workflow payload — one cohesive animation+workflow PR per user decision, main intentionally retains the same workflow content); P07 (user-published branch `fixDifficultyDuplicationInInfoDat`, commit `20f42…`, source-removed, all gates green), P03 (user-published authoritative branch `losslessJsonPrecision`, clean, tracking origin; earlier premature commit `96da7540` undone by `git reset --soft` under explicit user approval; source removal done, main green 2974/0/0), P09 (user-published `osSpecificFixes` commit `ac13fe3d5`; candidate green 940/0/0; source removal done, post-removal main green 2968/0/0), P08 (candidate green 935/0/0, 11 owned paths staged, diff accepted; source removal done, post-removal main green 2967/0/0 — awaiting user commit on `pr/preview-runtime-resources`), and P04 (candidate green 978/0/0, 24 owned paths staged incl. the settled `04_Options.unity` stripped-declaration repair — user may commit; **main removal attempted, hit retained-P02 dependencies, exactly undone — main green 2967/0/0 restored; removal blocked pending P02 dependency resolution**); P05 (candidate green 949/0/0, 47 owned paths staged and diff accepted; source removal done, post-removal main green 2952/0/0 — awaiting user commit on `pr/grid-text-rendering`), and P06 (candidate green 949/0/0, 17 owned paths staged and diff accepted; source removal done, post-removal main green 2937/0/0 — awaiting user commit on `pr/cjk-font-rendering`); remaining items pending their own gates. Scoped `git add` on candidate worktrees' owned paths is explicitly user-authorized; the main index remains user-managed. This document is left unstaged by default; index state remains user-managed except for the task-scoped synchronization noted below.

The current intent is **eight PRs: the existing main GLS PR and seven extracted PRs** — P04 folded into P02 by user decision, so the P04 worktree remains only as a validated overlap backup (no separate PR). The objective is for every extracted PR to compile and pass its own coverage on `dev`, without requiring the main GLS PR or another unmerged extraction. Independence is a proposed boundary until demonstrated in an isolated worktree.

The initial review covered the staged inventory, relevant implementation/test diffs, and the three protected commits. The 2026-10-05 follow-up refreshed the inventory and corrected light-ID ownership. Those inventory figures are **historical** snapshots (1,167 changed index paths at the follow-up, 1,150 at the initial review — both pre-consolidation). Current state after the rewrite and the approved index sync: `git diff --cached` is zero and the remaining work is unstaged/untracked only (418 modified / 291 untracked / 7 deleted, plus incidental AudioLink runtime noise). This document is a feature/dependency plan, not a completed per-hunk manifest for every path. Refresh that manifest before extraction because development is continuing.

Source worktree:

- Directory: `C:\src\BeatSaberStuff\ChroMapper-gls-color-shifts`
- Branch: `glsColorDistShiftsRibbonsEasingsIcons` (this document previously listed a stale branch name)
- Reviewed HEAD (original history): `4c3166d05b64e060887d027156c004000ab9fe9f`
- Amended HEAD (current, second rewrite): `1e9f576f8b0ff45053b18f6afe884df8922b25fe`
- Backup ref for the pre-rewrite tip: `backup/glsColorDistShiftsRibbonsEasingsIcons-pre-rewrite` → `4c3166d05b64e060887d027156c004000ab9fe9f`
- Backup ref for the first-rewrite tip: `backup/glsColorDistShiftsRibbonsEasingsIcons-pre-rewrite-2` → `6f45b75a6c2f599a1c65c4eebebab94a297e9c2d`
- Reviewed local `dev`/`origin/dev` and merge base: `4e9c8b0014bf7ffe9c7a5a7c59e9176bc8ad53e1` (preserved verbatim as the merge's second parent in amended history)

Use that pinned `dev` commit for the first isolation checks. If `dev` advances, explicitly update the recorded base and revalidate. Do not use the main worktree's HEAD as the starting point for an extracted PR.

### Execution record — history consolidation (2026-10-05)

- User approved the history rewrite of the three protected commits, a task-scoped source index synchronization, and the sequential removal of validated extracted changes.
- Recovery root (outside the repo): `C:\src\BeatSaberStuff\Recovery\ChroMapper-gls-color-shifts-20261006T025404Z\`. Contains the full pre-rewrite snapshot (real index binary, every tracked worktree file, all untracked non-ignored files, `head-tree.tar` of `4c3166d05`), SHA-256/git-blob manifests, GUID dependency map, selected-path and blob maps, per-commit tree mapping, verification output, message files, and the gated rewrite script (`scripts/Rewrite-GlsEnvAssets.ps1`; tree generation complete, commit phase executed under lead control).
- Environment bundle actually consolidated: **736 selected paths**, manifest at `trees/selected-paths.txt` with canonical (clean-filtered) blobs in `trees/selected-blob-map.psv`. Scope: 22 modified `Assets/Editor/Environments/LightIDTables/*.json`, `EnvironmentMaterialSO.asset` + `EnvironmentMeshSO.asset`, 47 `Assets/__Scenes/Environments/*.unity` + 47 `Data/*.json`, and 600 `Assets/_Graphics/Materials/Environments/**/*.mat` (582 modified + 18 new) with the 18 new companion `.meta` files. Importer C#, unrelated materials/fixtures, and mapper scenes were excluded. No `.meta` deletions or extra folder metas were required; the only files absent from the base tree were the 18 new `.mat` + 18 `.mat.meta`.
- GUID dependency audit (`manifests/guid-map.psv`): 1,567 referenced GUIDs — 978 already in the base tree, 586 inside the selected set, 3 unresolved (2 Unity built-in resource GUIDs + 1 pre-existing external `m_Script` reference unchanged from base). Nothing else had to accompany the bundle.
- Commit mapping, original → amended — **final (second rewrite, Pyro flag folded in)**. First-rewrite intermediate SHAs are in `trees/mapping-old-to-newcommit.psv`; final mapping is `trees-v2/mapping-old-to-newcommit.psv`:
  - `7f791739ba975975d5716d59f2cc4e2dedfa05e2` → `e8ebe46178cd8695a00cde25660a7bd6dcfffc5d` (first pass `327c0285e5cd3d70527a19b64b4969c852b753bb`)
  - `070276115065c4232d7bc5cfba2286df3efd4d23` → `da6569a63425ea74dbf46a4faa12040391bbed06` (first pass `0dbe880b48eab8ec676380311252aecb9ffd58ab`)
  - `463d598eec9fe9766a0d08661a9d202d1327cb98` → `d9790ecd80c51a58ea6e42ead23e69f4571887a5` (first pass `a96dd04ab79d5546d7067f5610ac5902dabcf0d2`)
  - Remaining first-parent descendants rewritten to `61b983bb`, `a11f8908`, `033c9d3e`, `9bb858fe`, merge `574d41f0` (second parent `4e9c8b001` unchanged), tip `1e9f576f`. Boundary parent `d928b6862` unchanged. Author/committer identities and dates preserved byte-exact from the original commit objects.
- Verification: every rewritten tree differs from its original only inside the 736-path selected set (0 non-selected violations, 0 selected-blob mismatches; `trees/verification.txt`). 40 of the 736 paths have CRLF worktree bytes whose canonical blobs normalize to LF (22 light-ID `.json` + 18 new `.mat.meta`; `trees/canonical-discrepancies.txt`).
- Index sync: the selected paths were restored into the index from the new tip only; cached diff vs HEAD is zero and working files are unchanged. Status now shows only non-selected work: 418 modified, 291 untracked, 7 deleted.
- Initial full default-suite gate on the amended history (`pwsh -NoProfile ./Assets/Tests/Test-ChroMapper.ps1`, PlayMode+EditMode, no manual/Explicit): **COMPLETE — 3023 passed, 1 failed, 0 skipped** (PlayMode 2494/1, EditMode 529/0). Artifacts: `TestResults/cli/20261005-202524/` (`playmode-results.xml`, `playmode-unity.log`, `editmode-results.xml`, `editmode-unity.log`). Single failure: `Tests.Editor.BasicEventFixedDurationParityTest.PyroFireFlashKeepsEqualHighlightAndNormalBrightness` — expected `light.Color.a` = 1.0 ±0.0005 at JsonTime 4.5 after a RedFlash in `PyroEnvironment`, got 0.823384166 (`BasicEventFixedDurationParityTest.cs:181`; that test file is still untracked work). No prior full-suite baseline exists in this worktree, so pre-existing vs new regression is undetermined; the rewrite did not change any worktree bytes. Side effect: Unity's runtime mutated the incidental `mat_AudioLink.mat` (media-time/playing fields), matching the known incidental-AudioLink caveat — left untouched, user-managed.
- Targeted rerun (`-TestFilter 'Tests.Editor.BasicEventFixedDurationParityTest.PyroFireFlashKeepsEqualHighlightAndNormalBrightness'`, 2026-10-05 ~20:41): **deterministic failure** — identical `0.823384166` at the same assertion; isolation rules out fixture-state/ordering interference, so the class-level filter rerun was not needed. Artifacts: `TestResults/cli/20261005-204136/` (`playmode-results.xml`, `playmode-unity.log`, `editmode-results.xml`, `editmode-unity.log`). Gate disposition pending lead's root-cause trace; no extraction/removal proceeds meanwhile.
- History-consolidation completion checklist (all executed under the approved rewrite, 2026-10-05/06):
  - [x] Historical target selection: first protected commit `7f791739` chosen to receive the consolidated bundle (contains the related The Second runtime-order fix).
  - [x] Replacement messages applied to exactly the three protected commits; all other messages preserved byte-for-byte.
  - [x] Rewritten trees verified per commit: first-parent topology preserved, merge second parent `4e9c8b001` kept verbatim, every rewrite differs from its original only inside the 736-path selected set (0 non-selected violations, 0 selected-blob mismatches).
  - [x] Canonical clean-filter blob handling audited for all 736 paths (40 CRLF-normalized files recorded); commit objects use canonical blobs.
  - [x] Backup ref `backup/glsColorDistShiftsRibbonsEasingsIcons-pre-rewrite` created at the pre-rewrite tip before refs moved.
  - [x] Task-scoped index synchronization executed (selected paths restored from new tip only); cached diff zero, working files unchanged.
  - [x] Full default-suite gate green — initial run 3023/1/0 (deterministic Pyro parity failure recorded above), then **3024/0/0** after the approved asset-only Pyro stabilization (`TestResults\cli\20261005-223138`).
- Transport preparation (mechanical, pre-extraction; no worktrees created, no source mutations, no tests):
  - [x] P07 bundle — `Recovery/.../P07-transport/`: 4-file scope (`DifficultySelect.cs`, `CharacteristicCustomPropertyItem.cs` canonical patch; `DifficultySetDuplicationTest.cs` + `.meta` copies), integrity manifest, verify + gated removal scripts.
  - [x] P09 bundle — `Recovery/.../P09-transport/`: 9-file scope (`PathUtils.cs`, 4 `StandaloneFileBrowser*.cs`, `TextureProcessor3DWriteTextures.compute`, `packages.config` canonical patch; `PlatformDependencyImportTest.cs` + `.meta` copies), integrity manifest, verify + gated removal scripts. Lead verified `dev..HEAD` is empty for all seven production paths, so every outstanding delta there is P09-owned.
  - [x] P03 bundle — `Recovery/.../P03-transport/`: 62 production files classified (48 whole-file diffs + 14 surgical hunk subsets built via scratch baseline seeded from pinned `4e9c8b001`), forward `patches/base-to-candidate/` + `patches/main-removal-inverse/` per file, `files/` full copies, `P03-integrity.psv` (70 rows: raw sha256 + raw/canonical/HEAD blobs), `P03-test-ownership.psv` split manifest, `SHA256SUMS.txt`, concatenated `review-forward.patch`/`review-main-inverse.patch`, `hunk-transport-report.txt` (per-hunk TRANSPORTED/PARTIAL/EXCLUDED with marker reasons). Split tests: `HueDegreesDisplayTest` transports 5 standalone picker cases + picker helper/const/FieldInfo (distribution-row cases stay in main, removal patch removes only transported members); `SavePreservesAuthoredFileOrderTest` transports only `AuthoredFloatPrecisionSurvivesSave` + shared setup/teardown (chronology/WorldCaves cases stay). Offline apply verification (no repo writes): forward applies cleanly on a true `4e9c8b001` snapshot (62/62); whole-64 inverse set applies cleanly on the frozen canonical main snapshot and `git apply -R` reconstructs the worktree canonical blobs; both split-test inverses land byte-for-byte (modulo CRLF) on `removal-remainder/`. Three inverse patches (`BaseCustomEvent.cs`, `V3FloatFxEvent.cs`, `V3LightTranslationBase.cs`) were re-derived from frozen main context after the originals' dev-context hunks conflicted with interleaved non-P03 changes (content-identity `HasSameContent` — P02-owned, not protected GLS — and conditional extension-easing export); originals preserved in `patches/conflict-superseded/`. Two mechanical patch defects found and repaired: double-mangled BOM bytes in `SimpleJSON.cs` patch pairs, and a missing EOF-newline hunk in `SimpleJSONUnity.cs` patch pairs. Lead technical patch review completed 2026-10-06 (forward 1247 lines, inverse 1422 lines, all four split-test subsets, and the 3-patch re-derivation accepted); no removal script authored for P03 — extraction and tests still pending.
  - [x] P04 bundle — `Recovery/.../P04-transport/`: 13 production files classified (8 whole-file transports + 4 surgical C# hunk transports + `03_Mapper.unity` surgical YAML transport) built via scratch baseline seeded from pinned `4e9c8b001`, forward `patches/base-to-candidate/` + `patches/main-removal-inverse/` (13 per-file patches each), `files/` full copies + `candidate-files/` surgical candidates, `P04-integrity.psv` (13 rows: raw sha256 + raw/canonical/HEAD blobs), `P04-test-ownership.psv` (5 whole-copy test files + metas), `SHA256SUMS.txt`, concatenated `review-forward.patch`/`review-main-inverse.patch`, `hunk-transport-report.txt` (per-hunk TRANSPORTED/EXCLUDED + retained excluded-hunk summaries). Kept-vs-excluded within mixed files: `CameraController` transports cursor interface/state + lock-owner + OnEnable/OnDisable cursor handling only (cameraHome fields, Disconnect/HandleMapLoaded FOV+settings handlers, SyncPlayerTrack methods/binding/home-pose stay in main — P02); `AudioTimeSyncController` transports only `internal static Instance` + Awake set + OnDestroy conditional clear (OnTimeFlushPending and TrackLength `ClearSettingNotifications` stay in main — P02/P08); `BeatmapInputController` transports `PlaybackHoverSuppressed` + the Update early gate calling the dev-existing `HandleHoverChanged(TContainer)` seam (ShouldHighlightHoveredObject/IsGradientHit stay in main — P01); `GridLane` transports only `SetGridVisible` (edge-overdraw constants stay in main — P05); `03_Mapper.unity` transports only the UIMode `editModeContext: {fileID: 405385889}` link. Placement Arc/NJS and their mixed tests excluded pending case review. Offline apply verification (no repo writes): forward 13/13 clean on a true `4e9c8b001` snapshot producing `scratch/candidate`; inverse 13/13 clean on a frozen canonical-main snapshot producing `scratch/minus`; `main-minus + forward` reconstructs the worktree canonical blob for all 13 files. No removal script authored for P04 — pending lead patch review.
  - [x] P08 bundle — `Recovery/.../P08-transport/` (prepared separately): 11 source files (9 production + 2 tests) with forward/inverse patches and verifier; offline patch roundtrip only — not extracted, not tested. Recorded exclusions: `MirrorRendererSO.OnDisable` unsubscribe, collection per-subscriber cleanup, and the Settings callback shim stay in P01 conservatively because `GLSGroupPagePoolingTest` needs the callback semantics; `MapReloadLifetime` source-helper test cases (both) stay pending P02 case ownership. `AudioTimeSyncController` TrackLength/ClearSettings cleanup does transport to P08.
  - [x] P07/P09 removal-helper hardening — both `Remove-*FromSource.ps1` now preflight `git diff --cached` over their scoped paths and verify HEAD still holds the pinned production blobs **before** any worktree mutation; a scoped staged change aborts untouched rather than being discovered after overwrite. SHA256SUMS regenerated for both bundles.
- Scene-flag discrepancy resolved to disk-vs-IDE (2026-10-05 ~20:50): an unsaved IDE buffer shows `HighlightMatchesNormalColor: 1` on the `PyroEnvironment.unity` `&2086386425` (ID 1) block, but the **on-disk file still has `0` in all 7 BasicLightEffect blocks** (verified lines 240643–240666; file mtime 19:27, unchanged since before the gate). `git hash-object` = `adc32cb4b42a7aa4f66a18ca3b9ec6b47028bf1e`, byte-identical to the frozen selected-blob map and to `snapshot/worktree` (diff --no-index zero); HEAD `6f45b75a` unchanged. Extracted context for the lead: `Recovery/.../artifacts/pyro/` (manager block `&2086386428`, all 7 BasicLightEffect blocks + GameObject, `LightSwitchEventEffect` JSON records). **Blocker resolved 2026-10-05 ~22:30:** lead applied the approved minimal asset-only correction — a one-line patch (`Recovery/.../artifacts/Pyro-event1-highlight-flag.patch`) persisting `HighlightMatchesNormalColor: 0→1` on `&2086386425` (Event1/Type-1 manager) only; other six blocks stay 0. `git diff --text` shows only that line; new canonical blob `daeb7e361079ddd478ad38999ecf2b2259438f37` (was `adc32cb4…`). No C#/assertion changes. Targeted rerun 22:30 `TestResults\cli\20261005-223055`: **1 passed / 0 failed**. Full default suite re-run `TestResults\cli\20261005-223138`: **3024 passed / 0 failed / 0 skipped** — gate green. A second history rewrite (seeded map = frozen v1 map + only the approved Pyro blob override, `trees-v2/`, script `scripts/Rewrite-GlsEnvAssets-v2.ps1`) folds the final Pyro blob into the same first protected commit.
- Second-rewrite prepare phase complete 2026-10-05 ~22:45 (tree regeneration only — no commits, no refs): `trees-v2/selected-blob-map.psv` = frozen v1 map with only the Pyro row overridden to `daeb7e36…` (736 upserts); seeded map re-verified against worktree — 736 files, 0 drift; all 9 original commits' trees regenerated in an alternate index — every commit: 736 deltas, 0 non-selected violations, 0 selected-blob mismatches; new tip tree `2f61d49043546c545d24be17a88b524073c77d1a` vs current HEAD tree differs in exactly `Assets/__Scenes/Environments/PyroEnvironment.unity`. `-CreateCommits` **executed by lead 2026-10-05 ~23:05** (script fixed: `Get-GitBytes` restored missing `ArgumentList` forwarding): `trees-v2/mapping-old-to-newcommit.psv` records original→final SHAs; `backup/glsColorDistShiftsRibbonsEasingsIcons-pre-rewrite-2` → `6f45b75a` created; branch CAS-updated `6f45b75a`→`1e9f576f` and the Pyro path index-synced from the new tip — cached diff 0, worktree bytes identical to the green-3024 suite run.
- P07 extraction executed 2026-10-05 (first real candidate): worktree `C:\src\BeatSaberStuff\ChroMapper-pr-difficulty-set-ownership` @ `pr/difficulty-set-ownership` from pinned dev `4e9c8b001`; ignored deps provisioned from the source cache (`Assets/Packages/` NuGet dirs + analyzer local-package + `AddressableAssetsData/Windows`); test-only baseline → expected red (`Expected: 1, But was: 8`, `TestResults\cli\20261005-224721`); `P07-production-files.patch` applied → targeted green 1/0/0 (`20261005-224929`) → candidate full suite green 935/0/0 (`20261005-225004`). Unity runtime noise in the candidate snapshot to `artifacts/P07-candidate-noise/` then restored — final candidate diff is exactly the 4 owned paths (`artifacts/P07-candidate-complete.diff`, `1140d1bd…`; commit-ready alt-index tree `0270183e4062af4df1ca5e76c005adc4ce595b23` + `commit-message.txt` + `candidate-tree-vs-dev.patch` under `artifacts/P07-commit/`). Source removal run after lead acceptance via gated `Remove-P07FromSource.ps1 -CandidateValidated` (all preflights passed; worktree-only restore of the 2 prod files from pin `6f45b75a`, delete of the 2 test files; no index/staging changes). **Post-removal main full suite: 3023 passed / 0 failed / 0 skipped** (`TestResults\cli\20261005-230201`) — main stays green. An earlier extraction commit `fe49d1b0372621b96bf346aab01acf6f7cc0cfb4` (tree `0270183e…`, parent `4e9c8b001`) was superseded: the user renamed the branch to `fixDifficultyDuplicationInInfoDat`, published their own commit `20f42…` to remote, and that is authoritative. P07 end-to-end complete.
- P03 candidate validated 2026-10-05: worktree `C:\src\BeatSaberStuff\ChroMapper-pr-json-color-precision` @ `pr/json-color-precision` from pinned dev `4e9c8b001`; deps provisioned via the settled cache recipe; 8 test artifacts copied (whole `BeatOnlyJsonPrecisionTest` + `ColorPickerPrecisionTest` with metas, split candidate `HueDegreesDisplayTest` + `SavePreservesAuthoredFileOrderTest` with their existing metas). Test-only baseline on unfixed dev: **13 passed / 36 failed** — expected functional red across JSON round-trip, fog attenuation, hue-degree display/parse (0.5→"0.5" vs "180"; 255→360 label), alpha precision, authored-color channels (`TestResults\cli\20261005-231834`). `review-forward.patch` applied clean (62 diffs, no extras); post-fix filter **49/0/0** (`20261005-232033`); candidate full suite **983/0/0** (`20261005-232108`). Runtime noise snapshot at `artifacts/P03-candidate-noise/`; final candidate status = exactly 70 owned paths (62 prod M + 8 test ??); complete diff `artifacts/P03-candidate-complete.diff` (1806 lines, `92662183…`). Lead accepted the full 1806-line diff; **source removal executed ~23:25** — preflight verified all 70 scoped files against `P03-integrity.psv` (0 drift) and scoped cached index clean; `review-main-inverse.patch` applied (62 prod inverses + 2 split-test member removals, preserving `HasSameContent`, conditional-easing, and all P01/P02 context); only the 4 whole-owned test files deleted; `HueDegreesDisplayTest`/`SavePreservesAuthoredFileOrderTest` remainders verified byte-equal (mod CRLF) to `removal-remainder/`; fully-P03 files (`SimpleJSON.cs` et al.) canonical-equal to dev blobs. Undo recipe preserved: `artifacts/P03-preremoval-worktree.diff` (62-file pre-removal diff, `8cd6689e…`) + `git apply -R review-main-inverse.patch` + `files/` backups. **Post-removal main suite: 2974/0/0 green** (`TestResults\cli\20261005-232950`). The earlier commit `96da7540` was **undone at the user's explicit request** ("undo and leave staged") via `git reset --soft 4e9c8b001` — branch back at dev base, all 70 owned paths now **staged** (worktree-vs-index empty), no commit; user will commit/push themselves. Prepared artifacts retained under `artifacts/P03-commit/` (alt-index tree `3df03e345b1a2aa4400402ef6d1656d694299fe2`, `candidate-tree-vs-dev.patch`, `tree-manifest.txt`); no further commits/refs/pushes without an explicit ask.
- P05 bundle — `Recovery/.../P05-transport/` (prepared offline; lead reviewed forward/inverse text views 1716+1716, `GridCoverage.hlsl`, Teko scalar/reference, `Verify-P05Integrity`, existing `CustomEventLogoRenderingTest` + `RegenerateTekoTool`): 39 tracked sections + 8 untracked copies, 104 integrity-manifest rows. Scope: P01 GLSIcon/ribbons and P04 `SetGridVisible` excluded; generic `Sprites.mat` + full CustomEventLogoRender pixel coverage included; Teko binary payload is the exact frozen regenerated source (not the P01 anchor file; dev/HEAD identical), native/Noto payload excluded, features/kerning table cleared as already-existing-asset regen — verify visually when extracted; deletion-controller/meta paired scenes + locale IDs 43/105 only. Offline scope accepted; candidate extraction executed 2026-10-06 (see P05 row — baseline expected-red → green 949/0/0, 47 paths staged).

## Non-negotiable ownership rules

1. Do not stage, unstage, reset, stash/pop, or otherwise rearrange the source index. Git staged state is managed by the user. Narrow explicit exception already granted by the user: the task-scoped index synchronization used for the consolidation (`git restore --staged --source=<new tip> -- <selected paths>` only). All other index state remains user-managed.
2. Anything depending on changes introduced by the three protected commits stays in the current main worktree/PR. Original reviewed hashes → amended current hashes (same feature content; messages retitled; consolidated environment bundle added to the first):
   - `7f791739ba975975d5716d59f2cc4e2dedfa05e2` → `e8ebe46178cd8695a00cde25660a7bd6dcfffc5d` — "GLS event icons and stuff" → "Add GLS easing icons, song-boundary protection, and corrected environment imports"
   - `070276115065c4232d7bc5cfba2286df3efd4d23` → `da6569a63425ea74dbf46a4faa12040391bbed06` — "tmp" → "Implement per-light GLS color timelines and distribution editing"
   - `463d598eec9fe9766a0d08661a9d202d1327cb98` → `d9790ecd80c51a58ea6e42ead23e69f4571887a5` — "more stuff" → "Refine GLS icons, hover controls, and timeline cache behavior"
   Both hash sets are protected: the originals remain reachable via `backup/glsColorDistShiftsRibbonsEasingsIcons-pre-rewrite`; the amended commits are the current history.
3. **All light-ID changes stay in the main worktree.** This includes environment import/export ordering and identities, the matching light-ID tables, runtime registration/remapping, generated/cloned-light identity changes, and dependent tests/fixtures. Do not move them to the animation/environment PR.
4. Keep generated environment data, scenes, material/mesh/texture lookup assets, and matching references together in the main worktree. Do not extract the bulk imported asset set piecemeal.
5. A shared filename does not automatically make every hunk dependent. However, an extraction must not carry an anchor's implementation, schema, GUID, generated asset payload, or helper API merely to make its own code compile.
6. If a candidate cannot work independently without protected main-PR changes, retain that part and its tests in the main PR. Record the decision here rather than dropping coverage or weakening assertions.
7. Keep source files and their `.meta` assets together. Preserve serialized GUIDs and companion references.
8. Treat staged, unstaged, and untracked content as separate sources. Use index blobs for the staged snapshot, not indiscriminate copies of current working files.

### What the history check actually established

This check describes the **original reviewed history**. In the **amended current history**, the same feature changes ride rewritten commits and the consolidated 736-path environment bundle is now committed inside the first protected commit (`e8ebe461`), held constant through all descendants — so the bulk environment directories **are** part of current committed history, exactly once, by design.

The three protected commits do **not** change the bulk `Assets/Editor/Environments/` imports/light-ID tables, `Assets/__Scenes/Environments/` scene/data assets, or `Assets/_Graphics/Materials/Environments/` material set. The reviewed committed branch delta against `origin/dev` also has no changes in those asset directories.

They do contain related runtime ordering/evaluation work. In particular, `7f791739b` changes `SmoothStepPositionGroupEventEffect` to restore The Second's ring slot order instead of trusting exported sibling order. The other protected changes include GLS filtering/timeline behavior, shared color interpolation/ribbon work, and the song-boundary infrastructure.

Therefore, it would be inaccurate to say the bulk light-ID/import asset changes are already checked into those three commits. Nevertheless, the user's ownership decision is explicit: **the import/identity correction and light-ID work form part of the main PR and stay there regardless of whether a particular hunk is already committed.**

`Update-GameCoreRingIndexes.ps1`, currently untracked, also describes the environment-before-gameplay capture ordering and corresponding GameCore ring-index corrections. It is supporting context, not permission to execute the script or include it automatically.

## Worktree and PR tracker

All proposed directories are siblings under `C:\src\BeatSaberStuff`. P01 already exists. P02–P09 have not been created. Branch names and PR titles are proposed names, not created branches or published PRs. (Superseded 2026-10-06: all eight worktrees now exist with the branches shown in "Current execution status — 2026-10-06"; the per-row cells below remain the execution history.)

| ID | Worktree directory | Branch | Proposed PR title | Extraction status | Independent verification |
| --- | --- | --- | --- | --- | --- |
| P01 | `ChroMapper-gls-color-shifts` | Existing `glsColorDistShiftsRibbonsEasingsIcons` | GLS distributions, easings, ribbons, and environment/light-ID integration | History consolidation complete; approved asset-only Pyro flag stabilization persisted on disk (Event1 `HighlightMatchesNormalColor` 0→1, blob `adc32cb4`→`daeb7e36`); targeted gate rerun green; **full default suite green 3024/0/0** (`TestResults\cli\20261005-223138`) | Initial full default suite on amended history: 3023 passed / 1 failed / 0 skipped; targeted rerun: failed identically (deterministic); post-stabilization targeted rerun: 1 passed / 0 failed (`TestResults\cli\20261005-223055`); post-stabilization full suite: 3024 passed / 0 failed / 0 skipped |
| P02 | `ChroMapper-pr-animation-environment` | `pr/animation-environment-parity` | Chroma/Noodle animation and environment-enhancement parity **plus the P04 preview/workflow payload** | **User decision 2026-10-06**: one cohesive animation PR — "just include the workflow changes in the animation PR, and also leave it in the main big PR and we can deal with merge conflicts whichever merges second"; no micro-splitting. Main retains the shared workflow intentionally (P04 removal NOT retried — resolved by policy, not by code fix). Candidate worktree at `C:\src\BeatSaberStuff\ChroMapper-pr-animation-environment` (branch `pr/animation-environment-parity`, pinned dev `4e9c8b001`); deps provisioned from cache; **verified 24-file workflow payload seeded** — frozen P04 `base-to-candidate` 13 prod patches + `scope-supplement/extra-scene-forward.patch` (04_Options 12-line decl repair) + 5 frozen test pairs + `teardown-stabilization.diff` (`-p0`), with `EditModePlayingCameraTest.cs` taken verbatim from the user-owned P04 candidate worktree to include the lead's 2-blank-line format fix; **all 24 seeded files' canonical blobs verified identical to the P04 candidate's accepted staged blobs** (`git hash-object` join, 24/24); no staging until the cohesive animation cohort gates pass; animation-exclusive scope still under lead audit. **Phase-2 cohort implemented 2026-10-06** (settled scope only): 20 whole-file copies from current main (4 Animations + 2 Appearances + BaseMaterial + V2/V3Material + ObstacleContainer + Track/TracksManager + MapLoader + ATSC `4b4f`-content + CameraController `06e116`-content + GeometryGridContainer + ParametricBoxLight/ParametricBloomFogLightController/BloomFogObject + BloomfogRenderingController), 4 new pairs (BaseProviderManager/FogAnimator/TubeBloomAnimator/EventOrderComparer + metas), 6 surgical files (BaseObstacle `CustomVisualScale`+scale-parse only; ObjectContainer `IsPlacementVisual` only; BasePlacement `CreateVisual` flag only; `Obstacle.prefab` `outlineRenderer` binding only; Easings HeckByName/HeckNamed/4 Heck classes only — dev ByName/ByID/shaderID/Elastic untouched; GeometryContainer main-body + **dev light-identity restored** in the 3 specified areas — `controller.ID`/`Register(false)` ILightWithId, unconditional child-marker rewrite, unconditional unregister/Register(false) controller loop — keeping the P02 instantiate-root/SetParent fix + TubeBloom setters), `03_Mapper.unity` TracksManager `cameraManager: {fileID: 1206999167}` (verified against source), ordering closure (BaseObject + BaseDifficulty whole copies = FileOrder/WithFileOrder/SongBpm only; BaseCustomEvent CompareTo→HasSameContent hunk), and the 5 ruled compile seams (BeatmapRuntimeContext: `OnBloomFogParamsChanged`+`NotifyBloomFogParamsChanged`+active `ColorScheme` assign only — GLS counts/gradient excluded; LightController `[NonSerialized] public HasInitialized` visibility only; LightPairRotationEffect whole file = coherent movement delta; `BeatmapObjectContainerCollection<T>` `SortComparer` default stub only; CustomEventGridContainer whole file — authorized, no GLS/light-ID content). 2 pure test pairs copied (PointDefinitionParityTest, AnimatePropertyScrubTest). **Initial compile+filter green: 13/0/0** (`TestResults\cli\20261006-093232`); tracked forward diff `artifacts/P02-cohort-tracked-forward.diff` (`c6ae0ce7…`); 68 rows total (46 M + 22 ??), zero staging, no full suite until the chronology/gameplay/material test cohort lands. **Phase-3 chronology/gameplay/portable-test cohort implemented 2026-10-06** (settled scope only): V2/V3/V4 difficulty writers — full dev→main chronology deltas applied (783 lines, zero GLS/light-ID tokens, all three now byte-identical to main — `WithFileOrder`/`InFileOrder`/`EventOrderComparer`/stable custom-event order); `BaseEvent` CompareTo→`HasSameContent` payload block only; `BeatmapObjectUpdatedAction` CanMerge `HasSameContent` check only; `MultiNetListener` 3 `Networked` guards only; `BeatmapObjectContainerCollection<T>` semantic ordering ported surgically (IContainerPoolFilter/IncludeAll, `RefreshPool<TFilter>` overload with filter conditions + index-aware recycle, `TryBinarySearch`→`TryBinarySearchForObjectIndexLogged`+private pair with bounds-reference-then-`HasSameContent`, SpawnObject `UpperBoundBy`+SortComparer insertion, ContainsObject new lookup, skip-null do/while dequeue, containerIsAlive registry-clear — all P01 deferred-pool/rebind/activepool machinery deliberately omitted); `EnumerableExtensions` `UpperBoundBy` comparer overload only; `EventGridContainer` 3-line surgical (using/override/rename); `GLSEventGridContainer` mechanical callsite rename (required by the settled API change, same rename main applied); NoteGridContainer/ObstacleGridContainer/BeatmapObjectCallbackController whole copies (verified zero GLS/light-ID diff content); `TestBase` minimal `CleanupTestObjects` hook only; `TestUtils` minimal load-helper extension (ReloadMap/LoadMapper signature params, env/songLength/difficultyInfo propagation, `ownedTestSong`+`ReplaceTestSong` verbatim helper per lead update for `MapReloadLifetimeTest`, unconditional `CaptureCurrentMapAsSharedBaseline` after 03 load — no SwapMapInPlace/perf logs/in-place APIs). **11 portable test pairs copied** (PointDefinitionBaseProviderTest, AdditionalAnimationParityTest, MaterialTrackAnimationTest, TrackScrubParityTest, EnvironmentEnhancementLoadOrderTest, FogAnimationTests, TubeBloomAnimationTests, EventOrderingAndIdentityTest, SameTimeEventOrderTest, SavePreservesAuthoredFileOrderTest, MapReloadLifetimeTest) + fixtures `WorldCavesInEnvironmentEssence.json` and `SpellsLaserWallFixture.json` (the latter a runtime-discovered missing dependency, exact copy). First cohort run hit one compile hole (dev's `GLSEventGridContainer` called the renamed `TryBinarySearch` — mechanical rename applied) and one missing fixture (above); **final cohort filter: 51/0/0 green** (`TestResults\cli\20261006-100157`). Forward diff regenerated (`4571cc78…`, 6294 lines); 108 rows (60 M + 48 ??), zero staging; no full suite until the cohesive scope is final. **Final production/test cohort 2026-10-06** (lead-settled, phase-3 frozen to `artifacts/P02-phase3-tracked-forward.diff` `4571cc78…` before overwrite): 4 whole remaining deltas applied (MovementEffect `SameTypeIndex=0` seed, TrackLaneRingsPositionEffect incremental fixed-pair+`SameTypeIndex`, TrackLaneRingsRotationEffect v2 counterSpin/forward-tick-skip/cached-advance, TimeHelper `GetShaderTimeValue`=`timeSinceLevelLoad` only — all byte-identical to main); 4 portable Salty test pairs + 4 Salty fixture pairs; the 2 experimental native-transform test pairs (ParametricBoxEnhancementTransformTest, EnvironmentPositionPrecedenceTest) **proved portable on the dev environment** — no P01 asset dependency surfaced, retained in scope. **Final cohort filter: 20/0/0 green** (`TestResults\cli\20261006-102332`, all PlayMode). Run-mutated env materials (BackColumns, Panic BottomCones1/Panels4TrackLaneRing/TopCones1, TrackConstruction) + AudioLink/ProjectSettings noise restored to HEAD. Forward diff regenerated (`07de38f6…`, 6563 lines); 132 rows (64 M + 68 ??), zero staging. **FULL default suite 2026-10-06: first run 1060/2** (`20261006-104917`) — two `EventTweakSelectionTest` failures root-caused to the settled `CompareTo→HasSameContent` identity migration being incomplete at callsites the scope list missed: `FinalizeBasicEventTweak`'s no-op guard used payload-`CompareTo` that no longer exists. Completed the mechanical migration at exactly the callsites main migrated (9 files: `BeatmapEventInputController`, `BeatmapBPMChange/NJSEvent/ObstacleInputController`, `RotationCommand` ×4, `BeatmapObjectModifiedAction` ×3, `BeatmapGLSEventBoxModifiedAction` CanMerge only — its `RebindModifiedGroupContainer`/`TryRebindLoadedGlsGroup` stays P01, `SelectionController` ×4 `.Where` filters, `GLSEventGridContainer` ×2 group guards, `BeatmapAssertion` sorted-check→`EventOrderComparer`); excluded CMMath/lerp/ribbon-refactor hunks in the same files. Failed class re-verified 14/0/0 (`20261006-105525`), then **full gate GREEN 1062/0/0** (`TestResults\cli\20261006-105554` = 884 PlayMode + 178 EditMode). Run noise restored (5 env materials, AudioLink mat/rt, ProjectSettings, `NotoSansCJKjp.asset` test-mutated atlas — restored to dev HEAD). **141 owned paths staged** (73 M + 68 ??: prior 132 + 9 migration files) under the user's stage-validated authorization; staged diff `Recovery/.../artifacts/P02-candidate-staged.diff` (`e75cf3c1…`, 268194 lines, 141 paths). **User then renamed the branch to `envEnhAndAnimAndRenderParity`, advanced the base to `32f89b586` (updated dev carrying PRs #672+#673 → P03-inherited `BaseCustomEvent.GetExportData`, `SavePreservesAuthoredFloatPrecision` case, `SimpleJSONExtension` `RoundBeat`), and committed the candidate themselves as `9702572e0` ("Chroma / Noodle animation, position, environment enhancement parity", 140 files — `SavePreservesAuthoredFileOrderTest.cs.meta` now HEAD-tracked so 141→140).** Pre-test state snapshot at `artifacts/P02-newbase-pretest/` (6 dirty-file raw bytes + status + unstaged diff — all user/editor-owned, none restored). **Updated-base full gate GREEN: 1112/0/0** (`TestResults\cli\20261006-124311` = 915 PlayMode + 197 EditMode — includes ~50 inherited-baseline cases). Post-run: zero new noise — all 6 dirty files byte-identical to pre-test snapshots, nothing restored. Scoped diff `artifacts/P02-candidate-staged-newbase.diff` (`04c86ed4…`, 268165 lines, 140 paths = `32f89b586..9702572e0`). | **USER-COMMITTED `9702572e0` on `envEnhAndAnimAndRenderParity` @ updated dev `32f89b586`; full gate green 1112/0/0; tree clean except user's own 6 dirty files** |
| P03 | `ChroMapper-pr-json-color-precision` | `losslessJsonPrecision` (user branch; created as `pr/json-color-precision`) | Preserve non-time JSON precision and fix color-picker precision | Transport prepared 2026-10-06: 62 production files + 4 test files (2 split member-level) in `Recovery/.../P03-transport/` with forward + main-removal-inverse patches and integrity/ownership manifests; lead patch review accepted; candidate worktree created 2026-10-05 at `C:\src\BeatSaberStuff\ChroMapper-pr-json-color-precision` (branch `pr/json-color-precision`, pinned dev `4e9c8b001`); deps provisioned from source cache; 8 test artifacts copied (2 whole tests + metas, 2 split candidate subsets + metas); test-only baseline confirmed expected red (13/36, `TestResults\cli\20261005-231834`); `review-forward.patch` (62 diffs) applied clean; post-fix filter green 49/0/0 (`20261005-232033`); **candidate full suite green 983/0/0** (`20261005-232108`); complete 70-path diff at `Recovery/.../artifacts/P03-candidate-complete.diff`; candidate diff accepted by lead; **source removal executed 2026-10-05 ~23:25** — preflight verified 70 scoped files vs frozen manifest + scoped index clean, `review-main-inverse.patch` applied (62 prod + 2 split-test member removals; `HasSameContent`/conditional-easing context preserved), 4 whole-owned test files deleted; **post-removal main full suite green 2974/0/0** (`TestResults\cli\20261005-232950`); earlier commit `96da7540` undone at user request via `git reset --soft 4e9c8b001`; user committed/pushed themselves — commit `c57cdf1561c150a809f9e2c18462f992add8e2ab` (tree `3df03e34…`), **authoritative branch `losslessJsonPrecision` tracking origin, clean** | Baseline expected-red (36 functional failures); post-fix targeted 49/0/0; candidate full suite 983/0/0; post-removal main suite 2974/0/0 — all green |
| P04 | `ChroMapper-pr-preview-editor-workflow` | `pr/preview-editor-workflow` | Fix preview/editor transitions, feedback, and initialization | Transport prepared 2026-10-06: 13 production files (8 whole-file + 4 surgical + scene YAML surgical) + 5 whole-copy tests in `Recovery/.../P04-transport/` with forward + main-removal-inverse patches and integrity/ownership manifests; offline roundtrip verified 13/13; lead patch review accepted (523+523 lines). Candidate worktree at `C:\src\BeatSaberStuff\ChroMapper-pr-preview-editor-workflow` (branch `pr/preview-editor-workflow`, pinned dev `4e9c8b001`); deps provisioned from cache; baseline targeted confirmed expected red (`NotePulseInterruptedByModeSwitchAnimatesAgainAfterGameplayReturns` — interrupted pulse left renderer frozen above baseline, `TestResults\cli\20261006-010347`); `review-forward.patch` applied (13 prod, +157/-31) + remaining 4 test pairs copied; 6-class targeted: 32/12 — all 12 `OptionsKeybindRebindTest` SetUp failures from a **pre-existing dangling-ref defect in dev's `04_Options.unity`** (addedObject fileIDs 2147000004/2147000014 lack stripped RectTransform decls; `dev..HEAD` empty for the scene; main's worktree already carries the fix). **Settled scope supplement**: added exactly the two existing stripped declarations (Better Toggle child `4143766582454679844` under Options Section instances 2147000001/2147000011) — `Recovery/.../P04-transport/scope-supplement/` (`extra-scene-forward.patch` `0bbacbb9…`, README); grew scope to **24 owned paths** (14 prod + 10 tests); `OptionsKeybindRebindTest` rerun 12/0/0 (`20261006-011151`) → **full suite green 978/0/0** (`20261006-011223`); noise archived `artifacts/P04-candidate-noise/` + analyzer dll restored from cache; **test-teardown stabilization applied** (shared `PreviewWorkflowTestBase` in `EditModePlayingCameraTest.cs` captures `Settings.Instance.Animations` in `[SetUp]` and restores Normal UIMode + editing camera + Animations in `[UnityTearDown]`; the 4 preview/Playing fixtures now derive from it — CameraCursorLockTest already compliant, unchanged; delta vs frozen copies `P04-transport/scope-supplement/teardown-stabilization.diff`); 4-fixture filter 28/0/0 (`20261006-012044`) → final full suite **978/0/0** (`20261006-012128`); **exactly 24 paths staged** (`artifacts/P04-candidate-staged.diff` `6e45dac5…`, 1887 lines); candidate diff accepted by lead (incl. the mandated teardown stabilization + 12-line scene repair); **main source removal attempted then exactly undone** — the inverse removed `ATSC.Instance` which retained P02 `BaseProviderManager.cs:108` (untracked) requires to compile, and 39 retained animation-parity tests failed (Salty*/Paradigm/Aurora/Censored/The15Sublimit PlayMode suite — `CameraController.SetLockState` NRE via dev `UIMode.OnPlayToggle`, Playing-entry workspace switch, die-banish/color parity; `TestResults\cli\20261006-012851`); exact undo executed (12 per-file inverse patches reversed ex-ATSC, ATSC kept at post-P08 `4b4f4745` residue with the shared `Instance` trio retained, 10 test files restored byte-exact; `04_Options` stripped decls retained for P01 scene health); post-undo main gate green **2967/0/0** (`20261006-014221`); **status: RESOLVED BY USER POLICY 2026-10-06 — main intentionally retains the workflow payload** ("leave it in the main big PR and we can deal with merge conflicts whichever merges second"); the identical verified 24-file payload is seeded into the P02 animation candidate instead (24/24 canonical blobs match this candidate's staged set); candidate remains independently ready (24 staged, 978/0/0) — historical, user may commit or discard | Baseline expected-red (interrupted pulse); 32+12 targeted → repair → 12/0/0; candidate full 978/0/0; removal attempt → compile dep + 39 retained-runtime failures → exact undo → main green 2967/0/0 |
| P05 | `ChroMapper-pr-grid-text-rendering` | `pr/grid-text-rendering` | Improve grid and world-space text anti-aliasing | Transport prepared + lead forward/inverse/verify review done 2026-10-06 (prepared offline; offline scope accepted, no functional green claim): `Recovery/.../P05-transport/` — 39 tracked sections + 8 untracked copies, 104 integrity-manifest rows; `GridCoverage.hlsl` (48), Teko scalar/reference (135), `Verify-P05Integrity` (177), existing `CustomEventLogoRenderingTest` (359) + `RegenerateTekoTool` (118) lines reviewed; P01 GLSIcon/ribbons excluded, P04 `SetGridVisible` excluded, generic `Sprites.mat` + full CustomEventLogoRender pixel coverage included; Teko binary payload is exact frozen regenerated source (not P01 anchor; dev/HEAD identical), native/Noto payload excluded; deletion-controller/meta paired scenes and locale IDs 43/105 only. Candidate worktree at `C:\src\BeatSaberStuff\ChroMapper-pr-grid-text-rendering` (branch `pr/grid-text-rendering`, pinned dev `4e9c8b001`); deps provisioned from cache; baseline targeted confirmed expected functional red (`TestResults\cli\20261006-015614`: `NodeTextEdgesRetainBlendedCoverage` blended coverage 0, `OpaqueLogoInteriorHasNoMissingPixels` 813–1662 discarded interior pixels per case, `XYGridOverlaysOnlyTheRearHalfOfNodeDecoration` rear-grid overlay failures — 4/11); `P05-production-files.patch` applied clean (39 sections: 37 M + 2 D `MeasureLinesRenderingOrderController.cs`+`.meta`, +217/−619) + exact 8 untracked companions copied (`CustomEventLogoRenderingTest`+`.meta`, `GridCoverage.hlsl`+`.meta`, `RegenerateTekoFontAtlas.cs`+`.meta`, `Teko 3D Outline BPM.mat`+`.meta`) — no new atlas generation; targeted rerun **15/0/0** (`20261006-015803`) with 39 PNG render artifacts under `TestResults\CustomEventLogoRenderingTest\`; **full default suite green 949/0/0** (`20261006-015901`, PlayMode 771 + EditMode 178); run noise archived `artifacts/P05-candidate-noise/` and restored, analyzer dll recopied from cache byte-exact (`0fee34d9…`); **exactly 47 paths staged** (`artifacts/P05-candidate-staged.diff` `f9ee78ea…`, 137235 lines — dominated by the frozen Teko asset payload); candidate diff accepted by lead (all 47 raw pins matched the frozen forward review incl. the 11 spaced filenames; staged Teko asset blob `0bc17474…` = exact frozen post-image; PNG renders inspected); **source removal executed 2026-10-06** — `Verify-P05Integrity.ps1` 104/104 rows PASSED (zero drift vs frozen source scope), `P05-main-inverse-owned-hunks.patch` applied clean (37 mods + restored the 2 `MeasureLinesRenderingOrderController` tracked files; canonical blobs `c0eea0e8`/`2accd245` match pinned HEAD), exact 8 transported files hash-verified vs frozen copies then deleted (GridCoverage/RegenerateTeko/BPM-mat pairs were untracked; the test pair left no index trace); pre-removal undo captured at `artifacts/P05-preremoval-worktree.diff` (`ada73cc4…`) + `P05-preremoval-hashes.txt`; retained residue verified — locale diffs are P01 GLS strings only, `04_Options` keeps the 2 shared stripped decls, `GridLane` keeps `SetGridVisible`+P01, `Settings` keeps the P01 shim, ATSC shared trio + `OnTimeFlushPending` untouched; GridCoverage consumers were only the 3 P05-owned shaders; **post-removal main gate green 2952/0/0** (`TestResults\cli\20261006-021133`, PlayMode 2448 + EditMode 504 = 2967 − 15 removed cases); user's `NotoSansCJKjp.asset` bytes preserved (`cbba0a10…`) | Baseline expected-red (4/11 functional rendering failures); post-fix targeted 15/0/0; candidate full suite 949/0/0; post-removal main suite 2952/0/0 — all green |
| P06 | `ChroMapper-pr-cjk-font-rendering` | `pr/cjk-font-rendering` | Fix CJK and fallback-symbol rendering in menus | Transport prepared + lead forward/inverse/verify review done 2026-10-06: `Recovery/.../P06-transport/` — 19 integrity-manifest rows (+ helper test fixture), `P06-production-files.patch` + `P06-main-inverse-owned-hunks.patch`, `Verify-P06Integrity.ps1`; source-field native-font `.otf` baseline available (no P05 dependency); P01 protected font payload retained. Candidate worktree at `C:\src\BeatSaberStuff\ChroMapper-pr-cjk-font-rendering` (branch `pr/cjk-font-rendering`, pinned dev `4e9c8b001`); deps provisioned from cache; baseline targeted confirmed expected red (`SongListViewportUsesRectMask2D` — `Expected: null, But was: <Viewport (UnityEngine.UI.Mask)>`, `TestResults\cli\20261006-022908`); `P06-production-files.patch` applied clean (4 mods, +223/−9) + 13 adds copied (`TMPFallbackMaterialHolder`+`EnableCjkMultiAtlas` pairs, `SongInfoCjkFontTest`/`SongListMissingGlyphTest`/`SongListCjkMaskTest` pairs, unicode `51eb1` fixture `Info.dat`+folder/file metas); **Noto scalar transported on the dev base only** — candidate font blob `c70fb44b`→`ad457385` = exact `m_IsMultiAtlasTexturesEnabled: 0→1` flip, main's 11.8MB user payload never copied; 3-class filtered run **15/0/0** (`20261006-023045`); **full default suite green 949/0/0** (`20261006-023115`, PlayMode 770 + EditMode 179); test-mutated atlas/glyph data restored to the post-patch baseline (`artifacts/P06-postpatch-baseline/`, font canonical back to `ad457385`), run noise archived `artifacts/P06-run-noise/` + analyzer dll recopied from cache; **exactly 17 paths staged** (4 M + 13 A, `artifacts/P06-candidate-staged.diff` `3675c339…`, 1268 lines — initially written to the candidate-local `artifacts/` by mistake; re-exported to the Recovery path byte-identical and lead-reviewed there); candidate diff accepted by lead; **source removal executed 2026-10-06** — `Verify-P06Integrity.ps1` 19/19 rows PASSED (main font raw `cbba0a10…` = user's frozen pre-existing bytes), `P06-main-inverse-owned-hunks.patch` applied clean (4 paths; font inverse = scalar-only `1→0`, post-image canonical `f239ce3f` = exact frozen main-minus-p06 — all retained P01 glyph/atlas payload untouched), pre-removal undo at `artifacts/P06-preremoval-worktree.diff` (`4f6a4bfd…`) + raw font backup `P06-preremoval-NotoSansCJKjp.asset`; consumer preflight proved `TMPFallbackMaterialHolder` used only by the owned `SongListItem`+test and `EnableCjkMultiAtlas` self-contained; all 13 deletion files hash-matched frozen copies before deletion, fixture dir held exactly `Info.dat`+meta (removed non-recursive); SongListItem/prefab/scene reverted fully to HEAD (wholly P06-owned); **post-removal main gate green 2937/0/0** (`TestResults\cli\20261006-021133`→`20261006-024320`, PlayMode 2434 + EditMode 503 = 2952 − 15 removed cases); post-run font bytes identical to the post-inverse baseline (`cf85e547…`, archived `P06-postinverse-NotoSansCJKjp.asset`) — zero test mutation | Baseline expected-red (stencil Mask not RectMask2D); post-fix filtered 15/0/0; candidate full suite 949/0/0; post-removal main suite 2937/0/0 — all green |
| P07 | `ChroMapper-pr-difficulty-set-ownership` | `fixDifficultyDuplicationInInfoDat` (user branch; created as `pr/difficulty-set-ownership`) | Prevent duplicate difficulty/characteristic headers | Candidate worktree created 2026-10-05 at `C:\src\BeatSaberStuff\ChroMapper-pr-difficulty-set-ownership` (branch `pr/difficulty-set-ownership`, pinned dev `4e9c8b001`); deps provisioned from source cache; test-only baseline run confirmed expected red (`Expected: 1, But was: 8`, `TestResults\cli\20261005-224721`); `P07-production-files.patch` applied to the 2 production files; post-fix targeted run green (1/0/0, `TestResults\cli\20261005-224929`); **full default suite green 935/0/0** (`TestResults\cli\20261005-225004`); candidate complete 4-file diff archived at `Recovery/.../artifacts/P07-candidate-complete.diff`; candidate diff accepted by lead; **source removal executed 2026-10-05 ~23:00** via gated `Remove-P07FromSource.ps1 -CandidateValidated` (drift + staged-scope + HEAD-blob preflights passed; 2 prod restored from pin, 2 test files deleted); **post-removal main full suite green 3023/0/0** (`TestResults\cli\20261005-230201`); commit-ready artifacts at `Recovery/.../artifacts/P07-commit/` (alt-index tree `0270183e…` on dev `4e9c8b001` + message + patch); **P07 complete** — user-published commit `20f42…` on authoritative branch `fixDifficultyDuplicationInInfoDat` (tracking origin, clean); an earlier extraction commit `fe49d1b0` (tree `0270183e…`, parent dev `4e9c8b001`, hooks honored) was superseded and remains historical only | Baseline expected-red (`Expected: 1, But was: 8`); post-fix targeted 1/0/0; candidate full suite 935/0/0; post-removal main suite 3023/0/0 — all green |
| P08 | `ChroMapper-pr-preview-runtime-resources` | `pr/preview-runtime-resources` | Reduce preview rendering/audio overhead and release resources | Transport prepared 2026-10-06: 11 source files (9 production + 2 tests) in `Recovery/.../P08-transport/` with forward/inverse patches + verifier; `MirrorRendererSO.OnDisable` unsubscribe, per-subscriber cleanup, Settings callback shim stay P01 (GLSGroupPagePoolingTest callback semantics); `MapReloadLifetime` cases pending P02 ownership; ATSC TrackLength/ClearSettings cleanup transports. Candidate worktree at `C:\src\BeatSaberStuff\ChroMapper-pr-preview-runtime-resources` (branch `pr/preview-runtime-resources`, pinned dev `4e9c8b001`); deps provisioned from cache; baseline targeted confirmed expected red (`MirrorRenderSkipsCanvasCallbacksAndRestoresThemAfterward` — mirror camera ran UI callbacks during reflection render, Expected 0/But was 1; `TestResults/cli/20261006-003307`); `P08-production-files.patch` applied (9 prod) → targeted green 1/0/0 (`20261006-003440`) → **full suite green 935/0/0** (`20261006-003507`); **exactly the 11 owned paths staged** (user-authorized candidate staging; staged diff `Recovery/.../artifacts/P08-candidate-staged.diff` `dc8becd2…`, 407 lines); runtime noise archived at `artifacts/P08-candidate-noise/` then restored to pre-test baseline; candidate diff accepted by lead; **source removal executed 2026-10-06 ~00:50** — re-verify 22-row integrity PASSED, scoped index clean; `P08-main-inverse-owned-hunks.patch` applied (9 prod; ATSC keeps `Instance`/`Awake`/`OnDestroy`/`OnTimeFlushPending`, MirrorRendererSO keeps `OnDisable` unsubscribe + Settings shim — both canonical blobs match the manifest's `main-minus-p08` residue); test pair deleted; undo recipe `Recovery/.../artifacts/P08-preremoval-worktree.diff` (`76343989…`, 377 lines) + `git apply -R` + `files/` backups; **post-removal main full suite green 2967/0/0** (`TestResults\cli\20261006-004755`) | Baseline expected-red (mirror UI-callback leak); post-fix targeted 1/0/0; candidate full suite 935/0/0; post-removal main suite 2967/0/0 — all green |
| P09 | `ChroMapper-pr-platform-compatibility` | `osSpecificFixes` (user branch; created as `pr/platform-compatibility`) | Fix cross-platform paths, file dialogs, and compute imports | Candidate worktree at `C:\src\BeatSaberStuff\ChroMapper-pr-platform-compatibility` (branch `pr/platform-compatibility`, pinned dev `4e9c8b001`); deps provisioned from cache; baseline targeted confirmed expected red (`TextureChannelPackerCompiles(OpenGLCore)` sampler failure, 10/1, `TestResults\cli\20261005-234722`); `P09-production-files.patch` applied → targeted green 11/0/0 (`20261005-234857`) → **full suite green 940/0/0** (`20261005-234934`); **exactly the 9 owned paths staged** (user-authorized candidate staging; staged diff `Recovery/.../artifacts/P09-candidate-staged.diff`); candidate diff accepted by lead; user committed the staged 9 paths as `ac13fe3d5` (published on `osSpecificFixes`), then a lead `commit --amend --no-edit` — the only explicitly authorized exception — applied the comment edit removing the stale `PathUtilsTest` reference (staged diff `Recovery/.../artifacts/P09-amend-pathutils-comment.diff` `12a26dde…`); current local HEAD `ee834cfb412bf8d6496f06f9c14b81a5bbf9efac`, clean, ahead 1 / behind 1 vs origin, not yet pushed; the PathUtilsTest cases were already in dev `4e9c8b001` via `a50c2ec` (48-line file incl. `CombineRootedChildDiscardsEarlierSegments`, meta `c8de4547…`); `TestsEditMode.PathUtilsTest` 5/0/0 green (run dir `20261006-004446`); no test added or changed; **source removal executed 2026-10-05 ~23:55** via gated `Remove-P09FromSource.ps1 -CandidateValidated` (drift + staged-scope + HEAD-blob preflights passed; 7 prod restored from pin, 2 test files deleted); **post-removal main full suite green 2968/0/0** (`TestResults\cli\20261006-001824`); one transient `SongInfoCjkFontTest` failure on the first post-removal run was traced to an accidental worktree restore of the user's `NotoSansCJKjp.asset` — user's bytes restored, isolated rerun 8/0/0 green, authoritative full run green | Baseline expected-red (OpenGLCore sampler); post-fix targeted 11/0/0; candidate full suite 940/0/0; post-removal main suite 2968/0/0 — all green |

## Original patch-note ownership

This table preserves the order and feature content of the user's original list. Repeated ribbon/visibility notes are intentionally retained. P02 owns the independently extractable animation/transform behavior; P01 owns all light-ID/import behavior and any integration that requires it.

| Note | Original patch note | PR/worktree owner | Boundary or reason |
| --- | --- | --- | --- |
| N01 | BeatToTheFuture adding TrueHSV support to V2 Chroma transition events, support in CM. | P01 — main | Canonical classification, requirement integration, and shared interpolation/ribbon follow-ups build on the protected commits. This plan covers CM, not changes to the separate BeatToTheFuture repository. |
| N02 | ChromaGLS support for light color distributions: Hue, Saturation, Color intensity/value including HDR, and RGB Brightness/A. | P01 — main | One connected GLS data/UI/evaluation/ribbon feature. |
| N03 | Support distributions both per event and at the event-box level. | P01 — main | Keep both scopes, normal/strobe inheritance, serialization, and copy/paste together. |
| N04 | Fix anti-aliasing issues for the grid and all node text / grid text. | P05 — grid/text; P01 for protected GLS icon/ribbon rendering | General grid/TMP/Teko changes can separate. GLS icon shaders/textures and ribbon rendering built on the anchors cannot. |
| N05 | GLS transition ribbons accurately preview the color per light over time, with an off setting in graphics. | P01 — main | Timeline, per-light strips, distributions, easings, visibility, and picking are interconnected. |
| N06 | Add all Easings.net easings for everything GLS — Ctrl+Shift+Scroll hover. | P01 — main | Includes Color, Rotation, Translation, and FloatFX, not a subset. |
| N07 | GLS light-color support for easings other than linear — Ctrl+Shift+Scroll hover. | P01 — main | Part of extended color evaluation and editing. |
| N08 | Replace GLS strobe's default InOutCircle easing with any other easing — Shift+Scroll hover. | P01 — main | Independent pulse easing still shares the GLS tween/ribbon path. |
| N09 | GLS strobe color can use a different color easing than the main color — Alt+Shift+Scroll hover. | P01 — main | Keep separate color clocks, input, serialization, and preview parity together. |
| N10 | GLS lerp type can be HSV — Ctrl+Middle Click, applying to color, strobe-color, and separate-color strobe-fade easing. | P01 — main | Shared GLS interpolation and protected color helpers. |
| N11 | GLS easing icons on event-node faces and additional hover information to declutter color nodes. | P01 — main | Direct continuation of the icon anchors. |
| N12 | Respect loop count and easing on rotation-extension nodes like the game, and show when they have values. | P01 — main | GLS extension semantics, controls, icons, and playback stay together. |
| N13 | Stop adding easing/loop count to rotation-extension nodes by default; scroll values into existence. | P01 — main | Keep placement defaults and optional serialization with N12. |
| N14 | Flag GLS event-box lanes red when earlier lanes capture all matching lights/objects. | P01 — main | Filter ownership, light counts, and lane presentation are part of main integration. |
| N15 | Fix editor scale for accurate note jump distance back to stable. | P04 — workflow/initialization | Standalone editor-scale correction; do not pull animated jump/track work along with it. |
| N16 | Time precision decimal places affect only time, not every JSON number; round colors in the picker, not JSON. | P03 — precision | Explicit time writers plus lossless generic numbers/colors and picker input semantics. |
| N17 | Ribbons use the same color space as lights and accurately emulate individual lights in strips. | P01 — main | Protected ribbon shader/timeline plus physical light/import integration. |
| N18 | Setting to turn off GLS ribbons. | P01 — main | Same visibility setting as N05, retained explicitly in this list. |
| N19 | Setting to turn off GLS preview ghost nodes. | P01 — main | GLS preview pooling, visibility, and settings wiring. |
| N20 | Song-boundary test matrix and fixes for shift, paste, Ctrl+Arrow, Alt-drag, etc. | P01 — main | The helpers, operations, and test bases already originate in `7f791739b`. |
| N21 | Chinese, Japanese, Korean, and hopefully more glyphs render in menus, possibly bookmarks. | P06 — CJK | Menu/song metadata/source-font rendering and fallback lifetime. Bookmark/additional-script claims remain to be verified. |
| N22 | Fix The Second Environment's ring order after Ring Zoom. | P01 — main | Runtime reordering and its regression are already in `7f791739b`. |
| N23 | Minor bugs such as node flickering under hover-scroll bindings. | P01 — main for GLS flickering | GLS rebind/pooling/raycast fixes stay main. Classify any unrelated minor fix by behavior, not as an automatic miscellaneous extraction. |
| N24 | Fix camera softlock when switching to GLS mode from Playing. | P04 — workflow | Workspace/mode transition and cursor ownership, not animated camera-track evaluation. |
| N25 | Escape during Playing/Preview returns to the previous edit mode instead of opening the glitchy pause menu. | P04 — workflow | Keep previous-mode bookkeeping, pause routing, and scene references together. |
| N26 | Remove hover outline/tweaking while playing in Playing/Preview; no white noodle notes/hovered walls blocking the preview. | P04 — workflow; P01 for GLS-specific consumers | Extract the shared playback suppression gate. Keep ribbon-specific input changes and protected GLS hover consumers main. |
| N27 | Performance fixes for Basic Event Ring Rotation. | P02 — animation/environment, if independent | Basic ring evaluator performance is a candidate on the dev baseline. Any import/order/identity-dependent portion or test stays P01. |
| N28 | Disable AudioLink CPU-side readback to save per-frame time. | P08 — runtime resources | `SpectrogramDataProvider` readback change; do not sweep incidental AudioLink material state into it. |
| N29 | Prevent UI rendering in mirror CPU calculations. | P08 — runtime resources | Scope canvas suppression before nested rendering and restore it afterward. |
| N30 | Make Chroma+ maps work correctly, including environment enhancements. | P02 — independent animation/enhancement behavior; P01 — import/light-ID integration | Do not claim P02 alone fixes every map. Identity-correct imported objects, generated-light registration, and asset-dependent rendering remain main. |
| N31 | Note-color animation, noodle-wall animation, note-jump tracks, material keywords, geometry, fog, and other time-lerped/rewindable/fast-forwardable Chroma behavior. | P02 — animation/environment; P01 for light-ID/import-dependent geometry/light output | Keep the connected animation lifecycle together. Distinguish transform/material animation from which physical/generated light an event targets. |
| N32 | Large performance improvements for ring-heavy environments and cloned-ring behavior fixes. | P02 — independent evaluator/clone-lifecycle fixes; P01 — cloned-ring identity/import corrections | Do not move native ChromaID rewriting, imported ring indexes, or ID-dependent fixtures with the generic performance refactor. |
| N33 | Basic Event/GLS tab switching while playing no longer permanently stops XY-grid note pulses. | P04 — workflow | Reset interrupted `VisualFeedback` state; its test uses ordinary notes and deterministic playback. |
| N34 | Fix duplicate difficulty headers after specific operation sequences. | P07 — difficulty metadata | Parent-set ownership, empty-set handling, and phantom characteristic sets. |
| N35 | Fix glitchy lines next to the color-type picker at specific UI scales, caused by shadows. | P01 — main | GLS picker/mapper-scene UI changes stay with the protected GLS scene work. |
| N36 | Fix HJD line not appearing until the first timeline scroll. | P04 — workflow/initialization | Publish HJD on load; retain rendering integration checks against P05. |

## P01 — main GLS and environment/light-ID integration

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-gls-color-shifts` (existing).

**Why this stays together:** the protected GLS data, input, icon, tween, timeline, ribbon, filter, and serialized scene changes are already connected. Physical-light identity and import ordering must remain consistent with both live-light preview and per-light ribbons. The user's light-ID ownership correction overrides the initial suggestion to include registration/remapping in P02.

### Contains

- All three protected commits and their dependent follow-ups.
- All GLS distribution scopes, channels, strobe inheritance, editing, copy/paste, and serialization.
- All GLS easing/lerp/strobe/rotation-extension behavior and controls across all four node types.
- GLS node icons, hover details, visibility settings, ribbons, color spaces, physical-light strips, picking, pooling, rebinds, and lane ownership warnings.
- TrueHSV CM integration and protected requirement/color-helper changes.
- Song-boundary helpers, operation fixes, test matrix, and dependent test infrastructure.
- The Second's anchored ring ordering and its test.
- GLS picker shadow fix and protected GLS shader/atlas/prefab/scene/tool changes.
- Bulk regenerated environment scenes/data and their matching material/mesh/texture lookup assets and references.
- Changed environment light-ID tables and imported object/ring identities.
- Runtime light registration, requested-key allocation, lightID-to-controller remapping, unregister/rebuild behavior, and registration performance work.
- Light-ID-specific changes in `BasicLightEffect`, `BasicEventEffectManager`, `EnvironmentDescriptor`, `GeometryContainer`, and dependent callers.
- ChromaID rewrite changes for cloned objects/rings. Keep these separate from independently extractable transform/lifetime fixes in the same method.
- Light-controller adapter/calibration or generated-light output changes that require the refreshed imports, with their dependent coverage.
- Fix changing BPM not recalculating cached GLS light preview timings and getting lights all out of sync with the map till reload.

### Tests and fixtures

Keep all protected GLS/ribbon/icon/input/serialization/filter/requirement/boundary coverage, including their companion bases and `.meta` files.

Keep generated-light, exact-ID, remapping, imported-calibration, and corrected-environment-identity cases here. Examples requiring case-level review include `KamikaziLightArrayTest`, `SpellsLaserWallTest`, `InfluxEnvironmentParityTest`, `WorldCavesInEnvironmentTest`, `AuroraMapParityTest`, `BasicLightAdapterAlphaParityTest`, and Censored light-color/reset coverage. A fixture-based test is not automatically independent merely because its map JSON is portable.

Keep cross-PR integration cases here when they exercise both extracted animation/workflow features and protected main behavior. Do not duplicate or remove their coverage as a shortcut.

### Validation

Run the complete default suite after approved extractions are reconciled. Verify live preview and rendered per-light ribbon parity against the corrected imported environments. Check all GLS node types, both distribution scopes, normal/strobe colors, all easing channels, off settings, and backwards/forwards seeking.

## P02 — independent animation and environment-enhancement parity  -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-animation-environment`.

**Why this is one PR initially:** parsing, event chronology, point definitions, tracks, enhancement targets, camera/note/wall lifetimes, and seek evaluation form one load/evaluation/reset lifecycle. Splitting notes, walls, fog, materials, and parenting prematurely would create a stack of prerequisites rather than independently mergeable PRs.

### Contains

- Point-definition parsing/modifiers, repeats, log-and-skip handling for malformed point data and missing named point definitions, and Heck-specific named easing evaluation.
- Deterministic animation sampling, retained values, stopped seeks, rewind/fast-forward, transition/parent semantics, and reset behavior.
- Note color/look/jump/path animation, Noodle wall transforms and animated lifetimes, and correctly timed note spawning.
- Animated player-camera track binding/detachment, home-pose restoration, and synchronous seek behavior.
- Material tracks, explicit shader-keyword semantics, geometry material/transform animation, double-sided transparent planar geometry, and geometry target lifetime cleanup.
- Fog/tube-bloom animation, component target ownership, late fog parameter publication, and propagation of animated light multipliers to initialized physical lights.
- Enhancement instruction ordering, independent position/rotation/scale precedence, preservation of authored parametric-box transforms during light refreshes, clone scene ownership, and map reload cleanup.
- Authored callback/save ordering and content-versus-local-identity separation for lookup, edit detection, and action merging, including matching network copies without local file ordinals and preventing received actions from being sent back to peers.
- Basic movement, ring-zoom, and paired-laser same-type event indexing, V2-only ring counter-spin handling, track-position-aware laser rotation snapshots, and incremental ring rotation/zoom playback evaluation.
- Preview/editor workflow fixes: gameplay workspace and camera activation while Playing, Escape restoration of the previous mode/workspace, camera cursor-lock ownership, GLS grid visibility, playback hover suppression, keybind chord rebinding, and visual-feedback reset on mode switches.

### Explicitly excluded

- Light-ID tables, corrected native import order, bulk regenerated environments/materials, requested light-key allocation, registration/remapping/rebuild behavior, and generated/cloned light identities.
- ChromaID rewriting in enhancement clone loops.
- The Second's protected ring-slot change.
- Protected GLS helpers, evaluators, ribbon shaders, and schemas.

Preserve the dev registration path in this extraction. Do not copy new `Register(controller, requestedKey)` calls or invent fallback behavior to disguise the absence of main-PR registration changes. If the intended enhancement fix genuinely requires that API, keep that specific fix in P01.

### Tests and fixtures

Candidate coverage: `AnimatePropertyScrubTest`, `AdditionalAnimationParityTest`, `PointDefinitionParityTest`, `PointDefinitionBaseProviderTest`, `TrackScrubParityTest`, `MaterialTrackAnimationTest`, `FogAnimationTests`, `TubeBloomAnimationTests`, `EnvironmentEnhancementLoadOrderTest`, `EnvironmentPositionPrecedenceTest`, `ParametricBoxEnhancementTransformTest`, and independent note/wall/camera fixture cases.

Candidate ordering coverage: `EventOrderingAndIdentityTest`, `SameTimeEventOrderTest`, and ordering cases from `SavePreservesAuthoredFileOrderTest`.

Classify `VaguenessJourneyEnvironmentScaleTest`, `RingRotationWaveParityTest`, full-map tests, and mixed map-parity classes by case. Exact native object/ring IDs, corrected light tables, refreshed materials, or Playing-from-another-workspace assertions may require P01 or P04. Such integration cases stay P01 unless a genuinely independent production path is demonstrated without changing what the test proves.

The generic environment/BPM/song-length test-loader parameters can travel with this PR where needed. Its in-place reload optimization calls animation/environment cleanup APIs, so the whole current `TestUtils` must not be transplanted into unrelated extracted PRs.

### Validation

Tests must pass using dev's existing imported assets, ordinary registration behavior, and no protected GLS additions. Verify runtime output/transform/material state across forward playback, paused seeks, rewinds, replays, difficulty changes, and reloads. Use deterministic clocks rather than requiring native audio advancement. Use profiler evidence for performance claims and relevant existing ring correctness coverage.

## P03 — JSON/time precision and color-picker precision -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-json-color-precision`.

**Why independent:** numeric serialization policy and picker quantization can be fixed without new GLS schemas, animated tracks, or corrected environment IDs.

### Contains

- Lossless generic JSON number formatting and float color-channel writing.
- Explicit beat/time/duration rounding at V2/V3/V4 export call sites.
- Custom-event export duration rounding without mutating the editable data tree.
- Picker slider quantization while preserving imported and typed channel precision.
- Full-precision RGB/alpha text and standalone hue-degree display/picker label correction.

Representative files: `Assets/Plugins/SimpleJSON*.cs`, V2/V3/V4 writers, `BaseCustomEvent` export-data hunks, HSV picker files, and `Assets/_Prefabs/UI/Picker 2.0.prefab`.

### Tests

- `BeatOnlyJsonPrecisionTest`.
- `ColorPickerPrecisionTest`.
- Standalone picker cases from `HueDegreesDisplayTest`.
- Authored-float-precision coverage from `SavePreservesAuthoredFileOrderTest`.

Keep distribution-row hue tests in P01. Keep ordering hunks in P02 and protected GLS easing/extension serialization in P01 even when the same writer contains rounding changes.

### Validation

Round-trip V2/V3/V4 time fields and non-time data, including HDR/tiny color channels, fog/transform values, arbitrary custom data, normalized animation times, and mapping-session minutes. Verify slider input differs intentionally from loaded/text input, and saving does not alter live authored values.

## P04 — preview/editor workflow, feedback, and initialization  -- IN THEORY THIS IS PR'd WITH P02? BUT IDK FULLY, TEST AT END AFTER ALL MERGE, SEE IF ANY DIFF LEFT

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-preview-editor-workflow`.

**Why independent:** editor mode/cursor/hover transitions and ordinary grid initialization do not inherently require GLS distribution/ribbon or Noodle evaluation changes.

### Contains

- Playing/Preview workspace transition guards and prior-mode restoration on Escape.
- Pause routing and matching mapper-scene references.
- Cursor-lock ownership and native-cursor test seam.
- Shared unpaused-preview hover suppression and grid visibility changes.
- XY pulse reset after tab deactivation during playback.
- Accurate editor NJS scale and initial HJD publication on map load.
- Independent placement-default initialization fixes such as arc/NJS activation-time defaults.
- The newly staged general options keybind-rebinding fix and its `OptionsKeybindRebindTest`, if its chord behavior proves independent. Keep new GLS action/binding declarations in P01.

Representative files: `EditModeContext`, `UIMode`, `PauseManager`, cursor-only `CameraController` hunks, playback-only `BeatmapInputController` hunks, `VisualFeedback`, `EditorScaleController`, `GridRenderingController`, and visibility-only `GridLane`/`GridViewController` hunks.

### Tests

- `EditModePlayingCameraTest`, including `CameraCursorLockTest` in that file.
- `UIModePreviewEscapeTest`.
- `PreviewPlaybackHoverTest`.
- `VisualFeedbackEditModePulseTest`.
- Relevant placement activation and options keybind cases.

Do not move animated player-track camera changes from P02 or protected GLS hover/ribbon interaction changes from P01. Do not copy entire mixed placement/input files.

### Validation

Test transition/escape behavior from Gameplay, Basic Events, and GLS; Playing/Preview swaps; paused/unpaused hover behavior; repeated cursor requests; and tab switching during ordinary playback. Dedicated staged proof of editor-scale and initial-HJD behavior was not established in the initial review, so include explicit verification of both before claiming completion.

## P05 — grid and world-space text rendering  -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-grid-text-rendering`.

**Why independent:** grid coverage and ordinary world-space text blending/atlas quality have a separate rendering surface from protected GLS ribbon/icon functionality.

### Contains

- `GridCoverage.hlsl` and its `.meta`, plus XY/XYExpand/XZ coverage/blending changes.
- Grid mesh overdraw and matching shader insets in `GridLane`/`GridPlane`.
- TMP SDF blend/depth/render-order changes.
- Teko atlas regeneration tool, atlas, matching materials, and ordinary node/grid-text prefab changes.
- Removal of the obsolete measure-line rendering-order option/controller with its independent scene/settings/localization references.
- Camera-AA default change only if confirmed as the intended rendering behavior rather than incidental state.

### Excluded

Protected `GLSIconSprite` changes, GLS icon textures/atlas generation, and `BasicGradient` ribbon behavior remain P01. CJK source-font/fallback lifetime and song-list masking are P06. Preserve protected font glyph/atlas additions when dividing assets.

### Validation

Inspect imported assets and rendered pixels, not source-code shape. Check distances, viewing angles, UI scales, AA settings, render scales, grid edges, HJD/sub-beat visibility, text outlines, bloom-mask clearing, and transparency ordering. Existing relevant correctness/render coverage should run after extraction. No passing shader compile alone establishes visual correctness.

## P06 — CJK and fallback-symbol rendering  -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-cjk-font-rendering`.

**Why independent:** song metadata rendering, fallback material lifetime, and menu masking can be corrected independently of GLS and animation features.

### Contains

- Independent CJK multi-atlas configuration and `EnableCjkMultiAtlas` tool.
- Source-font song-list title/artist/folder rendering, missing Unicode substitution, and safe recycled-row behavior.
- `TMPFallbackMaterialHolder` and matching source-font prefab references.
- Song-list viewport rectangular clipping in `01_SongSelectMenu`.
- Matching depersonalized CJK metadata fixture and `.meta` files.

### Tests

- `SongInfoCjkFontTest`.
- `SongListMissingGlyphTest`.
- `SongListCjkMaskTest`.

### Protected overlap

`NotoSansCJKjp.asset` was modified by `7f791739b`. Do not transplant the complete current font asset and thereby extract protected baked glyph/atlas payloads. Apply independently required configuration/resource changes against dev's own font asset, preserving the inherited anchor changes in P01. If a necessary P06 fix cannot be represented independently, retain that portion in P01 and document the reduced P06 scope.

### Validation

Prove glyph resolution, visible pixels inside the actual menu mask, fallback lifetime across asset cleanup, recycled-row behavior, missing scalar substitution, and metadata preservation. Verify claimed script coverage. Bookmark rendering and additional languages remain unverified claims, not guaranteed scope.

## P07 — difficulty-set ownership  -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-difficulty-set-ownership`.

**Why independent:** this is a focused song-menu metadata ownership bug with a direct reproduction and no GLS/light-ID dependency.

### Contains

- `DifficultySelect` saves/removes difficulties through their actual parent set.
- Consistent ownership of selected characteristic sets, including empty sets.
- `CharacteristicCustomPropertyItem` avoids phantom sets for untouched metadata.
- `DifficultySetDuplicationTest` and companion `.meta`.

### Validation

Run the original operation-sequence reproduction, existing difficulty/song-info save tests, deletion/recreation cases, and serialized metadata checks. Keep JSON precision changes in P03; do not export the whole V2/V4 info writer merely because this feature also saves metadata.

## P08 — preview runtime overhead and resource lifetime  -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-preview-runtime-resources`.

**Why grouped:** these are relatively small preview rendering/audio cost and resource-lifetime fixes without a GLS schema or environment identity dependency.

### Contains

- Disable AudioLink CPU readback in `SpectrogramDataProvider`.
- Mirror canvas callback suppression before nested `Camera.Render`, with guaranteed restoration afterward.
- `AudioManager` FFT/native-buffer cleanup on cancellation and destruction.
- PCM sample release in `SampleBufferManager`.
- Relevant audio/settings subscription cleanup and narrowly scoped callback-detachment fixes.
- Batchmode spectrogram allocation avoidance in waveform/audio preview paths.

### Tests and evidence

- `MirrorCameraUiSuppressionTest`.
- Relevant existing audio/load/cancellation correctness coverage.
- Profiler and lifetime evidence for the performance/resource claims.

`MapReloadLifetimeTest` mixes animation geometry cleanup and test-owned audio-clip release. Geometry/track target cleanup belongs P02. The test-helper audio case follows the required helper ownership, not automatically P08. Divide cases only when their APIs and semantics remain independently testable.

Do not include incidental AudioLink material runtime values. That material currently has unstaged changes. Keep registration/mapping performance in P01 and basic ring evaluator performance in P02.

## P09 — platform compatibility   -- PR'd

**Directory:** `C:\src\BeatSaberStuff\ChroMapper-pr-platform-compatibility`.

**Why independent:** host-independent path semantics, native dialog selection, and compute import compatibility are infrastructure corrections with their own production/test seams.

### Contains

- Host-independent Windows/Unix/UNC rooted-child path combining in `PathUtils`.
- Native dialog wrapper selection by editor host, not selected player build target.
- OpenGL-compatible explicit sampler binding in `TextureProcessor3DWriteTextures.compute`.
- Relevant package reference/import configuration and `PlatformDependencyImportTest`.

### Validation

Run existing `PathUtilsTest`, the platform import tests, and supported host/target combinations. Verify actual Mono/drawing dependency resolution using the project's established package provisioning, plus OpenGLCore and Direct3D shader compilation/output where supported. Windows-only success is not Linux validation. Do not weaken repository package security settings to make imports pass.

## Shared-file and test ownership map

Ownership here means individual behavior/API hunks. This is not permission to copy complete files from the main worktree.

| Shared file/area | P01 retains | Extracted ownership |
| --- | --- | --- |
| `BasicLightEffect` | Requested light keys, registration IDs, remapping, unregister/rebuild, registration performance, import-dependent calibration | P02 may take independent fixed-duration/intensity/Heck-easing behavior after proving it works without P01 |
| `BasicEventEffectManager` / `EnvironmentDescriptor` | New requested-key registration overloads and light-ID integration | P02 may take independent disabled-ring/animation lifecycle behavior |
| `GeometryContainer` | Requested-key calls, light-controller ID reassignment/registration, native ChromaID rewriting | P02 may take independent material animator, transform/fog target, clone scene ownership, and cleanup behavior |
| Environment scenes/data/tables/material/mesh/texture lookup assets | Entire refreshed import/identity/reference set | No bulk asset extraction to P02 |
| `CameraController` | Any protected integration-only cases | P04 cursor ownership; P02 animated player-track binding/home-pose lifecycle |
| `AudioTimeSyncController` | Protected consumers | P04 playback-hover singleton; P02 pending animation flush; P08 subscription cleanup |
| `BeatmapObjectContainerCollection` | GLS deferred pooling, group rebind, protected preview filtering | P02 event ordering/content lookup and independently required geometry cleanup; P08 independent subscription cleanup |
| V2/V3/V4 writers and `BaseCustomEvent` | Protected GLS schemas/easing/extension behavior | P03 rounding/export-copy behavior; P02 callback/save ordering |
| `BaseDifficulty` | GLS-only BPM exposure if required by protected consumers | P02 file ordinals/ordering; do not move unrelated fields wholesale |
| `Easings` | Protected GLS easing math, labels, shader dispatch, and cleanup of those changes | P02 independent Heck-specific named curve table/functions, using dev-compatible existing APIs |
| `GridLane` / `GridRenderingController` | Protected GLS integrations | P05 AA overdraw/insets; P04 visibility/HJD initialization |
| `BeatmapInputController` and placement bases | Ribbon picking/blocking, GLS hover mutation, protected boundary behavior | P04 narrow unpaused-preview suppression and independent initialization |
| `Settings`, locales, options/mapper scenes | GLS settings/rows/fields/assets and protected references | Assign each independent setting/entry/component/reference to P04–P08 as appropriate |
| Font assets | Protected baked glyph/atlas changes | P05 independent Teko quality/material changes; P06 independent CJK configuration/resources |
| `HueDegreesDisplayTest` | Distribution-row cases | P03 standalone picker cases |
| `SavePreservesAuthoredFileOrderTest` | Cases requiring protected behavior | P02 authored chronology; P03 numeric precision |
| Shared test infrastructure | Protected boundary optimizations and main-specific consumers | P02 owns animation-dependent in-place reload support; other PRs use baseline-compatible helpers and only the capabilities they need |
| Full-map parity tests | Import/ID-dependent or genuinely cross-PR integration cases | Independent animation/workflow cases only after isolation; classify by case, not filename |

### Test infrastructure constraints

- Do not copy the entire current `TestUtils`/`TestBase` into every extraction. The optimized reload path calls new animation/environment cleanup APIs.
- Keep high-cardinality boundary test optimizations with P01 when they depend on the protected boundary infrastructure.
- Do not drop tests, reduce assertions, add speculative production fallback guards, or pin source spelling to achieve a green branch.
- Input tests use isolated `InputTestFixture` devices rather than host devices.
- Playback tests use deterministic time while preserving the real playback state/event lifecycle. Native audio advancement cannot be required for Linux Jenkins.
- Restore preview/UI/camera/animation settings in teardown even after failed assertions.
- Preserve all custom-data sections referenced by retained fixture events, including point definitions/materials/environment.
- Performance-only extraction does not call for new behavior-shape tests. Use profiler evidence and run relevant existing correctness coverage.

## Extraction sequence and mergeability gates

The history-consolidation phase above has been executed under the user's rewrite approval. Per-category commands: **P07 executed end-to-end** (user-published commit `20f42…` on `fixDifficultyDuplicationInInfoDat`, source removal done, post-removal main suite 3023/0/0 green). **P03 executed and user-published** (candidate green 983/0/0, diff accepted, source removal done, main suite 2974/0/0 green; `96da7540` undone via `git reset --soft` under explicit user approval; user committed/pushed as authoritative branch `losslessJsonPrecision`). **P09 executed** (baseline red `TextureChannelPackerCompiles(OpenGLCore)` 10/1 → patched 11/0/0 → candidate full 940/0/0 green; 9 owned paths staged and diff accepted; source removal applied to main — **post-removal main gate green 2968/0/0**, `TestResults\cli\20261006-001824`). **P08 executed** (candidate green 935/0/0, 11 owned paths staged — user owns `pr/preview-runtime-resources`; source removal applied to main — post-removal main gate green 2967/0/0, `TestResults\cli\20261006-004755`). **P04 candidate executed; main removal resolved by user overlap policy** (candidate green 978/0/0, 24 owned paths staged — user owns `pr/preview-editor-workflow`; removal attempted, hit retained-P02 dependencies, exactly undone — main green 2967/0/0 restored, `TestResults\cli\20261006-014221`; user settled: workflow changes ship inside the single cohesive P02 animation PR AND remain in main for whichever merges second — the same 24-file verified payload is now seeded in `pr/animation-environment-parity`). **P05 executed end-to-end** (baseline expected-red 4/11 → post-fix targeted 15/0/0 → candidate full suite green 949/0/0, `TestResults\cli\20261006-015901`; 47 owned paths staged and diff accepted — user owns `pr/grid-text-rendering`; source removal applied to main — post-removal main gate green **2952/0/0**, `TestResults\cli\20261006-021133`). **P06 executed end-to-end** (baseline expected-red `SongListViewportUsesRectMask2D` → post-fix filtered 15/0/0 → candidate full suite green 949/0/0, `TestResults\cli\20261006-023115`; 17 owned paths staged and diff accepted — user owns `pr/cjk-font-rendering`; source removal applied to main, scalar-only font inverse preserving the retained P01 glyph payload — post-removal main gate green **2937/0/0**, `TestResults\cli\20261006-024320`). Other categories remain un-executed. Transport bundles exist for P07 (consumed), P09 (consumed), P03 (consumed), P08 (consumed), P04 (consumed), P06 (consumed), and P05 (consumed) as mechanical preparation only.

### Phase 1 — preserve and classify

- [x] Obtain approval for the documented boundaries and proposed names. (Granted 2026-10-05: history rewrite, task-scoped index sync, and sequential extraction/removal of validated extracted changes are all approved — no further per-PR user approval is required. Per-group technical review of each extraction's scope remains mandatory before it executes.)
- [x] Refresh HEAD/base/index inventory and note unstaged/untracked work separately. (Full snapshot + SHA-256/git-blob manifests captured 2026-10-05 under `Recovery/ChroMapper-gls-color-shifts-20261006T025404Z\`; post-sync status is 418 modified / 291 untracked / 7 deleted, all non-selected work.)
- [ ] Build a file/hunk/test/fixture/asset manifest assigning every staged change an owner. (Done only for the consolidated 736-path environment bundle — `trees/selected-paths.txt`; the remaining modified/untracked inventory is not yet classified. Deferred to a future fine-grained PR audit — not a blocker to the validated extractions or gates.)
- [x] Mark protected anchor dependencies and all light-ID/import changes P01. (Ownership rules above; amended-history protected commits recorded in rule 2.)
- [ ] Identify required `.meta` assets and scene/prefab GUID references per PR. (Done for the environment bundle — `manifests/guid-map.psv`; other PRs pending. Deferred to a future fine-grained PR audit — not a blocker to the validated extractions or gates.)
- [ ] Classify mixed/full-map tests, diagnostic helpers, fixture generators, and harness changes without deleting any of them. (Deferred to a future fine-grained PR audit — not a blocker to the validated extractions or gates.)

Do not snapshot by committing or staging the source worktree. Do not reset or remove source changes to create the appearance of a split. User-managed index state remains untouched; the completed consolidation used filesystem snapshots plus an alternate index, not staging.

### Phase 2 — create isolated candidate worktrees

- [x] Create P02–P09 from the recorded dev baseline with the proposed branches, not from main HEAD. (All eight created; user-renamed/user-published branches recorded in the status table above.)
- [x] Provision each worktree's required gitignored Unity/NuGet packages through the existing setup process. (Done per the settled cache recipe during each extraction.)
- [x] Extract candidates in approximately this order: P07, P03, P09, P08, P04, P05, P06, then P02. (Order followed; all eight candidates exist and hold full-suite gates — see "Current execution status — 2026-10-06".)
- [x] Leave the original worktree intact while candidates are assembled and validated. (Held through assembly/validation; only the sequential approved removals and the four NoteLook paths have since touched it.)

The order above reduces extraction risk; it is **not** an intended dependency chain. Every extracted PR must work on dev without the earlier candidates merged. If this cannot be achieved, record a narrower extraction or keep the dependent part in P01 rather than silently turning the plan into stacked PRs.

### Phase 3 — prove each candidate

For each extracted worktree:

- [ ] Verify the relevant regression fails for the expected functional assertion on unfixed dev before applying its fix when the test-first requirement applies.
- [ ] Apply only that PR's implementation/asset hunks and associated tests/fixtures.
- [ ] Compile/import on the baseline without protected GLS/light-ID APIs or sibling PRs.
- [ ] Run targeted tests and the complete default suite from that exact worktree root:

```powershell
pwsh ./Assets/Tests/Test-ChroMapper.ps1
```

Use `-TestFilter 'Fully.Qualified.TestName'` for targeted cases. Do not run manual/Explicit diagnostics automatically. Check Unity's exact project path for locks; a different worktree open in Unity is not a blocker. Follow the established lock-retry and hang-diagnosis rules rather than waiting indefinitely.

- [ ] Perform imported/rendered asset checks for P05/P06 and rendered mirror checks for P08.
- [ ] Verify Windows/Linux behavior for affected platform/clock/path/import functionality.
- [ ] Record profiler evidence for performance claims from an explicitly identified capture, not source correlation or an unbound latest report.
- [ ] Audit the PR diff against dev for leaked protected code/assets and unrelated changes.
- [ ] Record the tested base and exact test/log/result paths here.

Do not build/deploy ChroMapper during this split unless separately requested. The test script is the established verification path, not permission for a manual player build or deployment.

### Phase 4 — reconcile main after validation

- [x] Remove validated extracted changes from the original working tree. (Sequential-removal approval was granted 2026-10-05 — recorded here as approval, **not** blanket removal; P07's four scoped paths were removed from the source worktree on 2026-10-05 after candidate validation. Full-suite gate is green as of 2026-10-05 (3024/0/0 post-stabilization, then 3023/0/0 post-P07-removal). Staged-state changes remain user-managed except the approved index-sync exception. Update 2026-10-06: scoped removal now complete at the documented boundary — P07/P03/P09/P08/P05/P06 owned paths plus the 56 main-exclusive P02 files (52 + 4 NoteLook) removed after their candidate gates; this does NOT claim all P02 production left main.)
- [x] Reconcile approved successful extractions with P01 so features are neither duplicated nor lost. (Done at the documented dependency-overlap boundaries in "Current execution status — 2026-10-06"; the granular per-path audit is deferred to future fine-grained review.)
- [x] Preserve all protected commits and all light-ID/import work in P01. (Verified at each removal gate; the P01 scope is intact.)
- [x] Run main's complete suite after the current scoped source reconciliation. (Main complete suite green post-removal: 2846/0/0, `TestResults\cli\20261006-172549`; combined cross-PR integration/rendering verification after merges remains a separate second-merge follow-up.)
- [x] Update this tracker with final scope adjustments, verification results, and PR identifiers. (This update.)

Rebase/merge conflict resolution must combine owned hunks semantically. Do not replace a mixed mapper scene, writer, geometry container, or collection wholesale with either branch's version.

## Outstanding decisions and evolving content

- [ ] **Binary environment assets:** retained P01 by design. Any further import/render-queue/material backfill updates stay with that main asset bundle; do not assume they have been reviewed by this plan.
- [ ] **Independent P02 scope:** exact-ID/import-dependent map tests and generated-light registration are P01. Establish which transform/material/fog/ring cases pass using the old dev asset bundle.
- [ ] **Basic-light fixed-duration/calibration cases:** distinguish independent event semantics from changes requiring refreshed scene calibration/controllers. Keep the latter P01.
- [ ] **CJK asset overlap:** do not extract protected baked glyphs; verify the independent configuration/resource change on dev.
- [ ] **Editor scale and initial HJD:** require explicit runtime/render validation before completion claims.
- [ ] **Full-map diagnostics:** some tests use machine-local installed maps or absolute screenshot paths, and some are Explicit. Preserve them with their owner, but do not count them as portable default CI coverage. Assess non-Explicit cases separately.
- [ ] **Shared helper ownership:** avoid creating hidden P02 prerequisites in every smaller PR through `TestUtils` or diagnostic flags.
- [ ] **Diagnostic logging/helpers:** retain them unless removal is explicitly authorized or the user has confirmed the issue is resolved. Assign them to their feature owner; do not silently discard them.
- [ ] **Fixture-generation scripts:** assign map generators to the matching fixtures and inspect side effects before deciding whether they are PR content. Do not run map-installing generators during extraction.
- [ ] **Newly staged content:** Censored full-map/color/reset fixtures/tests and options keybind fixes are now staged; this supersedes the initial review's observation that some Censored files were untracked. Classify their staged versions, not later unstaged edits.
- [ ] **Unstaged/untracked work:** refreshed inventory includes ongoing importer/material/source changes, Censored test edits, song-list edits, incidental AudioLink material state, and untracked backfill/planning scripts. Do not absorb these automatically into an index-based extraction.
- [ ] **Separate game plugins:** BeatToTheFuture/ChromaGLS/Heck changes are outside this ChroMapper worktree plan. CM PR scopes should identify interoperability requirements without pretending this split includes those repositories.

## Verification record template

Fill this in only after actual isolation and verification, once per candidate:

```text
PR/worktree ID:
Branch:
Base commit:
Extracted implementation/asset manifest:
Extracted tests/fixtures:
Protected/main-only hunks retained:
Expected unfixed regression result:
Targeted test result and log/XML paths:
Full default suite result and log/XML paths:
Windows/Linux verification:
Rendered/manual checks:
Profiler evidence, if relevant:
Remaining blockers or scope adjustments:
Independent mergeability established: yes/no
```

## Summary

The final ownership rule is **main GLS plus all light-ID/import integration**, with independent animation/environment-transform behavior extracted only where it works without that main integration. The eight other proposed worktrees cover animation, numeric precision, workflow/initialization, AA, CJK, difficulty metadata, runtime resources, and platform compatibility. No extraction or verification is complete until the isolated dev-based worktree demonstrates it.

Execution state 2026-10-06 (see "Current execution status" for the authoritative detail): all eight candidate worktrees gated green, main green 2846/0/0 (`TestResults\cli\20261006-172549`), 56 scoped main-exclusive files removed, and remaining work is user-owned follow-up only (P02 13-path addendum staging, second-merge conflict resolution, combined suite).
