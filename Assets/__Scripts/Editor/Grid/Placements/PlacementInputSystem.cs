using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class PlacementInputSystem : MonoBehaviour,
                                    CMInput.IPlacementControllersActions,
                                    CMInput.ICancelPlacementActions
{
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private PrecisionPlacementController precisionPlacementController;

    private PlacementState state;
    private PlacementProvider currentProvider;
    private Vector2 mousePosition;
    private bool applicationFocus;
    private bool applicationFocusChanged;

    private bool CanInteract
    {
        get
        {
            return !Input.GetMouseButton((int)MouseButton.Right)
                && KeybindsController.IsMouseInWindow
                && !SongTimelineController.IsHovering
                && !SceneTransitionManager.IsLoading
                && !DeleteToolController.IsActive
                && !NodeEditorController.IsActive
                && applicationFocus
                && !UIMode.PreviewMode;
        }
    }

    private void Awake() => GridViewController.OnGridViewUpdated += RefreshBound;

    private void OnDestroy()
    {
        GridViewController.OnGridViewUpdated -= RefreshBound;
        Intersections.Clear();
    }

    private void Update()
    {
        if (((state == PlacementState.Drag && !Input.GetMouseButton((int)MouseButton.Left))
                || (state == PlacementState.DragAtTime && !Input.GetMouseButton((int)MouseButton.Right)))
            && currentProvider != null)
        {
            currentProvider.Lane.MoveXYGridByZ(0f);
            HandleDragFinished();
        }

        if (Application.isFocused != applicationFocus)
        {
            applicationFocus = Application.isFocused;
            applicationFocusChanged = true;
            return;
        }

        if (applicationFocusChanged) applicationFocusChanged = false;
        if (PauseManager.IsPaused) return;

        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
        var gridHit = Intersections
            .RaycastAll(ray, 11)
            .Select(intersectionHit => (hit: intersectionHit,
                provider: intersectionHit.GameObject.transform.parent.GetComponent<PlacementProvider>()))
            .Where(grid => grid.provider != null)
            .OrderBy(grid => grid.hit.Distance)
            .ToArray();

        if (HandleExitWhen(
            (!CanInteract && state == PlacementState.Hover)
            || gridHit.Length == 0))
            return;

        var (hit, provider) = gridHit[0];
        if (currentProvider != provider && BoxSelectionPlacementController.State != SelectionState.Selecting)
        {
            currentProvider = provider;

            RefreshBound();
            foreach (var placement in currentProvider.Placements) placement.Initialize(currentProvider);
        }

        if (HandleExitWhen(PersistentUI.Instance.DialogBoxIsEnabled)
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;

        precisionPlacementController.UpdateMousePosition(hit.Point);
        foreach (var placement in currentProvider.Placements) placement.UpdateState(hit, state);
    }

    private void RefreshBound()
    {
        if (currentProvider == null) return;

        var boundLocal = currentProvider.Lane.XY.Grid.bounds;
        // Transform the bounds into the pseudo-world space we use for selection
        var localTransform = currentProvider.transform;
        var localScale = localTransform.localScale;
        var boundsNew = localTransform.InverseTransformBounds(boundLocal);
        boundsNew.center += localTransform.localPosition;
        boundsNew.extents = new Vector3(
            boundsNew.extents.x * localScale.x,
            boundsNew.extents.y * localScale.y,
            boundsNew.extents.z * localScale.z
        );

        foreach (var placement in currentProvider.Placements) placement.Bounds = boundsNew;
    }

    public void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (!context.performed || currentProvider == null) return;
        foreach (var placement in currentProvider.Placements) placement.Cancel();
    }

    public void OnPlaceObject(InputAction.CallbackContext context)
    {
        if (currentProvider == null
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsMouseInWindow
            || !context.performed
            || state != PlacementState.Hover
            || !CanInteract
            || PersistentUI.Instance.DialogBoxIsEnabled
            || applicationFocusChanged)
            return;
        foreach (var placement in currentProvider.Placements.Where(p => p.IsActive && p.CanPlace)) placement.Apply();
    }

    public void OnInitiateClickandDrag(InputAction.CallbackContext context)
    {
        if (currentProvider == null) return;
        if (context.performed)
        {
            foreach (var placement in currentProvider.Placements) placement.HideVisual();

            var dragRay = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (!Intersections.Raycast(dragRay, 9, out var dragHit)) return;

            var placements = currentProvider
                .Placements
                .Where(p => p.CanClickAndDrag)
                .Select(p => (p, container: p.StartDrag(dragHit.GameObject)))
                .Where(p => p.container != null);
            if (!placements.Any()) return;

            state = PlacementState.Drag;
        }
        else if (context.canceled && state == PlacementState.Drag) HandleDragFinished();
    }

    public void OnInitiateClickandDragatTime(InputAction.CallbackContext context)
    {
        if (currentProvider == null) return;
        if (context.performed)
        {
            var dragRay = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (!Intersections.Raycast(dragRay, 9, out var dragHit)) return;

            var placements = currentProvider
                .Placements
                .Where(p => p.CanClickAndDrag && p.IsActive)
                .Select(p => (p, container: p.StartDrag(dragHit.GameObject)))
                .Where(p => p.container != null)
                .ToArray();
            if (!placements.Any()) return;

            var (placement, con) = placements.First();
            state = PlacementState.DragAtTime;
            var newZ = placement.GetContainerPosZ(con);
            currentProvider.Lane.MoveXYGridByZ(newZ);
        }
        else if (context.canceled && state == PlacementState.DragAtTime)
        {
            currentProvider.Lane.MoveXYGridByZ(0);
            HandleDragFinished();
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        mousePosition = Mouse.current.position.ReadValue();
    }

    public void OnPrecisionPlacementToggle(InputAction.CallbackContext context)
    {
        switch (Settings.Instance.PrecisionPlacementMode)
        {
            case PrecisionPlacementMode.Off:
                precisionPlacementController.TogglePrecisionPlacement(false);
                break;
            case PrecisionPlacementMode.Hold:
                precisionPlacementController.TogglePrecisionPlacement(context.performed);
                break;
            case PrecisionPlacementMode.Toggle:
                if (context is { started: true, performed: false })
                    precisionPlacementController.TogglePrecisionPlacement(!PrecisionPlacementController.IsEnabled);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleDragFinished()
    {
        if (state == PlacementState.Hover) return;
        foreach (var placement in currentProvider.Placements.Where(p => p.IsDragging)) placement.FinishDrag();
        state = PlacementState.Hover;
    }

    private bool HandleExitWhen(bool shouldExit)
    {
        if (!shouldExit) return false;
        if (currentProvider == null) return true;
        foreach (var placement in currentProvider.Placements) placement.Exit();
        return true;
    }
}
