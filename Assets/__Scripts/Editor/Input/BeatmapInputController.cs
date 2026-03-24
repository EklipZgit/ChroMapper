using System;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GlobalIntersectionCache
{
    internal static GameObject firstHit;
}

public class BeatmapInputController<TContainer> : MonoBehaviour, CMInput.IBeatmapObjectsActions
    where TContainer : ObjectContainer
{
    [Header("State")] public bool IsSelecting;
    public bool IsHovering;
    public TContainer HoveredObject;

    [Header("Dependencies")] [SerializeField]
    protected CustomStandaloneInputModule CustomStandaloneInputModule;

    [SerializeField] private CameraManager cameraManager;
    [SerializeField] protected EditModeContext editContext;
    [SerializeField] private EditingMode editMode;
    [SerializeField] private ObstaclePlacement obstaclePlacement;

    protected bool MassSelect;
    private Vector2 mousePosition;
    private float timeWhenFirstSelecting;

    private void Start() => DeleteToolController.OnDeleteToolActivated += HandleDeleteToolActivated;
    private void OnDestroy() => DeleteToolController.OnDeleteToolActivated -= HandleDeleteToolActivated;

    private void HandleDeleteToolActivated()
    {
        if (IsHovering) HoveredObject.RefreshOutlineColor();
    }

    // Update is called once per frame
    private void Update()
    {
        if (!editContext.EditingMode.HasFlag(editMode)) return;
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (obstaclePlacement.IsPlacing)
        {
            timeWhenFirstSelecting = Time.time;
            return;
        }

        if (Application.isFocused && RaycastFirstObject(out var first))
        {
            if (HoveredObject != first && IsHovering) HoveredObject.Highlighted = false;
            HoveredObject = first;
            HoveredObject.Highlighted = true;
            IsHovering = true;
        }
        else if (IsHovering)
        {
            HoveredObject.Highlighted = false;
            IsHovering = false;
        }
        else
            IsHovering = false;

        if (!IsSelecting || Time.time - timeWhenFirstSelecting < 0.5f) return;
        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
        foreach (var hit in Intersections.RaycastAll(ray, 9))
        {
            if (!GetComponentFromTransform(hit.GameObject, out var obj)) continue;
            if (!SelectionController.IsObjectSelected(obj.ObjectData)) SelectionController.Select(obj.ObjectData, true);
        }
    }

    private void LateUpdate() => GlobalIntersectionCache.firstHit = null;

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (DeleteToolController.IsActive && context.performed) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return; //Returns if the mouse is on top of UI

        if (!Application.isFocused) return;

        RaycastFirstObject(out var obj);
        if (obj != null && !obj.Dragged && context.performed) CompleteDelete(obj);
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || obstaclePlacement.IsPlacing)
            return;

        IsSelecting = context.performed;
        if (!context.performed) return;
        timeWhenFirstSelecting = Time.time;
        if (!RaycastFirstObject(out var firstObject)) return;
        var obj = firstObject.ObjectData;
        if (MassSelect
            && SelectionController.SelectedObjects.Count == 1
            && SelectionController.SelectedObjects.First() != obj)
            SelectionController.SelectBetween(SelectionController.SelectedObjects.First(), obj, true);
        else if (SelectionController.IsObjectSelected(obj))
            SelectionController.Deselect(obj);
        else if (!SelectionController.IsObjectSelected(obj)) SelectionController.Select(obj, true);
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context) =>
        mousePosition = context.ReadValue<Vector2>();

    public void OnJumptoObjectTime(InputAction.CallbackContext context)
    {
        if (!context.performed) return; // TODO: Find a way to detect if other keybinds are held
        RaycastFirstObject(out var con);
        if (con != null)
        {
            // TODO make this use an AudioTimeSyncController reference when Zenject is added.
            BeatmapObjectContainerCollection
                .GetCollectionForType(con.ObjectData.ObjectType)
                .BeatmapContext.Atsc.MoveToSongBpmTime(con.ObjectData.SongBpmTime);
        }
    }

    public void OnMassSelectModifier(InputAction.CallbackContext context) => MassSelect = context.performed;

    protected virtual bool GetComponentFromTransform(GameObject t, out TContainer obj) => t.TryGetComponent(out obj);

    protected bool RaycastFirstObject(out TContainer firstObject)
    {
        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
        if (GlobalIntersectionCache.firstHit == null)
        {
            if (Intersections.Raycast(ray, 9, out var hit)) GlobalIntersectionCache.firstHit = hit.GameObject;
        }

        if (GlobalIntersectionCache.firstHit != null)
        {
            var container = GlobalIntersectionCache.firstHit.GetComponentInParent<TContainer>();
            if (container != null && ValidObject(container))
            {
                firstObject = container;
                return true;
            }
        }

        firstObject = null;
        return false;
    }

    protected virtual bool ValidObject(TContainer container) => true;

    public void CompleteDelete(TContainer obj)
    {
        BeatmapObjectContainerCollection
            .GetCollectionForType(obj.ObjectData.ObjectType)
            .DeleteObject(obj.ObjectData, true, true, "Deleted by the user.");
    }
}
