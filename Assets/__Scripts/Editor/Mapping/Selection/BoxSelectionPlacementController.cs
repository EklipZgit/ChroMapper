using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BoxSelectionPlacementController : PlacementController<BaseEvent, EventContainer, EventGridContainer>,
                                               CMInput.IBoxSelectActions
{
    [SerializeField] public CustomEventGridContainer CustomCollection;
    [SerializeField] public EventGridContainer EventGridContainer;
    [SerializeField] public CreateEventTypeLabels Labels;

    private readonly HashSet<BaseObject> selected = new();

    private readonly List<ObjectType> selectedTypes = new();
    private HashSet<BaseObject> alreadySelected = new();

    public static bool KeybindPressed;
    private Vector3 originPos;
    private Intersections.IntersectionHit previousHit;
    private Vector3 transformed;
    public static bool SelectActivated { get; private set; }
    public static bool IsSelecting { get; private set; }

    protected override bool CanClickAndDrag { get; set; } = false;

    public override bool IsValid => Settings.Instance.BoxSelect && (KeybindPressed || IsSelecting);

    public override int PlacementXMin => int.MinValue;

    public override int PlacementXMax => int.MaxValue;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || instantiatedContainer is null) return;
        Gizmos.color = Color.red;
        var boxyBoy = instantiatedContainer.GetComponent<BoxCollider>();
        if (boxyBoy == null) return;
        var bounds = new Bounds
        {
            center = boxyBoy.bounds.center, size = instantiatedContainer.transform.lossyScale / 2f
        };
        Gizmos.DrawMesh(
            instantiatedContainer.GetComponentInChildren<MeshFilter>().mesh,
            bounds.center,
            instantiatedContainer.transform.rotation,
            bounds.size);
    }

    public void OnActivateBoxSelect(InputAction.CallbackContext context) =>
        KeybindPressed = context.performed;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) => null;

    // TODO: v3 check?
    public override BaseEvent GenerateOriginalData() => new();

    protected override bool TestForType<T>(Intersections.IntersectionHit hit, ObjectType type)
    {
        if (!base.TestForType<T>(hit, type)) return false;
        selectedTypes.Add(type);
        return true;
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        previousHit = hit;
        transformed = transformedPoint;

        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);

        roundedHit = new Vector3(
            Mathf.Ceil(roundedHit.x),
            Mathf.Ceil(roundedHit.y),
            roundedHit.z
        );

        instantiatedContainer.transform.localPosition = roundedHit - new Vector3(0.5f, 1, 0);
        if (!IsSelecting)
        {
            Bounds = default;
            selectedTypes.Clear();
            TestForType<EventPlacement>(hit, ObjectType.Event);
            TestForType<NotePlacement>(hit, ObjectType.Note);
            TestForType<ObstaclePlacement>(hit, ObjectType.Obstacle);
            TestForType<CustomEventPlacement>(hit, ObjectType.CustomEvent);
            TestForType<BPMChangePlacement>(hit, ObjectType.BpmChange);
            TestForType<ArcPlacement>(hit, ObjectType.Arc);
            TestForType<ChainPlacement>(hit, ObjectType.Chain);
            TestForType<NJSEventPlacement>(hit, ObjectType.NJSEvent);

            instantiatedContainer.transform.localScale =
                Vector3.right + Vector3.up + (Vector3.forward * 0.001f); // temporary fix to nuclear bloom
            var localScale = instantiatedContainer.transform.localScale;
            instantiatedContainer.transform.localPosition -= new Vector3(localScale.x / 2, 0, 0);
        }
        else
        {
            var originShove = originPos;
            float xOffset = 0;
            float yOffset = 0;

            // When moving from right to left, move the origin to the right and make
            // the selection larger as the origin points are on the left
            if (roundedHit.x <= originPos.x + 1)
            {
                xOffset = -1;
                originShove.x += 1;
            }

            if (roundedHit.y <= originPos.y)
            {
                yOffset = -1;
                originShove.y += 1;
            }

            instantiatedContainer.transform.localPosition = originShove;
            var newLocalScale = roundedHit + new Vector3(xOffset, yOffset, 0.5f) - originShove;

            var newLocalScaleY = Mathf.Max(newLocalScale.y, 1);
            if (yOffset < 0) newLocalScaleY = Mathf.Min(-1, newLocalScale.y);

            newLocalScale = new Vector3(newLocalScale.x, newLocalScaleY, newLocalScale.z);
            instantiatedContainer.transform.localScale = newLocalScale;

            var startSongBpmBeat = instantiatedContainer.transform.localPosition.z / EditorScaleController.EditorScale;
            var endSongBpmBeat = (instantiatedContainer.transform.localPosition.z + newLocalScale.z)
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

                    if (!bo.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID))
                    {
                        return;
                    }

                    var left = instantiatedContainer.transform.localPosition.x
                        + instantiatedContainer.transform.localScale.x;
                    var right = instantiatedContainer.transform.localPosition.x;
                    if (right < left) (left, right) = (right, left);

                    var top = instantiatedContainer.transform.localPosition.y
                        + instantiatedContainer.transform.localScale.y;
                    var bottom = instantiatedContainer.transform.localPosition.y;
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

    public override void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        if (!IsValid && IsSelecting) StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
        base.OnMousePositionUpdate(context);
    }

    internal override void ApplyToMap()
    {
        if (!IsSelecting)
        {
            IsSelecting = true;
            originPos = instantiatedContainer.transform.localPosition;
            alreadySelected = new HashSet<BaseObject>(SelectionController.SelectedObjects);
        }
        else
        {
            StartCoroutine(WaitABitFuckOffOtherPlacementControllers());
        }
    }

    private IEnumerator WaitABitFuckOffOtherPlacementControllers()
    {
        yield return new WaitForSeconds(0.1f);
        IsSelecting = false;
        selected.Clear(); // oh shit turned out i didnt need to rewrite the whole thing, just move it over here
        OnPhysicsRaycast(previousHit, transformed);
        SelectionController.OnSelectionChanged?.Invoke();
    }

    public override void CancelPlacement()
    {
        if (IsSelecting)
        {
            IsSelecting = false;
            foreach (var selectedObject in selected) SelectionController.Deselect(selectedObject, false);
            SelectionController.OnSelectionChanged?.Invoke();
        }
    }

    public override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued) { }
}
