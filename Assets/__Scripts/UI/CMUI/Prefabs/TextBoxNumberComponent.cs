using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextBoxNumberComponent : CMUIComponentWithLabel<float>, INavigable, IQuickSubmitComponent
{
    [SerializeField] private TMP_InputField inputField;

    private Action<float> onEndEdit;
    private Action<float> onSelect;
    private Action<float> onDeselect;

    [field: SerializeField] public Selectable Selectable { get; set; }

    public enum NumberClamping
    {
        None,
        Min,
        Max,
        Clamp
    }

    [Header("Input Validation")] public NumberClamping Clamping;
    public float MinValue;
    public float MaxValue;

    /// <summary>
    /// Assigns a callback when the user deselects the textbox after making changes.
    /// </summary>
    public TextBoxNumberComponent OnEndEdit(Action<float> onEndEdit)
    {
        this.onEndEdit = onEndEdit;
        return this;
    }

    /// <summary>
    /// Assigns a callback when the user selects text.
    /// </summary>
    public TextBoxNumberComponent OnSelect(Action<float> onSelect)
    {
        this.onSelect = onSelect;
        return this;
    }

    /// <summary>
    /// Assigns a callback when the user deselects text.
    /// </summary>
    public TextBoxNumberComponent OnDeselect(Action<float> onDeselect)
    {
        this.onDeselect = onDeselect;
        return this;
    }

    /// <summary>
    /// Restricts allowed characters to match certain types of content (such as numbers, email addresses, passwords, etc.)
    /// </summary>
    /// <param name="contentType">Content type to apply to this text box.</param>
    public TextBoxNumberComponent WithContentType(TMP_InputField.ContentType contentType)
    {
        inputField.contentType = contentType;
        return this;
    }

    /// <summary>
    /// Configures whether or not this textbox can support multiple lines of text.
    /// </summary>
    /// <param name="lineType">Line type to apply to this text box.</param>
    public TextBoxNumberComponent WithLineType(TMP_InputField.LineType lineType)
    {
        inputField.lineType = lineType;
        return this;
    }

    /// <summary>
    /// Sets the maximum character length for this textbox.
    /// </summary>
    /// <param name="characterLength">Maximum character length.</param>
    public TextBoxNumberComponent WithMaximumLength(int characterLength)
    {
        inputField.characterLimit = characterLength;
        return this;
    }

    /// <summary>
    /// Assigns an initial value.
    /// </summary>
    /// <param name="value"></param>
    public TextBoxNumberComponent WithInitialValue(float value)
    {
        inputField.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    private void Start()
    {
        OnValueUpdated(Value);
        inputField.onValueChanged.AddListener(InputFieldValueChanged);
        inputField.onEndEdit.AddListener(InputFieldEndEdit);
        inputField.onSelect.AddListener(InputFieldSelect);
        inputField.onDeselect.AddListener(InputFieldDeselect);
    }

    private void InputFieldValueChanged(string res)
    {
        if (ParseAndValidate(res, out var val)) Value = val;
    }

    private void InputFieldEndEdit(string res)
    {
        if (ParseAndValidate(res, out var val)) onEndEdit?.Invoke(val);
    }

    private void InputFieldSelect(string res)
    {
        if (ParseAndValidate(res, out var val)) onSelect?.Invoke(val);
    }

    private void InputFieldDeselect(string res)
    {
        if (ParseAndValidate(res, out var val)) onDeselect?.Invoke(val);
    }

    private bool ParseAndValidate(string res, out float val)
    {
        if (!float.TryParse(res, NumberStyles.Float, CultureInfo.InvariantCulture, out val)) return false;
        val = Clamping switch
        {
            NumberClamping.Min => Mathf.Max(MinValue, val),
            NumberClamping.Max => Mathf.Min(MaxValue, val),
            NumberClamping.Clamp => Mathf.Clamp(val, MinValue, MaxValue),
            _ => val
        };
        return true;
    }

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();
        inputField.onSelect.RemoveAllListeners();
        inputField.onDeselect.RemoveAllListeners();
    }

    protected override void OnValueUpdated(float updatedValue)
    {
        if (!inputField.isFocused) inputField.SetTextWithoutNotify(updatedValue.ToString(CultureInfo.InvariantCulture));
    }
}
