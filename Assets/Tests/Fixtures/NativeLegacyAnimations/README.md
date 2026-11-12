# Native legacy animation references

Read-only Beat Saber 1.44.1 captures from `DiagnosticTools/NativeEnvironmentAnimationSnapshot` on November 6, 2026:

- Gaga: `20261107T003507735-GagaEnvironment` (one logo).
- Halloween: `20261107T010223028-HalloweenEnvironment` (four crows and seven zombie hands, captured in Spooky Beat).
- Halloween2: `20261107T003529415-Halloween2Environment` (one crow and seven skeleton hands).

Each fixture retains the native startup, one-second and two-second song times, animation states and local transform poses. The main environment JSON stores the corresponding clip names, speeds, phases and wrap modes on its Animation components. The forty later state-time comparisons differ from song-relative playback by at most 0.31 ms. Transform comparisons allow 2 mm and 0.5 degrees for native/CM evaluation and clock rounding. They exercise the original clip curves, not curves fitted to these samples.

The first Halloween capture, `20261107T003557344-HalloweenEnvironment`, was excluded because Pray For Riddim - VIP disables its crows. Native RandomAnimationStartTime chooses different starting phases for the hands/crows between recordings. These fixtures and the main exports freeze one verified arrangement for deterministic CM seeking.
