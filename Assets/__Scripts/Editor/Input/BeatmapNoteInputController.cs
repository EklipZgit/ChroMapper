using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeatmapNoteInputController : BeatmapInputController<NoteContainer>, CMInput.INoteObjectsActions
{
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private ArcAppearanceSO arcAppearance;
    [SerializeField] private ChainAppearanceSO chainAppearance;
    
    private static readonly Dictionary<int, int> cutDirectionMovedBackward = new()
    {
        { (int)NoteCutDirection.Any, (int)NoteCutDirection.Any },
        { (int)NoteCutDirection.DownLeft, (int)NoteCutDirection.Down },
        { (int)NoteCutDirection.Left, (int)NoteCutDirection.DownLeft },
        { (int)NoteCutDirection.UpLeft, (int)NoteCutDirection.Left },
        { (int)NoteCutDirection.Up, (int)NoteCutDirection.UpLeft },
        { (int)NoteCutDirection.UpRight, (int)NoteCutDirection.Up },
        { (int)NoteCutDirection.Right, (int)NoteCutDirection.UpRight },
        { (int)NoteCutDirection.DownRight, (int)NoteCutDirection.Right },
        { (int)NoteCutDirection.Down, (int)NoteCutDirection.DownRight },
        { (int)NoteCutDirection.None, (int)NoteCutDirection.None }
    };

    private static readonly Dictionary<int, int> cutDirectionMovedForward = new()
    {
        { (int)NoteCutDirection.Any, (int)NoteCutDirection.Any },
        { (int)NoteCutDirection.Down, (int)NoteCutDirection.DownLeft },
        { (int)NoteCutDirection.DownLeft, (int)NoteCutDirection.Left },
        { (int)NoteCutDirection.Left, (int)NoteCutDirection.UpLeft },
        { (int)NoteCutDirection.UpLeft, (int)NoteCutDirection.Up },
        { (int)NoteCutDirection.Up, (int)NoteCutDirection.UpRight },
        { (int)NoteCutDirection.UpRight, (int)NoteCutDirection.Right },
        { (int)NoteCutDirection.Right, (int)NoteCutDirection.DownRight },
        { (int)NoteCutDirection.DownRight, (int)NoteCutDirection.Down },
        { (int)NoteCutDirection.None, (int)NoteCutDirection.None }
    };

    public void OnUpdateNoteDirection(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed) return;

        var shiftForward = context.GetScrollDirection(Settings.Instance.InvertScrollNoteAngle);
        RaycastFirstObject(out var note);
        if (note != null) ScrollUpdateDirection(note, shiftForward);
    }

    public void OnUpdateNotePreciseDirection(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed) return;

        var shiftForward = context.GetScrollDirection(Settings.Instance.InvertScrollNoteAngle);
        RaycastFirstObject(out var note);
        if (note != null) ScrollPreciseUpdateDirection(note, shiftForward);
    }

    public void ScrollUpdateDirection(NoteContainer note, int direction)
    {
        var original = BeatmapFactory.Clone(note.ObjectData);
        note.NoteData.CutDirection =
            (direction > 0 ? cutDirectionMovedBackward : cutDirectionMovedForward)[note.NoteData.CutDirection];

        if (note.NoteData.CutDirection == (int)NoteCutDirection.Any && Settings.Instance.MapVersion >= 3) // janky!
        {
            note.NoteData.AngleOffset += direction > 0 ? 45 : -45;
            note.NoteData.AngleOffset = (int)Mathf.Repeat(note.NoteData.AngleOffset, 360);
        }

        BeatmapObjectContainerCollection
            .GetCollectionForType<NoteGridContainer>(ObjectType.Note)
            .RefreshSpecialAngles(note.ObjectData, false, false);

        var actions = new List<BeatmapAction>
        {
            new BeatmapObjectModifiedAction(
                note.ObjectData,
                note.ObjectData,
                original,
                "Update Note Direction",
                mergeType: ActionMergeType.NoteDirectionChange)
        };
        CommonNotePlacement.UpdateAttachedSlidersDirection(note.NoteData, actions);

        if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(
                    actions,
                    true,
                    true,
                    "Update Note Direction",
                    mergeType: ActionMergeType.NoteDirectionChange));
        }
        else
            BeatmapActionContainer.AddAction(actions[0]);
    }

    public void ScrollPreciseUpdateDirection(NoteContainer note, int direction)
    {
        // V2 note unsupported. Could implement either ME or NE for V2 note.
        if (Settings.Instance.MapVersion < 3) return;

        var original = BeatmapFactory.Clone(note.ObjectData);

        var prec = scrollPrecisionController.GetCurrentAngleOffsetPrecision();
        var value = (int)(Mathf.Round((note.NoteData.AngleOffset + (direction * prec)) * 1_000f) / 1_000f);
        note.NoteData.AngleOffset = (int)Mathf.Repeat(value, 360);

        BeatmapObjectContainerCollection
            .GetCollectionForType<NoteGridContainer>(ObjectType.Note)
            .RefreshSpecialAngles(note.ObjectData, false, false);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                note.ObjectData,
                note.ObjectData,
                original,
                mergeType: ActionMergeType.NotePreciseDirectionTweak));
    }
}
