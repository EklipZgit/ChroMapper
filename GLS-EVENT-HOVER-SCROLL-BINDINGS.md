# Event hover scroll bindings (GLS + Basic Event)

Tip copy for the hover bindings on event nodes and ribbons. In each tip,
`{action:<guid>}` is a placeholder for the *currently bound* chord of the
InputAction whose action id is `<guid>` in `Assets/Input/Master.inputactions`.
Resolve the live binding via `InputActionAsset.FindAction("<guid>")` (or the
matching `CMInput` action property) and render it with
`InputAction.GetBindingDisplayString()` — users can rebind these keys, so the
defaults below are reference only.

The same actions drive both the inner event-box editor and the outer GLS group
preview, so every GLS tip applies in both views.

## GLS tips

### GLS light color node

- "You can hover over a GLS light color node and use `{action:12efb566-15c0-4db8-a04f-f1af295c8e79}` to tweak its brightness!"
  (Tweak Brightness (Hover), default Alt+Scroll)
- "You can hover over a GLS light color node and use `{action:8d8173b1-0211-441b-95b0-d820a206057a}` to tweak its strobe frequency!"
  (Tweak Strobe Frequency (Hover), default Ctrl+Alt+Scroll; adjusts 1/N strobe or a customData.strobeInterval)
- "You can hover over a GLS light color node and use `{action:9acf7284-6b19-4fe2-9f57-4d62d467dd15}` to tweak its strobe brightness!"
  (Tweak Strobe Brightness (Hover), default Ctrl+Alt+Shift+Scroll)
- "You can hover over a GLS light color node and use `{action:6c2f6ca1-ec7c-4b16-ae82-e2d3259f65cb}` to cycle its strobe fade easing (off → fade → authored easings)!"
  (Toggle Strobe Fade (Hover), default Shift+Scroll)
- "You can hover over a GLS light color node and use `{action:9cfcd847-c28a-48ad-9e05-6cf5fdf3f171}` to cycle its transition easing!"
  (Tweak Easing (Hover), default Ctrl+Shift+Scroll)
- "You can hover over a GLS light color node and use `{action:93d082ac-752a-4b2d-a710-3e39ac2706a5}` to tweak its strobe color easing!"
  (Tweak Strobe Color Easing (Hover), default Alt+Shift+Scroll)
- "You can hover over a GLS light color node and use `{action:d9642d22-c5ed-46c3-add0-4f8e6f15559f}` to mirror its color (swap primary/secondary)!"
  (Mirror (Hover), default middle click)
- "You can hover over a GLS light color node and use `{action:6adf792e-36f7-441c-88be-7ad30dd741ab}` to toggle its RGB / TrueHSV lerp type!"
  (Toggle Color Lerp Type (Hover), default Ctrl+middle click)

### GLS color ribbon (transition between two color nodes)

- "You can hover over a GLS color ribbon and use `{action:12efb566-15c0-4db8-a04f-f1af295c8e79}` to toggle the transition's RGB / TrueHSV lerp type!"
  (Tweak Brightness (Hover), default Alt+Scroll)
- "You can hover over a GLS color ribbon and use `{action:6c2f6ca1-ec7c-4b16-ae82-e2d3259f65cb}` to cycle the transition's strobe fade easing!"
  (Toggle Strobe Fade (Hover), default Shift+Scroll)
- "You can hover over a GLS color ribbon and use `{action:9cfcd847-c28a-48ad-9e05-6cf5fdf3f171}` to cycle the transition's easing!"
  (Tweak Easing (Hover), default Ctrl+Shift+Scroll)
- "You can hover over a GLS color ribbon and use `{action:93d082ac-752a-4b2d-a710-3e39ac2706a5}` to tweak the transition's strobe color easing!"
  (Tweak Strobe Color Easing (Hover), default Alt+Shift+Scroll)

### GLS rotation node

- "You can hover over a GLS rotation node and use `{action:ec35709c-0503-45d5-a460-03450dad1b41}` to tweak its rotation angle!"
  (Tweak Angle (Hover), default Alt+Scroll)
- "You can hover over a GLS rotation node and use `{action:f5be2c40-29da-46cd-a64b-a61ff42eb564}` to move it to a different axis lane!"
  (Tweak Axis (Hover), default Ctrl+Alt+Scroll; skips lanes already occupied at that beat)
- "You can hover over a GLS rotation node and use `{action:51f5ea98-6938-4c72-97e0-0fa657702ebb}` to cycle its easing!"
  (Tweak Easing (Hover), default Ctrl+Shift+Scroll)
- "You can hover over a GLS rotation node and use `{action:ee00d968-6cbc-40e8-921d-f31636675e13}` to tweak its loop count!"
  (Tweak Loop (Hover), default Ctrl+Alt+Shift+Scroll)
- "You can hover over a GLS rotation node and use `{action:3973edf5-5bed-478d-a410-d72beb1bd603}` to cycle its rotation direction!"
  (Tweak Direction (Hover), default middle click)

### GLS translation node

- "You can hover over a GLS translation node and use `{action:b70438c0-b7a3-4dfa-8bd3-d6bf4906a769}` to tweak its translation value!"
  (Tweak Value (Hover), default Alt+Scroll; un-YEETs a YEETed node before adjusting)
- "You can hover over a GLS translation node and use `{action:b168aaf1-9e62-407e-9341-c11284a940d6}` to move it to a different axis lane!"
  (Tweak Axis (Hover), default Ctrl+Alt+Scroll; skips lanes already occupied at that beat)
- "You can hover over a GLS translation node and use `{action:04ec5b5d-f072-4ce0-a61b-7a71d432a0b1}` to cycle its easing!"
  (Tweak Easing (Hover), default Ctrl+Shift+Scroll)
- "You can hover over a GLS translation node and use `{action:3d4587dc-8e26-4ce2-98c8-804559e48ba6}` to toggle its YEET override!"
  (YEET Translation (Hover), default Shift+Z)

### GLS float FX node

- "You can hover over a GLS float FX node and use `{action:146a8e3f-7192-4cd3-bef6-45b7e5e5ec31}` to tweak its effect value!"
  (Tweak Value (Hover), default Alt+Scroll)
- "You can hover over a GLS float FX node and use `{action:e96bf7bc-54ce-42a1-9314-544d68370e27}` to cycle its easing!"
  (Tweak Easing (Hover), default Ctrl+Shift+Scroll)

## Basic event tips

### Basic event light node

- "You can hover over a light event node and use `{action:276c98b6-1db9-4975-be8c-23de8b862b07}` to tweak its brightness!"
  (Tweak Event Main, default Alt+Scroll)
- "You can hover over a light event node and use `{action:16423471-7d49-4057-b1a3-b4640fda6899}` to cycle its node type (on / flash / fade / off)!"
  (Tweak Event Ctrl Alt, default Ctrl+Alt+Scroll)
- "You can hover over a light event node and use `{action:c8954a16-3194-4e4c-9b2e-132cb70fdb7b}` to cycle its transition easing!"
  (Tweak Event Ctrl Shift, default Ctrl+Shift+Scroll)
- "You can hover over a light event node and use `{action:1a34df05-8e5b-4e52-b486-6eaceeca0b52}` to invert its color!"
  (Invert Event Value, default middle click)

### Basic event ribbon (light transition ribbon)

- "You can hover over a light transition ribbon and use `{action:276c98b6-1db9-4975-be8c-23de8b862b07}` to cycle the transition's RGB / HSV / TrueHSV lerp type!"
  (Tweak Event Main, default Alt+Scroll)
- "You can hover over a light transition ribbon and use `{action:c8954a16-3194-4e4c-9b2e-132cb70fdb7b}` to cycle the transition's easing!"
  (Tweak Event Ctrl Shift, default Ctrl+Shift+Scroll)

### Ring rotation event

- "You can hover over a ring rotation event and use `{action:276c98b6-1db9-4975-be8c-23de8b862b07}` to tweak its rotation amount (first scroll creates 90° plus a direction)!"
  (Tweak Event Main, default Alt+Scroll)
- "You can hover over a ring rotation event and use `{action:16423471-7d49-4057-b1a3-b4640fda6899}` to tweak its rotation speed!"
  (Tweak Event Ctrl Alt, default Ctrl+Alt+Scroll)
- "You can hover over a ring rotation event and use `{action:c8954a16-3194-4e4c-9b2e-132cb70fdb7b}` to tweak its propagation count!"
  (Tweak Event Ctrl Shift, default Ctrl+Shift+Scroll)
- "You can hover over a ring rotation event and use `{action:8f7e326a-f827-47c8-a191-35ca18854059}` to tweak its rotation step (travel distance)!"
  (Tweak Event Ctrl Shift Alt, default Ctrl+Shift+Alt+Scroll)
- "You can hover over a ring rotation event and use `{action:1a34df05-8e5b-4e52-b486-6eaceeca0b52}` to cycle its rotation direction!"
  (Invert Event Value, default middle click)

### Ring zoom event

- "You can hover over a ring zoom event and use `{action:276c98b6-1db9-4975-be8c-23de8b862b07}` to tweak its zoom step!"
  (Tweak Event Main, default Alt+Scroll)
- "You can hover over a ring zoom event and use `{action:16423471-7d49-4057-b1a3-b4640fda6899}` to tweak its zoom speed!"
  (Tweak Event Ctrl Alt, default Ctrl+Alt+Scroll)
- "You can hover over a ring zoom event and use `{action:c8954a16-3194-4e4c-9b2e-132cb70fdb7b}` to fine-tune its zoom step!"
  (Tweak Event Ctrl Shift, default Ctrl+Shift+Scroll; 10x finer step than the main tweak)
- "You can hover over a ring zoom event and use `{action:1a34df05-8e5b-4e52-b486-6eaceeca0b52}` to invert its zoom direction!"
  (Invert Event Value, default middle click; negates the step sign)

### Laser rotation / laser speed event

- "You can hover over a laser speed event and use `{action:276c98b6-1db9-4975-be8c-23de8b862b07}` to tweak its laser speed!"
  (Tweak Event Main, default Alt+Scroll)
- "You can hover over a laser speed event and use `{action:16423471-7d49-4057-b1a3-b4640fda6899}` to toggle its rotation lock (lockRotation)!"
  (Tweak Event Ctrl Alt, default Ctrl+Alt+Scroll)
- "You can hover over a laser speed event and use `{action:1a34df05-8e5b-4e52-b486-6eaceeca0b52}` to cycle its rotation direction!"
  (Invert Event Value, default middle click)

## Selection tips

- "With an object selected, hold `{action:3266236c-fda3-4697-bd0e-2a659c9cb072}`
  and use `{action:99ccd235-d96f-4545-b9da-116c171bb16b}` on another object to
  select everything between the two — including the object you click!"
  (Mass Select Modifier + Select Objects, default Ctrl+Shift+click; works on
  notes, obstacles, arcs, chains, events, BPM changes, and NJS events — the
  range is grouped, so a note-to-note range also picks up obstacles, arcs, and
  chains in between)

## Notes

- `{action:276c98b6-1db9-4975-be8c-23de8b862b07}` (Tweak Event Main) also tweaks
  BPM event tempo, toggles color boost events, and adjusts the generic value of
  other basic event types.
- `{action:7a637f25-63f4-434a-b1bc-5db5e34f9e04}` (Tweak Event Alternative,
  default Alt+Shift+Scroll) currently has no effect on the nodes and ribbons
  above.
- The tweak step size comes from the scroll precision setting; while not over a
  GLS node or ring rotation, `{action:380a1a0d-b07c-4b29-8b15-5785bfca4a68}`
  (Scroll Precision > Scroll, default Ctrl+Alt+Shift+Scroll) changes that
  global precision.
- All scroll tweaks honor the "invert scroll" settings
  (`Settings.Instance.InvertScrollEventValue` / `InvertPrecisionScroll`).

## Reference

| Action id | Map | Action | Default binding |
|---|---|---|---|
| `12efb566-15c0-4db8-a04f-f1af295c8e79` | GLS Color Objects | Tweak Brightness (Hover) | Alt+Scroll |
| `8d8173b1-0211-441b-95b0-d820a206057a` | GLS Color Objects | Tweak Strobe Frequency (Hover) | Ctrl+Alt+Scroll |
| `9acf7284-6b19-4fe2-9f57-4d62d467dd15` | GLS Color Objects | Tweak Strobe Brightness (Hover) | Ctrl+Alt+Shift+Scroll |
| `6c2f6ca1-ec7c-4b16-ae82-e2d3259f65cb` | GLS Color Objects | Toggle Strobe Fade (Hover) | Shift+Scroll |
| `9cfcd847-c28a-48ad-9e05-6cf5fdf3f171` | GLS Color Objects | Tweak Easing (Hover) | Ctrl+Shift+Scroll |
| `93d082ac-752a-4b2d-a710-3e39ac2706a5` | GLS Color Objects | Tweak Strobe Color Easing (Hover) | Alt+Shift+Scroll |
| `d9642d22-c5ed-46c3-add0-4f8e6f15559f` | GLS Color Objects | Mirror (Hover) | Middle click |
| `6adf792e-36f7-441c-88be-7ad30dd741ab` | GLS Color Objects | Toggle Color Lerp Type (Hover) | Ctrl+Middle click |
| `ec35709c-0503-45d5-a460-03450dad1b41` | GLS Rotation Objects | Tweak Angle (Hover) | Alt+Scroll |
| `f5be2c40-29da-46cd-a64b-a61ff42eb564` | GLS Rotation Objects | Tweak Axis (Hover) | Ctrl+Alt+Scroll |
| `51f5ea98-6938-4c72-97e0-0fa657702ebb` | GLS Rotation Objects | Tweak Easing (Hover) | Ctrl+Shift+Scroll |
| `ee00d968-6cbc-40e8-921d-f31636675e13` | GLS Rotation Objects | Tweak Loop (Hover) | Ctrl+Alt+Shift+Scroll |
| `3973edf5-5bed-478d-a410-d72beb1bd603` | GLS Rotation Objects | Tweak Direction (Hover) | Middle click |
| `b70438c0-b7a3-4dfa-8bd3-d6bf4906a769` | GLS Translation Objects | Tweak Value (Hover) | Alt+Scroll |
| `b168aaf1-9e62-407e-9341-c11284a940d6` | GLS Translation Objects | Tweak Axis (Hover) | Ctrl+Alt+Scroll |
| `04ec5b5d-f072-4ce0-a61b-7a71d432a0b1` | GLS Translation Objects | Tweak Easing (Hover) | Ctrl+Shift+Scroll |
| `3d4587dc-8e26-4ce2-98c8-804559e48ba6` | GLS Translation Objects | YEET Translation (Hover) | Shift+Z |
| `146a8e3f-7192-4cd3-bef6-45b7e5e5ec31` | GLS FloatFX Objects | Tweak Value (Hover) | Alt+Scroll |
| `e96bf7bc-54ce-42a1-9314-544d68370e27` | GLS FloatFX Objects | Tweak Easing (Hover) | Ctrl+Shift+Scroll |
| `276c98b6-1db9-4975-be8c-23de8b862b07` | Event Objects | Tweak Event Main | Alt+Scroll |
| `7a637f25-63f4-434a-b1bc-5db5e34f9e04` | Event Objects | Tweak Event Alternative | Alt+Shift+Scroll |
| `16423471-7d49-4057-b1a3-b4640fda6899` | Event Objects | Tweak Event Ctrl Alt | Ctrl+Alt+Scroll |
| `c8954a16-3194-4e4c-9b2e-132cb70fdb7b` | Event Objects | Tweak Event Ctrl Shift | Ctrl+Shift+Scroll |
| `8f7e326a-f827-47c8-a191-35ca18854059` | Event Objects | Tweak Event Ctrl Shift Alt | Ctrl+Shift+Alt+Scroll |
| `1a34df05-8e5b-4e52-b486-6eaceeca0b52` | Event Objects | Invert Event Value | Middle click |
| `380a1a0d-b07c-4b29-8b15-5785bfca4a68` | Scroll Precision | Scroll | Ctrl+Alt+Shift+Scroll |
| `3266236c-fda3-4697-bd0e-2a659c9cb072` | Beatmap Objects | Mass Select Modifier | Ctrl |
| `99ccd235-d96f-4545-b9da-116c171bb16b` | Beatmap Objects | Select Objects | Shift+Left click |
