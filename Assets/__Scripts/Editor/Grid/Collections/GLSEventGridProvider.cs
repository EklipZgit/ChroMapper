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
    public BaseEventBoxGroup LastContext;

    public void MarkRemove()
    {
        LastContext ??= groupContext;
        if (LastContext != null) groupContext = null;
        markRemove = true;
        enabled = true;
    }

    private void LateUpdate()
    {
        if (markRemove)
        {
            if (groupContext == null && editMode.EditingMode.HasFlag(EditingMode.EventBox))
                editMode.EditingMode = EditingMode.GLS;
            else
                RefreshTrack();

            LastContext = null;
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

            int p0;
            int p1;
            if (filter.Type == (int)IndexFilterType.Division)
            {
                p0 = box.IndexFilter.Param0;
                p1 = box.IndexFilter.Param1 + 1;
            }
            else
            {
                p0 = box.IndexFilter.Param0 + 1;
                p1 = box.IndexFilter.Param1;
            }

            label.SetText(
                $"[{i + 1}]\n\n{DistributionTypeToString(box.BeatDistributionType)}\n[{box.BeatDistribution}]\n\n{FilterTypeToString(filter.Type)}\n[{p0},{p1}]");

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
