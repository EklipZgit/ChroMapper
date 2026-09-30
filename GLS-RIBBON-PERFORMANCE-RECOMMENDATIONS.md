# GLS Ribbon Performance Recommendations

## Scope

This document records the read-only performance audit of the GLS color-transition ribbon implementation. It covers timeline construction and mutation, viewport retention, appearance refreshes, texture uploads, collider maintenance, and `BasicGradient.shader` rendering.

The implementation already has several strong foundations that should be preserved:

- Transition retention uses an augmented interval treap instead of scanning the whole map during viewport movement.
- Timeline lookup uses identity dictionaries and binary search rather than repeated linear searches.
- Physical-light timelines are cached per GLS group ID and light-count revision.
- Group edits update the existing physical-light timeline incrementally.
- Ribbon animation is evaluated by the GPU; map scans and color sampling do not run every frame.
- Unity hierarchy/component discovery is cached outside hover and other recurring hot paths.

The work below is divided into sessions that can be developed independently. Sessions in the same parallel phase should avoid editing the same files where possible. Where file overlap is unavoidable, the integration notes identify it.

## Parallel phase 1: independent investigation and implementation sessions

### Session A: sparse or bounded timeline storage

**Priority:** Highest

**Primary files:**

- `Assets/__Scripts/Environments/Effects/GLS/GLSColorTimeline.cs`
- `Assets/__Scripts/Utils/DataStructures/StateChunksContainer.cs`
- `Assets/__Scripts/Utils/DataStructures/SortedBucketArray.cs`
- New targeted tests under `Assets/Tests/Editor/`

**Problem**

Every physical light receives both a group-state container and an event-state container. `StateChunksContainer` currently uses a `SortedBucketArray` with one eagerly allocated `List<T>` per ten beats. `GLSColorTimeline` resizes both containers for every light to the estimated maximum beat.

This scales with `maximum beat × physical light count`, even when most buckets are empty. For example, a 10,000-beat map with 100 lights creates roughly 200,000 empty bucket lists for one GLS group ID. The existing 100,000-beat clamp can still permit approximately two million lists per group ID.

**Recommendations**

1. Give data-only GLS timelines a sparse bucket implementation, or allow `StateChunksContainer` to use a dynamically selected bucket width that caps the number of buckets.
2. Preserve ordered predecessor/successor queries, identity-based state lookup, and incremental insertion/removal behavior.
3. Avoid changing the bucket behavior of gameplay effects unless shared benchmarks and tests demonstrate that the change is safe for all callers.
4. Eliminate the initial default bucket allocation followed immediately by `Resize` for timeline-only containers.
5. Add scale tests covering long maps, high light counts, sparse events, and extreme authored tail beats.

**Success criteria**

- Empty storage scales with the number of populated time regions or with a small bounded bucket count, not directly with maximum authored beat.
- Timeline lookup remains logarithmic or bounded by a small bucket.
- Incremental add/remove parity tests continue to pass.
- A benchmark records timeline construction time, allocated bytes, bucket/node counts, and retained memory.

### Session B: genuinely lazy legacy transition timelines

**Priority:** High

**Primary files:**

- `Assets/__Scripts/Beatmap/Appearances/GLSEventCommon.cs`
- `Assets/Tests/Editor/GLSColorTransitionCacheTest.cs`
- `Assets/Tests/Editor/GLSColorTimelineBoundsTest.cs`

**Problem**

Initial cache construction always builds the legacy filter-only sequences and transition interval index. When the environment supplies a physical light count, it then builds the physical per-light timeline as well. Known-light group IDs therefore pay for two complete transition representations, while the legacy results are filtered out during viewport queries.

**Recommendations**

1. Store group identities and stable group ordering immediately, but do not build legacy `ColorFilterSequence` data for IDs with a known physical light count.
2. Mark the legacy representation dirty and construct it only if an unknown-light-count fallback is actually requested.
3. Ensure an environment unload or light-count reset can lazily construct the legacy representation from the retained groups.
4. Retire unused legacy sequence dictionaries and interval entries when an ID transitions from unknown to known physical light count.
5. Keep map changes, environment changes, and cloned group identity behavior covered by tests.

**Success criteria**

- Known-light cache initialization performs only the physical-light build.
- Unknown environments retain existing ribbon behavior.
- Switching between known and unknown light counts produces no stale interval entries.
- Startup allocations and cache initialization time are measured before and after the change.

### Session C: shader-path and fragment-cost cleanup

**Priority:** Medium-high; validate with GPU measurements

**Primary files:**

- `Assets/_Graphics/Shaders/Object/BasicGradient.shader`
- Shader/ribbon parity tests under `Assets/Tests/Editor/`

**Problem**

An active physical-light timeline fragment reads seven texture rows, or nine when strobing, and performs easing, interpolation, color conversion, tone mapping, and blend work. The shader source also computes the scalar ribbon interpolation before testing the physical-light timeline branch, even though timeline ribbons return a different result.

Inactive portions of a distributed strip return transparent black after the timing-row lookup. They avoid the remaining timeline samples but still reach the output/blend stage.

**Recommendations**

1. Move the `_UseLightTimeline` branch before scalar color/easing work so timeline ribbons are guaranteed not to evaluate discarded scalar interpolation. Confirm the generated shader previously did or did not optimize this automatically.
2. Benchmark replacing the inactive-range `return 0` with `clip`/discard. Keep it only if target desktop GPUs improve; discard can regress some architectures.
3. Inspect compiled shader variants and GPU captures for dynamic-branch divergence, texture fetch count, register pressure, overdraw, and time spent in RGB/HSV conversion.
4. Keep ordinary Basic Event ribbons on their cheap scalar path.
5. Do not reduce timing precision without long-map parity coverage; the timeline currently uses float texture storage because half precision can lose beat-time accuracy.
6. Consider splitting color rows into half precision and timing/metadata rows into float precision only if bandwidth is proven to be material and the extra texture binding does not cost more.

**Success criteria**

- Pixel parity remains unchanged for RGB, legacy HSV, true HSV, strobe, faded strobe, distributed timing, and independent easing tracks.
- GPU captures show the timeline path does no unused scalar interpolation.
- Any use of discard is supported by measured frame-time improvement on the intended graphics APIs.

### Session D: profiling fixtures and stress maps

**Priority:** High because it supplies evidence for every other session

**Primary files:**

- New editor tests or profiling helpers under `Assets/Tests/Editor/`
- Optional manually invoked diagnostic tooling under `Tools/`
- No production behavior changes

**Problem**

The current tests strongly cover correctness and parity but do not establish performance budgets or representative worst-case collection sizes.

**Recommendations**

1. Create deterministic fixtures for:
   - many GLS group IDs with known light counts;
   - one group ID with a high physical light count;
   - thousands of color groups and nodes;
   - long sparse maps with very large final beat values;
   - alternating filters and same-time boxes;
   - many simultaneously visible long ribbons;
   - repeated group replacement, drag, undo, and redo.
2. Measure separately:
   - initial cache/timeline creation;
   - incremental group removal and insertion;
   - viewport-boundary queries;
   - loaded-ribbon refresh fan-out;
   - managed allocations and retained timeline memory;
   - texture upload time and render-thread/GPU cost.
3. Record realistic maximum group counts, light counts, nodes per group, visible ribbon counts, and ribbon screen coverage before selecting hard thresholds.
4. Keep performance diagnostics manual or appropriately tolerant if stable timing assertions are unsuitable for Jenkins.

**Success criteria**

- Each proposed optimization has a repeatable before/after measurement.
- Tests distinguish CPU construction cost, managed allocation cost, upload cost, draw-call cost, and fragment cost.
- Correctness tests remain separate from timing-sensitive diagnostics.

## Parallel phase 2: sessions that depend on phase 1 understanding

### Session E: targeted invalidation and replacement coalescing

**Priority:** Highest for interactive editing

**Depends on:** Session D measurements. It can otherwise proceed independently of Sessions A-C.

**Primary files:**

- `Assets/__Scripts/Beatmap/Appearances/GLSEventCommon.cs`
- `Assets/__Scripts/Editor/Grid/Collections/GLSGroupColorGridContainer.cs`
- `Assets/__Scripts/Beatmap/Containers/GLSGroupContainer.cs`
- Relevant action/replacement classes
- Targeted transition cache and editor interaction tests

**Problem**

A color-group mutation currently refreshes every loaded color group and every preview ghost. Each ribbon refresh loops over the physical lights, prepares tweens, rewrites its texture, and uploads it. A normal replacement deletes the old group and spawns the replacement, so the complete refresh and incremental timeline mutation may occur twice.

**Recommendations**

1. Coalesce remove-plus-add into one logical color-group replacement transaction.
2. Apply the final timeline mutation before refreshing visuals, rather than rendering the intermediate removed state.
3. Expose the physical timeline's changed source and target identities, or a narrowly scoped changed time/owner set, to the collection layer.
4. Refresh only loaded containers whose outgoing or incoming ribbon changed.
5. Include outer preview ghosts that represent a changed child identity or changed same-time aggregate.
6. Preserve correct behavior for standalone insertion, standalone deletion, bulk actions, undo/redo, networked actions, and rejected drag restoration.
7. Avoid a global pool refresh; update existing loaded containers in place.

**Success criteria**

- One replacement produces one timeline update and one targeted visual refresh phase.
- Edit cost scales with changed sources/targets and affected lights, not every visible GLS node.
- Unrelated loaded ribbon textures are not rewritten or uploaded.
- Hover ownership and retained offscreen source behavior remain correct.

### Session F: timeline query storage and cache locality

**Priority:** Medium; pursue after profiling

**Depends on:** Session A's storage decision and Session D measurements.

**Primary files:**

- `Assets/__Scripts/Environments/Effects/GLS/GLSColorTimeline.cs`
- `Assets/__Scripts/Beatmap/Appearances/GLSEventCommon.cs`

**Problem**

The physical timeline stores outgoing and incoming states in dictionaries keyed by `(event reference, light index)`. Ribbon preparation and bound recomputation repeatedly hash tuple keys while iterating sequentially across every light. Dense all-light filters therefore pay dictionary overhead where contiguous state storage could be more cache-friendly and smaller.

**Recommendations**

1. Measure dictionary lookup and memory cost separately for dense and sparse filters.
2. Consider a per-event state record containing outgoing/incoming arrays or compact light-index maps.
3. Use dense arrays only when density justifies their null slots; retain sparse storage for narrow filters.
4. Preserve constant-time source/light and target/light lookup for rendering and hover resolution.
5. Avoid rebuilding full per-event arrays for small incremental edits.

**Success criteria**

- The chosen representation improves measured refresh and bound-recomputation time without inflating sparse-map memory.
- Incremental edits remain scoped to affected lights and identities.

### Session G: CPU texture packing and upload cleanup

**Priority:** Medium

**Depends on:** Session D measurements. Coordinate with Session H because both affect texture ownership.

**Primary files:**

- `Assets/__Scripts/Beatmap/Appearances/GLSEventCommon.cs`
- `Assets/__Scripts/Beatmap/Shared/LightGradientController.cs`

**Problem**

Every timeline refresh clears the complete `9 × lightCount` color array, then overwrites all rows for valid lights. Each ribbon subsequently calls `Texture2D.SetPixels` and `Apply`, producing a separate upload. Boost-state lookups and tween preparation are repeated per valid physical light.

**Recommendations**

1. Remove the full `Array.Clear`; explicitly zero all required rows only for invalid lights while overwriting every row for valid lights.
2. Benchmark `SetPixelData` with persistent compatible storage against `SetPixels`.
3. Cache endpoint boost results within a ribbon update when many lights resolve the same event endpoints, but retain direct lookup if dictionary/cache overhead is worse for small light counts.
4. Avoid uploading when the packed payload and ribbon bounds are unchanged. Use an explicit revision/version from the timeline and relevant appearance/boost state instead of deep array comparisons.
5. Ensure pooled ribbons invalidate revisions when their owner, light count, appearance, or texture layout changes.

**Success criteria**

- No full scratch-array clear occurs for rows that will immediately be overwritten.
- Unchanged ribbon refresh requests do not upload texture data.
- The selected upload API demonstrates lower CPU cost or allocation pressure.

## Parallel phase 3: larger rendering architecture

### Session H: shared GPU texture storage and draw-call reduction

**Priority:** Potentially very high on dense visible maps, but most invasive

**Depends on:** Session C shader measurements, Session D stress fixtures, and coordination with Session G.

**Primary files:**

- `Assets/__Scripts/Beatmap/Appearances/GLSEventCommon.cs`
- `Assets/__Scripts/Beatmap/Shared/LightGradientController.cs`
- `Assets/_Graphics/Shaders/Object/BasicGradient.shader`
- Pool/collection ownership code as needed

**Problem**

Each visible ribbon owns a separate texture and binds it through a `MaterialPropertyBlock`. The texture sampler is not an instanced property, so different ribbon textures are likely to split batches even though scalar properties use GPU instancing. Dense GLS views can therefore scale toward one draw call and one texture object/upload per ribbon.

**Recommendations**

1. Verify batching behavior with Unity Frame Debugger and a GPU capture before redesigning.
2. If texture binding is the draw-call limiter, use shared storage:
   - a `Texture2DArray` with one slice per pooled ribbon;
   - a shared atlas with an instanced row/slice offset; or
   - a structured/graphics buffer with an instanced base index, if supported by all target graphics APIs.
3. Allocate slots at the ribbon-pool lifetime, not per appearance refresh.
4. Upload only dirty slots and retain stable slot ownership while a container remains pooled.
5. Keep ordinary Basic Event ribbons compatible with the same material or explicitly split shader/material variants if that improves batching.
6. Account for variable physical light counts without reallocating the entire shared resource during ordinary pool reuse.

**Success criteria**

- Frame Debugger confirms materially fewer batches/draw calls.
- Texture object count no longer scales one-for-one with visible ribbons, or the alternative buffer design demonstrably removes the same bottleneck.
- Dirty updates remain localized to changed ribbon slots.
- Pool creation, recycling, environment changes, and map unload release shared resources correctly.

### Session I: geometry and overdraw experiments

**Priority:** Optional; only if GPU captures show fragment/overdraw cost remains high after Session H

**Depends on:** Sessions C, D, and preferably H.

**Primary files:**

- Ribbon mesh/prefab generation
- `Assets/_Graphics/Shaders/Object/BasicGradient.shader`
- `Assets/__Scripts/Beatmap/Shared/LightGradientController.cs`

**Problem**

A timeline ribbon is one union-sized quad covering the earliest-to-latest active strip interval. Distributed per-light delays can leave substantial masked areas that still rasterize and perform at least a timing lookup. Long overlapping transparent ribbons can add further overdraw.

**Recommendations**

1. Measure overdraw and inactive fragment coverage before changing geometry.
2. Compare the current union quad against:
   - discard after the timing lookup;
   - a small segmented mesh that excludes large inactive regions; or
   - grouped strips sharing identical or adjacent time bounds.
3. Do not create one renderer per light; the added draw calls and GameObjects would likely cost more than the saved fragments.
4. Preserve exact UV-to-light hover mapping if geometry changes.

**Success criteria**

- The chosen approach reduces GPU frame time on distributed-wave stress scenes.
- Draw-call and collider costs do not regress more than the fragment savings.
- Ribbon hit testing remains constant-time and selects the correct physical light.

## Low-risk cleanup session

### Session J: redundant collider synchronization and small hot-path cleanup

**Priority:** Low; safe to run independently, but coordinate edits to `LightGradientController.cs` with Sessions G/H

**Primary files:**

- `Assets/__Scripts/Beatmap/Shared/LightGradientController.cs`

**Problem**

`UpdateColorTimeline` calls `SetVisible(true)`, which synchronizes an existing collider's collision group, and then calls `UpdateDuration`, which synchronizes it again. The second call normally exits quickly, but it is redundant on every visible ribbon refresh.

**Recommendations**

1. Establish one lifecycle owner for collider-group synchronization after duration/transform changes.
2. Preserve lazy collider creation and cached mesh/UV projection.
3. Keep chunk changes correct when a pooled or moved ribbon changes owner.
4. Review nearby repeated property assignments only where profiling or clear call ordering proves redundancy.

**Success criteria**

- One collider synchronization occurs per ribbon refresh or ownership/chunk change.
- No component lookup or recovery logic is introduced into frame/hover hot paths.

## Suggested integration order

1. Run Session D first or in parallel with Sessions A-C to establish baselines.
2. Integrate Session A and Session B independently; both reduce initialization cost but touch different layers.
3. Integrate Session E after its targeted invalidation contract is covered by tests.
4. Integrate the low-risk parts of Sessions C, G, and J.
5. Re-profile CPU, render thread, draw calls, GPU time, allocations, and retained memory.
6. Proceed with Session H only if per-ribbon texture bindings/draw calls remain material.
7. Proceed with Session I only if fragment overdraw remains material after the batching work.
8. Consider Session F only when tuple-key dictionary cost is visible in profiling; it is not worth additional storage complexity without evidence.

## Cross-session correctness requirements

Every session must preserve coverage for:

- inner and outer GLS color ribbons;
- same-time box aggregation and serialized winner order;
- incoming head ribbons and terminal held tails;
- filter ownership, chunking, division/step filters, limits, reversal, and random seeds;
- distributed timing, brightness, color, and strobe color;
- RGB, legacy HSV, and true HSV interpolation;
- interval, color, strobe-color, and strobe-pulse easing;
- boost state at each endpoint;
- BPM changes and SongBpmTime conversion;
- hover-to-physical-light destination resolution;
- viewport retention in both scroll directions;
- pool reuse, map changes, environment changes, undo/redo, drag, paste, and network replacement;
- unknown-light-count fallback behavior.

For any implementation session, write the regression or scale test first and demonstrate the unfixed behavior before changing production code. Do not use `Assert.Multiple` in ChroMapper tests.

## Performance evidence to report

Each completed session should report:

- fixture dimensions: groups, nodes, light count, maximum beat, and visible ribbons;
- cache/timeline construction milliseconds;
- incremental edit milliseconds;
- managed allocations and retained bytes;
- ribbon texture bytes uploaded per edit/refresh;
- visible ribbon texture count;
- draw-call/batch count;
- CPU main-thread, render-thread, and GPU frame time;
- Release/editor platform and graphics API;
- correctness tests run and their result.

