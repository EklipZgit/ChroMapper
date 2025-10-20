using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoxSelectionPlacementController : BasePlacement<BaseEvent, EventContainer, EventGridContainer>,
                                               CMInput.IBoxSelectActions
{
    public static SelectionState State;
    [SerializeField] public CustomEventGridContainer CustomCollection;
    [SerializeField] public EventGridContainer EventGridContainer;
    [SerializeField] public CreateEventTypeLabels Labels;

    private readonly HashSet<BaseObject> selected = new();

    private readonly List<ObjectType> selectedTypes = new();
    private HashSet<BaseObject> alreadySelected = new();
    private Vector3 originPos;

    public override bool CanClickAndDrag => false;

    public override bool CanPlace => Settings.Instance.BoxSelect && State != SelectionState.Idle;

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
        if (State != SelectionState.Selecting) State = context.performed ? SelectionState.Standby : SelectionState.Idle;
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) => null;

    // TODO: v3 check?
    protected override BaseEvent GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        selectedTypes.Clear();
        selectedTypes.AddRange(
            provider
                .Placements
                .Where(p => p.GetType() != GetType())
                .Select(p => p.ObjectDataType));
    }

    protected override void UpdatePlacement(
        Vector3 rawHit,
        Vector3 roundedHit,
        PlacementState state)
    {
        rawHit = new Vector3(
            Mathf.Ceil(rawHit.x - VanillaOffset.x),
            Mathf.Ceil(rawHit.y - VanillaOffset.y),
            rawHit.z
        );

        PlacementVisualContainer.transform.localPosition = rawHit - new Vector3(0.5f, 1, 0);
        if (State != SelectionState.Selecting)
        {
            PlacementVisualContainer.transform.localScale =
                Vector3.right + Vector3.up + (Vector3.forward * 0.001f); // temporary fix to nuclear bloom
            var localScale = PlacementVisualContainer.transform.localScale;
            PlacementVisualContainer.transform.localPosition -= new Vector3(localScale.x / 2, 0, 0);
        }
        else
        {
            var originShove = originPos;
            float xOffset = 0;
            float yOffset = 0;

            // When moving from right to left, move the origin to the right and make
            // the selection larger as the origin points are on the left
            if (rawHit.x <= originPos.x + 1)
            {
                xOffset = -1;
                originShove.x += 1;
            }

            if (rawHit.y <= originPos.y)
            {
                yOffset = -1;
                originShove.y += 1;
            }

            PlacementVisualContainer.transform.localPosition = originShove;
            var newLocalScale = rawHit + new Vector3(xOffset, yOffset, 0.5f) - originShove;

            var newLocalScaleY = Mathf.Max(newLocalScale.y, 1);
            if (yOffset < 0) newLocalScaleY = Mathf.Min(-1, newLocalScale.y);

            newLocalScale = new Vector3(newLocalScale.x, newLocalScaleY, newLocalScale.z);
            PlacementVisualContainer.transform.localScale = newLocalScale;

            var startSongBpmBeat =
                PlacementVisualContainer.transform.localPosition.z / EditorScaleController.EditorScale;
            var endSongBpmBeat = (PlacementVisualContainer.transform.localPosition.z + newLocalScale.z)
                / EditorScaleController.EditorScale;
            if (startSongBpmBeat > endSongBpmBeat)
                (startSongBpmBeat, endSongBpmBeat) = (endSongBpmBeat, startSongBpmBeat);

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

            foreach (var combinedObj in SelectionController.SelectedObjects.ToArray())
            {
                if (!selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
                    SelectionController.Deselect(combinedObj, false);
            }

            selected.Clear();
        }
    }

    public override void HandleApply()
    {
        if (State == SelectionState.Selecting)
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
        else
        {
            State = SelectionState.Selecting;
            originPos = PlacementVisualContainer.transform.localPosition;
            alreadySelected = new HashSet<BaseObject>(SelectionController.SelectedObjects);
        }
    }

    private IEnumerator WaitABitFuckOffOtherPlacementControllers()
    {
        yield return new WaitForSeconds(0.1f);
        State = SelectionState.Idle;
        selected.Clear(); // oh shit turned out i didnt need to rewrite the whole thing, just move it over here
        // HandlePhysicsRaycast();
        SelectionController.OnSelectionChanged?.Invoke();
    }

    public override void Cancel()
    {
        if (State != SelectionState.Selecting) return;
        State = SelectionState.Idle;
        foreach (var selectedObject in selected) SelectionController.Deselect(selectedObject, false);
        SelectionController.OnSelectionChanged?.Invoke();
    }

    protected override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued) { }
}
