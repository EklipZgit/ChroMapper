using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

public class GLSEventGridProvider : MonoBehaviour
{
    [SerializeField] private EditModeContext editMode;
    [SerializeField] private GridLane gridLane;

    [Header("Prefab")] [SerializeField] private TextMeshProUGUI labelPrefab;
    [SerializeField] private RectTransform targetCanvas;

    private readonly Stack<TextMeshProUGUI> reuseLabels = new();
    private readonly Stack<TextMeshProUGUI> usedLabels = new();
    private BaseEventBoxGroup groupContext;

    public BaseEventBoxGroup GroupContext
    {
        get => groupContext;
        set
        {
            if (groupContext == value) return;
            groupContext = value;
            RefreshTrack();
        }
    }

    private bool markRemove;
    private BaseEventBoxGroup lastContext;

    public bool MarkRemove
    {
        get => markRemove;
        set
        {
            markRemove = value;
            lastContext ??= groupContext;
            enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (markRemove)
        {
            if (groupContext == lastContext)
            {
                groupContext = lastContext = null;
                if (editMode.EditingMode.HasFlag(EditingMode.EventBox)) editMode.EditingMode = EditingMode.GLS;
            }
            else
                RefreshTrack();

            markRemove = false;
        }

        enabled = false;
    }

    private void RefreshTrack()
    {
        if (groupContext == null)
        {
            gridLane.Lane = 0;
            return;
        }

        gridLane.Lane = groupContext.AbstractBoxes.Count;

        while (usedLabels.TryPop(out var label))
        {
            label.enabled = false;
            reuseLabels.Push(label);
        }

        for (var i = 0; i < groupContext.AbstractBoxes.Count; i++)
        {
            if (!reuseLabels.TryPop(out var label)) label = Instantiate(labelPrefab, targetCanvas);
            usedLabels.Push(label);

            var pos = label.rectTransform.localPosition;
            pos.x = i;
            label.rectTransform.localPosition = pos;

            var box = groupContext.AbstractBoxes[i];
            var filter = box.IndexFilter;
            label.SetText(
                $"[{i + 1}]\n\n{(DistributionType)box.BeatDistributionType}\n[{box.BeatDistribution}]\n\n{(IndexFilterType)filter.Type}\n[{filter.Param0},{filter.Param1}]");

            label.enabled = true;
        }
    }
}
