using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoxSelectionPlacement : BasePlacement<BaseObstacle, ObstacleContainer, ObstacleGridContainer>,
                                     CMInput.IBoxSelectActions
{
    // Preserve ownership of the click frame after completion because other input callbacks run later that same frame.
    public int LastCompletionFrame { get; private set; } = -1;
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] public CustomEventGridContainer CustomCollection;
    [SerializeField] public EventGridContainer EventGridContainer;
    [SerializeField] public CreateEventTypeLabels Labels;
    [SerializeField] private BeatmapRuntimeContext beatmapContext;
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;
    private readonly Dictionary<int, Dictionary<Type, float>> glsGroupCondition = new();

    private readonly HashSet<BaseObject> selected = new();
    private HashSet<BaseObject> alreadySelected = new();
    private Vector3 originPos;
    // Store both drag corners in beat space so scrolling or BPM changes cannot alter the selection range.
    private float originSongBpmBeat;
    private float currentSongBpmBeat;
    // Resolve cursor time in the active view's timeline coordinate system, not the separate box-rendering track.
    private Transform beatCoordinateTrack;
    private ObjectType selectedTypes = 0;

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

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) => null;

    // TODO: v3 check?
    protected override BaseObstacle GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        selectedTypes = 0;

        // Get all object types from placements in provider
        // Box Select is flagged as "None" so it doesnt interfere with other placements
        foreach (var placement in provider.Placements) selectedTypes |= placement.ObjectDataType;

        glsGroupCondition.Clear();
        if (!provider.TryGetComponent<GLSGroupTrack>(out var glsGroupTrack))
        {
            // Use the active view's placement track for the universal beat axis in note and event views.
            beatCoordinateTrack = provider.Placements
                .FirstOrDefault(placement => !ReferenceEquals(placement, this) && placement.PlacementTrack != null)
                ?.PlacementTrack
                ?? PlacementTrack;
            return;
        }

        // Use the active view's moving timeline transform so cursor and rendered-node beats share one origin.
        beatCoordinateTrack = glsGroupTrack.Track.ObjectParentTransform;

        foreach (var (type, id, offset) in glsGroupGridProvider.ActiveGlsTracks.SelectMany(GetTrackData))
        {
            glsGroupCondition.TryAdd(id, new Dictionary<Type, float>());
            glsGroupCondition[id][type] = offset + (BeatmapConstant.LaneSize / 2f);
        }

        return;

        IEnumerable<(Type, int, float)> GetTrackData(GLSGroupTrack glsTrack)
        {
            var offset = 0f;
            if (glsTrack.TrackDefinition.ColorTrack)
            {
                yield return (typeof(BaseLightColorEventBoxGroup), glsTrack.TrackDefinition.ID,
                    glsTrack.GridLane.transform.localPosition.x
                    + (offset * BeatmapConstant.LaneSize));
                offset++;
            }

            if (glsTrack.TrackDefinition.RotationTracks.Any(x => x))
            {
                yield return (typeof(BaseLightRotationEventBoxGroup), glsTrack.TrackDefinition.ID,
                    glsTrack.GridLane.transform.localPosition.x
                    + (offset * BeatmapConstant.LaneSize));
                offset++;
            }

            if (glsTrack.TrackDefinition.TranslationTracks.Any(x => x))
            {
                yield return (typeof(BaseLightTranslationEventBoxGroup), glsTrack.TrackDefinition.ID,
                    glsTrack.GridLane.transform.localPosition.x
                    + (offset * BeatmapConstant.LaneSize));
                offset++;
            }

            if (glsTrack.TrackDefinition.FloatFXTrack)
            {
                yield return (typeof(BaseVfxEventEventBoxGroup), glsTrack.TrackDefinition.ID,
                    glsTrack.GridLane.transform.localPosition.x
                    + (offset * BeatmapConstant.LaneSize));
            }
        }
    }

    public override void UpdateState(Intersections.IntersectionHit hit, PlacementInputState inputState)
    {
        if (!CanPlace && !IsPlacing)
        {
            if (!PlacementVisualContainer.gameObject.activeSelf) return;
            HideVisual();
            State = PlacementState.Idle;
            return;
        }

        base.UpdateState(hit, inputState);
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        // Convert the cursor through the active timeline track so time is independent of box-renderer offsets.
        currentSongBpmBeat = (beatCoordinateTrack ?? PlacementTrack).InverseTransformPoint(hit.Point).z
            / EditorScaleController.EditorScale;
        LanePosition = new Vector3(
            Mathf.FloorToInt(
                (localPoint.x
                    - (gridViewController.IsOdd
                        ? 0.3f
                        : 0f))
                / BeatmapConstant.LaneSize),
            Mathf.FloorToInt(
                (localPoint.y - BeatmapConstant.YOffset - (BeatmapConstant.LaneSize / 2f)) / BeatmapConstant.LaneSize),
            localPoint.z);

        if (!IsPlacing)
        {
            PlacementVisualContainer.transform.localScale =
                (Vector3.right + Vector3.up + (Vector3.forward * Mathf.Epsilon)) * BeatmapConstant.LaneSize;
            PlacementVisualContainer.transform.localPosition = new Vector3(
                (LanePosition.x * BeatmapConstant.LaneSize)
                + (gridViewController.IsOdd
                    ? BeatmapConstant.LaneSize / 2f
                    : 0f),
                (LanePosition.y * BeatmapConstant.LaneSize) + BeatmapConstant.YOffset + (BeatmapConstant.LaneSize / 2f),
                LanePosition.z);
        }
        else
        {
            var originShove = originPos;
            float sizeX = 1;
            float sizeY = 1;

            // there's probably elegant way to do this,
            // i just cant think now
            if (LanePosition.x < originPos.x)
            {
                var difference = Math.Abs(LanePosition.x - originPos.x);
                sizeX += difference;
                originShove.x -= difference;
            }

            if (LanePosition.y < originPos.y)
            {
                var difference = Math.Abs(LanePosition.y - originPos.y);
                sizeY += difference;
                originShove.y -= difference;
            }

            // Render both beat endpoints through the timeline transform so grid and box depth use identical scaling.
            var startSongBpmBeat = Mathf.Min(originSongBpmBeat, currentSongBpmBeat);
            var endSongBpmBeat = Mathf.Max(originSongBpmBeat, currentSongBpmBeat);
            var startZ = GetBoxLocalZForSongBpmBeat(startSongBpmBeat);
            var endZ = GetBoxLocalZForSongBpmBeat(endSongBpmBeat);

            PlacementVisualContainer.transform.localPosition = new Vector3(
                (originShove.x * BeatmapConstant.LaneSize)
                + (gridViewController.IsOdd
                    ? BeatmapConstant.LaneSize / 2f
                    : 0f),
                (originShove.y * BeatmapConstant.LaneSize) + BeatmapConstant.YOffset + (BeatmapConstant.LaneSize / 2f),
                startZ);
            // Keep both box corners on the same preview positions used before the Ctrl-click began the drag.
            var scale = new Vector3(
                LanePosition.x + sizeX - originShove.x,
                LanePosition.y + sizeY - originShove.y,
                Mathf.Abs(endZ - startZ) + Mathf.Epsilon);
            PlacementVisualContainer.transform.localScale = new Vector3(
                scale.x * BeatmapConstant.LaneSize,
                scale.y * BeatmapConstant.LaneSize,
                scale.z);
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        if (!IsPlacing) return;

        // Select strictly from the two hovered beat values, never from visual or collision coordinates.
        var (startSongBpmBeat, endSongBpmBeat) = GetSongBpmBounds();

        // Doing a jank bitmask to ensure we include all object types in the search
        SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
            startSongBpmBeat,
            endSongBpmBeat,
            selectedTypes,
            (_, bo) =>
            {
                if (!bo.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID)) return;

                // With GLS preview nodes enabled, select groups only by their rendered inner-event beats below.
                if (bo is BaseEventBoxGroup
                    && !Mathf.Approximately(Settings.Instance.GLSOuterTrackGhostNodeOpacity, 0f))
                    return;

                // Default single-lane objects to a guaranteed in-box point before resolving spatial object types.
                var p = (Vector2)PlacementVisualContainer.transform.localPosition;

                switch (bo)
                {
                    case IObjectBounds obj:
                        p = obj.GetCenter();
                        p.y += BeatmapConstant.YOffset + (BeatmapConstant.LaneSize / 2f);
                        break;
                    case BaseNJSEvent:
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

                            p = new Vector2(
                                (position.Value.x * BeatmapConstant.LaneSize) + BoundsPosition.x,
                                (position.Value.y * BeatmapConstant.LaneSize)
                                + BeatmapConstant.YOffset
                                + (BeatmapConstant.LaneSize / 2f));
                            break;
                        }
                    case BaseCustomEvent custom:
                        p = new Vector2(
                            ((0.5f + CustomCollection.CustomEventTypes.IndexOf(custom.Type)) * BeatmapConstant.LaneSize)
                            + BoundsPosition.x,
                            BeatmapConstant.YOffset + BeatmapConstant.LaneSize);
                        break;
                    case BaseEventBoxGroup glsGroup:
                        p = GetGlsGroupSelectionPosition(glsGroup);
                        break;
                    case BaseGLSEvent glsEvent:
                        p = new Vector2(
                            (glsEvent.BoxIndex * BeatmapConstant.LaneSize)
                            + BoundsPosition.x
                            + (BeatmapConstant.LaneSize / 2f),
                            BeatmapConstant.YOffset + BeatmapConstant.LaneSize);
                        break;
                    default:
                        Debug.LogWarning($"Unsupported object type {bo.GetType()} in box selection");
                        return;
                }

                // Check if calculated position is outside bounds.
                if (!IsWithinSelectionXY(p)) return;

                if (!alreadySelected.Contains(bo) && selected.Add(bo))
                    SelectionController.Select(bo, true, false, false);
            });

        // Include logical groups represented by visible inner-event preview nodes inside the box's beat range.
        SelectGlsGroupsFromPreviewNodes(startSongBpmBeat, endSongBpmBeat);

        foreach (var combinedObj in SelectionController
            .SelectedObjects
            .Where(combinedObj => !selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
            .ToArray())
            SelectionController.Deselect(combinedObj, false);

        selected.Clear();
    }

    // Select an owning GLS group when any of its rendered preview-node times falls inside the current box.
    private void SelectGlsGroupsFromPreviewNodes(float startSongBpmBeat, float endSongBpmBeat)
    {
        if (Mathf.Approximately(Settings.Instance.GLSOuterTrackGhostNodeOpacity, 0f)) return;

        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        for (var typeInt = 1; typeInt <= 32; typeInt++)
        {
            var type = (ObjectType)(1 << typeInt);
            if ((selectedTypes & type) == 0) continue;

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(type);
            if (collection == null) continue;

            foreach (var group in collection.LoadedObjects.OfType<BaseEventBoxGroup>())
            {
                if (!group.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID)
                    || !IsWithinSelectionXY(GetGlsGroupSelectionPosition(group)))
                    continue;

                var previewEvent = group.ReadOnlyBoxes
                    .SelectMany(box => box.ReadOnlyEvents)
                    .FirstOrDefault(evt => startSongBpmBeat - epsilon < evt.SongBpmTime
                        && evt.SongBpmTime < endSongBpmBeat + epsilon);
                if (previewEvent == null || alreadySelected.Contains(group)) continue;

                // Retain the group in this frame's box result without repeatedly selecting or logging it.
                selected.Add(group);
                if (SelectionController.IsObjectSelected(group)) continue;

                // Keep targeted runtime evidence until ghost-node box selection is confirmed.
                // Debug.Log(
                //     $"[BoxSelection] Selected GLS group id={group.ID} via preview node at beat "
                //     + $"{previewEvent.SongBpmTime:F6}.");
                SelectionController.Select(group, true, false, false);
            }
        }
    }

    // Resolve a GLS group lane once for both primary-node and ghost-node selection checks.
    private Vector2 GetGlsGroupSelectionPosition(BaseEventBoxGroup group)
    {
        float offset = short.MinValue;
        if (glsGroupCondition.TryGetValue(group.ID, out var typeToOffset))
            offset = typeToOffset.GetValueOrDefault(group.GetType(), short.MinValue);
        return new Vector2(offset, BeatmapConstant.YOffset + BeatmapConstant.LaneSize);
    }

    // Apply the box's current horizontal and vertical bounds to a resolved node position.
    private bool IsWithinSelectionXY(Vector2 position)
    {
        var left = PlacementVisualContainer.transform.localPosition.x
            + PlacementVisualContainer.transform.localScale.x;
        var right = PlacementVisualContainer.transform.localPosition.x;
        if (right < left) (left, right) = (right, left);

        var top = PlacementVisualContainer.transform.localPosition.y
            + PlacementVisualContainer.transform.localScale.y;
        var bottom = PlacementVisualContainer.transform.localPosition.y;
        if (top < bottom) (top, bottom) = (bottom, top);

        return position.x >= left && position.x <= right && position.y >= bottom && position.y < top;
    }

    public override void HandleApply()
    {
        // Record placement state at click handling time to establish ordering against outer GLS group entry.
        // Debug.Log($"[GLS Drag Box] frame={Time.frameCount}, stateBefore={State}, isPlacing={IsPlacing}.");
        if (IsPlacing)
        {
            // Keep the completed beat-space range in the diagnostic so it can be checked against the cursor beats.
            var (startSongBpmBeat, endSongBpmBeat) = GetSongBpmBounds();
            var visualStartZ = PlacementVisualContainer.transform.localPosition.z;
            var visualEndZ = visualStartZ
                + Mathf.Max(0f, PlacementVisualContainer.transform.localScale.z - Mathf.Epsilon);
            // Debug.Log(
            //     $"[BoxSelection] Completed selection: calculated beats {startSongBpmBeat:F6} to "
            //     + $"{endSongBpmBeat:F6}; visual local Z {visualStartZ:F6} to {visualEndZ:F6}; "
            //     + $"editor scale {EditorScaleController.EditorScale:F6}; track local Z "
            //     + $"{PlacementVisualContainer.transform.parent.localPosition.z:F6}.");

            // Latch the finishing click so hovered objects cannot consume it after this placement returns to Idle.
            LastCompletionFrame = Time.frameCount;
            State = PlacementState.Idle;
            Exit();
            selected.Clear(); // oh shit turned out i didnt need to rewrite the whole thing, just move it over here
            SelectionController.OnSelectionChanged?.Invoke();
        }
        else
        {
            State = PlacementState.Placing;
            originPos = LanePosition;
            // Capture the exact unsnapped start beat once; subsequent scroll movement must not move this endpoint.
            originSongBpmBeat = currentSongBpmBeat;
            alreadySelected = new HashSet<BaseObject>(SelectionController.SelectedObjects);
        }
    }

    // Normalize the drag's immutable start beat and latest hovered beat for all selection paths.
    private (float Start, float End) GetSongBpmBounds()
    {
        return originSongBpmBeat <= currentSongBpmBeat
            ? (originSongBpmBeat, currentSongBpmBeat)
            : (currentSongBpmBeat, originSongBpmBeat);
    }

    // Convert an absolute beat through the active timeline before placing the visual in the box track's local space.
    private float GetBoxLocalZForSongBpmBeat(float songBpmBeat)
    {
        var timelineTrack = beatCoordinateTrack ?? PlacementTrack;
        var timelinePoint = timelineTrack.TransformPoint(Vector3.forward * (songBpmBeat * EditorScaleController.EditorScale));
        return PlacementVisualContainer.transform.parent.InverseTransformPoint(timelinePoint).z;
    }

    public override void Exit()
    {
        if (IsPlacing) return;
        ResetHysteresis();
        HideVisual();
    }

    public override void Cancel()
    {
        base.Cancel();
        if (!IsPlacing) return;
        State = PlacementState.Idle;
        foreach (var selectedObject in selected) SelectionController.Deselect(selectedObject, false);
        SelectionController.OnSelectionChanged?.Invoke();
    }
}
