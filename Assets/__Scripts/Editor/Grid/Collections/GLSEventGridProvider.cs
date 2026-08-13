using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beatmap.Base;
using Beatmap.Enums;
using TMPro;
using UnityEngine;

public class GLSEventGridProvider : MonoBehaviour
{
    public event Action<BaseEventBoxGroup> OnGroupChanged;

    [SerializeField] private EditModeContext editMode;
    [SerializeField] private GridLane gridLane;
    [SerializeField] private AudioTimeSyncController atsc;

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
        // Notify inner GLS collections when an outer group retires so stale child nodes cannot recreate a deleted parent.
        if (LastContext != null)
        {
            Debug.Log(
                $"[GLSGroupContext] Retiring group id={LastContext.ID} beat={LastContext.JsonTime} " +
                $"type={LastContext.GetType().Name}.");
            GroupContext = null;
        }
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
            atsc.VisualBeatOrigin = 0;
            return;
        }

        var boxes = groupContext.ReadOnlyBoxes;
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

            var sb = new StringBuilder();

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

            sb.AppendLine($"[{i + 1}]");
            sb.AppendLine();
            sb.AppendLine(DistributionTypeToString(box.BeatDistributionType));
            sb.AppendLine($"[{box.BeatDistribution}]");
            sb.AppendLine();
            sb.AppendLine(FilterTypeToString(filter.Type));
            sb.AppendLine($"[{p0},{p1}]");
            switch (box)
            {
                case BaseLightRotationEventBox lreb:
                    sb.AppendLine();
                    sb.Append(((Axis)lreb.Axis).ToString());
                    break;
                case BaseLightTranslationEventBox lteb:
                    sb.AppendLine();
                    sb.Append(((Axis)lteb.Axis).ToString());
                    break;
            }

            label.SetText(sb.ToString());

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
