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
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private SelectionController selectionController;
    // Read the authoritative mutable definition directly so startup cannot miss an earlier environment notification.
    private TracksDefinitionSO TrackDefinition => beatmapRuntimeContext.TracksDefinition;

    // Input handlers mutate the displayed event in place, so retain that identity for Basic Light state removal.
    private static BeatmapObjectUpdatedAction UpdatedEventAction(
        BaseObject edited,
        BaseObject original,
        ActionMergeType mergeType = ActionMergeType.None)
        => new(edited, original, mergeType: mergeType);

    public static bool IsHoveringRingOrZoom { get; private set; }

    private bool isScrolling;
    private Vector3 lastMousePosition;
    private EventContainer lastMetadataFailureContainer;
    private string lastMetadataFailureReason;

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
    }

    private void HidePreviewVisual() => selectionController?.HideEventPlacementVisual();

    private void ShowPreviewVisual() => selectionController?.ShowEventPlacementVisual();

    protected override void LateUpdate()
    {
        base.LateUpdate();
        // Laser speed uses the same precision-aware hover input ownership as Basic Event ring controls.
        IsHoveringRingOrZoom = IsHovering && HoveredObject != null &&
                               (IsRingRotationEvent(HoveredObject) || IsRingZoomEvent(HoveredObject)
                                                                   || IsLaserSpeedEvent(HoveredObject));
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
            eventAppearance.SetAppearance(e, TrackDefinition);
            BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original));
        }
        else if (IsRingRotationEvent(e) || IsLaserSpeedEvent(e))
        {
            // Ring and laser rotations share Chroma's unspecified -> CW -> CCW direction cycle.
            var direction = e.EventData.CustomDirection;
            e.EventData.CustomDirection = direction switch
            {
                null => 1,
                0 => null,
                1 => 0,
                _ => null
            };
            e.EventData.WriteCustom();
            eventAppearance.SetAppearance(e, TrackDefinition);
            BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original));
        }
        else if (IsRingZoomEvent(e))
        {
            // Invert step value between positive and negative
            if (e.EventData.CustomStep.HasValue)
            {
                e.EventData.CustomStep = -e.EventData.CustomStep.Value;
                e.EventData.WriteCustom();
                eventAppearance.SetAppearance(e, TrackDefinition);
                BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original));
            }
        }
        else if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind != BasicEventKind.Lights)
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
            eventAppearance.SetAppearance(e, TrackDefinition);
            BeatmapActionContainer.AddAction(UpdatedEventAction(e.ObjectData, original));
        }
    }

    public void OnTweakEventCtrlAlt(InputAction.CallbackContext context)
    {
        if (!context.performed || Keyboard.current == null || Keyboard.current.shiftKey.isPressed) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var hasComponents = TryGetEventComponents(e, out var eventComponents);
        var isLaserSpeed = hasComponents && eventComponents.HasFlag(BasicEventComponent.LightRotation);
        // Keep dispatch evidence until laser-speed hover routing is confirmed in the mapper.
        // UnityEngine.Debug.Log(
        //     $"[EventScroll] action=CtrlAlt, direction={modifier}, eventType={e.EventData.Type}, "
        //     + $"components={(hasComponents ? eventComponents.ToString() : "<unavailable>")}, "
        //     + $"laserSpeed={isLaserSpeed}");

        var original = BeatmapFactory.Clone(e.ObjectData);
        // var isRingRot = IsRingRotationEvent(e);
        // var isRingZoom = IsRingZoomEvent(e);
        // UnityEngine.Debug.Log($"[OnTweakEventCtrlAlt] IsRingRotationEvent={isRingRot}, IsRingZoomEvent={isRingZoom}");

        if (isLaserSpeed)
        {
            // Chroma implies unlocked rotation when lockRotation is absent, so never serialize false.
            e.EventData.CustomLockRotation = e.EventData.CustomLockRotation == true ? null : true;
            e.EventData.WriteCustom();
            // Keep mutation evidence until lock-direction editing is confirmed in the mapper.
            // UnityEngine.Debug.Log(
            //     $"[EventScroll] Laser lockRotation={e.EventData.CustomLockRotation?.ToString() ?? "<removed>"}.");
            FinalizeBasicEventTweak(e, original, ActionMergeType.LaserLockRotationTweak);
        }
        else if (IsRingRotationEvent(e))
        {
            // Match ring zoom's interval ladder so rotation speed is not tied to degree-sized rotation increments.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomSpeed,
                GetRingZoomPrecision(),
                0f,
                false,
                0f,
                v => e.EventData.CustomSpeed = v);
            FinalizeBasicEventTweak(e, original, ActionMergeType.RingSpeedTweak);
        }
        else if (IsRingZoomEvent(e))
        {
            // SmoothStepRingZoom only applies to The Second's legacy ring right now.
            if (IsSmoothStepRingZoomEvent(e))
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
                0f,
                v => e.EventData.CustomSpeed = v);
            FinalizeBasicEventTweak(e, original, ActionMergeType.RingSpeedTweak);
        }
        else if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
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
            // Match ring zoom's step ladder so propagation remains controllable at every precision level.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomProp,
                GetRingZoomPrecision(),
                null,
                false,
                0f,
                v => e.EventData.CustomProp = v);
            FinalizeBasicEventTweak(e, original, ActionMergeType.RingPropagationTweak);
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
                0f,
                v => e.EventData.CustomStep = v);
            FinalizeBasicEventTweak(e, original, ActionMergeType.RingStepTweak);
        }
        else if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
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

    public void OnTweakEventCtrlShiftAlt(InputAction.CallbackContext context)
    {
        if (!context.performed || Keyboard.current == null) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged || !IsRingRotationEvent(e)) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var original = BeatmapFactory.Clone(e.ObjectData);
        // Match ring zoom's step ladder and preserve Chroma's valid negative step values.
        TweakCustomFloat(
            e.EventData,
            modifier,
            e.EventData.CustomStep,
            GetRingZoomPrecision(),
            null,
            false,
            0f,
            v => e.EventData.CustomStep = v);
        FinalizeBasicEventTweak(e, original, ActionMergeType.RingStepTweak);

        if (e.EventData.CompareTo(original) == 0) return;
        isScrolling = true;
        HidePreviewVisual();
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

        if (IsLaserSpeedEvent(e))
        {
            if (KeybindsController.IsSelectKeyHeld) return;
            // Preserve integer values in i while using custom speed only when fractional precision is required.
            TweakLaserSpeed(e, modifier, original);
            return;
        }

        if (isRingRot)
        {
            // UnityEngine.Debug.Log($"[TweakMain] Ring Rotation detected - tweaking CustomRingRotation");
            if (KeybindsController.IsSelectKeyHeld) return;
            // Seed unset basic-event ring rotation at 90 degrees so Alt+Scroll matches the game's default rotation baseline.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomRingRotation,
                GetRingRotationPrecision(),
                null,
                false,
                90f,
                v => e.EventData.CustomRingRotation = v);
            FinalizeBasicEventTweak(e, original, ActionMergeType.RingRotationValueTweak);
            return;
        }

        if (isRingZoom)
        {
            // UnityEngine.Debug.Log($"[TweakMain] Ring Zoom detected - tweaking CustomStep");
            if (KeybindsController.IsSelectKeyHeld) return;
            // SmoothStepRingZoom only applies to The Second's legacy ring right now.
            if (IsSmoothStepRingZoomEvent(e))
            {
                TweakSmoothStepRingZoom(e, modifier, original);
                return;
            }

            // Use the requested zoom precision ladder so coarse/fine ring zoom edits follow the precision UI.
            TweakCustomFloat(
                e.EventData,
                modifier,
                e.EventData.CustomStep,
                GetRingZoomPrecision(),
                null,
                false,
                0f,
                v => e.EventData.CustomStep = v);
            FinalizeBasicEventTweak(e, original, ActionMergeType.RingZoomStepTweak);
            return;
        }

        if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            if (KeybindsController.IsControlKeyHeld || KeybindsController.IsSelectKeyHeld) return;

            var prec = scrollPrecisionController.GetCurrentBrightnessPrecision() / 100f;
            var value = Mathf.Round((e.EventData.FloatValue + (modifier * prec)) * 1_000f) / 1_000f;
            e.EventData.FloatValue = Mathf.Max(0f, value);

            RefreshPrevEventContainer(e);

            // Alt+scroll changes brightness only and must not fall through into the generic event-value tweak.
            if (e.EventData.CompareTo(original) == 0) return;
            eventAppearance.SetAppearance(e, TrackDefinition);
            BeatmapActionContainer.AddAction(
                UpdatedEventAction(e.ObjectData, (BaseEvent)original, ActionMergeType.EventMainTweak));
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

        eventAppearance.SetAppearance(e, TrackDefinition);
        BeatmapActionContainer.AddAction(
            UpdatedEventAction(e.ObjectData, (BaseEvent)original, ActionMergeType.EventMainTweak));
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

        if (TrackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            // UnityEngine.Debug.Log($"[TweakAlternative] Light event detected - returning (handled by custom InputActions)");
            return;
        }

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, TrackDefinition);
        BeatmapActionContainer.AddAction(
            UpdatedEventAction(e.ObjectData, (BaseEvent)original, ActionMergeType.EventAltTweak));
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

    // Guard pooled and cloned hover containers while retaining evidence about whichever initialization field is missing.
    private bool TryGetEventComponents(EventContainer e, out BasicEventComponent components)
    {
        components = BasicEventComponent.None;
        if (e == null) return false;

        if (e.EventData == null)
        {
            LogMetadataFailure(e, "EventData is null");
            return false;
        }

        var tracksDefinition = e.TracksDefinition ?? TrackDefinition;
        if (tracksDefinition == null)
        {
            LogMetadataFailure(e, "both container and runtime TracksDefinition are null");
            return false;
        }

        lastMetadataFailureContainer = null;
        lastMetadataFailureReason = null;
        components = tracksDefinition.GetBasicOrDefault(e.EventData.Type).Components;
        return true;
    }

    private void LogMetadataFailure(EventContainer e, string reason)
    {
        // Log only when the failing container or reason changes so LateUpdate cannot flood the console.
        if (lastMetadataFailureContainer == e && lastMetadataFailureReason == reason) return;

        lastMetadataFailureContainer = e;
        lastMetadataFailureReason = reason;
        // UnityEngine.Debug.LogWarning(
        //     $"[EventMetadata] Cannot classify hovered event: {reason}; "
        //     + $"container={e.name}, active={e.gameObject.activeInHierarchy}, "
        //     + $"containerTracks={(e.TracksDefinition == null ? "null" : e.TracksDefinition.name)}, "
        //     + $"runtimeTracks={(TrackDefinition == null ? "null" : TrackDefinition.name)}.");
    }

    // Use the hovered container's active definition so input and node appearance classify the same environment track.
    private bool IsRingRotationEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && components.HasFlag(BasicEventComponent.RingRotation);

    // Shared wheel actions need an immediate hit test instead of relying on last frame's hover state.
    public bool IsPointerOverRingRotation()
    {
        return RaycastFirstObject(out var eventContainer)
               && eventContainer != null
               && !eventContainer.Dragged
               && IsRingRotationEvent(eventContainer);
    }

    // Global wheel handlers use this shared query so ring rotation owns overlapping modifier chords.
    public static bool IsRingRotationHoveredByPointer()
    {
        var controller = FindFirstObjectByType<BeatmapEventInputController>();
        return controller != null && controller.IsPointerOverRingRotation();
    }

    // Cursor interval shares Ctrl+Shift+Scroll with every Basic Event node that has a hover-specific edit.
    public static bool IsCursorIntervalOwnedByPointer()
    {
        var controller = FindFirstObjectByType<BeatmapEventInputController>();
        if (controller == null || !controller.RaycastFirstObject(out var eventContainer) || eventContainer == null
            || eventContainer.Dragged)
            return false;

        if (controller.IsRingRotationEvent(eventContainer) || controller.IsRingZoomEvent(eventContainer))
            return true;

        var definitions = eventContainer.TracksDefinition ?? controller.TrackDefinition;
        return definitions != null
               && definitions.GetBasicOrDefault(eventContainer.EventData.Type).Kind == BasicEventKind.Lights;
    }

    // Zoom capability is independent so mixed component tracks remain supported.
    private bool IsRingZoomEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && (components.HasFlag(BasicEventComponent.RingZoom)
            || IsSmoothStepRingZoomEvent(e));

    // SmoothStepRingZoom only applies to The Second's legacy ring right now.
    private bool IsSmoothStepRingZoomEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && components.HasFlag(BasicEventComponent.SmoothStepRingZoom);

    // Laser-speed semantics belong to tracks consumed by Basic Event light-rotation components.
    private bool IsLaserSpeedEvent(EventContainer e) =>
        TryGetEventComponents(e, out var components)
        && components.HasFlag(BasicEventComponent.LightRotation);

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

    // Laser speed supports fractional editing above the ordinary integer precision level.
    private float GetLaserSpeedPrecision() => scrollPrecisionController.CurrentPrecision switch
    {
        ScrollPrecision.Low => 5f,
        ScrollPrecision.Medium => 1f,
        ScrollPrecision.High => 0.5f,
        _ => 0.1f
    };

    private void TweakLaserSpeed(EventContainer e, int modifier, BaseObject original)
    {
        var currentSpeed = e.EventData.CustomSpeed ?? e.EventData.Value;
        var speed = Mathf.Max(0f, Mathf.Round((currentSpeed + (modifier * GetLaserSpeedPrecision())) * 10f) / 10f);
        var integerSpeed = Mathf.RoundToInt(speed);

        e.EventData.Value = integerSpeed;
        // Integer speed is represented canonically by i; custom speed is reserved for its fractional override.
        e.EventData.CustomSpeed = Mathf.Approximately(speed, integerSpeed) ? null : speed;
        e.EventData.WriteCustom();
        // Keep serialized-state evidence until fractional-to-integer cleanup is confirmed in the node editor.
        // UnityEngine.Debug.Log(
        //     $"[EventScroll] Laser speed={speed:0.0}, i={e.EventData.Value}, "
        //     + $"customSpeed={e.EventData.CustomSpeed?.ToString() ?? "<removed>"}, "
        //     + $"jsonHasSpeed={e.EventData.CustomData?.HasKey(e.EventData.CustomKeySpeed) == true}.");
        FinalizeBasicEventTweak(e, original, ActionMergeType.LaserSpeedTweak);
    }

    private void TweakSmoothStepRingZoom(EventContainer e, int modifier, BaseObject original)
    {
        var currentStep = e.EventData.CustomStep ?? e.EventData.Value;
        // Keep enough precision for the zoom ladder while removing floating-point drift near integer boundaries.
        var step = Mathf.Round((currentStep + (modifier * GetRingZoomPrecision())) * 100f) / 100f;
        var integerStep = Mathf.RoundToInt(step);

        e.EventData.Value = integerStep;
        // Integer smooth steps are represented canonically by i; custom step is reserved for fractional overrides.
        e.EventData.CustomStep = Mathf.Approximately(step, integerStep) ? null : step;
        e.EventData.WriteCustom();
        // Keep serialized-state evidence until The Second's fractional-to-integer cleanup is confirmed.
        // UnityEngine.Debug.Log(
        //     $"[EventScroll] SmoothStepRingZoom step={step:0.##}, i={e.EventData.Value}, "
        //     + $"customStep={e.EventData.CustomStep?.ToString() ?? "<removed>"}, "
        //     + $"jsonHasStep={e.EventData.CustomData?.HasKey(e.EventData.CustomKeyStep) == true}.");
        FinalizeBasicEventTweak(e, original, ActionMergeType.RingZoomStepTweak);
    }

    private void TweakCustomFloat(BaseEvent evt, int modifier, float? current, float step, float? min, bool logarithmic, float defaultValue, Action<float?> setter)
    {
        // UnityEngine.Debug.Log($"[TweakCustomFloat] Called: current={current}, modifier={modifier}, step={step}, min={min}, logarithmic={logarithmic}");
        var value = current ?? defaultValue;
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

    private void FinalizeBasicEventTweak(EventContainer e, BaseObject original, ActionMergeType mergeType)
    {
        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e, TrackDefinition);
        BeatmapActionContainer.AddAction(
            UpdatedEventAction(e.ObjectData, (BaseEvent)original, mergeType));
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

        eventAppearance.SetAppearance(e, TrackDefinition);
        BeatmapActionContainer.AddAction(
            UpdatedEventAction(e.ObjectData, original, ActionMergeType.LightLerpTypeTweak));
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

        eventAppearance.SetAppearance(e, TrackDefinition);
        BeatmapActionContainer.AddAction(
            UpdatedEventAction(e.ObjectData, original, ActionMergeType.LightEasingTweak));
        RefreshPrevEventContainer(e);
        beatmapRuntimeContext.Descriptor?.BasicEventEffectManager.Refresh();
    }
}
