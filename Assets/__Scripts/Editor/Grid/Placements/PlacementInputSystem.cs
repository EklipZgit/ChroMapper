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
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private PrecisionPlacementController precisionPlacementController;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;
    private bool applicationFocus;
    private bool applicationFocusChanged;

    private PlacementProvider currentProvider;
    private PlacementInputState inputState;
    private bool isOnGrid;
    private Vector2 mousePosition;

    private bool CanInteract =>
        !Input.GetMouseButton((int)MouseButton.Right)
        && KeybindsController.IsMouseInWindow
        && !SongTimelineController.IsHovering
        && !SceneTransitionManager.IsLoading
        && !DeleteToolController.IsActive
        && !NodeEditorController.IsActive
        && applicationFocus
        && !UIMode.PreviewMode;

    private void Start() => gridViewController.OnGridViewUpdated += RefreshBound;

    private void Update()
    {
        if (((inputState == PlacementInputState.Drag && !Input.GetMouseButton((int)MouseButton.Left))
                || (inputState == PlacementInputState.DragAtTime && !Input.GetMouseButton((int)MouseButton.Right)))
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
        var hasHit = Intersections.Raycast(ray, 11, out var hit);
        var provider = hasHit ? hit.GameObject.transform.parent.GetComponent<PlacementProvider>() : null;

        if (HandleExitWhen(
            (!CanInteract && inputState == PlacementInputState.Hover)
            || !hasHit
            || provider == null))
            return;

        if (currentProvider != provider && !boxSelectionPlacement.IsPlacing)
        {
            if (currentProvider != null) Exit(currentProvider);
            currentProvider = provider;

            RefreshBound();
            foreach (var placement in currentProvider.Placements) placement.Initialize(currentProvider);
        }

        if (HandleExitWhen(PersistentUI.Instance.DialogBoxIsEnabled)
            || currentProvider == null
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;

        isOnGrid = true;
        precisionPlacementController.UpdateMousePosition(hit.Point);
        foreach (var placement in currentProvider.Placements) placement.UpdateState(hit, inputState);
    }

    private void OnDestroy()
    {
        gridViewController.OnGridViewUpdated -= RefreshBound;
        Intersections.Clear();
    }

    public void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (!context.performed || currentProvider == null) return;
        foreach (var placement in currentProvider.Placements) placement.Cancel();
    }

    public void OnPlaceObject(InputAction.CallbackContext context)
    {
        if (currentProvider == null
            || !context.performed
            || !KeybindsController.IsMouseInWindow
            || inputState != PlacementInputState.Hover
            || applicationFocusChanged
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || PersistentUI.Instance.DialogBoxIsEnabled
            || !CanInteract)
            return;
        foreach (var placement in currentProvider
            .Placements
            .Where(p => p.AllowPlacement && p.CanPlace))
            placement.Apply();
    }

    public void OnInitiateClickandDrag(InputAction.CallbackContext context)
    {
        if (currentProvider == null || inputState != PlacementInputState.Hover) return;
        if (context.performed)
        {
            foreach (var p in currentProvider.Placements) p.HideVisual();

            var dragRay = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (!Intersections.Raycast(dragRay, 9, out var dragHit)) return;

            var container = currentProvider
                .Placements
                .Where(p => p.CanClickAndDrag)
                .Select(p => p.StartDrag(dragHit.GameObject))
                .FirstOrDefault(con => con != null);
            if (container == null) return;

            inputState = PlacementInputState.Drag;
        }
        else if (context.canceled && inputState == PlacementInputState.Drag) HandleDragFinished();
    }

    public void OnInitiateClickandDragatTime(InputAction.CallbackContext context)
    {
        if (currentProvider == null || inputState != PlacementInputState.Hover) return;
        if (context.performed)
        {
            var dragRay = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (!Intersections.Raycast(dragRay, 9, out var dragHit)) return;

            var (placement, container) = currentProvider
                .Placements
                .Where(p => p.CanClickAndDrag)
                .Select(p => (p, container: p.StartDrag(dragHit.GameObject)))
                .FirstOrDefault(pair => pair.container != null);
            if (container == null) return;

            inputState = PlacementInputState.DragAtTime;
            var newZ = placement.GetContainerPosZ(container);
            currentProvider.Lane.MoveXYGridByZ(newZ);
        }
        else if (context.canceled && inputState == PlacementInputState.DragAtTime)
        {
            currentProvider.Lane.MoveXYGridByZ(0);
            HandleDragFinished();
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context) =>
        mousePosition = Mouse.current.position.ReadValue();

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

    private void RefreshBound()
    {
        if (currentProvider == null) return;

        var boundLocal = currentProvider.Lane.XY.Grid.bounds;
        // Transform the bounds into the pseudo-world space we use for selection
        var localTransform = currentProvider.transform;
        var localScale = localTransform.localScale;
        var boundsNew = localTransform.InverseTransformBounds(boundLocal);
        boundsNew.center += new Vector3(
            localTransform.localPosition.x / localScale.x,
            localTransform.localPosition.y / localScale.y,
            localTransform.localPosition.z / localScale.z);
        boundsNew.extents = new Vector3(
            boundsNew.extents.x / localScale.x,
            boundsNew.extents.y / localScale.y,
            boundsNew.extents.z / localScale.z
        );

        foreach (var placement in currentProvider.Placements)
        {
            placement.Bounds = boundsNew;
            placement.BoundsPosition = localTransform.localPosition;
        }
    }

    private void HandleDragFinished()
    {
        if (inputState == PlacementInputState.Hover) return;
        foreach (var placement in currentProvider.Placements.Where(p => p.IsDragging)) placement.FinishDrag();
        inputState = PlacementInputState.Hover;
    }

    private bool HandleExitWhen(bool shouldExit)
    {
        if (!shouldExit) return false;
        if (currentProvider == null) return true;
        Exit(currentProvider);
        isOnGrid = false;
        return true;
    }

    private void Exit(PlacementProvider provider)
    {
        if (!isOnGrid) return;
        foreach (var placement in provider.Placements) placement.Exit();
    }
}
