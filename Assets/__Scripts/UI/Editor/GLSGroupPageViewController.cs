using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GLSGroupPageViewController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;

    [SerializeField] private ButtonComponent textPrefab;
    [SerializeField] private RectTransform targetTransform;

    private readonly List<ButtonComponent> loadedText = new();
    private readonly Dictionary<string, ButtonComponent> groupToText = new();

    private void Start()
    {
        beatmapRuntimeContext.OnTracksDefinitionChanged += HandleTracksDefinitionChanged;
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        glsGroupGridProvider.OnGroupPageChanged += HandleGroupPageChanged;

        HandleEditModeChanged(editModeContext.EditingMode);
    }

    private void OnDestroy()
    {
        beatmapRuntimeContext.OnTracksDefinitionChanged -= HandleTracksDefinitionChanged;
        editModeContext.OnEditModeChanged -= HandleEditModeChanged;
        glsGroupGridProvider.OnGroupPageChanged -= HandleGroupPageChanged;
    }

    private void HandleTracksDefinitionChanged(TracksDefinitionSO td)
    {
        foreach (var text in loadedText) Destroy(text.gameObject);
        loadedText.Clear();
        groupToText.Clear();

        foreach (var n in td.Gls.Values.Select(g => g.Group).Distinct())
        {
            var text = Instantiate(textPrefab, targetTransform);
            text.name = n;
            text.SetLabelText(n);
            text.OnClick(() => glsGroupGridProvider.SetGroupPage(n));
            text.gameObject.SetActive(true);
            loadedText.Add(text);
            groupToText.Add(n, text);
        }

        HandleGroupPageChanged(glsGroupGridProvider.CurrentGroup);
    }

    private void HandleEditModeChanged(EditingMode mode)
    {
        canvasGroup.alpha = mode.HasFlag(EditingMode.GLS) ? 1 : 0;
        canvasGroup.blocksRaycasts = mode.HasFlag(EditingMode.GLS);
    }

    private void HandleGroupPageChanged(string group)
    {
        foreach (var t in loadedText) t.SetLabelColor(new(0.25f, 0.25f, 0.25f));
        if (!groupToText.TryGetValue(glsGroupGridProvider.CurrentGroup, out var text)) return;
        text.SetLabelColor(Color.white);
    }
}
