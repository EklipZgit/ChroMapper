using System.Globalization;
using UnityEngine;

public class TextBoxFloatComponent : TextBoxNumberComponent<float>
{
    protected override bool ParseAndValidate(string res, out float val)
    {
        if (!float.TryParse(res, NumberStyles.Float, CultureInfo.InvariantCulture, out val)) return false;
        val = ValidateValue(val);
        return true;
    }

    protected override void OnValueUpdated(float updatedValue)
    {
        if (!InputField.isFocused) InputField.SetTextWithoutNotify(updatedValue.ToString(CultureInfo.InvariantCulture));
    }

    protected override float ValidateValue(float val) =>
        Clamping switch
        {
            NumberClamping.Min => Mathf.Max(MinValue, val),
            NumberClamping.Max => Mathf.Min(MaxValue, val),
            NumberClamping.Clamp => Mathf.Clamp(val, MinValue, MaxValue),
            _ => val
        };

    protected override float AddValue(float val, float delta) => val + (delta * ScrollDelta);
}
