using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleEnumPicker : EnumPicker<Toggle>
{
    [SerializeField] private Toggle[] toggles;

    private int i = 0;

    public override void CreateOptionForEnumValue(Enum enumValue)
    {
        var toggle = toggles[i];

        items.Add(enumValue, toggle);

        var colorBlock = toggle.colors;
        colorBlock.normalColor = normalColor;
        colorBlock.selectedColor = colorBlock.highlightedColor = colorBlock.pressedColor = selectedColor;
        toggle.colors = colorBlock;

        toggle.onValueChanged.AddListener((x) =>
        {
            if (Locked)
                return;

            // Delete is a toggleable placement mode; clicking its active icon again exits delete mode.
            if (!toggle.isOn)
            {
                if (enumValue.ToString() == "Delete")
                    OnEnumValueSelected(enumValue);
                return;
            }

            Select(toggle);
            OnEnumValueSelected(enumValue);
        });

        if (enumValue.ToString() == "Delete")
        {
            // Make the delete-mode shortcut discoverable on the trashcan tooltip.
            var tooltip = toggle.GetComponent<Tooltip>();
            // Unity components require their overloaded null comparison before adding a fallback.
            if (tooltip == null)
                tooltip = toggle.gameObject.AddComponent<Tooltip>();
            tooltip.TooltipOverride = "Delete mode";
            tooltip.AdvancedTooltip = "Toggle delete mode";
            tooltip.AppearDelay = 0.25f;
            tooltip.HotkeyActionMap = "Workflows";
            tooltip.HotkeyActionName = "Toggle Delete Tool";
        }

        // Poor man's for-loop
        i++;
    }

    // Enforce toggle state
    protected override void Select(Toggle selectedGraphic)
    {
        selectedGraphic.SetIsOnWithoutNotify(true);
        SetNormalColor(selectedGraphic, selectedColor);

        foreach (var toggle in toggles)
        {
            if (toggle != selectedGraphic)
            {
                toggle.SetIsOnWithoutNotify(false);
                SetNormalColor(toggle, normalColor);
            }
        }
    }

    private void SetNormalColor(Toggle toggle, Color color)
    {
        var colorBlock = toggle.colors;
        colorBlock.normalColor = color;
        toggle.colors = colorBlock;
    }
}
