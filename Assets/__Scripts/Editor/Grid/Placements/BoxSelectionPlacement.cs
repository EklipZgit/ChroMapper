using System;
using System.Collections.Generic;
using ZLinq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoxSelectionPlacement : BasePlacement<BaseObstacle, ObstacleContainer, ObstacleGridContainer>,
                                     CMInput.IBoxSelectActions
{
    // Distinguish no-op, complete, and monotonic-expansion queries in the scrolling hot path.
    private enum SelectionQueryMode : byte
    {
        None,
        Full,
        Expand
    }

    // Mask preview-backed GLS groups out of the normal backing query when their child beats drive selection.
    private const ObjectType GlsGroupObjectTypes = ObjectType.GLSColor
        | ObjectType.GLSRotation
        | ObjectType.GLSTranslation
        | ObjectType.GLSFloatFx;

    // Preview-node selection only applies to the four GLS collection types.
    private static readonly ObjectType[] glsObjectTypes =
    {
        ObjectType.GLSColor,
        ObjectType.GLSRotation,
        ObjectType.GLSTranslation,
        ObjectType.GLSFloatFx
    };

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
    // Reuse logical GLS candidates while merging beat-range, loaded carry-in, and selected carry-in sources.
    private readonly HashSet<BaseEventBoxGroup> glsPreviewCandidates = new();
    // Reuse selection snapshots and mutation buffers so each hover frame remains allocation-free.
    private readonly HashSet<BaseObject> alreadySelected = new();
    private readonly List<BaseObject> deselectionBuffer = new();
    private Action<BeatmapObjectContainerCollection, BaseObject> selectionCandidateCallback;
    private bool hasPreviousSnappedState;
    private bool hasPreviousSelectionQuery;
    private Vector3 originPos;
    // Store both drag corners in beat space so scrolling or BPM changes cannot alter the selection range.
    private float originSongBpmBeat;
    private float currentSongBpmBeat;
    // Resolve cursor time in the active view's timeline coordinate system, not the separate box-rendering track.
    private Transform beatCoordinateTrack;
    private Vector2 previousSnappedState;
    private ObjectType selectedTypes = 0;
    private float selectionLeft;
    private float selectionRight;
    private float selectionBottom;
    private float selectionTop;
    private float previousSelectionStartBeat;
    private float previousSelectionEndBeat;
    private float previousSelectionLeft;
    private float previousSelectionRight;
    private float previousSelectionBottom;
    private float previousSelectionTop;

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
            // Resolve the timeline track with Unity-aware null checks and no temporary LINQ iterator.
            beatCoordinateTrack = PlacementTrack;
            foreach (var placement in provider.Placements)
            {
                if (ReferenceEquals(placement, this) || placement.PlacementTrack == null)
                    continue;
                beatCoordinateTrack = placement.PlacementTrack;
                break;
            }

            return;
        }

        // Use the active view's moving timeline transform so cursor and rendered-node beats share one origin.
        beatCoordinateTrack = glsGroupTrack.Track.ObjectParentTransform;

        foreach (var (type, id, offset) in glsGroupGridProvider.ActiveGlsTracks.AsValueEnumerable().SelectMany(GetTrackData))
        {
            glsGroupCondition.TryAdd(id, new Dictionary<Type, float>());
            glsGroupCondition[id][type] = offset / BeatmapConstant.LaneSize;
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

            if (glsTrack.TrackDefinition.RotationTracks.AsValueEnumerable().Any(x => x))
            {
                yield return (typeof(BaseLightRotationEventBoxGroup), glsTrack.TrackDefinition.ID,
                    glsTrack.GridLane.transform.localPosition.x
                    + (offset * BeatmapConstant.LaneSize));
                offset++;
            }

            if (glsTrack.TrackDefinition.TranslationTracks.AsValueEnumerable().Any(x => x))
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

    protected override void ResetHysteresis()
    {
        base.ResetHysteresis();
        hasPreviousSnappedState = false;
        previousSnappedState = Vector2.zero;
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
        // // Convert the cursor through the active timeline track so time is independent of box-renderer offsets.
        // // Unity transforms require their overloaded null comparison when selecting the timeline fallback.
        var timelineTrack = beatCoordinateTrack != null ? beatCoordinateTrack : PlacementTrack;
        currentSongBpmBeat = timelineTrack.InverseTransformPoint(hit.Point).z
            / EditorScaleController.EditorScale;

        var raw = (Vector2)localPoint;
        raw.x -= gridViewController.IsOdd ? 0.5f : 0f;
        if (!hasPreviousSnappedState)
        {
            previousSnappedState = new Vector2(Mathf.Floor(raw.x), Mathf.Floor(raw.y));
            hasPreviousSnappedState = true;
        }
        else
            previousSnappedState = BeatmapPositionHelper.SnapWithHysteresis(raw, previousSnappedState);

        LanePosition = new Vector3(
            previousSnappedState.x,
            previousSnappedState.y,
            localPoint.z);

        if (!IsPlacing)
        {
            PlacementVisualContainer.transform.localScale =
                Vector3.right + Vector3.up + (Vector3.forward * Mathf.Epsilon);
        }
        else
        {
            var originShove = originPos;
            var sizeX = 1f;
            var sizeY = 1f;

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

            // // Render both beat endpoints through the timeline transform so grid and box depth use identical scaling.
            // // Keep both box corners on the same preview positions used before the Ctrl-click began the drag.
            var startSongBpmBeat = Mathf.Min(originSongBpmBeat, currentSongBpmBeat);
            var endSongBpmBeat = Mathf.Max(originSongBpmBeat, currentSongBpmBeat);
            var startZ = GetBoxLocalZForSongBpmBeat(startSongBpmBeat);
            var endZ = GetBoxLocalZForSongBpmBeat(endSongBpmBeat);
            PlacementVisualContainer.transform.localScale = new Vector3(
                LanePosition.x + sizeX - originShove.x,
                LanePosition.y + sizeY - originShove.y,
                Mathf.Abs(endZ - startZ) + Mathf.Epsilon);
            LanePosition = new Vector3(originShove.x, originShove.y, startZ);
        }

        PlacementVisualContainer.transform.localPosition = new Vector3(
            LanePosition.x + (gridViewController.IsOdd ? 0.5f : 0f),
            LanePosition.y,
            LanePosition.z);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        if (!IsPlacing) return;

        // // Select strictly from the two hovered beat values, never from visual or collision coordinates.
        var (startSongBpmBeat, endSongBpmBeat) = GetSongBpmBounds();

        selectionLeft = PlacementVisualContainer.transform.localPosition.x
            + PlacementVisualContainer.transform.localScale.x;
        selectionRight = PlacementVisualContainer.transform.localPosition.x;
        if (selectionRight < selectionLeft)
            (selectionLeft, selectionRight) = (selectionRight, selectionLeft);

        selectionTop = PlacementVisualContainer.transform.localPosition.y
            + PlacementVisualContainer.transform.localScale.y;
        selectionBottom = PlacementVisualContainer.transform.localPosition.y;
        if (selectionTop < selectionBottom)
            (selectionTop, selectionBottom) = (selectionBottom, selectionTop);

        // Reuse the prior logical result when only the moving beat edge expands with unchanged lane bounds.
        var queryMode = PrepareSelectionQuery(
            startSongBpmBeat,
            endSongBpmBeat,
            out var previousStartSongBpmBeat,
            out var previousEndSongBpmBeat);
        if (queryMode == SelectionQueryMode.None)
            return;

        // Skip the redundant outer-group backing query when preview child beats provide their logical selection path.
        var usesGlsPreviewNodeTimes = UsesGlsPreviewNodeTimes(Settings.Instance.GLSOuterTrackGhostNodeOpacity);
        var directlySelectedTypes = usesGlsPreviewNodeTimes
            ? selectedTypes & ~GlsGroupObjectTypes
            : selectedTypes;

        if (queryMode == SelectionQueryMode.Expand)
        {
            // Query only newly exposed time slices; existing logical selections cannot leave an expanding box.
            var epsilon = BeatmapObjectContainerCollection.Epsilon;
            if (startSongBpmBeat < previousStartSongBpmBeat - epsilon)
            {
                SelectObjectsInBeatRange(
                    startSongBpmBeat,
                    previousStartSongBpmBeat,
                    directlySelectedTypes,
                    usesGlsPreviewNodeTimes,
                    false);
            }

            if (endSongBpmBeat > previousEndSongBpmBeat + epsilon)
            {
                SelectObjectsInBeatRange(
                    previousEndSongBpmBeat,
                    endSongBpmBeat,
                    directlySelectedTypes,
                    usesGlsPreviewNodeTimes,
                    false);
            }

            return;
        }

        // Rebuild only when shrinking, reversing, or changing spatial bounds can remove prior matches.
        selected.Clear();
        SelectObjectsInBeatRange(
            startSongBpmBeat,
            endSongBpmBeat,
            directlySelectedTypes,
            usesGlsPreviewNodeTimes,
            true);

        // Buffer deselections before mutating SelectedObjects, avoiding the previous per-frame LINQ array.
        deselectionBuffer.Clear();
        foreach (var combinedObj in SelectionController.SelectedObjects)
        {
            if (!selected.Contains(combinedObj) && !alreadySelected.Contains(combinedObj))
                deselectionBuffer.Add(combinedObj);
        }

        foreach (var combinedObj in deselectionBuffer)
            SelectionController.Deselect(combinedObj, false);
    }

    // Run direct and preview-backed selection over one full or incremental beat interval.
    private void SelectObjectsInBeatRange(
        float startSongBpmBeat,
        float endSongBpmBeat,
        ObjectType directlySelectedTypes,
        bool usesGlsPreviewNodeTimes,
        bool includeSelectedGlsCarryIns)
    {
        SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
            startSongBpmBeat,
            endSongBpmBeat,
            directlySelectedTypes,
            selectionCandidateCallback ??= HandleSelectionCandidate);

        if (usesGlsPreviewNodeTimes)
        {
            SelectGlsGroupsFromPreviewNodes(
                startSongBpmBeat,
                endSongBpmBeat,
                includeSelectedGlsCarryIns);
        }
    }

    // Resolve one backing object through a cached delegate so each hover query avoids closure allocation.
    private void HandleSelectionCandidate(BeatmapObjectContainerCollection _, BaseObject bo)
    {
        if (!bo.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID))
            return;

        // Default single-lane objects to a guaranteed in-box point before resolving spatial object types.
        var position = new Vector2(selectionLeft, selectionBottom);
        switch (bo)
        {
            case IObjectBounds obj:
                position = obj.GetCenter() / BeatmapConstant.LaneSize;
                break;
            case BaseNJSEvent:
            case BaseBpmEvent:
                // Bpm events are in a separate single lane so we don't need to get position
                break;
            case BaseEvent evt:
                {
                    var eventPosition = evt.GetPosition(
                        Labels,
                        EventGridContainer.PropagationEditing,
                        EventGridContainer.EventTypeToPropagate);

                    // Not visible = notselectable
                    if (!eventPosition.HasValue) return;

                    position = new Vector2(
                        eventPosition.Value.x + (BoundsPosition.x / BeatmapConstant.LaneSize),
                        eventPosition.Value.y);
                    break;
                }
            case BaseCustomEvent custom:
                position = new Vector2(
                    0.5f
                    + CustomCollection.CustomEventTypes.IndexOf(custom.Type)
                    + (BoundsPosition.x / BeatmapConstant.LaneSize),
                    0.5f);
                break;
            case BaseEventBoxGroup glsGroup:
                // Use the shared lane-center calculation for primary and preview GLS nodes.
                position = GetGlsGroupSelectionPosition(glsGroup);
                break;
            case BaseGLSEvent glsEvent:
                // Test inner GLS events at their rendered lane centers so an adjacent lane cannot match the box edge.
                position = GetGlsEventSelectionPosition(glsEvent.BoxIndex, BoundsPosition.x);
                break;
            default:
                Debug.LogWarning($"Unsupported object type {bo.GetType()} in box selection");
                return;
        }

        if (!IsWithinSelectionXY(position))
            return;

        if (!alreadySelected.Contains(bo)
            && selected.Add(bo)
            && !SelectionController.IsObjectSelected(bo))
        {
            SelectionController.Select(bo, true, false, false);
        }
    }

    // Cache query geometry and identify monotonic expansion without losing shrink/reverse correctness.
    private SelectionQueryMode PrepareSelectionQuery(
        float startSongBpmBeat,
        float endSongBpmBeat,
        out float previousStartSongBpmBeat,
        out float previousEndSongBpmBeat)
    {
        previousStartSongBpmBeat = previousSelectionStartBeat;
        previousEndSongBpmBeat = previousSelectionEndBeat;
        var spatialBoundsUnchanged = hasPreviousSelectionQuery
            && Mathf.Approximately(previousSelectionLeft, selectionLeft)
            && Mathf.Approximately(previousSelectionRight, selectionRight)
            && Mathf.Approximately(previousSelectionBottom, selectionBottom)
            && Mathf.Approximately(previousSelectionTop, selectionTop);
        var changed = !hasPreviousSelectionQuery
            || !Mathf.Approximately(previousSelectionStartBeat, startSongBpmBeat)
            || !Mathf.Approximately(previousSelectionEndBeat, endSongBpmBeat)
            || !spatialBoundsUnchanged;
        if (!changed)
            return SelectionQueryMode.None;

        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        var monotonicallyExpanded = spatialBoundsUnchanged
            && BeatBoundsMonotonicallyExpand(
                previousSelectionStartBeat,
                previousSelectionEndBeat,
                startSongBpmBeat,
                endSongBpmBeat,
                epsilon);
        var exposesNewBeatSlice = startSongBpmBeat < previousSelectionStartBeat - epsilon
            || endSongBpmBeat > previousSelectionEndBeat + epsilon;
        if (monotonicallyExpanded && !exposesNewBeatSlice)
        {
            // Keep the last processed bounds so sub-epsilon scrolling accumulates instead of being discarded each frame.
            return SelectionQueryMode.None;
        }

        hasPreviousSelectionQuery = true;
        previousSelectionStartBeat = startSongBpmBeat;
        previousSelectionEndBeat = endSongBpmBeat;
        previousSelectionLeft = selectionLeft;
        previousSelectionRight = selectionRight;
        previousSelectionBottom = selectionBottom;
        previousSelectionTop = selectionTop;
        return monotonicallyExpanded
            ? SelectionQueryMode.Expand
            : SelectionQueryMode.Full;
    }

    // Treat epsilon-sized endpoint jitter as expansion so scrolling does not trigger a full rebuild spuriously.
    internal static bool BeatBoundsMonotonicallyExpand(
        float previousStart,
        float previousEnd,
        float currentStart,
        float currentEnd,
        float epsilon) =>
        currentStart <= previousStart + epsilon
        && currentEnd >= previousEnd - epsilon;

    // Select an owning GLS group when any of its rendered preview-node times falls inside the current box.
    private void SelectGlsGroupsFromPreviewNodes(
        float startSongBpmBeat,
        float endSongBpmBeat,
        bool includeSelectedCarryIns)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        // Convert immutable beat bounds once for all four GLS group collections.
        var startJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(
            startSongBpmBeat - epsilon);
        var endJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(
            endSongBpmBeat + epsilon);
        foreach (var type in glsObjectTypes)
        {
            if ((selectedTypes & type) == 0)
                continue;

            // Query logical group data by beat while retaining loaded/selected carry-ins whose previews outlive the group beat.
            switch (BeatmapObjectContainerCollection.GetCollectionForType(type))
            {
                case GLSGroupColorGridContainer colorCollection:
                    SelectGlsGroupsFromPreviewNodes(
                        colorCollection,
                        startSongBpmBeat,
                        endSongBpmBeat,
                        startJsonTime,
                        endJsonTime,
                        includeSelectedCarryIns,
                        epsilon);
                    break;
                case GLSGroupRotationGridContainer rotationCollection:
                    SelectGlsGroupsFromPreviewNodes(
                        rotationCollection,
                        startSongBpmBeat,
                        endSongBpmBeat,
                        startJsonTime,
                        endJsonTime,
                        includeSelectedCarryIns,
                        epsilon);
                    break;
                case GLSGroupTranslationGridContainer translationCollection:
                    SelectGlsGroupsFromPreviewNodes(
                        translationCollection,
                        startSongBpmBeat,
                        endSongBpmBeat,
                        startJsonTime,
                        endJsonTime,
                        includeSelectedCarryIns,
                        epsilon);
                    break;
                case GLSGroupFloatFXGridContainer floatFxCollection:
                    SelectGlsGroupsFromPreviewNodes(
                        floatFxCollection,
                        startSongBpmBeat,
                        endSongBpmBeat,
                        startJsonTime,
                        endJsonTime,
                        includeSelectedCarryIns,
                        epsilon);
                    break;
            }
        }
    }

    // Select preview-backed groups independently of whether their visual containers are currently pooled.
    private void SelectGlsGroupsFromPreviewNodes<TGroup>(
        BeatmapObjectContainerCollection<TGroup> collection,
        float startSongBpmBeat,
        float endSongBpmBeat,
        float startJsonTime,
        float endJsonTime,
        bool includeSelectedCarryIns,
        float epsilon)
        where TGroup : BaseEventBoxGroup
    {
        glsPreviewCandidates.Clear();

        // Binary-search group start beats so widening a box does not scan unrelated portions of a long map.
        var startIndex = FindFirstGroupAtOrAfter(collection.MapObjects, startJsonTime);
        for (var index = startIndex;
             index < collection.MapObjects.Count && collection.MapObjects[index].JsonTime <= endJsonTime;
             index++)
        {
            glsPreviewCandidates.Add(collection.MapObjects[index]);
        }

        // Include loaded long-lived previews whose parent group began before the current beat range.
        foreach (var loadedObject in collection.LoadedContainers.Keys)
        {
            if (loadedObject is TGroup group)
                glsPreviewCandidates.Add(group);
        }

        // Reevaluate unloaded selected groups only when a full rebuild can remove prior matches.
        if (includeSelectedCarryIns)
        {
            foreach (var selectedObject in SelectionController.SelectedObjects)
            {
                if (selectedObject is TGroup group)
                    glsPreviewCandidates.Add(group);
            }
        }

        foreach (var group in glsPreviewCandidates)
        {
            if (!group.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID)
                || !IsWithinSelectionXY(GetGlsGroupSelectionPosition(group))
                || !HasGlsPreviewEventBetween(group, startSongBpmBeat, endSongBpmBeat, epsilon)
                || alreadySelected.Contains(group))
            {
                continue;
            }

            // Retain the logical group even after its viewport container has been recycled.
            selected.Add(group);
            if (SelectionController.IsObjectSelected(group))
                continue;

            SelectionController.Select(group, true, false, false);
        }
    }

    // Find the first group whose sorted JSON beat can contribute to the current selection interval.
    private static int FindFirstGroupAtOrAfter<TGroup>(IReadOnlyList<TGroup> groups, float jsonTime)
        where TGroup : BaseEventBoxGroup
    {
        var lower = 0;
        var upper = groups.Count;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) / 2);
            if (groups[middle].JsonTime < jsonTime)
                lower = middle + 1;
            else
                upper = middle;
        }

        return lower;
    }

    // Match a logical group's rendered preview beats without requiring any corresponding scene objects.
    internal static bool HasGlsPreviewEventBetween(
        BaseEventBoxGroup group,
        float startSongBpmBeat,
        float endSongBpmBeat,
        float epsilon)
    {
        // Reuse the preview renderer's time-sorted cache and initialize backing-only groups on first selection.
        IReadOnlyList<BaseGLSEvent> orderedEvents;
        switch (group)
        {
            case BaseLightColorEventBoxGroup colorGroup:
                EnsureOrderedEvents(colorGroup);
                orderedEvents = colorGroup.OrderedEvents;
                break;
            case BaseLightRotationEventBoxGroup rotationGroup:
                EnsureOrderedEvents(rotationGroup);
                orderedEvents = rotationGroup.OrderedEvents;
                break;
            case BaseLightTranslationEventBoxGroup translationGroup:
                EnsureOrderedEvents(translationGroup);
                orderedEvents = translationGroup.OrderedEvents;
                break;
            case BaseVfxEventEventBoxGroup floatFxGroup:
                EnsureOrderedEvents(floatFxGroup);
                orderedEvents = floatFxGroup.OrderedEvents;
                break;
            default:
                return false;
        }

        var lowerBeat = startSongBpmBeat - epsilon;
        var upperBeat = endSongBpmBeat + epsilon;
        var lower = 0;
        var upper = orderedEvents.Count;
        while (lower < upper)
        {
            var middle = lower + ((upper - lower) / 2);
            if (orderedEvents[middle].SongBpmTime <= lowerBeat)
                lower = middle + 1;
            else
                upper = middle;
        }

        return lower < orderedEvents.Count && orderedEvents[lower].SongBpmTime < upperBeat;

        // Populate an unloaded group's sorted cache once; rendered groups normally arrive here already populated.
        static void EnsureOrderedEvents<TBox>(BaseEventBoxGroup<TBox> typedGroup)
            where TBox : BaseEventBox
        {
            if (typedGroup.OrderedEvents.Count == 0 && typedGroup.ReadOnlyBoxes.Count > 0)
                typedGroup.ResortOrderedEvents();
        }
    }

    // Resolve a GLS group lane once for both primary-node and ghost-node selection checks.
    private Vector2 GetGlsGroupSelectionPosition(BaseEventBoxGroup group)
    {
        float offset = short.MinValue;
        if (glsGroupCondition.TryGetValue(group.ID, out var typeToOffset))
            offset = typeToOffset.GetValueOrDefault(group.GetType(), short.MinValue);
        // Test the node center in upstream's lane-unit coordinate system so gaps do not shift selection by one lane.
        return GetGlsLaneCenter(offset);
    }

    // Keep GLS hit tests on the rendered lane center even when track gaps make lane offsets non-contiguous.
    internal static Vector2 GetGlsLaneCenter(float offset) => new(offset + 0.5f, 0.5f);

    // Match GLSEventContainer's local 0.5-lane center before translating the inner grid into box-selection space.
    internal static Vector2 GetGlsEventSelectionPosition(int boxIndex, float boundsPositionX) =>
        GetGlsLaneCenter(boxIndex + (boundsPositionX / BeatmapConstant.LaneSize));

    // Match the GLS rendering mode when deciding whether the logical group beat remains selectable.
    internal static bool UsesGlsPreviewNodeTimes(float previewOpacity) =>
        !Mathf.Approximately(previewOpacity, 0f);

    // Apply the box's current horizontal and vertical bounds to a resolved node position.
    private bool IsWithinSelectionXY(Vector2 position) =>
        position.x >= selectionLeft
        && position.x <= selectionRight
        && position.y >= selectionBottom
        && position.y < selectionTop;

    public override void HandleApply()
    {
        // Record placement state at click handling time to establish ordering against outer GLS group entry.
        if (IsPlacing)
        {
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
            // Force the first logical query of each new drag even if it reuses the previous box coordinates.
            hasPreviousSelectionQuery = false;
            // Start each drag with an empty logical result; preexisting selections remain tracked separately below.
            selected.Clear();
            // Reuse the snapshot set because starting a box selection should not allocate with selection size.
            alreadySelected.Clear();
            alreadySelected.UnionWith(SelectionController.SelectedObjects);
        }
    }

    // Normalize the drag's immutable start beat and latest hovered beat for all selection paths.
    private (float Start, float End) GetSongBpmBounds()
    {
        return GetSongBpmBounds(originSongBpmBeat, currentSongBpmBeat);
    }

    // Normalize both drag directions without reconstructing beat endpoints from the rendered transform.
    internal static (float Start, float End) GetSongBpmBounds(float originBeat, float currentBeat) =>
        originBeat <= currentBeat
            ? (originBeat, currentBeat)
            : (currentBeat, originBeat);

    // Convert an absolute beat through the active timeline before placing the visual in the box track's local space.
    private float GetBoxLocalZForSongBpmBeat(float songBpmBeat)
    {
        // Unity transforms require their overloaded null comparison when selecting the timeline fallback.
        var timelineTrack = beatCoordinateTrack != null ? beatCoordinateTrack : PlacementTrack;
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
        // Release the canceled drag's persistent logical selection set before the next box starts.
        selected.Clear();
        SelectionController.OnSelectionChanged?.Invoke();
    }
}
