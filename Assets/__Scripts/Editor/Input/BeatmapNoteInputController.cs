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

    private readonly float
        diagonalStickMaxTime = 0.3f; // This controls the maximum time that a note will stay a diagonal

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly List<bool> heldKeys = new() { false, false, false, false };

    private bool diagonal;
    private bool flagDirectionsUpdate;
    private bool updateAttachedSliderDirection;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }

    //Do some shit later lmao
    public void OnInvertNoteColors(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsMouseInWindow
            || !context.performed)
            return;

        RaycastFirstObject(out var note);
        if (note != null && !note.Dragged) InvertNote(note);
    }

    public void OnQuickUpDirectionModifier(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);

    public void OnQuickDownDirectionModifier(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);

    public void OnQuickLeftDirectionModifier(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    public void OnQuickRightDirectionModifier(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);

    public void OnQuickAnyDirectionModifier(InputAction.CallbackContext context)
    {
        if (!Settings.Instance.QuickNoteEditing) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsMouseInWindow
            || !context.performed)
            return;

        RaycastFirstObject(out var note);
        if (note != null && !note.Dragged) UpdateDirection(note, (int)NoteCutDirection.Any);
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

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

    public void InvertNote(NoteContainer note)
    {
        if (note.NoteData.Type == (int)NoteType.Bomb) return;

        var original = BeatmapFactory.Clone(note.ObjectData);
        var newType = note.NoteData.Type == (int)NoteType.Red
            ? (int)NoteType.Blue
            : (int)NoteType.Red;
        note.NoteData.Type = newType;
        noteAppearance.SetNoteAppearance(note);
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
        collection.RefreshSpecialAngles(note.ObjectData, false, false);
        collection.RefreshSpecialAngles(original, false, false);

        var actions =
            new List<BeatmapAction> { new BeatmapObjectModifiedAction(note.ObjectData, note.ObjectData, original) };
        InvertAttachedSliders(note, actions);

        BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, true, "Note inversion"));
    }

    private void InvertAttachedSliders(NoteContainer note, ICollection<BeatmapAction> actions)
    {
        var noteData = note.NoteData;
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var arcData = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(arcData.JsonTime - noteData.JsonTime) < epsilon
                && arcData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(arcData.TailJsonTime - noteData.JsonTime) < epsilon
                && arcData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead || isConnectedToTail)
            {
                var arcOriginal = BeatmapFactory.Clone(arcData);
                arcData.Color = noteData.Color;
                arcAppearance.SetArcAppearance(arcContainer.Value as ArcContainer);

                actions.Add(new BeatmapObjectModifiedAction(arcData, arcData, arcOriginal));
            }
        }

        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var chainData = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(chainData.JsonTime - noteData.JsonTime) < epsilon
                && chainData.GetPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var chainOriginal = BeatmapFactory.Clone(chainData);
                chainData.Color = noteData.Color;
                chainAppearance.SetChainAppearance(chainContainer.Value as ChainContainer);

                actions.Add(new BeatmapObjectModifiedAction(chainData, chainData, chainOriginal));
            }
        }
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

        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        var value = (int)(Mathf.Round((note.NoteData.AngleOffset + (direction * prec)) * 1_000f) / 1_000f);
        note.NoteData.AngleOffset += value;
        note.NoteData.AngleOffset = (int)Mathf.Repeat(note.NoteData.AngleOffset, 360);

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

    public void UpdateDirection(NoteContainer note, int value)
    {
        if (note.ObjectData is not BaseNote noteData) return;
        var originalData = BeatmapFactory.Clone(noteData);
        ToggleDiagonalAngleOffset(noteData, value);
        noteData.CutDirection = value;

        var actions = new List<BeatmapAction>
        {
            new BeatmapObjectModifiedAction(
                noteData,
                noteData,
                originalData,
                "Quick edit",
                true,
                ActionMergeType.NoteDirectionChange)
        };
        CommonNotePlacement.UpdateAttachedSlidersDirection(noteData, actions);

        if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(
                    actions,
                    true,
                    false,
                    "Quick edit",
                    ActionMergeType.NoteDirectionChange),
                true);
            SelectionController.OnSelectionChanged?.Invoke();
        }
        else
            BeatmapActionContainer.AddAction(actions[0], true);
    }

    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    private void HandleDirectionValues()
    {
        if (!Settings.Instance.QuickNoteEditing) return;
        DeleteToolController.UpdateDeletion(false);

        var upNote = heldKeys[upKey];
        var downNote = heldKeys[downKey];
        var leftNote = heldKeys[leftKey];
        var rightNote = heldKeys[rightKey];
        var previousDiagonalState = diagonal;

        var handleUpDownNotes = upNote ^ downNote; // XOR: True if the values are different, false if the same
        var handleLeftRightNotes = leftNote ^ rightNote;

        diagonal = handleUpDownNotes && handleLeftRightNotes;

        if (previousDiagonalState && !diagonal)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }
        
        RaycastFirstObject(out var note);
        if (note == null || note.Dragged) return;
        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote)
                UpdateDirection(note, (int)NoteCutDirection.Up);
            else
                UpdateDirection(note, (int)NoteCutDirection.Down);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote)
                UpdateDirection(note, (int)NoteCutDirection.Left);
            else
                UpdateDirection(note, (int)NoteCutDirection.Right);
        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote)
                    UpdateDirection(note, (int)NoteCutDirection.UpLeft);
                else
                    UpdateDirection(note, (int)NoteCutDirection.DownLeft);
            }
            else
            {
                if (upNote)
                    UpdateDirection(note, (int)NoteCutDirection.UpRight);
                else
                    UpdateDirection(note, (int)NoteCutDirection.DownRight);
            }
        }
    }

    private void ToggleDiagonalAngleOffset(BaseNote note, int newCutDirection)
    {
        if (note.CutDirection == (int)NoteCutDirection.Any
            && newCutDirection == (int)NoteCutDirection.Any
            && note.AngleOffset != 45)
            note.AngleOffset = 45;
        else
            note.AngleOffset = 0;
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(diagonalStickMaxTime);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeys)
            .Any())
            flagDirectionsUpdate = true;
    }
}
