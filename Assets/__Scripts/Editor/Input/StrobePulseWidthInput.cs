using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

// Strobe pulse width is the GLS scroll tweak whose ownership is negotiated with the timeline: the
// same wheel tick can mean editing the hovered color node or changing the cursor interval. This
// helper answers ownership from the action's configured bindings and the current pointer target so
// both sides agree without reading physical modifier keys.
public static class StrobePulseWidthInput
{
    public static readonly Guid PulseWidthActionId = new Guid("9fc4d3fb-3c94-4acd-82ee-8c7b4c16d1ae");

    public const string InteractionName = "StrobePulseWidth";

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void RegisterInteraction()
    {
        // Fixture runtime resets drop project registrations; isolated test fixtures re-register on Setup.
        InputSystem.RegisterInteraction(typeof(StrobePulseWidthInteraction), InteractionName);
    }

    public static bool OwnsInput(InputAction.CallbackContext context)
    {
        var action = context.action;
        if (action != null && action.id != PulseWidthActionId)
        {
            action = FindPulseWidthAction(action.actionMap?.asset);
        }

        return OwnsInput(action, context.control);
    }

    public static bool OwnsInput(InputAction pulseWidthAction, InputControl triggeringControl)
    {
        if (pulseWidthAction == null
            || pulseWidthAction.id != PulseWidthActionId
            || !pulseWidthAction.enabled
            || triggeringControl == null)
        {
            return false;
        }

        // The wheel only belongs to pulse width while the pointer is on an editable color node.
        if (!BeatmapGLSEventColorInputController.IsStrobePulseWidthOwnedByPointer()
            && !BeatmapGLSGroupColorInputController.IsStrobePulseWidthOwnedByPointer())
        {
            return false;
        }

        var bindings = pulseWidthAction.bindings;
        for (var i = 0; i < bindings.Count; i++)
        {
            if (bindings[i].isPartOfComposite)
            {
                continue;
            }

            var partEnd = CompositePartEnd(bindings, i);
            if (ChordActive(pulseWidthAction, bindings, i, partEnd, triggeringControl)
                && !SiblingChordVetoes(pulseWidthAction, i, partEnd, triggeringControl))
            {
                return true;
            }

            i = partEnd - 1;
        }

        return false;
    }

    private static InputAction FindPulseWidthAction(InputActionAsset asset) =>
        asset == null ? null : asset.FindAction(PulseWidthActionId);

    private static int CompositePartEnd(ReadOnlyArray<InputBinding> bindings, int rootIndex)
    {
        var end = rootIndex + 1;
        while (end < bindings.Count && bindings[end].isPartOfComposite)
        {
            end++;
        }

        return end;
    }

    // A chord is active when the triggering control resolves one of its non-modifier parts and every
    // configured modifier part has a pressed resolved control right now.
    private static bool ChordActive(
        InputAction action,
        ReadOnlyArray<InputBinding> bindings,
        int rootIndex,
        int partEnd,
        InputControl triggeringControl)
    {
        if (partEnd == rootIndex + 1)
        {
            return TerminalMatches(bindings[rootIndex], triggeringControl);
        }

        var terminalFound = false;
        for (var p = rootIndex + 1; p < partEnd; p++)
        {
            var part = bindings[p];
            if (IsModifierPart(part))
            {
                if (!AnyResolvedControlPressed(action, part.effectivePath))
                {
                    return false;
                }
            }
            else if (!terminalFound && TerminalMatches(part, triggeringControl))
            {
                terminalFound = true;
            }
        }

        return terminalFound;
    }

    // A sibling binding on the same map whose active chord strictly contains the pulse binding's
    // modifiers keeps its own more-specific tweak; identical chords intentionally stay shared.
    private static bool SiblingChordVetoes(
        InputAction pulseWidthAction,
        int pulseRoot,
        int pulsePartEnd,
        InputControl triggeringControl)
    {
        var map = pulseWidthAction.actionMap;
        if (map == null)
        {
            return false;
        }

        var pulseBindings = pulseWidthAction.bindings;
        var pulseModifierCount = CountModifierParts(pulseBindings, pulseRoot + 1, pulsePartEnd);
        foreach (var action in map.actions)
        {
            if (action == pulseWidthAction || !action.enabled)
            {
                continue;
            }

            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].isPartOfComposite)
                {
                    continue;
                }

                var partEnd = CompositePartEnd(bindings, i);
                if (CountModifierParts(bindings, i + 1, partEnd) > pulseModifierCount
                    && ChordActive(action, bindings, i, partEnd, triggeringControl)
                    && ContainsAllModifiers(
                        pulseWidthAction, pulseRoot + 1, pulsePartEnd, action, i + 1, partEnd))
                {
                    return true;
                }

                i = partEnd - 1;
            }
        }

        return false;
    }

    private static int CountModifierParts(ReadOnlyArray<InputBinding> bindings, int start, int end)
    {
        var count = 0;
        for (var i = start; i < end; i++)
        {
            if (IsModifierPart(bindings[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool ContainsAllModifiers(
        InputAction pulseAction,
        int pulseStart,
        int pulseEnd,
        InputAction siblingAction,
        int siblingStart,
        int siblingEnd)
    {
        var pulseBindings = pulseAction.bindings;
        var siblingBindings = siblingAction.bindings;
        for (var p = pulseStart; p < pulseEnd; p++)
        {
            if (!IsModifierPart(pulseBindings[p]))
            {
                continue;
            }

            var matched = false;
            for (var s = siblingStart; s < siblingEnd && !matched; s++)
            {
                if (IsModifierPart(siblingBindings[s])
                    && ModifierPartsEquivalent(
                        pulseBindings[p], pulseAction, siblingBindings[s], siblingAction))
                {
                    matched = true;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ModifierPartsEquivalent(
        InputBinding pulsePart,
        InputAction pulseAction,
        InputBinding siblingPart,
        InputAction siblingAction)
    {
        if (string.Equals(
            pulsePart.effectivePath, siblingPart.effectivePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return PathsResolveTogether(pulsePart.effectivePath, siblingPart.effectivePath, pulseAction.controls)
            || PathsResolveTogether(pulsePart.effectivePath, siblingPart.effectivePath, siblingAction.controls);
    }

    private static bool PathsResolveTogether(
        string pathA, string pathB, ReadOnlyArray<InputControl> controls)
    {
        for (var i = 0; i < controls.Count; i++)
        {
            var control = controls[i];
            if (InputControlPath.Matches(pathA, control) && InputControlPath.Matches(pathB, control))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsModifierPart(InputBinding part) =>
        part.name != null && part.name.StartsWith("modifier", StringComparison.OrdinalIgnoreCase);

    private static bool TerminalMatches(InputBinding part, InputControl control)
    {
        var path = part.effectivePath;
        return !string.IsNullOrEmpty(path) && InputControlPath.Matches(path, control);
    }

    private static bool AnyResolvedControlPressed(InputAction action, string effectivePath)
    {
        if (string.IsNullOrEmpty(effectivePath))
        {
            return false;
        }

        var controls = action.controls;
        for (var i = 0; i < controls.Count; i++)
        {
            var control = controls[i];
            if (control.IsPressed() && InputControlPath.Matches(effectivePath, control))
            {
                return true;
            }
        }

        return false;
    }
}

// Staying in the waiting phase on every unowned tick is what keeps the wheel un-consumed for the
// timeline even when shortcutKeysConsumeInput is enabled.
public class StrobePulseWidthInteraction : IInputInteraction
{
    public void Process(ref InputInteractionContext context)
    {
        if (context.ReadValue<float>() != 0f
            && StrobePulseWidthInput.OwnsInput(context.action, context.control))
        {
            context.Performed();
        }
    }

    public void Reset()
    {
    }
}
