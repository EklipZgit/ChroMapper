using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextBoxIntComponent : TextBoxNumberComponent<int>
{
    protected override bool ParseAndValidate(string res, out int val)
    {
        if (!int.TryParse(res, NumberStyles.Integer, CultureInfo.InvariantCulture, out val)) return false;
        val = Clamping switch
        {
            NumberClamping.Min => Math.Max(MinValue, val),
            NumberClamping.Max => Math.Min(MaxValue, val),
            NumberClamping.Clamp => Math.Clamp(val, MinValue, MaxValue),
            _ => val
        };
        return true;
    }

    protected override void OnValueUpdated(int updatedValue)
    {
        if (!InputField.isFocused) InputField.SetTextWithoutNotify(updatedValue.ToString(CultureInfo.InvariantCulture));
    }
}
