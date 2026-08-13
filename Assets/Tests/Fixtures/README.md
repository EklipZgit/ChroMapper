# Ring rotation reproduction fixtures

Each ring-rotation regression input lives in its own subfolder under `Assets/Tests/Fixtures`. A complete modern input contains the map files and four CSV files emitted by the optional BeatSaberChromaGLS diagnostic build. Older fixed-state fixtures predate the callback/render streams and retain their original two files.

## Folder contents

For example, `RingRotationTest_170fps/` contains:

- `Info.dat`
- `BPMInfo.dat` (or the audio timing file used by that map version)
- the tested difficulty/lightshow `.dat`
- `ChromaGLS-RingWaveStarts.csv`
- `ChromaGLS-RingHalfBeatStates.csv`
- `ChromaGLS-BasicEventCallbacks.csv`
- `ChromaGLS-RingRenderStates.csv`

Do not hand-edit captured CSV values. Tests may normalize runtime-only IDs and fixed-sequence origins, but the source rows must remain the captured Beat Saber output.

## Capturing a new reproduction

1. Copy the complete test map into a Beat Saber CustomWIPLevels folder.
2. Deploy the trace-enabled build with:

   ```powershell
   C:\src\BeatSaberStuff\BeatSaberChromaGLS\build-all-versions.ps1 -RingTrace
   ```

   The project defaults `DUMP_CHROMA_RING_TRACE` to `false`; the switch passes the temporary MSBuild override. A normal deploy without `-RingTrace` compiles out every diagnostic patch and allocation.

4. Start Beat Saber and play the map once from the beginning without seeking. Use the playback speed that the regression is intended to cover.
5. Exit or disable the plugin so its buffered writers flush.
6. Copy these files from the Beat Saber installation's `Logs` directory into a new fixture subfolder:

   - `ChromaGLS-RingWaveStarts.csv`
   - `ChromaGLS-RingHalfBeatStates.csv`
   - `ChromaGLS-BasicEventCallbacks.csv`
   - `ChromaGLS-RingRenderStates.csv`

7. Copy the exact map `Info.dat`, BPM/audio timing data, and tested difficulty/lightshow file into the same subfolder.
8. Record the Beat Saber version, Heck/Chroma build, ChromaGLS build, capture UTC, environment, and playback speed in the test or a fixture-specific README.
9. Redeploy without `-RingTrace` after capturing to restore the normal diagnostics-free plugin.

## CSV roles

### `ChromaGLS-RingWaveStarts.csv`

This is the event and propagation-order record. It contains:

- every logical `WAVE_ADD` with unique IDs despite Chroma's object pooling;
- map event time and callback song time;
- target, step, float propagation, and flex speed;
- every postfix-observed `DEST_STATE` change that survived a complete Chroma fixed tick;
- fixed sequence, ring index, resulting destination, and speed.

Use it to assert map-to-wave conversion, callback/fixed-tick phase, fractional truncation, propagation timing, and the final same-tick overwrite result. Intermediate setters overwritten during the same tick are intentionally not intercepted because doing so changed game behavior.

`DEST_STATE` is a sparse change stream, not a fixed-size ring-by-wave matrix. Multiple
waves may resolve to the same destination or overwrite one another before the postfix, so
valid captures at different render/fixed phases can contain different row counts. The tests
replay the captured wave starts and compare the complete surviving transition stream rather
than assuming a particular count.

### `ChromaGLS-RingHalfBeatStates.csv`

This is the independent post-`FixedUpdate` state checkpoint record. Once per half beat and ring system it contains every ring's:

- current rotation;
- destination rotation;
- rotation speed;
- rotation momentum (`current - previous` for the sampled fixed tick).

Use it to assert fixed-step recurrence accuracy and compare the preview model at the recorded song times. Recurrence tests that inject captured assignment sequences deliberately do not validate scheduling, seek/state selection, or rendering. It does not contain the rendered transform or `TimeHelper.InterpolationFactor`, so it cannot by itself prove screenshot parity; future visual captures must record one of those values.

The startup wave begins before the song clock. Its state at song zero therefore depends on how many fixed ticks elapsed during scene startup; even runs at the same nominal framerate can differ by a tick. Song-time model comparisons must report that phase difference rather than silently deriving the preview pre-roll from each capture.

### `ChromaGLS-BasicEventCallbacks.csv`

This records authored event time, actual callback song time, render/fixed clocks, and the
`CallbacksInTime` ahead-time bucket. Ordinary visible light/ring timing uses only the
zero-ahead bucket; the other rows are look-ahead projections, not duplicate visual callbacks.

### `ChromaGLS-RingRenderStates.csv`

This records every ring on every rendered frame: previous/current fixed rotation, destination,
speed, raw `TimeHelper.InterpolationFactor`, interpolated angle, and final transform. Tests use
it to distinguish fixed recurrence from render extrapolation and to verify high-speed overshoot.
Rows at song time zero may retain an identity transform because the OEM manager suppresses its
`LateUpdate` while the audio controller is stopped.
