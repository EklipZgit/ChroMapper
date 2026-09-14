using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GLSColorShiftArrayViewController : MonoBehaviour
{
    [SerializeField] private GLSColorShiftRowView rowTemplate;
    [SerializeField] private Transform listRoot;
    [SerializeField] private ButtonComponent addButton;

    private Action<string[]> onShiftsChanged;
    private bool suppressNotify;
    private readonly List<GLSColorShiftRowView> rows = new();

    private static readonly string[] TargetTokens = { "h", "s", "v", "r", "g", "b", "f" };
    private static readonly string[] TargetDisplay = { "Hue", "Sat", "Val", "Red", "Green", "Blue", "Bright/Alpha" };
    private static readonly Dictionary<string, int> TargetTokenToIndex = new(StringComparer.OrdinalIgnoreCase)
    {
        { "h", 0 }, { "s", 1 }, { "v", 2 }, { "r", 3 }, { "g", 4 }, { "b", 5 }, { "f", 6 },
    };

    private static readonly string[] EasingTokens =
    {
        "lin", "iq", "oq", "ioq", "ic", "oc", "ioc", "iqt", "oqt", "ioqt", "iqn", "oqn", "ioqn",
        "is", "os", "ios", "ie", "oe", "ioe", "icr", "ocr", "iocr", "ib", "ob", "iob", "iel", "oel", "ioel", "ibo", "obo", "iobo", "step"
    };
    private static readonly string[] EasingDisplay =
    {
        "Linear", "In Quad", "Out Quad", "InOut Quad", "In Cubic", "Out Cubic", "InOut Cubic",
        "In Quart", "Out Quart", "InOut Quart", "In Quint", "Out Quint", "InOut Quint",
        "In Sine", "Out Sine", "InOut Sine", "In Expo", "Out Expo", "InOut Expo",
        "In Circ", "Out Circ", "InOut Circ", "In Back", "Out Back", "InOut Back",
        "In Elastic", "Out Elastic", "InOut Elastic", "In Bounce", "Out Bounce", "InOut Bounce", "Step"
    };
    private static readonly Dictionary<string, int> EasingTokenToIndex = new(StringComparer.OrdinalIgnoreCase)
    {
        { "lin", 0 }, { "iq", 1 }, { "oq", 2 }, { "ioq", 3 }, { "ic", 4 }, { "oc", 5 }, { "ioc", 6 },
        { "iqt", 7 }, { "oqt", 8 }, { "ioqt", 9 }, { "iqn", 10 }, { "oqn", 11 }, { "ioqn", 12 },
        { "is", 13 }, { "os", 14 }, { "ios", 15 }, { "ie", 16 }, { "oe", 17 }, { "ioe", 18 },
        { "icr", 19 }, { "ocr", 20 }, { "iocr", 21 }, { "ib", 22 }, { "ob", 23 }, { "iob", 24 },
        { "iel", 25 }, { "oel", 26 }, { "ioel", 27 }, { "ibo", 28 }, { "obo", 29 }, { "iobo", 30 }, { "step", 31 },

        { "easeLinear", 0 }, { "easeInQuad", 1 }, { "easeOutQuad", 2 }, { "easeInOutQuad", 3 },
        { "easeInCubic", 4 }, { "easeOutCubic", 5 }, { "easeInOutCubic", 6 }, { "easeInQuart", 7 }, { "easeOutQuart", 8 }, { "easeInOutQuart", 9 },
        { "easeInQuint", 10 }, { "easeOutQuint", 11 }, { "easeInOutQuint", 12 }, { "easeInSine", 13 }, { "easeOutSine", 14 }, { "easeInOutSine", 15 },
        { "easeInExpo", 16 }, { "easeOutExpo", 17 }, { "easeInOutExpo", 18 }, { "easeInCirc", 19 }, { "easeOutCirc", 20 }, { "easeInOutCirc", 21 },
        { "easeInBack", 22 }, { "easeOutBack", 23 }, { "easeInOutBack", 24 }, { "easeInElastic", 25 }, { "easeOutElastic", 26 }, { "easeInOutElastic", 27 },
        { "easeInBounce", 28 }, { "easeOutBounce", 29 }, { "easeInOutBounce", 30 }, { "easeStep", 31 },
    };

    private void Awake()
    {
        if (addButton != null)
        {
            addButton.OnClick(HandleAddClicked);
        }
        
        if (rowTemplate != null)
        {
            rowTemplate.gameObject.SetActive(false);
        }
    }

    public void Initialize(Action<string[]> onChanged)
    {
        onShiftsChanged = onChanged;
    }

    public void SetShifts(string[] shifts)
    {
        suppressNotify = true;
        try
        {
            ClearRows();
            if (shifts != null)
            {
                foreach (var s in shifts)
                {
                    if (TryParseShiftString(s, out var targetIdx, out var offset, out var easingIdx, out var isLight))
                    {
                        CreateRow(targetIdx, offset, easingIdx, isLight);
                    }
                    else
                    {
                        Debug.LogWarning($"[GLSColorShiftArrayView] Skipping invalid shift entry: '{s}'");
                    }
                }
            }
        }
        finally
        {
            suppressNotify = false;
        }
    }

    public string[] GetShifts() => rows.Select(BuildString).ToArray();

    private void ClearRows()
    {
        foreach (var r in rows)
        {
            if (r != null) Destroy(r.gameObject);
        }
        rows.Clear();
    }

    private void HandleAddClicked()
    {
        CreateRow(0, 0f, 0, false);
        NotifyChanged();
    }

    private void CreateRow(int targetIdx, float offset, int easingIdx, bool isLight)
    {
        if (rowTemplate == null || listRoot == null)
        {
            Debug.LogError("[GLSColorShiftArrayView] rowTemplate/listRoot not wired. No auto-creation.");
            return;
        }

        var row = Instantiate(rowTemplate, listRoot);
        row.gameObject.SetActive(true);
        row.name = $"ShiftRow_{rows.Count}";

        var targetDD = row.TargetDropdown;
        var valInput = row.ValueInput;
        var easingDD = row.EasingDropdown;
        var lightTog = row.LightToggle;
        var remBtn = row.RemoveButton;

        if (targetDD != null)
        {
            targetDD.WithOptions(TargetDisplay).WithLabel("Target");
            targetDD.SetValueWithoutNotify(Mathf.Clamp(targetIdx, 0, TargetDisplay.Length - 1));
            targetDD.OnValueChanged(_ => { if (!suppressNotify) NotifyChanged(); });
        }
        if (valInput != null)
        {
            valInput.WithLabel("Value");
            valInput.SetValueWithoutNotify(offset);
            valInput.OnValueChanged(_ => { if (!suppressNotify) NotifyChanged(); });
            valInput.OnEndEdit(_ => { if (!suppressNotify) NotifyChanged(); });
        }
        if (easingDD != null)
        {
            easingDD.WithOptions(EasingDisplay).WithLabel("Ease");
            easingDD.SetValueWithoutNotify(Mathf.Clamp(easingIdx, 0, EasingDisplay.Length - 1));
            easingDD.OnValueChanged(_ => { if (!suppressNotify) NotifyChanged(); });
        }
        if (lightTog != null)
        {
            lightTog.WithLabel("Light");
            lightTog.SetValueWithoutNotify(isLight);
            lightTog.OnValueChanged(_ => { if (!suppressNotify) NotifyChanged(); });
        }
        if (remBtn != null)
        {
            remBtn.WithLabel("X");
            remBtn.OnClick(() =>
            {
                rows.Remove(row);
                Destroy(row.gameObject);
                if (!suppressNotify) NotifyChanged();
            });
        }

        rows.Add(row);
    }

    private void NotifyChanged()
    {
        if (suppressNotify) return;
        var arr = GetShifts();
        onShiftsChanged?.Invoke(arr);
    }

    private string BuildString(GLSColorShiftRowView row)
    {
        var target = TargetTokens[Mathf.Clamp(row.TargetDropdown != null ? row.TargetDropdown.Value : 0, 0, TargetTokens.Length - 1)];
        var offset = row.ValueInput != null ? row.ValueInput.Value : 0f;
        var easing = EasingTokens[Mathf.Clamp(row.EasingDropdown != null ? row.EasingDropdown.Value : 0, 0, EasingTokens.Length - 1)];
        var isLight = row.LightToggle != null && row.LightToggle.Value;
        var offsetStr = offset.ToString(CultureInfo.InvariantCulture);
        return isLight ? $"{target},{offsetStr},{easing},l" : $"{target},{offsetStr},{easing}";
    }

    private static bool TryParseShiftString(string raw, out int targetIdx, out float offset, out int easingIdx, out bool isLight)
    {
        targetIdx = 0;
        offset = 0f;
        easingIdx = 0;
        isLight = false;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var parts = raw.Split(',');
        if (parts.Length < 3) return false;

        var targetPart = parts[0]?.Trim() ?? string.Empty;
        bool found = false;
        foreach (var ch in targetPart)
        {
            var key = ch.ToString().ToLowerInvariant();
            if (TargetTokenToIndex.TryGetValue(key, out var idx))
            {
                targetIdx = idx;
                found = true;
                break;
            }
        }
        if (!found) return false;
        if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out offset)) return false;
        if (float.IsNaN(offset) || float.IsInfinity(offset)) return false;
        var easingPart = parts[2].Trim();
        if (!EasingTokenToIndex.TryGetValue(easingPart, out easingIdx))
        {
            easingIdx = 0;
        }
        if (parts.Length >= 4)
        {
            isLight = string.Equals(parts[3].Trim(), "l", StringComparison.OrdinalIgnoreCase);
        }
        return true;
    }
}
