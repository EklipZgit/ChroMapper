using System;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<EventContainer>, CMInput.IEventObjectsActions
{
    private const string TheSecondEnvironmentId = "TheSecondEnvironment";

    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private SelectionController selectionController;
    private TrackDefinitionsSO trackDefinition;

    public static bool IsHoveringRingOrZoom { get; private set; }

    private bool isScrolling;
    private Vector3 lastMousePosition;

    private void Start()
    {
        beatmapRuntimeContext.OnTrackDefinitionsChanged += HandleTrackDefinitionChanged;
        lastMousePosition = Input.mousePosition;
    }

    private void HidePreviewVisual() => selectionController?.HideEventPlacementVisual();

    private void ShowPreviewVisual() => selectionController?.ShowEventPlacementVisual();

    private void OnDestroy() => beatmapRuntimeContext.OnTrackDefinitionsChanged -= HandleTrackDefinitionChanged;

    protected override void LateUpdate()
    {
        base.LateUpdate();
        var wasHovering = IsHoveringRingOrZoom;
        IsHoveringRingOrZoom = IsHovering && HoveredObject != null &&
            (IsRingRotationEvent(HoveredObject) || IsRingZoomEvent(HoveredObject));

        UpdatePreviewVisualOnMouseMove();
    }

    private void UpdatePreviewVisualOnMouseMove()
    {
        if (!isScrolling) return;

        var currentMousePosition = Input.mousePosition;
        var mouseMoved = Vector3.Distance(currentMousePosition, lastMousePosition) > 0.1f;

        if (mouseMoved)
        {
            isScrolling = false;
            ShowPreviewVisual();
        }
        else
        {
            HidePreviewVisual();
        }

        lastMousePosition = currentMousePosition;
    }

    private void HandleTrackDefinitionChanged(TrackDefinitionsSO obj) => trackDefinition = obj;

    public void OnInvertEventValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsMouseInWindow
            || !context.performed)
            return;

        RaycastFirstObject(out var e);
        if (e != null && !e.Dragged) InvertEvent(e);
    }

    public void OnTweakEventMain(InputAction.CallbackContext context)
    {
        if (!context.performed || Keyboard.current == null || Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        // UnityEngine.Debug.Log($"[EventScroll] keys={GetHeldModifiers()}, action=Main, direction={modifier}, eventType={e.EventData.Type}, ringRotation={IsRingRotationEvent(e)}, ringZoom={IsRingZoomEvent(e)}");

        var original = BeatmapFactory.Clone(e.ObjectData);
        TweakMain(e, modifier);

        // Only hide preview visual if data was actually modified
        if (e.EventData.CompareTo(original) != 0)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventMain] Data modified - hiding preview visual");
            isScrolling = true;
            HidePreviewVisual();
        }
    }

    public void OnTweakEventAlternative(InputAction.CallbackContext context)
    {
        if (!context.performed || Keyboard.current == null || Keyboard.current.ctrlKey.isPressed) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        // UnityEngine.Debug.Log($"[EventScroll] keys={GetHeldModifiers()}, action=Alternative, direction={modifier}, eventType={e.EventData.Type}, ringRotation={IsRingRotationEvent(e)}, ringZoom={IsRingZoomEvent(e)}");

        var original = BeatmapFactory.Clone(e.ObjectData);
        TweakAlternative(e, modifier);

        // Only hide preview visual if data was actually modified
        if (e.EventData.CompareTo(original) != 0)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventAlternative] Data modified - hiding preview visual");
            isScrolling = true;
            HidePreviewVisual();
        }
    }

    public void InvertEvent(EventContainer e)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        if (e.EventData.IsColorBoostEvent())
        {
            e.EventData.Value = e.EventData.Value > 0 ? 0 : 1;
            eventAppearance.SetAppearance(e, trackDefinition);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
        }
        else if (e.EventData.Type == (int)EventTypeValue.RingRotation)
        {
            // Invert direction: unspecified -> CW (1) -> CCW (0) -> unspecified (null)
            var direction = e.EventData.CustomDirection;
            e.EventData.CustomDirection = direction switch
            {
                null => 1,
                0 => null,
                1 => 0,
                _ => null
            };
            e.EventData.WriteCustom();
            eventAppearance.SetAppearance(e, trackDefinition);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
        }
        else if (e.EventData.Type == (int)EventTypeValue.RingZoom)
        {
            // Invert step value between positive and negative
            if (e.EventData.CustomStep.HasValue)
            {
                e.EventData.CustomStep = -e.EventData.CustomStep.Value;
                e.EventData.WriteCustom();
                eventAppearance.SetAppearance(e, trackDefinition);
                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
            }
        }
        else if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind != BasicEventKind.Lights)
        {
            return;
        }
        else
        {
            switch (e.EventData.Value)
            {
                case > 0 and <= 8:
                    e.EventData.Value += 4;
                    break;
                case > 8 and <= 12:
                    e.EventData.Value -= 8; // white to blue
                    break;
            }

            RefreshPrevEventContainer(e);
            eventAppearance.SetAppearance(e, trackDefinition);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
        }
    }

    public void OnTweakEventCtrlAlt(InputAction.CallbackContext context)
    {
        if (!context.performed || Keyboard.current == null || Keyboard.current.shiftKey.isPressed) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        // UnityEngine.Debug.Log($"[EventScroll] keys={GetHeldModifiers()}, action=CtrlAlt, direction={modifier}, eventType={e.EventData.Type}, ringRotation={IsRingRotationEvent(e)}, ringZoom={IsRingZoomEvent(e)}");

        var original = BeatmapFactory.Clone(e.ObjectData);
        // var isRingRot = IsRingRotationEvent(e);
        // var isRingZoom = IsRingZoomEvent(e);
        // UnityEngine.Debug.Log($"[OnTweakEventCtrlAlt] IsRingRotationEvent={isRingRot}, IsRingZoomEvent={isRingZoom}");

        if (IsRingRotationEvent(e))
        {
            // Keep ring rotation speed edits aligned with GLS rotation precision instead of a fixed increment.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomSpeed,
                GetRingRotationPrecision(),
                0f,
                false,
                v => e.EventData.CustomSpeed = v);
            FinalizeRingTweak(e, original, ActionMergeType.RingSpeedTweak);
        }
        else if (IsRingZoomEvent(e))
        {
            // The Second derives zoom duration from the next event and therefore cannot honor custom ring speed.
            if (EnvironmentInfoHelper.GetCurrentEnvironment() == TheSecondEnvironmentId)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "This environment does not support ring speed. \nInstead place a duplicate step node at the point you want the step to complete",
                    null,
                    PersistentUI.DialogBoxPresetType.Ok);
                return;
            }

            // Keep ring zoom speed edits on the same zoom-specific precision ladder as the other zoom tweaks.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomSpeed,
                GetRingZoomPrecision(),
                0f,
                false,
                v => e.EventData.CustomSpeed = v);
            FinalizeRingTweak(e, original, ActionMergeType.RingSpeedTweak);
        }
        else if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventCtrlAlt] Light event detected - cycling node type");
            // Ctrl+Alt cycles the visible node type, including distinct RGB and HSV transition states.
            TweakLightNodeType(e, modifier);
        }

        // Only hide preview visual if data was actually modified
        if (e.EventData.CompareTo(original) != 0)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventCtrlAlt] Data modified - hiding preview visual");
            isScrolling = true;
            HidePreviewVisual();
        }
    }

    public void OnTweakEventCtrlShift(InputAction.CallbackContext context)
    {
        if (!context.performed || Keyboard.current == null || Keyboard.current.altKey.isPressed) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        // UnityEngine.Debug.Log($"[EventScroll] keys={GetHeldModifiers()}, action=CtrlShift, direction={modifier}, eventType={e.EventData.Type}, ringRotation={IsRingRotationEvent(e)}, ringZoom={IsRingZoomEvent(e)}");

        var original = BeatmapFactory.Clone(e.ObjectData);
        var isRingRot = IsRingRotationEvent(e);
        var isRingZoom = IsRingZoomEvent(e);
        // UnityEngine.Debug.Log($"[OnTweakEventCtrlShift] IsRingRotationEvent={isRingRot}, IsRingZoomEvent={isRingZoom}");

        if (isRingRot)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventCtrlShift] Ring Rotation detected - tweaking CustomStep");
            // Keep ring rotation step edits on the same precision ladder as GLS rotation tweaks.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomStep,
                GetRingRotationPrecision(),
                0f,
                false,
                v => e.EventData.CustomStep = v);
            FinalizeRingTweak(e, original, ActionMergeType.RingStepTweak);
        }
        else if (isRingZoom)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventCtrlShift] Ring Zoom detected - tweaking CustomStep");
            // Keep ring zoom modifier-step edits on the same precision ladder as the main zoom tweak.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomStep,
                GetRingZoomPrecision(),
                0f,
                false,
                v => e.EventData.CustomStep = v);
            FinalizeRingTweak(e, original, ActionMergeType.RingStepTweak);
        }
        else if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventCtrlShift] Light event detected - tweaking Easing");
            TweakEasing(e, modifier);
        }

        // Only hide preview visual if data was actually modified
        if (e.EventData.CompareTo(original) != 0)
        {
            // UnityEngine.Debug.Log($"[OnTweakEventCtrlShift] Data modified - hiding preview visual");
            isScrolling = true;
            HidePreviewVisual();
        }
    }

    private static string GetHeldModifiers()
    {
        if (Keyboard.current == null) return "None";
        return $"Ctrl={Keyboard.current.ctrlKey.isPressed},Alt={Keyboard.current.altKey.isPressed},Shift={Keyboard.current.shiftKey.isPressed}";
    }

    protected override bool GetComponentFromTransform(GameObject t, out EventContainer obj) =>
        t.transform.parent.TryGetComponent(out obj);

    // for event that frequently gets changed
    public void TweakMain(EventContainer e, int modifier)
    {
        // UnityEngine.Debug.Log($"[TweakMain] Called with modifier={modifier}, e={e?.EventData?.Type}");
        var original = BeatmapFactory.Clone(e.ObjectData);

        var isRingRot = IsRingRotationEvent(e);
        var isRingZoom = IsRingZoomEvent(e);
        // UnityEngine.Debug.Log($"[TweakMain] IsRingRotationEvent={isRingRot}, IsRingZoomEvent={isRingZoom}");

        if (isRingRot)
        {
            // UnityEngine.Debug.Log($"[TweakMain] Ring Rotation detected - tweaking CustomRingRotation");
            if (KeybindsController.IsSelectKeyHeld) return;
            // Match GLS rotation hover precision so ring rotation value edits scale with the active tweak precision.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomRingRotation,
                GetRingRotationPrecision(),
                null,
                false,
                v => e.EventData.CustomRingRotation = v);
            FinalizeRingTweak(e, original, ActionMergeType.RingRotationValueTweak);
            return;
        }

        if (isRingZoom)
        {
            // UnityEngine.Debug.Log($"[TweakMain] Ring Zoom detected - tweaking CustomStep");
            if (KeybindsController.IsSelectKeyHeld) return;
            // Use the requested zoom precision ladder so coarse/fine ring zoom edits follow the precision UI.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomStep,
                GetRingZoomPrecision(),
                null,
                false,
                v => e.EventData.CustomStep = v);
            FinalizeRingTweak(e, original, ActionMergeType.RingZoomStepTweak);
            return;
        }

        if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            if (KeybindsController.IsControlKeyHeld || KeybindsController.IsSelectKeyHeld) return;

            var prec = scrollPrecisionController.GetCurrentBrightnessPrecision() / 100f;
            var value = Mathf.Round((e.EventData.FloatValue + (modifier * prec)) * 1_000f) / 1_000f;
            e.EventData.FloatValue = Mathf.Max(0f, value);

            RefreshPrevEventContainer(e);

            // Alt+scroll changes brightness only and must not fall through into the generic event-value tweak.
            if (e.EventData.CompareTo(original) == 0) return;
            eventAppearance.SetAppearance(e, trackDefinition);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedAction(
                    e.ObjectData,
                    e.ObjectData,
                    original,
                    mergeType: ActionMergeType.EventMainTweak));
            // UnityEngine.Debug.Log(
            //     $"[EventScroll] Brightness-only tweak: value={e.EventData.Value}, brightness={e.EventData.FloatValue:F3}.");
            return;
        }

        if (KeybindsController.IsControlKeyHeld || KeybindsController.IsSelectKeyHeld) return;

        if (e.EventData.IsColorBoostEvent())
            e.EventData.Value = e.EventData.Value == 0 ? 1 : 0;
        else if (e.EventData.IsBpmEvent())
        {
            e.EventData.FloatValue += modifier;
            if (e.EventData.FloatValue < 1) e.EventData.FloatValue = 1;
        }
        else
        {
            e.EventData.Value += modifier;
            if (e.EventData.Value < 0) e.EventData.Value = 0;
        }

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                e.ObjectData,
                e.ObjectData,
                original,
                mergeType: ActionMergeType.EventMainTweak));
    }

    // for event that occasionally gets changed
    public void TweakAlternative(EventContainer e, int modifier)
    {
        // UnityEngine.Debug.Log($"[TweakAlternative] Called with modifier={modifier}, e={e?.EventData?.Type}");
        var original = BeatmapFactory.Clone(e.ObjectData);

        var isRingRot = IsRingRotationEvent(e);
        var isRingZoom = IsRingZoomEvent(e);
        // UnityEngine.Debug.Log($"[TweakAlternative] IsRingRotationEvent={isRingRot}, IsRingZoomEvent={isRingZoom}");

        // All modifier combinations are now handled by custom InputActions
        // This method only handles basic scroll behavior if needed
        if (isRingRot || isRingZoom)
        {
            // UnityEngine.Debug.Log($"[TweakAlternative] Ring event detected - returning (handled by custom InputActions)");
            return;
        }

        if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            // UnityEngine.Debug.Log($"[TweakAlternative] Light event detected - returning (handled by custom InputActions)");
            return;
        }

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                e.ObjectData,
                e.ObjectData,
                original,
                mergeType: ActionMergeType.EventAltTweak));
    }

    private void RefreshPrevEventContainer(EventContainer e)
    {
        var prevEvent = e.EventData.Prev;
        if (prevEvent != null)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (collection.LoadedContainers.TryGetValue(prevEvent, out var container))
                (container as EventContainer).RefreshAppearance();
        }
    }

    private static bool IsRingRotationEvent(EventContainer e) => e?.EventData?.Type == (int)EventTypeValue.RingRotation;

    private static bool IsRingZoomEvent(EventContainer e) => e?.EventData?.Type == (int)EventTypeValue.RingZoom;

    // Route ring rotation tweaks through the same precision source GLS rotation hover uses.
    private float GetRingRotationPrecision() => scrollPrecisionController.GetCurrentRotationPrecision();

    // Ring zoom uses a dedicated coarse-to-fine ladder that follows the active tweak precision selection.
    private float GetRingZoomPrecision() => scrollPrecisionController.CurrentPrecision switch
    {
        ScrollPrecision.Low => 1f,
        ScrollPrecision.Medium => 0.25f,
        ScrollPrecision.High => 0.05f,
        _ => 0.01f
    };

    private void TweakCustomFloat(BaseEvent evt, int modifier, float? current, float step, float? min, bool logarithmic, Action<float?> setter)
    {
        // UnityEngine.Debug.Log($"[TweakCustomFloat] Called: current={current}, modifier={modifier}, step={step}, min={min}, logarithmic={logarithmic}");
        var value = current ?? 0f;
        if (logarithmic) step = GetLogarithmicStep(value);
        value += modifier * step;
        if (min.HasValue) value = Mathf.Max(min.Value, value);
        // UnityEngine.Debug.Log($"[TweakCustomFloat] Setting new value: {value}");
        setter(value);
        evt.WriteCustom();
    }

    private static float GetLogarithmicStep(float value)
    {
        var abs = Mathf.Abs(value);
        if (abs < 0.1f) return 0.02f;
        if (abs < 0.3f) return 0.04f;
        if (abs < 1.0f) return 0.1f;
        if (abs < 2.0f) return 0.2f;
        if (abs < 5.0f) return 0.5f;
        return 1.0f;
    }

    private void FinalizeRingTweak(EventContainer e, BaseObject original, ActionMergeType mergeType)
    {
        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                e.ObjectData,
                e.ObjectData,
                original,
                mergeType: mergeType));
        beatmapRuntimeContext.Descriptor?.BasicEventEffectManager.Refresh();
    }

    private void TweakLightNodeType(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        const int nodeTypeCount = 5;
        var colorBase = e.EventData.Value switch
        {
            > 0 and <= 4 => 0,
            > 4 and <= 8 => 4,
            > 8 and <= 12 => 8,
            _ => -1
        };
        if (colorBase < 0) return;

        // Treat HSV transition as the fifth node type after the ordinary transition node.
        var nodeType = e.EventData.IsTransition && e.EventData.CustomLerpType == "HSV"
            ? 4
            : e.EventData.Value - colorBase - 1;
        var nextNodeType = (nodeType + modifier + nodeTypeCount) % nodeTypeCount;
        e.EventData.Value = colorBase + Math.Min(nextNodeType, 3) + 1;
        e.EventData.CustomLerpType = nextNodeType == 4 ? "HSV" : null;

        e.EventData.WriteCustom();

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original, mergeType: ActionMergeType.LightLerpTypeTweak));
        // Keep runtime evidence for the modifier routing and five-state node-type cycle until confirmed.
        // UnityEngine.Debug.Log(
        //     $"[EventScroll] Node-type tweak: state={nextNodeType}, value={e.EventData.Value}, "
        //     + $"lerpType={e.EventData.CustomLerpType ?? "RGB"}.");
        RefreshPrevEventContainer(e);
        beatmapRuntimeContext.Descriptor?.BasicEventEffectManager.Refresh();
    }

    private void TweakEasing(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        var easingList = Easing.DisplayNameToInternalName.Values.ToList();
        var current = e.EventData.CustomEasing ?? "easeLinear";
        var index = easingList.IndexOf(current);
        if (index < 0) index = 0;
        index = (index + modifier + easingList.Count) % easingList.Count;
        var newEasing = easingList[index];
        e.EventData.CustomEasing = newEasing == "easeLinear" ? null : newEasing;
        e.EventData.WriteCustom();

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original, mergeType: ActionMergeType.LightEasingTweak));
        RefreshPrevEventContainer(e);
        beatmapRuntimeContext.Descriptor?.BasicEventEffectManager.Refresh();
    }
}
