# Basic Event Movement Reference

This document records the movement behavior used by Beat Saber 1.44.1 and Heck/Chroma that ChroMapper's Basic Event preview needs to reproduce. It focuses on rotating lasers, ring rotation/propagation, and ring position (ring zoom).

## Sources and authority

The behavior below was checked against these sources:

- Beat Saber 1.44.1 decompilation:
  - `BeatSaberDecompilation/1.44.1/Main/LightRotationEventEffect.cs`
  - `BeatSaberDecompilation/1.44.1/Main/LightPairRotationEventEffect.cs`
  - `BeatSaberDecompilation/1.44.1/Main/LightPairSinMoveEventEffect.cs`
  - `BeatSaberDecompilation/1.44.1/Main/TrackLaneRing.cs`
  - `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsManager.cs`
  - `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsRotationEffect.cs`
  - `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsRotationEffectSpawner.cs`
  - `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsPositionStepEffectSpawner.cs`
- Heck/Chroma:
  - `Heck/Chroma/Lighting/ChromaRingsRotationEffect.cs`
  - `Heck/Chroma/HarmonyPatches/Events/RingRotationChromafier.cs`
  - `Heck/Chroma/HarmonyPatches/Events/RingStepChromafier.cs`
  - `Heck/Chroma/HarmonyPatches/Events/LightRotationChromafier.cs`

Line numbers refer to the versions in this workspace. Decompiled field defaults are not authoritative environment values because Unity serialized assets may override them. The algorithms are authoritative; actual environment multipliers, vectors, steps, and startup values must come from the serialized environment data used by that environment.

## Time domains

There are two movement models:

1. Laser rotation and paired sinusoidal movement integrate `IAudioTimeSource.lastFrameDeltaSongTime`. Their speed is therefore measured per song-second and follows song-time playback.
2. Ring rotation flex and ring position use fixed ticks. Destination changes happen in the rotation effect's fixed tick, ring state advances with `Mathf.Lerp` in the ring manager's fixed tick, and `LateUpdate` interpolates the two fixed states for smooth rendering.

ChroMapper must not replace a fixed-tick recurrence with a superficially similar continuous exponential unless it is the exact closed form of that recurrence, including `Mathf.Lerp` clamping.

---

## Single rotating lasers (`LightRotationEventEffect`)

### Base-game event behavior

On startup, the effect records the transform's starting rotation and disables itself:

- `BeatSaberDecompilation/1.44.1/Main/LightRotationEventEffect.cs:35-41`

For an event value of zero, it disables movement and restores the starting rotation:

- `BeatSaberDecompilation/1.44.1/Main/LightRotationEventEffect.cs:56-62`

For a positive event value, it:

1. restores the starting rotation;
2. applies a random self-space offset in `[0, 180)` degrees;
3. enables per-frame rotation;
4. selects a random direction;
5. computes angular speed as:

```text
rotationSpeed = eventValue * serializedRotationSpeedMultiplier * 20 * direction
```

- `BeatSaberDecompilation/1.44.1/Main/LightRotationEventEffect.cs:63-69`

Every render update rotates in self space by:

```text
deltaAngle = lastFrameDeltaSongTime * rotationSpeed
```

- `BeatSaberDecompilation/1.44.1/Main/LightRotationEventEffect.cs:43-46`

The resulting speed is degrees per song-second. The constant `20` is declared at `LightRotationEventEffect.cs:25`; the serialized multiplier is declared at lines 13-14.

### Chroma overrides

Chroma replaces the event handler when custom event data is present:

- `Heck/Chroma/HarmonyPatches/Events/LightRotationChromafier.cs:17-30`

It resolves precision speed from custom `speed`, falling back to the Basic Event value, and computes:

```text
rotationSpeed = precisionSpeed * 20 * direction
```

- `LightRotationChromafier.cs:32-43`
- `LightRotationChromafier.cs:59-67`

Direction values are side-aware for event 12 versus the other laser event. Without a custom direction, Chroma chooses a random sign. `lockPosition` prevents the stop/start event from resetting the transform; without it, Chroma preserves the base-game reset and random-start behavior:

- `LightRotationChromafier.cs:34-43`
- `LightRotationChromafier.cs:45-69`

### ChroMapper interpolation state

ChroMapper stores the event-start angle, random offset, direction, enabled state, lock state, and signed speed. It advances the previous event by `previous.Speed * deltaSongSeconds`, then evaluates the current interval as `Angle + Speed * seconds`:

- `ChroMapper/Assets/__Scripts/Environments/Effects/Basic/LightRotationEffect.cs:22-78`
- `ChroMapper/Assets/__Scripts/Environments/Effects/Basic/LightRotationEffect.cs:81-104`

---

## Paired rotating lasers (`LightPairRotationEventEffect`)

The paired effect keeps independent left/right rotation states but shares random values generated once per frame:

- `BeatSaberDecompilation/1.44.1/Main/LightPairRotationEventEffect.cs:8-21`
- `LightPairRotationEventEffect.cs:69-78`
- `LightPairRotationEventEffect.cs:133-157`

Left and right start with opposite configured start angles:

- `LightPairRotationEventEffect.cs:81-107`

A positive event resets that side to its random offset plus configured start angle and computes:

```text
rotationSpeed = eventValue * 20 * direction
```

- `LightPairRotationEventEffect.cs:178-191`

Each frame accumulates `lastFrameDeltaSongTime * rotationSpeed` and rebuilds local rotation from the stored base quaternion plus the accumulated angle:

- `LightPairRotationEventEffect.cs:110-123`

Left and right receive opposite random offsets and directions when their respective events are handled:

- `LightPairRotationEventEffect.cs:158-165`

The override event can force deterministic frame-derived offsets and positive-left/negative-right directions:

- `LightPairRotationEventEffect.cs:135-173`

---

## Paired sinusoidal laser movement (`LightPairSinMoveEventEffect`)

This effect uses phase, not degrees. For a positive event:

```text
phaseSpeed = eventValue * 1
phase = randomOffset + configuredStartValue
```

- `BeatSaberDecompilation/1.44.1/Main/LightPairSinMoveEventEffect.cs:67`
- `LightPairSinMoveEventEffect.cs:172-189`

Every update advances phase by song time:

```text
movementValue += lastFrameDeltaSongTime * speed
u = sin(movementValue) * 0.5 + 0.5
positionOffset = LerpUnclamped(startOffset, endOffset, u)
```

- `LightPairSinMoveEventEffect.cs:114-130`

The left and right sides mirror the X component, and the two event sides use opposite random phase offsets:

- `LightPairSinMoveEventEffect.cs:87-110`
- `LightPairSinMoveEventEffect.cs:141-168`

---

## Ring state and flex interpolation (`TrackLaneRing`)

Each ring stores:

- previous rotation;
- current rotation;
- destination rotation;
- rotation/flex speed;
- previous/current/destination Z position;
- move speed.

- `BeatSaberDecompilation/1.44.1/Main/TrackLaneRing.cs:6-24`

`Init` initializes position only. Rotation fields remain at their zero defaults; position must never be copied into rotation:

- `TrackLaneRing.cs:28-34`

Every fixed tick performs:

```text
previousRotation = rotation
rotation = Mathf.Lerp(rotation, destinationRotation, fixedDeltaTime * rotationSpeed)

previousPosition = position
position = Mathf.Lerp(position, positionOffset.z + destinationPosition, fixedDeltaTime * moveSpeed)
```

- `TrackLaneRing.cs:36-42`

`Mathf.Lerp` clamps its interpolation factor to `[0, 1]`. Therefore the exact closed form for `N` unchanged fixed ticks is:

```text
factor = 1 - Clamp01(fixedDeltaTime * speed)
valueN = destination - (destination - start) * factor^N
```

This is mathematically equivalent to the source recurrence, although using `Pow` instead of replaying every float operation may differ in the last floating-point bits.

A destination assignment changes the destination and speed but does not snap the current rotation:

- `TrackLaneRing.cs:50-54`
- `TrackLaneRing.cs:66-70`

Late rendering linearly interpolates previous and current fixed states using `TimeHelper.InterpolationFactor`:

- `TrackLaneRing.cs:44-48`

The ring manager executes at order `-2`, updates every ring in `FixedUpdate`, and applies render interpolation in `LateUpdate`:

- `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsManager.cs:4-5`
- `TrackLaneRingsManager.cs:59-77`

---

## Base-game ring rotation waves

### Event target

The base-game spawner chooses a random step and direction. The target angle is relative to ring zero's current **destination**, not its current interpolated angle:

```text
newFirstRingDestination = firstRingDestination + rotation * direction
```

- `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsRotationEffectSpawner.cs:58-75`
- `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsRotationEffect.cs:88-96`

This is why interrupted flex movement is not discarded: a later fast event still moves toward the cumulative target. If two callbacks occur before the prior wave assigns ring zero, the source naturally sees the older destination; ChroMapper must query/evaluate the assigned destination at that event time rather than blindly summing authored angles.

### Active waves and integer propagation

Each event creates an independent active effect containing:

- first-ring target angle;
- per-ring rotation step;
- flex speed;
- integer propagation speed;
- current progress position.

- `TrackLaneRingsRotationEffect.cs:8-19`
- `TrackLaneRingsRotationEffect.cs:77-86`

The serialized startup buildup is inserted as a normal independent wave:

- `TrackLaneRingsRotationEffect.cs:24-35`
- `TrackLaneRingsRotationEffect.cs:53-56`

The rotation effect executes at order `-3`, before the ring manager at order `-2`:

- `TrackLaneRingsRotationEffect.cs:4-6`
- `TrackLaneRingsManager.cs:4-6`

On every fixed tick, active effects are visited from newest to oldest. Each effect assigns a batch of `rotationPropagationSpeed` rings, then advances its progress. Completed waves are removed:

- `TrackLaneRingsRotationEffect.cs:58-75`

For ring index `i`, the assigned target is:

```text
waveAngle + i * waveStep
```

The assigned flex speed is the wave's flex speed:

- `TrackLaneRingsRotationEffect.cs:64-67`

Because active effects are visited newest-to-oldest, if multiple waves assign the same ring on one tick, the oldest wave assigns last and wins that tick.

---

## Chroma ring rotation waves

### Why Chroma replaces the base effect

Chroma disables the original rotation effect and reroutes both startup and authored waves through `ChromaRingsRotationEffect`:

- `Heck/Chroma/HarmonyPatches/Events/RingRotationChromafier.cs:112-147`

Its replacement exists specifically to restore float propagation:

- `Heck/Chroma/Lighting/ChromaRingsRotationEffect.cs:7-12`

It copies the environment's serialized startup angle, step, integer propagation speed, and flex speed into an independent Chroma wave:

- `ChromaRingsRotationEffect.cs:16-33`

### Custom parameters

For custom Chroma events:

- `step`, `prop`, `speed`, and `rotation` override environment/event defaults;
- `stepMult`, `propMult`, and `speedMult` multiply the resolved values;
- direction controls rotation sign and is side-aware;
- `_counterSpin` flips non-Big ring direction;
- reset emits the environment's base rotation with step `0`, propagation `50`, and flex speed `50`.

- `Heck/Chroma/HarmonyPatches/Events/RingRotationChromafier.cs:44-109`
- `RingRotationChromafier.cs:150-164`

Like the base game, Chroma calculates the new angle from ring zero's currently assigned destination:

- `RingRotationChromafier.cs:158-163`

### Exact float truncation and repeated assignment behavior

Each independent Chroma wave stores float `ProgressPos`, target angle, step, flex speed, and float propagation speed:

- `Heck/Chroma/Lighting/ChromaRingsRotationEffect.cs:36-45`
- `ChromaRingsRotationEffect.cs:117-128`

Every fixed tick does the following for each active wave, newest to oldest:

```text
ring = truncateTowardZero(ProgressPos)
ProgressPos += RotationPropagationSpeed

while ring < ProgressPos and ring < ringCount:
    ring[ring].destination = RotationAngle + ring * RotationStep
    ring[ring].rotationSpeed = RotationFlexySpeed
    ring++
```

- `ChromaRingsRotationEffect.cs:47-72`

The cast to `long` truncates the old float progress before incrementing. This creates varying batch sizes and can assign the same ring on multiple fixed ticks. For example, propagation `0.5` starts from ring zero on two consecutive ticks. Repeated assignments cannot be collapsed away: another overlapping wave may change the ring between them, after which the older wave's repeated assignment changes it back.

Chroma retains every active wave independently. A newer high-propagation wave can reach a later ring before an older low-propagation wave reaches it. When the older assignment eventually occurs, that ring receives the older destination and flex speed and may reverse direction. This apparently out-of-order reversal is source behavior, not a preview artifact.

If two waves assign the same ring on the same fixed tick, iteration remains newest-to-oldest, so the older wave's assignment wins.

### Deterministic ChroMapper representation

ChroMapper stores each unfinished Chroma wave as one compact descriptor containing its next absolute fixed frame, float progress cursor, first-ring destination, step, propagation, and flex speed:

- `ChroMapper/Assets/__Scripts/Environments/Effects/Basic/TrackLaneRingsRotationEffect.cs:426-458`

Snapshots copy the compact unfinished waves and actual per-ring rotations, destinations, and speeds. They do not pre-expand or copy every future per-ring assignment:

- `TrackLaneRingsRotationEffect.cs:52-114`
- `TrackLaneRingsRotationEffect.cs:436-458`

Snapshot advancement finds the next wave frame and processes wave descriptors newest-to-oldest. Each wave performs Chroma's float truncation at execution time, so repeated fractional assignments and older-wave same-frame wins are retained without a large assignment heap:

- `TrackLaneRingsRotationEffect.cs:291-358`

Absolute fixed frames are used rather than adding propagation delay directly to beat/JSON time because Chroma changes destinations only on fixed ticks. Fixed-frame ordering is monotonic with song/JSON time and avoids BPM-dependent delay arithmetic.

Rendering retains its evaluator during forward playback, advances it incrementally, evaluates adjacent fixed states `N` and `N + 1`, and interpolates between them at the render-time fraction. A rewind, edit, or state change resets it from the deterministic snapshot:

- `TrackLaneRingsRotationEffect.cs:182-260`

---

## Ring position / ring zoom

The base-game event alternates between serialized maximum and minimum position steps using `sameTypeIndex` parity. Ring `i` receives:

```text
destinationZ = i * selectedStep
moveSpeed = serializedMoveSpeed
```

- `BeatSaberDecompilation/1.44.1/Main/TrackLaneRingsPositionStepEffectSpawner.cs:42-50`

The movement itself is the fixed-tick `Mathf.Lerp` recurrence in `TrackLaneRing.FixedUpdateRing`, not a constant-speed move and not an arbitrary continuous exponential:

- `BeatSaberDecompilation/1.44.1/Main/TrackLaneRing.cs:36-42`
- `TrackLaneRing.cs:66-70`

Chroma's ring-step transpiler allows custom `step` and custom `speed` to replace the selected step and serialized move speed:

- `Heck/Chroma/HarmonyPatches/Events/RingStepChromafier.cs:39-49`
- `RingStepChromafier.cs:51-76`

ChroMapper snapshots the actual per-ring positions at event boundaries and evaluates the same clamped fixed-step recurrence, followed by interpolation between adjacent fixed states:

- `ChroMapper/Assets/__Scripts/Environments/Effects/Basic/TrackLaneRingsPositionEffect.cs:30-76`
- `TrackLaneRingsPositionEffect.cs:79-113`

---

## Empirical validation boundary

The formulas and ordering in this document are source-derived, but ChroMapper has not yet been scientifically synchronized against captured Beat Saber/Chroma output. Remaining visual differences may come from fixed-update phase, callback timing, serialized environment values, random choices, or floating-point accumulation. Beat Saber dispatches beatmap callbacks from `BeatmapCallbacksUpdater.LateUpdate`, while ring waves advance later through fixed ticks; ChroMapper currently maps those starts onto an absolute song-time fixed grid. The game's Unity fixed-loop phase relative to song start can place two nearby callbacks on the same upcoming tick when that absolute model separates them, or vice versa. Do not tune that phase by eye; record matching event sequences and timestamped callback/fixed-tick state in both programs before changing parity behavior.

## Required parity checklist

When changing Basic Event movement, verify all of the following:

- Use song-time deltas for laser rotation and paired sinusoidal movement.
- Apply the laser `20` speed multiplier exactly where the source does.
- Preserve environment-specific serialized laser speed multipliers.
- Use the ring's assigned destination, not its current angle, as the base for a new ring target.
- Keep every unfinished ring propagation wave independent.
- Preserve Chroma's float-progress truncation and repeated ring assignments.
- Process overlapping waves newest-to-oldest; older assignments win same-tick conflicts.
- Allow a newer fast wave to execute before pending assignments from an older slow wave.
- Allow those older delayed assignments to reverse a ring later.
- Apply destination/speed assignments before that fixed tick's ring `Mathf.Lerp`.
- Use `Clamp01(fixedDeltaTime * speed)` when collapsing fixed-step lerps.
- Interpolate previous/current fixed states for render-framerate smoothness.
- Treat the startup ring buildup as an independent wave.
- Preserve Chroma custom step, propagation, speed, multipliers, direction, counter-spin, lock, and reset behavior.
- Recompute snapshots deterministically after edits without rerolling unchanged random choices.
