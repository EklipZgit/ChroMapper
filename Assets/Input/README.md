# ChroMapper Input and Hotkey Architecture

This is the implementation guide for adding or changing ChroMapper hotkeys. The input path spans an authored Unity asset, generated C#, automatic callback installation, overlap filtering, context-specific callbacks, rebind UI, and saved override loading. A change is incomplete until every relevant layer agrees.

## Authoritative Data Flow

1. `Master.inputactions` defines action maps, actions, action types, bindings, composite parts, IDs, and default paths.
2. Unity's Input System importer generates `Master.cs` with wrapper structs and `I<ActionMapName>Actions` interfaces.
3. `CMInputCallbackInstaller` creates one shared `CMInput`, discovers generated interfaces, finds scene components implementing them, and installs callbacks.
4. `InputSystemPatch` prevents a simpler chord from firing when a more specific chord containing the same controls is active. `CMInputCallbackInstaller.Awake` disables Unity shortcut consumption on non-macOS so identical chords can serve context-specific actions in multiple maps.
5. Controllers accept only `context.performed`, verify hover/UI/object/modifier context, read the wheel direction, and perform an undoable command.
6. The options UI discovers authored action maps and actions. `LoadKeybindsController` restores saved overrides and can replace value composite names with legacy button composite names.

## Permanent Action Workflow

### 1. Edit `Master.inputactions`

Do not add permanent actions with `InputActionMap.AddAction` or `AddCompositeBinding` at runtime. The shared `Master` asset is enabled before scene controllers run; changing its setup then throws `InvalidOperationException` and leaves the callback unbound.

For a wheel chord:

- Use action type `Value`.
- Use expected control type `Axis`.
- Use `<Mouse>/scroll/y` as the final composite part.
- Use `OneModifier`, `TwoModifiers`, or `ThreeModifiers`, not the button-only legacy composites.
- Name modifier parts exactly `modifier`, or `modifier1`, `modifier2`, and `modifier3`.
- Name the value part exactly `binding`.
- Give every action and binding a unique stable GUID.

For a button such as middle click:

- Use action type `Button`.
- Bind the control directly unless modifiers are required.

### 2. Regenerate `Master.cs`

`Master.inputactions.meta` has wrapper generation enabled and sets the wrapper class to `CMInput`. Let Unity reimport the asset and regenerate `Master.cs`.

Never hand-edit generated action fields, constructor lookups, callback subscriptions, or generated interfaces. Never keep forwarding methods with old generated names. If a controller fails to implement an interface after an action rename, regenerate first and then implement the final generated callback name.

Action names become callback names by removing spaces and punctuation. For example, `Tweak Easing (Hover)` generates `OnTweakEasingHover`.

### 3. Implement the generated interface

The scene component responsible for the action map must implement `CMInput.I<ActionMapName>Actions`. Implement every method in that generated interface with the exact generated signature:

```csharp
public void OnActionName(InputAction.CallbackContext context)
```

`CMInputCallbackInstaller` handles subscription. Do not create another `CMInput` and do not manually subscribe permanent generated actions.

### 4. Guard callbacks by context

All generated callbacks receive `started`, `performed`, and `canceled`; mutation callbacks must reject anything except `context.performed`.

Before changing data, verify the relevant conditions:

- The intended object controller is hovering a valid object.
- The pointer is not over blocking UI where applicable.
- The object is not being dragged where applicable.
- Simpler chords reject extra modifiers that select a more-specific action.
- The callback uses the correct inversion setting through `GetScrollDirection`.
- The mutation goes through the existing command/action path so undo, appearance refresh, and merge behavior remain correct.

Do not solve overlap by globally rejecting all modifier keys. The scroll-precision action itself is a three-modifier chord. Shared behavior must yield only while a context-specific object owns that chord.

### 5. Preserve rebinding

Authored actions automatically appear in keybind options unless their action map or action name starts with `+`, the internal identifier.

An action name starting with `=` is persistent and is excluded from ChroMapper's more-specific-binding blocking logic.

The options UI determines required key count from the composite path. It must recognize both current and restored legacy names:

- `TwoModifiers` and `ButtonWithTwoModifiers`: three paths.
- `ThreeModifiers` and `ButtonWithThreeModifiers`: four paths.

`LoadKeybindsController` writes saved three-key overrides as modifier paths plus the final button/value path. Test a default binding, rebind it, restart/reload it, and rebind it again.

## Composite Rules

`ThreeModifiersComposite` is the value-capable custom equivalent of Unity's `TwoModifiers` composite. It supports an arbitrary final value such as mouse scroll Y.

Its value must be gated consistently in all read paths:

- `EvaluateMagnitude`
- unsafe `ReadValue`
- `ReadValueAsObject`

Every path must require `modifier1`, `modifier2`, and `modifier3`. Omitting the third modifier from `ReadValueAsObject` makes several enabled actions sharing the same chord resolve inconsistently even when `ReadValue` looks correct.

`ButtonWithThreeModifiers` is the legacy float/button composite used while rebuilding persisted overrides. Do not use it as the authored composite for a new axis action.

## Overlapping Chords

Overlapping actions are intentional. Basic events, GLS color events, GLS rotation events, and scroll precision can all be enabled while sharing controls.

`InputSystemPatch` caches each action's non-composite binding paths. A more-specific action blocks a simpler action only when it has more paths and contains every path from the simpler action. Equal chords are allowed so separate context controllers can react to the same physical input.

Implications:

- An `Alt+Scroll` callback must reject Ctrl and Shift when those modifiers select another operation.
- A `Ctrl+Alt+Scroll` callback must reject Shift when `Ctrl+Alt+Shift+Scroll` has another operation.
- Identical chords in different action maps need strict hover/object guards; equal chords do not block each other.
- Runtime-added actions are absent from the patch's startup cache and must not be used for permanent hotkeys.
- Shared actions such as scroll precision must use object-hover ownership flags to yield to node-specific actions without disabling themselves everywhere.

## Regression Checklist

For every modifier-wheel change, test both scroll directions and confirm logs/data for:

1. The requested chord on the requested object.
2. The same chord on every other object type sharing it.
3. Every simpler chord formed by releasing one modifier.
4. Scroll precision away from event nodes.
5. Scroll precision while hovering basic ring/zoom and GLS nodes.
6. Pointer-over-UI behavior.
7. Default keybind discovery in options.
8. Rebinding, saving, loading, and rebinding the restored composite.
9. A clean Unity compile/build with generated `Master.cs` matching `Master.inputactions`.

If a callback produces no log, inspect in this order: runtime exceptions during controller enable, generated interface/callback name, action-map installation, composite read paths, overlap blocking, callback modifier guards, hover validity, then command mutation.
