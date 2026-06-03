using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class GlobalIntersectionCache
{
    public static GameObject FirstHit;
    public static bool HasHit;
    public static bool HasRaycastThisFrame;
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
    [SerializeField] protected EditModeContext EditContext;
    [SerializeField] private EditingMode editMode;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private bool ignoreBaseInput;

    protected bool MassSelect;
    private Vector2 mousePosition;
    private float timeWhenFirstSelecting;
    private List<Intersections.IntersectionHit> preAllocIntersections = new();

    private void Start() => DeleteToolController.OnDeleteToolActivated += HandleDeleteToolActivated;

    private void OnDestroy() => DeleteToolController.OnDeleteToolActivated -= HandleDeleteToolActivated;

    private void HandleDeleteToolActivated()
    {
        if (IsHovering) HoveredObject.RefreshOutlineColor();
    }

    // Update is called once per frame
    private void Update()
    {
        if ((EditContext.EditingMode & editMode) == 0)
        {
            if (IsHovering) HoveredObject.Highlighted = false;
            IsHovering = false;
            return;
        }

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
        Intersections.RaycastAllNoAlloc(ray, 9, ref preAllocIntersections);
        foreach (var hit in preAllocIntersections)
        {
            if (!GetComponentFromTransform(hit.GameObject, out var obj)) continue;
            if (!SelectionController.IsObjectSelected(obj.ObjectData)) SelectionController.Select(obj.ObjectData, true);
        }
    }

    protected virtual void LateUpdate()
    {
        GlobalIntersectionCache.FirstHit = null;
        GlobalIntersectionCache.HasHit = false;
        GlobalIntersectionCache.HasRaycastThisFrame = false;
    }

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput || (DeleteToolController.IsActive && context.performed)) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return; //Returns if the mouse is on top of UI

        if (!Application.isFocused) return;

        RaycastFirstObject(out var obj);
        if (obj != null && !obj.Dragged && context.performed) CompleteDelete(obj);
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput
            || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
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
        if (ignoreBaseInput || !context.performed) return; // TODO: Find a way to detect if other keybinds are held
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
        if (!GlobalIntersectionCache.HasRaycastThisFrame)
        {
            var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (Intersections.Raycast(ray, 9, out var hit))
            {
                GlobalIntersectionCache.FirstHit = hit.GameObject;
                GlobalIntersectionCache.HasHit = hit.GameObject != null;
            }

            GlobalIntersectionCache.HasRaycastThisFrame = true;
        }

        if (!GlobalIntersectionCache.HasHit)
        {
            firstObject = null;
            return false;
        }

        var container = GlobalIntersectionCache.FirstHit.GetComponentInParent<TContainer>();
        if (container != null && ValidObject(container))
        {
            firstObject = container;
            return true;
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
