# Ring rotation reproduction fixtures

Each ring-rotation regression input lives in its own subfolder under `Assets/Tests/Fixtures`. A complete input contains the map files and the two CSV files emitted by the optional BeatSaberChromaGLS diagnostic build.

## Folder contents

For example, `RingRotationTest_170fps/` contains:

- `Info.dat`
- `BPMInfo.dat` (or the audio timing file used by that map version)
- the tested difficulty/lightshow `.dat`
- `ChromaGLS-RingWaveStarts.csv`
- `ChromaGLS-RingHalfBeatStates.csv`

Do not hand-edit captured CSV values. Tests may normalize runtime-only IDs and fixed-sequence origins, but the source rows must remain the captured Beat Saber output.

## Capturing a new reproduction

1. Copy the complete test map into a Beat Saber CustomWIPLevels folder.
2. Deploy the trace-enabled build with:

   ```powershell
   .\EklipZBeatSaberScripts\Deploy-GLSChroma.ps1 -RingTrace
   ```

   The project defaults `DUMP_CHROMA_RING_TRACE` to `false`; the switch passes the temporary MSBuild override. A normal deploy without `-RingTrace` compiles out every diagnostic patch and allocation.

4. Start Beat Saber and play the map once from the beginning without seeking. Use the playback speed that the regression is intended to cover.
5. Exit or disable the plugin so its buffered writers flush.
6. Copy these files from the Beat Saber installation's `Logs` directory into a new fixture subfolder:

   - `ChromaGLS-RingWaveStarts.csv`
   - `ChromaGLS-RingHalfBeatStates.csv`

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

### `ChromaGLS-RingHalfBeatStates.csv`

This is the independent state checkpoint record. Once per half beat and ring system it contains every ring's:

- current rotation;
- destination rotation;
- rotation speed;
- rotation momentum (`current - previous` for the sampled fixed tick).

Use it to assert cumulative interpolation accuracy and to ensure direct seek, rewind, stepping, and straight playback predict the same state.
