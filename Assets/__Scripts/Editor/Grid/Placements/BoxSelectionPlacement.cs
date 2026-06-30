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
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] public CustomEventGridContainer CustomCollection;
    [SerializeField] public EventGridContainer EventGridContainer;
    [SerializeField] public CreateEventTypeLabels Labels;
    [SerializeField] private BeatmapRuntimeContext beatmapContext;
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;

    private readonly HashSet<BaseObject> selected = new();
    private ObjectType selectedTypes = 0;
    private HashSet<BaseObject> alreadySelected = new();
    private readonly Dictionary<int, Dictionary<Type, float>> glsGroupCondition = new();
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
        if (!provider.TryGetComponent<GLSGroupTrack>(out _)) return;

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

            PlacementVisualContainer.transform.localPosition = new Vector3(
                (originShove.x * BeatmapConstant.LaneSize)
                + (gridViewController.IsOdd
                    ? BeatmapConstant.LaneSize / 2f
                    : 0f),
                (originShove.y * BeatmapConstant.LaneSize) + BeatmapConstant.YOffset + (BeatmapConstant.LaneSize / 2f),
                originShove.z);
            var scale = LanePosition + new Vector3(sizeX, sizeY, 0.5f) - originShove;
            PlacementVisualContainer.transform.localScale = new Vector3(
                scale.x * BeatmapConstant.LaneSize,
                scale.y * BeatmapConstant.LaneSize,
                scale.z);
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        if (!IsPlacing) return;

        var trackPos = PlacementVisualContainer.transform.parent.localPosition.z;
        var offset = (trackPos
                + PlacementVisualContainer.transform.localPosition.z
                - BeatmapConstant.ZOffset)
            / EditorScaleController.EditorScale;
        var startSongBpmBeat =
            (-trackPos / EditorScaleController.EditorScale)
            + offset;
        var endSongBpmBeat = ((-trackPos
                    + (PlacementVisualContainer.transform.localScale.z / BeatmapConstant.LaneSize))
                / EditorScaleController.EditorScale)
            + (offset / BeatmapConstant.LaneSize);
        if (startSongBpmBeat > endSongBpmBeat) (startSongBpmBeat, endSongBpmBeat) = (endSongBpmBeat, startSongBpmBeat);

        // Doing a jank bitmask to ensure we include all object types in the search
        SelectionController.ForEachObjectBetweenSongBpmTimeByGroup(
            startSongBpmBeat,
            endSongBpmBeat,
            selectedTypes,
            (_, bo) =>
            {
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
                        float o = short.MinValue;
                        if (glsGroupCondition.TryGetValue(glsGroup.ID, out var typeToOffset))
                            o = typeToOffset.GetValueOrDefault(glsGroup.GetType(), short.MinValue);
                        p = new Vector2(o, BeatmapConstant.YOffset + BeatmapConstant.LaneSize);
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
            Exit();
            selected.Clear(); // oh shit turned out i didnt need to rewrite the whole thing, just move it over here
            SelectionController.OnSelectionChanged?.Invoke();
        }
        else
        {
            State = PlacementState.Placing;
            originPos = LanePosition;
            alreadySelected = new HashSet<BaseObject>(SelectionController.SelectedObjects);
        }
    }

    public override void Exit()
    {
        if (IsPlacing) return;
        base.Exit();
    }

    public override void Cancel()
    {
        if (!IsPlacing) return;
        State = PlacementState.Idle;
        foreach (var selectedObject in selected) SelectionController.Deselect(selectedObject, false);
        SelectionController.OnSelectionChanged?.Invoke();
    }
}
