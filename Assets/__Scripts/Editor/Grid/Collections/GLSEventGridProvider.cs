using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

public class GLSEventGridProvider : MonoBehaviour
{
    public event Action<BaseEventBoxGroup> OnGroupChanged;

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

            OnGroupChanged?.Invoke(groupContext);
        }
    }

    private bool markRemove;
    private BaseEventBoxGroup lastContext;

    public void MarkRemove()
    {
        lastContext ??= groupContext;
        markRemove = true;
        enabled = true;
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

            lastContext = null;
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

        var boxes = groupContext switch
        {
            BaseLightColorEventBoxGroup lcebg => lcebg.Boxes.Cast<BaseEventBox>().ToList(),
            BaseLightRotationEventBoxGroup lrebg => lrebg.Boxes.Cast<BaseEventBox>().ToList(),
            BaseLightTranslationEventBoxGroup ltebg => ltebg.Boxes.Cast<BaseEventBox>().ToList(),
            BaseVfxEventEventBoxGroup veebg => veebg.Boxes.Cast<BaseEventBox>().ToList(),
            _ => Enumerable.Empty<BaseEventBox>().ToList()
        };

        gridLane.Lane = boxes.Count;

        while (usedLabels.TryPop(out var label))
        {
            label.enabled = false;
            reuseLabels.Push(label);
        }

        for (var i = 0; i < boxes.Count; i++)
        {
            if (!reuseLabels.TryPop(out var label)) label = Instantiate(labelPrefab, targetCanvas);
            usedLabels.Push(label);

            var pos = label.rectTransform.localPosition;
            pos.x = i;
            label.rectTransform.localPosition = pos;

            var box = boxes[i];
            var filter = box.IndexFilter;
            label.SetText(
                $"[{i + 1}]\n\n{DistributionTypeToString(box.BeatDistributionType)}\n[{box.BeatDistribution}]\n\n{FilterTypeToString(filter.Type)}\n[{filter.Param0},{filter.Param1}]");

            label.enabled = true;
        }
    }

    private string FilterTypeToString(int t)
    {
        return t switch
        {
            (int)IndexFilterType.Division => "Section",
            (int)IndexFilterType.StepAndOffset => "Step",
            _ => "???"
        };
    }

    private string DistributionTypeToString(int t)
    {
        return t switch
        {
            (int)DistributionType.Wave => "Wave",
            (int)DistributionType.Step => "Step",
            _ => "???"
        };
    }
}
