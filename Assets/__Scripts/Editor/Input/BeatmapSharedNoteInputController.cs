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
            NoteCommand.InvertColor(beatmapNoteInputController.HoveredObject.NoteData);
            return;
        }
            
        if (beatmapArcInputController.IsHovering)
        {
            SliderCommand.InvertColor(beatmapArcInputController.HoveredObject.ArcData);
            return;
        }
            
        if (beatmapChainInputController.IsHovering)
        {
            SliderCommand.InvertColor(beatmapChainInputController.HoveredObject.ChainData);
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
        {
            if (beatmapNoteInputController.IsHovering)
            {
                var note = beatmapNoteInputController.HoveredObject;
                if (note.ObjectData is not BaseNote noteData) return;
        
                NoteCommand.SetCutDirection(noteData, (int)cutDirection);
            }
        }
        else
            OnCutDirectionChanged?.Invoke((int)cutDirection);
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
