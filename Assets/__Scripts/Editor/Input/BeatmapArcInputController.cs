using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V4;
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
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollArcMultiplier)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        ChangeMu(HoveredObject, modifier);
    }

    public void ChangeMu(ArcContainer s, float modifier)
    {
        var headControlPointLengthMultiplier = s.ArcData.HeadControlPointLengthMultiplier + modifier;

        ArcCommand.SetHeadControlPointLengthMultiplier(s.ArcData, headControlPointLengthMultiplier);
    }

    public void OnChangingTmu(InputAction.CallbackContext context)
    {
        if (!context.performed || !IsHovering || HoveredObject.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollArcMultiplier)
            * scrollPrecisionController.GetCurrentMultiplierPrecision();
        ChangeTmu(HoveredObject, modifier);
    }

    public void ChangeTmu(ArcContainer s, float modifier)
    {
        var tailControlPointLengthMultiplier = s.ArcData.TailControlPointLengthMultiplier + modifier;

        ArcCommand.SetTailControlPointLengthMultiplier(s.ArcData, tailControlPointLengthMultiplier);
    }
}
