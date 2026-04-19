using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

// yo dawg i heard u like modifier, how about 3
// yes this is modified from 2 modifier
// any comment refer back to that

[DesignTimeVisible(false)] // Obsoleted by TwoModifiersComposite
[DisplayStringFormat("{modifier1}+{modifier2}+{modifier3}+{button}")]
public class ButtonWithThreeModifiers : InputBindingComposite<float>
{
    [InputControl(layout = "Button")] public int modifier1;
    [InputControl(layout = "Button")] public int modifier2;
    [InputControl(layout = "Button")] public int modifier3;
    [InputControl(layout = "Button")] public int button;

    [Tooltip(
        "Obsolete please use modifiers Order. If enabled, this will override the Input Consumption setting, allowing the modifier keys to be pressed after the button and the composite will still trigger.")]
    [Obsolete("Use ModifiersOrder.Unordered with 'modifiersOrder' instead")]
    public bool overrideModifiersNeedToBePressedFirst;

    public enum ModifiersOrder
    {
        Default = 0,
        Ordered = 1,
        Unordered = 2
    }

    [Tooltip(
        "By default it follows the Input Consumption setting to determine if the modifers keys need to be pressed first.")]
    public ModifiersOrder modifiersOrder = ModifiersOrder.Default;

    public override float ReadValue(ref InputBindingCompositeContext context) =>
        ModifiersArePressed(ref context) ? context.ReadValue<float>(button) : 0;

    private bool ModifiersArePressed(ref InputBindingCompositeContext context)
    {
        var modifiersDown = context.ReadValueAsButton(modifier1)
            && context.ReadValueAsButton(modifier2)
            && context.ReadValueAsButton(modifier3);

        if (!modifiersDown || modifiersOrder != ModifiersOrder.Ordered) return modifiersDown;
        var timestamp = context.GetPressTime(button);
        var timestamp1 = context.GetPressTime(modifier1);
        var timestamp2 = context.GetPressTime(modifier2);
        var timestamp3 = context.GetPressTime(modifier3);

        return timestamp1 <= timestamp && timestamp2 <= timestamp && timestamp3 <= timestamp;
    }

    public override float EvaluateMagnitude(ref InputBindingCompositeContext context) => ReadValue(ref context);

    protected override void FinishSetup(ref InputBindingCompositeContext context)
    {
        if (modifiersOrder != ModifiersOrder.Default) return;
    #pragma warning disable CS0618
        if (overrideModifiersNeedToBePressedFirst)
    #pragma warning restore CS0618
            modifiersOrder = ModifiersOrder.Unordered;
        else
        {
            modifiersOrder = InputSystem.settings.shortcutKeysConsumeInput
                ? ModifiersOrder.Ordered
                : ModifiersOrder.Unordered;
        }
    }
}
