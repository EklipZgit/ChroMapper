// this is also modified from input system

using System;
using System.ComponentModel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using Object = UnityEngine.Object;

[DisplayStringFormat("{modifier1}+{modifier2}+{modifier3}+{binding}")]
[DisplayName("Binding With Three Modifiers")]
public class ThreeModifiersComposite : InputBindingComposite
{
    [InputControl(layout = "Button")] public int modifier1;
    [InputControl(layout = "Button")] public int modifier2;
    [InputControl(layout = "Button")] public int modifier3;
    [InputControl] public int binding;

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

    public override Type valueType => m_ValueType;
    public override int valueSizeInBytes => m_ValueSizeInBytes;

    private int m_ValueSizeInBytes;
    private Type m_ValueType;
    private bool m_BindingIsButton;

    public override float EvaluateMagnitude(ref InputBindingCompositeContext context) =>
        ModifiersArePressed(ref context) ? context.EvaluateMagnitude(binding) : default;

    public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
    {
        if (ModifiersArePressed(ref context))
            context.ReadValue(binding, buffer, bufferSize);
        else
            UnsafeUtility.MemClear(buffer, m_ValueSizeInBytes);
    }

    private bool ModifiersArePressed(ref InputBindingCompositeContext context)
    {
        var modifiersDown = context.ReadValueAsButton(modifier1)
            && context.ReadValueAsButton(modifier2)
            && context.ReadValueAsButton(modifier3);
        if (!modifiersDown || !m_BindingIsButton || modifiersOrder != ModifiersOrder.Ordered) return modifiersDown;
        var timestamp = context.GetPressTime(binding);
        var timestamp1 = context.GetPressTime(modifier1);
        var timestamp2 = context.GetPressTime(modifier2);
        var timestamp3 = context.GetPressTime(modifier3);

        return timestamp1 <= timestamp && timestamp2 <= timestamp && timestamp3 <= timestamp;
    }


    internal static void DetermineValueTypeAndSize(
        ref InputBindingCompositeContext context,
        int part,
        out Type valueType,
        out int valueSizeInBytes,
        out bool isButton)
    {
        valueSizeInBytes = 0;
        isButton = true;

        Type type = null;
        foreach (var control in context.controls)
        {
            if (control.part != part) continue;

            var controlType = control.control.valueType;
            if (type == null || controlType.IsAssignableFrom(type))
                type = controlType;
            else if (!type.IsAssignableFrom(controlType)) type = typeof(Object);

            valueSizeInBytes = Math.Max(control.control.valueSizeInBytes, valueSizeInBytes);

            // *All* bound controls need to be buttons for us to classify this part as a "Button" part.
            // we love internals
            // isButton &= control.control.isButton;
        }

        valueType = type;
    }

    protected override void FinishSetup(ref InputBindingCompositeContext context)
    {
        DetermineValueTypeAndSize(
            ref context,
            binding,
            out m_ValueType,
            out m_ValueSizeInBytes,
            out m_BindingIsButton);

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

    public override object ReadValueAsObject(ref InputBindingCompositeContext context)
    {
        if (context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2))
            return context.ReadValueAsObject(binding);
        return null;
    }
}
