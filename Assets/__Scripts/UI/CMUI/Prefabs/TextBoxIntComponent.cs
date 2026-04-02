using System;
using System.Globalization;
using UnityEngine;

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

    protected override int ValidateValue(int val)
    {
        val = Clamping switch
        {
            NumberClamping.Min => Math.Max(MinValue, val),
            NumberClamping.Max => Math.Min(MaxValue, val),
            NumberClamping.Clamp => Math.Clamp(val, MinValue, MaxValue),
            _ => val
        };

        if (LoopAround) val = (int)Mathf.Repeat(val, LoopThreshold);
        return val;
    }

    protected override int AddValue(int val, float delta) => (int)(val + (delta * GetPrecisionValue()));
}
