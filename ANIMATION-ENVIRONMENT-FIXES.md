# Animation and environment fixes

User-visible fixes in `superluminalFinal`, including the current local changes. Map names are examples of the affected behavior. Separate GLS authoring features are covered in the GLS worktree's patch notes.

## Animation, scrubbing and reloading

- **Moving scenery follows its animation instead of twitching or snapping back.** This includes Superluminal's spinning molecule structures around beat 264.
- **Small scrubs through dense animation sections are faster, and walls stop flashing to the wrong position while the seek finishes.** Superluminal's incoming square frames were a particularly visible example.
- **Rewinding restores earlier positions, rotations and scales.** Animated shapes no longer keep a later pose or lose their original orientation; Bimbo Limbo's square frames keep their edges after scrubbing backward.
- **Flattened or temporarily hidden geometry keeps the correct orientation when it reappears.** The eye in theyaremanycolors has square pixels and open checkerboard gaps instead of overlapping blobs.
- **Restarting after the song ends resets the scene properly.** The opening squares in theyaremanycolors reappear on subsequent plays.
- **Ctrl+R updates the animation along with the map.** Changed paths, materials, environment geometry, fog and light settings take effect, removed objects disappear, and repeated reloads do not accumulate ring clones. Reloading during playback restores the cursor and resumes playback; plugins get the same preservation through the shared reload call.
- **Refreshing environment edits restores the previous objects correctly.** Removing or retargeting an enhancement undoes its old changes, and repeated refreshes do not accumulate clones or leave lights attached to old tracks. Plugins can refresh these edits without reloading the map or interrupting playback.
- **BPM edits update animation and lighting timing immediately.** Paths, gradients and movement effects no longer keep their previous timing after an edit or partial reload.
- **Editing an animation, changing its parent, or undoing it updates the paused scene.** Held positions, rotations and material colors no longer require reopening the map to look right.
- **Missing animation references and invalid environment searches no longer hang map loading.** The bad instruction is skipped so the rest of the map can load.

## Notes, walls, arcs and chains

- **Notes turn into their final cut angle at the game's pace.** Megamix's red and blue pairs around beat 414.375 no longer settle into matching angles too early.
- **Stacked notes jump from the correct heights.** Upper notes and their connected arcs stay aligned, including after adding or removing a note below them.
- **Changing difficulties refreshes note jump distance and timing.** A difficulty no longer inherits the previous one's jump settings just because both use the same NJS.
- **Incoming walls, arcs, bombs and chain slices use the correct approach timing.** Walls stay aligned with simultaneous notes in Bimbo Limbo, and individual chain slices no longer all use the head's approach timing.
- **Note bodies remain visible at long distances.** Distant Noodle notes in Heliov no longer show only their arrows until they get close.
- **Burst-slider slices use the correct rotation.** Coincident head/tail coordinates no longer add an unwanted 90-degree turn; the reported `localRotation: [0, 0, -51, 0]` now produces the expected tilt.
- **Dissolving notes and arrows appear at the intended times.** Superluminal's opening arrows retain their shape and transparency, and fake notes reappear when rewinding across their hit time.
- **Note, bomb, wall, arc and chain colors follow their animations correctly.** Combined color tracks, per-object colors and stopped color animations retain the right result when seeking or editing; chain slices also use their own color timing.
- **Note spawn flashes and their surrounding glow appear in Preview and Playing.** They follow the timeline, respect map/per-note hide flags, and can be disabled with the new **Hide Note Spawn Effect** graphics option.

## Player camera and viewing

- **Notes face the simulated player, even when you move the free Preview camera.** Inspecting a scene from another angle no longer turns the notes toward the inspection camera.
- **Animated player travel stays aligned with incoming objects.** Superluminal's platform/camera movement and approaching notes now agree through playback and seeks.
- **Player-track rolls keep the camera at the correct height relative to the notes.** Heliov no longer places the view above the notes between roughly beats 294 and 326.
- **360-degree turns remain part of Noodle note rotation.** Notes using animated world-rotation offsets no longer lose the map's accumulated turn, including the example from issue #628.

## Environment geometry and materials

- **Geometry can be selected and edited in the Node Editor again.** Shift-click works with collision enabled or disabled, the selection outline matches the shape, and edited geometry remains selectable.
- **Requested Chroma materials resolve across environments.** InterscopeCar, InterscopeConcrete, BTSPillar and the other supported presets no longer depend on the current environment happening to contain the source material.
- **Reflective surfaces regain missing texture, baked lighting and reflections.** This restores Superluminal's rocket and the gray surrounds of DELETE IT ALL's overhead yellow lights, among other affected surfaces.
- **Mirrors omit editor grids, labels and event nodes.** Big Mirror and other reflective surfaces show the map without reflecting its editing overlays; the editor still displays and selects those nodes normally.
- **Material options produce the intended appearance.** Explicitly cleared or changed shader options no longer leave the wrong material active, introduce unwanted dithering, or make geometry disappear only in built CM.
- **Change of scene's scaffolding and distant mountains render again.** The affected material also stays dark at a distance instead of incorrectly fading into the fog.
- **Edit mode no longer fills with bright-pink missing-shader bands.** Affected animated walls render with the editor's normal appearance and obey the wall-opacity setting.
- **Environment clones and their lights are found and animated correctly.** This restores missing pieces such as Bimbo Limbo's square frames and applies later instructions to the intended copies.
- **Saving preserves the order of environment changes.** Clone sizes and placements no longer change after a save/reload, including the mismatched left/right structures in Vagueness & Journey.
- **Environment visibility and multiple animation tracks are respected.** Objects explicitly hidden by the map stay hidden, and scenery or materials assigned to several tracks receive all their contributions.
- **Generated light shapes receive their physical light and glow.** Geometry blocks light beams only when the map enables collision for it.

## Lighting, fog and smoke

- **Rotating environment lights illuminate the correct surfaces.** Megamix's runway-side angles and pillars regain the missing illumination and reflective detail.
- **Superluminal's blue rays no longer show the extra circular pattern.** Their lighting follows the intended surface appearance.
- **Change of scene's horizon glow is less overblown, and its dock has the missing foreground light band.** The sun also loses the incorrect blue/cyan fringe.
- **Light colors, fades, flashes and gradients stay consistent through playback and scrubbing.** Earlier lights receive shared color changes without restarting their brightness animation, and white light events remain white.
- **Light and ring events affect the intended targets.** Per-light selections, propagation groups and ring-name filters no longer spill onto unrelated lights; an empty light selection stays empty when edited or used to generate transitions.
- **Animated fog and bloom settings reach the visible effects.** Changes to fog shape, opacity and light glow no longer update only part of an effect or linger after the animation is removed.
- **Scrolling clouds and drifting smoke follow the song timeline.** They stop drifting while paused and return to the same state after forward/backward scrubs, including Billie, Panic 2 and Britney environments.
- **Smoke fades into intersecting geometry instead of exposing a hard plane edge.** This also works with antialiasing and moving/deformed geometry. Smoke no longer starts with the wrong pattern or an extra cycle of particles already emitted.
- **Scaled rain fades at the intended heights.** Enlarging the rain effect no longer stretches its fade incorrectly.

## Audio previews and editing

- **Audio-reactive screens and objects work while paused and scrubbing.** Monstercat 2 screens and Panic 2 bars update at the selected time without requiring live audio playback.
- **Audio visualization uses the appropriate channel, frequency spacing and smoothing.** Its response no longer depends on the editor's audio output rate or an unrelated 2D spectrogram display adjustment.
- **Dense wall and custom-event sections are more responsive to edit.** CM keeps nearby editable objects available without loading the entire dense section at once.
- **Ground box selection uses the actual lane edges after view changes.** Returning from Playing or changing light-ID views no longer leaves selection snapping to stale boundaries or nonexistent lanes.

The detailed reproduction and coverage notes remain in `Assets/Tests/ChromaEnvironmentRequestRouting.md`, `Assets/Tests/ChromaLightRequestRouting.md`, `Assets/Tests/ChromaMaterialGeometryCoverage.md`, and the workspace's `EnvironmentEnhancementAndAnimationTroubleshooting.md`.
