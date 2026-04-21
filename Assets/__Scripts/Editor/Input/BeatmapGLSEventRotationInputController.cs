using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventRotationInputController : BeatmapGLSEventInputController<BaseLightRotationBase>,
                                                      CMInput.IGLSRotationObjectsActions
{
    public event Action<float> OnValueChanged;
    public event Action<int> OnDirectionChanged;
    public event Action<int> OnLoopChanged;

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly bool[] heldKeys = { false, false, false, false };
    private readonly bool[] heldKeysHover = { false, false, false, false };

    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    private bool flagDirectionsUpdate;
    private bool flagHoverDirectionsUpdate;

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void HandleKeyHoverUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagHoverDirectionsUpdate = true;
        heldKeysHover[id] = context.performed;
    }

    protected override void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }

        if (flagHoverDirectionsUpdate)
        {
            HandleHoverDirectionValues();
            flagHoverDirectionsUpdate = false;
        }
    }

    private bool diagonal;
    private bool hoverDiagonal;

    private void HandleDirectionValues()
    {
        var up = heldKeys[upKey];
        var down = heldKeys[downKey];
        var left = heldKeys[leftKey];
        var right = heldKeys[rightKey];
        var previousDiagonalState = diagonal;

        var upDown = up ^ down; // XOR: True if the values are different, false if the same
        var leftRight = left ^ right;

        diagonal = upDown && leftRight;

        if (previousDiagonalState && !diagonal)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }

        switch (upDown)
        {
            case true when !leftRight:
                NotifyValueChanged(up ? 0f : 180f);
                break;
            case false when leftRight:
                NotifyValueChanged(left ? 270f : 90f);
                break;
            default:
                {
                    if (diagonal)
                    {
                        if (left)
                            NotifyValueChanged(up ? 315f : 225f);
                        else
                            NotifyValueChanged(up ? 45f : 135f);
                    }

                    break;
                }
        }
    }

    private void HandleHoverDirectionValues()
    {
        if (!IsHovering) return;

        var up = heldKeysHover[upKey];
        var down = heldKeysHover[downKey];
        var left = heldKeysHover[leftKey];
        var right = heldKeysHover[rightKey];
        var previousDiagonalState = hoverDiagonal;

        var upDown = up ^ down; // XOR: True if the values are different, false if the same
        var leftRight = left ^ right;

        hoverDiagonal = upDown && leftRight;

        if (previousDiagonalState && !hoverDiagonal)
        {
            StartCoroutine(CheckForDiagonalHoverUpdate());
            return;
        }

        switch (upDown)
        {
            case true when !leftRight:
                GLSEventRotationCommand.SetValue(HoveredObject.EventData as BaseLightRotationBase, up ? 0f : 180f);
                break;
            case false when leftRight:
                GLSEventRotationCommand.SetValue(HoveredObject.EventData as BaseLightRotationBase, left ? 270f : 90f);
                break;
            default:
                {
                    if (hoverDiagonal)
                    {
                        if (left)
                        {
                            GLSEventRotationCommand.SetValue(
                                HoveredObject.EventData as BaseLightRotationBase,
                                up ? 315f : 225f);
                        }
                        else
                        {
                            GLSEventRotationCommand.SetValue(
                                HoveredObject.EventData as BaseLightRotationBase,
                                up ? 45f : 135f);
                        }
                    }

                    break;
                }
        }
    }

    public void NotifyValueChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }

    public void OnAngle0(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);
    public void OnAngle0Hover(InputAction.CallbackContext context) => HandleKeyHoverUpdate(context, upKey);

    public void OnAngle90(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);
    public void OnAngle90Hover(InputAction.CallbackContext context) => HandleKeyHoverUpdate(context, rightKey);

    public void OnAngle180(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);
    public void OnAngle180Hover(InputAction.CallbackContext context) => HandleKeyHoverUpdate(context, downKey);

    public void OnAngle270(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);
    public void OnAngle270Hover(InputAction.CallbackContext context) => HandleKeyHoverUpdate(context, leftKey);

    public void OnAngleHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightRotationBase;
            var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
            var prec = ScrollPrecisionController.GetCurrentRotationPrecision();
            var value = Mathf.Round((evt.Rotation + (delta * prec)) * 1_000f) / 1_000f;
            GLSEventRotationCommand.SetValue(evt, Mathf.Repeat(value, 360f));
        }
    }

    public void OnRotationDirectionLeft(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyDirectionChanged((int)LightRotationDirection.CounterClockwise);
    }

    public void OnRotationDirectionLeftHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventRotationCommand.SetDirection(
                HoveredObject.EventData as BaseLightRotationBase,
                LightRotationDirection.CounterClockwise);
        }
    }

    public void OnRotationDirectionAutomatic(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyDirectionChanged((int)LightRotationDirection.Automatic);
    }

    public void OnRotationDirectionAutomaticHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventRotationCommand.SetDirection(
                HoveredObject.EventData as BaseLightRotationBase,
                LightRotationDirection.Automatic);
        }
    }

    public void OnRotationDirectionRight(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyDirectionChanged((int)LightRotationDirection.Clockwise);
    }

    public void OnRotationDirectionRightHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventRotationCommand.SetDirection(
                HoveredObject.EventData as BaseLightRotationBase,
                LightRotationDirection.Clockwise);
        }
    }

    public void NotifyDirectionChanged(int value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnDirectionChanged?.Invoke(value);
    }

    public void OnChangeLoopCount(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyLoopChanged(1);
    }

    public void OnChangeLoopCountHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightRotationBase;
            GLSEventRotationCommand.SetLoop(evt, (evt.Loop + 1) % 5);
        }
    }

    public void OnResetLoopCount(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyLoopChanged(0);
    }

    public void OnResetLoopCountHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventRotationCommand.SetLoop(HoveredObject.EventData as BaseLightRotationBase, 0);
    }

    public void NotifyLoopChanged(int value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnLoopChanged?.Invoke(value);
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(0.1f);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeys)
            .Any())
            flagDirectionsUpdate = true;
    }

    private IEnumerator CheckForDiagonalHoverUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeysHover);
        yield return new WaitForSeconds(0.1f);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys
            .Except(heldKeysHover)
            .Any())
            flagHoverDirectionsUpdate = true;
    }
}
