using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapArcInputController : BeatmapInputController<ArcContainer>, CMInput.IArcObjectsActions
{
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private ArcAppearanceSO arcAppearance;

    public void OnChangingMu(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged || !context.performed) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollArcMultiplier)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        ChangeMu(e, modifier);
    }

    public void ChangeMu(ArcContainer s, float modifier)
    {
        var original = BeatmapFactory.Clone(s.ArcData);
        s.ChangeHeadMultiplier(modifier);
        arcAppearance.SetText(s);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                s.ObjectData,
                s.ObjectData,
                original,
                mergeType: ActionMergeType.ArcHeadMultTweak));
    }

    public void OnChangingTmu(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged || !context.performed) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollArcMultiplier)
            * scrollPrecisionController
                .GetCurrentMultiplierPrecision();
        ChangeTmu(e, modifier);
    }

    public void ChangeTmu(ArcContainer s, float modifier)
    {
        var original = BeatmapFactory.Clone(s.ArcData);
        s.ChangeTailMultiplier(modifier);
        arcAppearance.SetText(s);
        s.NotifySplineChanged();
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                s.ObjectData,
                s.ObjectData,
                original,
                mergeType: ActionMergeType.ArcTailMultTweak));
    }
}
