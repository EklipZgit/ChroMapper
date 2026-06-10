using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapSharedNoteInputController : MonoBehaviour, CMInput.ISharedNoteObjectsActions
{
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private ArcAppearanceSO arcAppearance;
    [SerializeField] private ChainAppearanceSO chainAppearance;
    
    [SerializeField] private BeatmapNoteInputController beatmapNoteInputController;
    [SerializeField] private BeatmapArcInputController beatmapArcInputController;
    [SerializeField] private BeatmapChainInputController beatmapChainInputController;

    public event Action<int> OnCutDirectionChanged;

    public void OnInvertNoteColors(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (beatmapNoteInputController.IsHovering)
        {
            InvertNote(beatmapNoteInputController.HoveredObject);
            return;
        }
            
        if (beatmapArcInputController.IsHovering)
        {
            InvertArc(beatmapArcInputController.HoveredObject);  
            return;
        }
            
        if (beatmapChainInputController.IsHovering)
        {
            InvertChain(beatmapChainInputController.HoveredObject);
            return;
        }
    }

    public void OnUpNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);
    public void OnDownNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);
    public void OnRightNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);
    public void OnLeftNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectNoteDirectionPerformed(NoteCutDirection.Any);
    }
    
    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectNoteDirectionPerformed(NoteCutDirection.UpLeft);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectNoteDirectionPerformed(NoteCutDirection.UpRight);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectNoteDirectionPerformed(NoteCutDirection.DownRight);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectNoteDirectionPerformed(NoteCutDirection.DownLeft);
    }
    
    private void OnDirectNoteDirectionPerformed(NoteCutDirection cutDirection)
    {
        if (KeybindsController.IsHoverKeyHeld && Settings.Instance.QuickNoteEditing)
            UpdateHoverNoteDirection((int)cutDirection);
        else
            OnCutDirectionChanged?.Invoke((int)cutDirection);
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

    public void InvertArc(ArcContainer arc)
    {
        var original = BeatmapFactory.Clone(arc.ArcData);
        var newType = arc.ArcData.Color == (int)NoteColor.Red
            ? (int)NoteColor.Blue
            : (int)NoteColor.Red;
        arc.ArcData.Color = newType;
        arcAppearance.SetArcAppearance(arc);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(arc.ObjectData, arc.ObjectData, original, "invert arc color"));
    }
    
    public void InvertChain(ChainContainer chain)
    {
        var original = BeatmapFactory.Clone(chain.ObjectData);
        var newType = chain.ChainData.Color == (int)NoteColor.Red
            ? (int)NoteColor.Blue
            : (int)NoteColor.Red;
        chain.ChainData.Color = newType;
        chainAppearance.SetChainAppearance(chain);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(chain.ObjectData, chain.ObjectData, original, "invert chain color"));
    }
    
    private void UpdateHoverNoteDirection(int value)
    {
        if (!beatmapNoteInputController.IsHovering) return;
        
        // TODO: Move the below to a command?
        var note = beatmapNoteInputController.HoveredObject;
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
    
    private void ToggleDiagonalAngleOffset(BaseNote note, int newCutDirection)
    {
        if (note.CutDirection == (int)NoteCutDirection.Any
            && newCutDirection == (int)NoteCutDirection.Any
            && note.AngleOffset != 45)
            note.AngleOffset = 45;
        else
            note.AngleOffset = 0;
    }
    
    #region Handling CutDirection From Input
    
    private readonly float
        diagonalStickMaxTime = 0.3f; // This controls the maximum time that a note will stay a diagonal

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly List<bool> heldKeys = new() { false, false, false, false };

    private bool diagonal;
    private bool flagDirectionsUpdate;
    private bool updateAttachedSliderDirection;
    
    public void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }
    
    protected void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }
    
    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    private void HandleDirectionValues()
    {
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

        // Get cut direction
        NoteCutDirection? cutDirection = null;
        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote)
                cutDirection = NoteCutDirection.Up;
            else
                cutDirection = NoteCutDirection.Down;
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote)
                cutDirection = NoteCutDirection.Left;
            else
                cutDirection = NoteCutDirection.Right;
        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote)
                    cutDirection = NoteCutDirection.UpLeft;
                else
                    cutDirection = NoteCutDirection.DownLeft;
            }
            else
            {
                if (upNote)
                    cutDirection = NoteCutDirection.UpRight;
                else
                    cutDirection = NoteCutDirection.DownRight;
            }
        }

        // Now actually do something with it
        if (cutDirection != null) OnDirectNoteDirectionPerformed(cutDirection.Value);
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

    #endregion
}
