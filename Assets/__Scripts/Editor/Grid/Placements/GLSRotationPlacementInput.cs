using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class GLSRotationPlacementInput : MonoBehaviour, CMInput.IGLSRotationPlacementActions
{
    public event Action<float> OnValueChanged;
    public event Action<float> OnValueDeltaChanged;
    public event Action<int> OnDirectionChanged;
    public event Action<int> OnLoopChanged;

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly bool[] heldKeys = { false, false, false, false };

    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    private bool flagDirectionsUpdate;

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }

    private bool diagonal;

    private void HandleDirectionValues()
    {
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

        switch (handleUpDownNotes)
        {
            // We handle simple up/down notes
            case true when !handleLeftRightNotes:
                OnValueChanged?.Invoke(upNote ? 0f : 180f);
                break;
            // We handle simple left/right notes
            case false when handleLeftRightNotes:
                OnValueChanged?.Invoke(leftNote ? 270f : 90f);
                break;
            default:
                {
                    if (diagonal) //We need to do a diagonal
                    {
                        if (leftNote)
                            OnValueChanged?.Invoke(upNote ? 315f : 225f);
                        else
                            OnValueChanged?.Invoke(upNote ? 45f : 135f);
                    }

                    break;
                }
        }
    }

    public void OnSetAngle0(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);
    public void OnSetAngle90(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);
    public void OnSetAngle180(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);
    public void OnSetAngle270(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    public void OnSetRotationDirectionLeft(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectionChanged?.Invoke((int)LightRotationDirection.CounterClockwise);
    }

    public void OnSetRotationDirectionAutomatic(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectionChanged?.Invoke((int)LightRotationDirection.Automatic);
    }

    public void OnSetRotationDirectionRight(InputAction.CallbackContext context)
    {
        if (context.performed) OnDirectionChanged?.Invoke((int)LightRotationDirection.Clockwise);
    }

    public void OnChangeLoopCount(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }

    public void OnResetLoopCount(InputAction.CallbackContext context)
    {
        if (context.performed) OnLoopChanged?.Invoke(0);
    }

    public void OnSetAnglePrecise(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueDeltaChanged?.Invoke(context.ReadValue<Vector2>().y);
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(0.3f);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeys)
            .Any())
            flagDirectionsUpdate = true;
    }
}
