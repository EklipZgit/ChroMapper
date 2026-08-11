# ChroMapper Agent Guide

## Input and Hotkey Changes

Before changing any input action, binding, callback, keybind UI, or overlap behavior, read `Assets/Input/README.md` completely.

- `Assets/Input/Master.inputactions` is the authoritative input definition.
- `Assets/Input/Master.cs` is generated from that asset. Never hand-edit it and never add temporary interface-forwarding callbacks instead of regenerating it.
- Permanent user-facing actions must be authored in `Master.inputactions`. Never mutate the enabled shared `InputActionAsset` from `OnEnable`, `Start`, or another runtime callback.
- Add the generated `CMInput.I<ActionMapName>Actions` interface to the scene controller and implement the exact generated `On<ActionName>` callback.
- Assume multiple enabled action maps can share a physical chord. Every callback must reject unrelated contexts, and shared global callbacks must yield only when a context-specific controller owns the chord.
- For mouse-wheel values, use `Value` with expected control type `Axis` and the value-capable `OneModifier`, `TwoModifiers`, or `ThreeModifiers` composite. The final part must be named `binding`.
- Three-modifier changes must preserve all four paths through authoring, generated code, options UI, saved overrides, and reload. `ThreeModifiersComposite.ReadValueAsObject` must test all three modifiers.
- Validate both the intended behavior and adjacent overlapping behavior. A hotkey is not fixed if it makes scroll precision, a simpler modifier chord, or another event type fire incorrectly.

## Build Boundary

A successful Unity batch build is sufficient unless the user explicitly requests deployment. Keep output in `C:\src\BeatSaberStuff\.build\chromapper`. Do not inspect, stop, modify, or deploy to the running ChroMapper install.
