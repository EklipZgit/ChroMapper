using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoxSelectionPlacement : BasePlacement<BaseEvent, EventContainer, EventGridContainer>,
                                     CMInput.IBoxSelectActions
{
    [SerializeField] public CustomEventGridContainer CustomCollection;
    [SerializeField] public EventGridContainer EventGridContainer;
    [SerializeField] public CreateEventTypeLabels Labels;

    private readonly HashSet<BaseObject> selected = new();
    private readonly HashSet<ObjectType> selectedTypes = new();
    private HashSet<BaseObject> alreadySelected = new();
    private Vector3 originPos;

    public override bool CanClickAndDrag => false;

    public override bool CanPlace => Settings.Instance.BoxSelect && State != PlacementState.Idle;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || PlacementVisualContainer is null) return;
        Gizmos.color = Color.red;
        var boxyBoy = PlacementVisualContainer.GetComponent<BoxCollider>();
        if (boxyBoy == null) return;
        var bounds = new Bounds
        {
            center = boxyBoy.bounds.center, size = PlacementVisualContainer.transform.lossyScale / 2f
        };
        Gizmos.DrawMesh(
            PlacementVisualContainer.GetComponentInChildren<MeshFilter>().mesh,
            bounds.center,
            PlacementVisualContainer.transform.rotation,
            bounds.size);
    }

    public void OnActivateBoxSelect(InputAction.CallbackContext context)
    {
        if (!IsPlacing) State = context.performed ? PlacementState.Active : PlacementState.Idle;
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) => null;

    // TODO: v3 check?
    protected override BaseEvent GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        selectedTypes.Clear();
        foreach (var objectType in provider
            .Placements
            .Where(p => p.GetType() != GetType())
            .Select(p => p.ObjectDataType))
            selectedTypes.Add(objectType);
    }

    public override void UpdateState(Intersections.IntersectionHit hit, PlacementInputState inputState)
    {
        if (!CanPlace)
        {
            if (!PlacementVisualContainer.gameObject.activeSelf) return;
            HideVisual();
            State = PlacementState.Idle;
            return;
        }

        base.UpdateState(hit, inputState);
    }

    protected override void UpdatePlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        localPoint.x = Mathf.Clamp(Mathf.Floor(localPoint.x), Bounds.min.x, Bounds.max.x - 1);
        localPoint.y = Mathf.Clamp(Mathf.Floor(localPoint.y), Bounds.min.y, Bounds.max.y - 1);

        if (!IsPlacing)
        {
            PlacementVisualContainer.transform.localPosition = localPoint;
            PlacementVisualContainer.transform.localScale =
                Vector3.right + Vector3.up + (Vector3.forward * Mathf.Epsilon);
        }
        else
        {
            var originShove = originPos;
            float sizeX = 1;
            float sizeY = 1;

            // there's probably elegant way to do this,
            // i just cant think now
            if (localPoint.x < originPos.x)
            {
                var difference = Math.Abs(localPoint.x - originPos.x);
                sizeX += difference;
                originShove.x -= difference;
            }

            if (localPoint.y < originPos.y)
            {
                var difference = Math.Abs(localPoint.y - originPos.y);
                sizeY += difference;
                originShove.y -= difference;
            }

            PlacementVisualContainer.transform.localPosition = originShove;
            var newLocalScale = localPoint + new Vector3(sizeX, sizeY, 0.5f) - originShove;
            PlacementVisualContainer.transform.localScale = newLocalScale;
        }
    }

    protected override void UpdateData(PlacementInputState inputState)
    {
        if (!IsPlacing) return;

        var startSongBpmBeat =
            PlacementVisualContainer.transform.localPosition.z / EditorScaleController.EditorScale;
        var endSongBpmBeat = (PlacementVisualContainer.transform.localPosition.z
                + PlacementVisualContainer.transform.localScale.z)
            / EditorScaleController.EditorScale;
        if (startSongBpmBeat > endSongBpmBeat) (startSongBpmBeat, endSongBpmBeat) = (endSongBpmBeat, startSongBpmBeat);

        SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
            startSongBpmBeat,
            endSongBpmBeat,
            true,
            true,
            true,
            true,
            (_, bo) =>
            {
                if (!selectedTypes.Contains(bo.ObjectType)) return;

                if (!bo.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID)) return;

                var left = PlacementVisualContainer.transform.localPosition.x
                    + PlacementVisualContainer.transform.localScale.x;
                var right = PlacementVisualContainer.transform.localPosition.x;
                if (right < left) (left, right) = (right, left);

                var top = PlacementVisualContainer.transform.localPosition.y
                    + PlacementVisualContainer.transform.localScale.y;
                var bottom = PlacementVisualContainer.transform.localPosition.y;
                if (top < bottom) (top, bottom) = (bottom, top);

                var p = new Vector2(left, bottom);

                switch (bo)
                {
                    case IObjectBounds obj:
                        p = obj.GetCenter();
                        break;
                    case BaseBpmEvent:
                        // Bpm events are in a separate single lane so we don't need to get position
                        break;
                    case BaseEvent evt:
                        {
                            var position = evt.GetPosition(
                                Labels,
                                EventGridContainer.PropagationEditing,
                                EventGridContainer.EventTypeToPropagate);

                            // Not visible = notselectable
                            if (!position.HasValue) return;

                            p = new Vector2(position.Value.x + Bounds.min.x, position.Value.y);
                            break;
                        }
                    case BaseCustomEvent custom:
                        p = new Vector2(
                            CustomCollection.CustomEventTypes.IndexOf(custom.Type) + Bounds.min.x + 0.5f,
                            0.5f);
                        break;
                }

                // Check if calculated position is outside bounds
                if (p.x < left || p.x > right || p.y < bottom || p.y >= top) return;

                if (!alreadySelected.Contains(bo) && selected.Add(bo))
                    SelectionController.Select(bo, true, false, false);
            });

        foreach (var combinedObj in SelectionController
            .SelectedObjects
            .Where(combinedObj => !selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
            .ToArray())
            SelectionController.Deselect(combinedObj, false);

        selected.Clear();
    }

    public override void HandleApply()
    {
        if (IsPlacing)
        {
            State = PlacementState.Idle;
            selected.Clear(); // oh shit turned out i didnt need to rewrite the whole thing, just move it over here
            SelectionController.OnSelectionChanged?.Invoke();
        }
        else
        {
            State = PlacementState.Placing;
            originPos = PlacementVisualContainer.transform.localPosition;
            alreadySelected = new HashSet<BaseObject>(SelectionController.SelectedObjects);
        }
    }

    public override void Cancel()
    {
        if (!IsPlacing) return;
        State = PlacementState.Idle;
        foreach (var selectedObject in selected) SelectionController.Deselect(selectedObject, false);
        SelectionController.OnSelectionChanged?.Invoke();
    }

    protected override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued) { }
}
