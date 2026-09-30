# Heck animation parity plan (CM preview vs Chroma)

Hand this file to a session to pick up any single item. Each item is independent unless it says otherwise.

- **CM worktree:** `C:\src\BeatSaberStuff\ChroMapper-gls-color-shifts` (all CM paths below are relative to this root)
- **Parity reference repo:** `C:\src\BeatSaberStuff\Heck` (Heck/Chroma/NoodleExtensions, Beat Saber 1.44.1)
- **Docs (authoritative behavior reference):**
  - Tracks and points: https://heck.aeroluna.dev/animation/tracks-and-points/
  - Additional events: https://heck.aeroluna.dev/animation/additional-events/
  - Properties: https://heck.aeroluna.dev/animation/properties/

## Ground rules for every session (see `.windsurfrules` / `AGENTS.md` at `C:\src\BeatSaberStuff`)

- **Test-first:** write the failing test, run `pwsh ./Assets/Tests/Test-ChroMapper.ps1 -TestFilter '<Full.Test.Name>'` from the repo root, confirm it fails with the expected assertion, only then implement. No `Assert.Multiple` in ChroMapper tests.
- Do not build the project. Document why above every changed block and name the regression test. Undo failed attempts (keep their logging).
- Everything must behave identically on Linux and Windows.
- Already supported (do not redo): all documented easings, `AnimateTrack`/`AssignPathAnimation` core properties, named point definitions (missing names log + skip), per-point easing/spline/`lerpHSV`, `duration`/`easing`/`repeat`, `AssignTrackParent`, `AssignPlayerToTrack` (without `target`), `AnimateComponent` → `BloomFogEnvironment` (`FogAnimator.cs`), individual `customData.animation` path animations.

---

## P1 — Point-definition engine: modifiers, bases, swizzling, smoothing (one workstream, ship incrementally)

Today only `ParseColor` supports any of this; every other property type silently misparses.

- **1a. Modifiers for all property types** — `opNone/opAdd/opSub/opMul/opDiv`, nested/chained (`[1.5, [2, "opMul"], [4, "opAdd"]]`). Currently only `ParseColor` handles ops (`Assets/__Scripts/Beatmap/Animations/PointDefinition.cs` ~line 248); `ParseFloat`/`ParseVector3`/`ParseQuaternion` ignore them.
  - Refs: docs "Modifiers" + "Bases" sections (tracks-and-points page); `Heck/Heck/Animation/PointDefinition/Modifier.cs`, `PointDefinition.cs` (`Group()` value/flag/modifier grouping), `FloatPointDefinition.cs`, `Vector3PointDefinition.cs`, `QuaternionPointDefinition.cs`.
- **1b. Base properties** — color bases already partially work (note0/note1, environment 0/1/W + boosts, obstacles; saber colors intentionally unsupported). Missing: all 15 player-transform bases (`baseHead*`, `baseLeftHand*`, `baseRightHand*`), all 10 scoring bases (`baseCombo`, `baseMultipliedScore`, `baseImmediateMaxPossibleMultipliedScore`, `baseModifiedScore`, `baseImmediateMaxPossibleModifiedScore`, `baseRelativeScore`, `baseMultiplier`, `baseEnergy`, `baseSongTime`, `baseSongLength`), movement-data bases (`baseNoteJumpMovementSpeed`, `baseNoteJumpStartBeatOffset`, `baseJumpDistance`, `basePlayerHeight`). Preview note: gameplay-only bases (score/combo/energy) can evaluate to sensible editor defaults (0 / song time), but must parse instead of silently producing wrong values.
  - Ref: `Heck/Heck/HeckImplementation/BaseProviders/*.cs` (PlayerTransformBaseProvider, ColorBaseProvider, ScoreBaseProvider, MovementDataBaseProvider).
- **1c. Swizzling** — `.xyzw` suffixes on bases, unordered/repeated (`baseHeadPosition.zzx`), dimension changing both ways (`baseHeadPosition.x`, `baseSongTime.xxx`), mixed bases in one point.
- **1d. Smoothing** — `.s[number]` (`baseHeadPosition.s10`, decimals as `.s0_4`), combinable with swizzle (`baseHeadPosition.xzy.s2`). Frame-rate-smoothed lerp toward the live value.
  - Refs for both: `Heck/Heck/Services/BaseProvider/BaseProviderManager.cs` (`GetProviderValues` split/parse), `Heck/Heck/Animation/PointDefinition/Values.cs` (`PartialProviderValues`, `SmoothProvidersValues`, `SmoothRotationProvidersValues`).
- **CM touchpoints:** `Assets/__Scripts/Beatmap/Animations/PointDefinition.cs` (parsers + `PointData`), `AnimateProperty.cs`, `TrackAnimator.AddPointDef`, `ObjectAnimator.AddPointDef`, `FogAnimator`. Suggested shape: a shared value-expression evaluator (base string → swizzle → smooth → modifiers) consumed by all parsers, replacing the per-type special cases; keep `ParseColor`'s color-scheme bases working.
- **Test sketch:** playmode test loading a fixture with modifier/base/swizzle/smooth points on `dissolve`/`offsetPosition`/`color`; assert evaluated values at fixed beats (e.g. `[1.5, [2, "opMul"]]` → 3; `["baseNote0Color", [0.4,0.4,0.4,1,"opMul"]]` vs scheme color; `baseSongTime.xxx`; `.s` smoothing only asserts it converges toward the source, not exact frames).

## P2 — `AnimateComponent` → `TubeBloomPrePassLight`

Docs example event ("lights start extremely bright then quickly dim"). Heck animates `colorAlphaMultiplier` and `bloomFogIntensityMultiplier` on every `TubeBloomPrePassLight` under track-bound objects (`Heck/Chroma/Animation/AnimateComponent.cs` lines 81-94, `TubeBloomLightCustomizer`).
- **CM touchpoints:** extend the `AnimateComponent` routing added for fog (`Assets/__Scripts/Editor/Grid/Collections/CustomEventGridContainer.cs` `AddCustomEvent`/`HandleObjectDelete` gate + a component animator alongside `Assets/__Scripts/Beatmap/Animations/FogAnimator.cs`). CM's analog of the component lives on geometry/light containers' MPB state — find where `bloomFogIntensityMultiplier`/color alpha are applied at load (GeometryContainer component handling) and drive those values per-frame.
- **Test sketch:** fixture with a track bound to geometry lights + `AnimateComponent`/`TubeBloomPrePassLight` events (instant + eased multi-keyframe); assert the applied multiplier state at seeks, mirroring `FogAnimationTests`.

## P3 — Animated `interactable`

Heck registers `interactable` as track AND path property (`Heck/NoodleExtensions/Plugin.cs` lines 35/44; consumption in `Heck/NoodleExtensions/Animation/AnimationHelper.cs` lines ~233/250/313). CM only honors static `uninteractable` customData. Preview impact: notes/walls flagged uninteractable should render as ghosted/non-cuttable state where CM shows such state; at minimum parse and store so data-level tooling sees it.
- **CM touchpoints:** `TrackAnimator.AddPointDef` + `ObjectAnimator.AddPointDef` new `interactable`/`_interactable` cases → aggregator on `ObjectAnimator`; surface through the container state the editor uses for uninteractable display.
- **Test sketch:** AnimateTrack `interactable` `[[1,0],[0,1]]` on a track; assert the object's effective interactable state flips after the event and restores on scrub-back.

## P4 — `AssignPlayerToTrack` `target` field

`target` (`Root`/`Head`/`LeftHand`/`RightHand`) is ignored; CM always drives the camera. Root/Head ≈ camera today; `LeftHand`/`RightHand` have no preview representation. Minimal parity: parse `target`, treat `Root`/`Head` as today, and document/no-op `LeftHand`/`RightHand` (or attach to saber visuals if the preview grows them).
- **CM touchpoints:** `Assets/__Scripts/Editor/Grid/Collections/CustomEventGridContainer.cs` (`AssignPlayerToTrack` case), `Assets/__Scripts/Editor/CameraController.cs` (`AddPlayerTrack`).
- **Test sketch:** event with `"target": "Head"` vs default asserts identical camera binding; unknown target logs and skips like Heck.

## P5 — `null` property erasing

`"property": null` must restore "as if never set" (docs: tracks-and-points → AnimateTrack/AssignPathAnimation warnings). Today `PointDefinition`'s `_ => new JSONArray()` branch animates the property to `default(T)` (e.g. `dissolve: null` → invisible). Note the docs' caveat: cannot interpolate from/to null and cannot update already-active objects in game; CM's seek-based preview can actually do better, but must at minimum match "as if never set".
- **CM touchpoints:** `PointDefinition` constructor (detect `JSONNull`/missing), `AnimateProperty`/`ObjectAnimator`/`TrackAnimator` removal path (mirror `RemoveEvent`), `CustomEventGridContainer` update flow.
- **Test sketch:** `AnimateTrack` with `dissolve: [[0,0],[1,1]]` then a later `dissolve: null`; seek past both → object back at pre-animation opacity; scrub back before the null → animated value.

## P6 — Strict point-value-count validation

Heck throws on wrong component counts (docs warning: color point must be exactly 5 numbers). CM parsers silently accept wrong counts. Add validation that logs the offending property/event and skips it (matching the game's error-and-skip style used by `AnimateProperty.SkipsMissingPointDefinition`), so malformed maps are visible in the editor instead of previewing wrong values.
- **CM touchpoints:** `PointDataParsers` (+ modifier/base parser from P1), existing `try/catch` in `ObjectAnimator.AddPointDef`.
- **Test sketch:** fixture with `[1, 5, 3, 0.5]` as a color point → load logs the documented error and the event is skipped, scene load completes (regression-guard against the wedged-transition failure shape `SpellsLaserWallTest` proved for missing point definitions).

---

### Suggested order

P1 (1a → 1b → 1c/1d) unlocks the most maps; then P2, P3, P4; P5/P6 are small follow-ups. P1's evaluator should be built so P2/P3 reuse it (TubeBloom and interactable points accept modifiers/bases in Heck too).
