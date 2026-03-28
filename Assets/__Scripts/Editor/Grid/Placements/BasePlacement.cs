using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public abstract class BasePlacement : MonoBehaviour
{
    [SerializeField] public ObjectType ObjectDataType;
    [SerializeField] public GameObject ObjectContainerPrefab;

    [Tooltip("This is required to be on game object with track as it is used to track and compare time")]
    [SerializeField]
    public Transform PlacementTrack;

    public bool CanPrecisionPlacement;
    public bool AdjustZScale;

    [Header("Dependencies")] [SerializeField]
    public CustomStandaloneInputModule CustomStandaloneInputModule;

    [SerializeField] public AudioTimeSyncController Atsc;
    [SerializeField] public BoxSelectionPlacement boxSelectionPlacement;

    [Header("360/90")] [SerializeField] public bool AssignTo360Tracks;
    [SerializeField] public TracksManager TracksManager;
    [SerializeField] public RotationCallbackController GridRotation;

    [Header("State")]
    [Tooltip("If you have multiple placement in a single grid, consider making control flow and toggle this condition")]
    public bool AllowPlacement = true;

    public PlacementState State;
    public bool IsDragging;
    public float JsonTimeRounded;
    protected Vector3 LanePosition;
    public Bounds Bounds;
    public Vector3 BoundsPosition;

    public virtual bool CanClickAndDrag => true;
    public virtual bool CanPlace => boxSelectionPlacement.State == PlacementState.Idle;

    public bool IsIdle => State == PlacementState.Idle;
    public bool IsActive => State == PlacementState.Active;
    public bool IsPlacing => State == PlacementState.Placing;

    public float RoundedJsonTime
    {
        get => JsonTimeRounded;
        set
        {
            SongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(value);
            JsonTimeRounded = value;
        }
    }

    protected float SongBpmTime { get; private set; } // No point rounding this

    protected static Vector2 GridOffset => Vector2.one * 0.5f;

    protected readonly List<ObjectContainer> DraggedAttachedSliderContainers = new();

    protected readonly Dictionary<IndicatorType, List<BaseSlider>> DraggedAttachedSliderDatas = new()
    {
        { IndicatorType.Head, new List<BaseSlider>() }, { IndicatorType.Tail, new List<BaseSlider>() }
    };

    protected readonly Dictionary<IndicatorType, List<BaseSlider>> OriginalDraggedAttachedSliderDatas = new()
    {
        { IndicatorType.Head, new List<BaseSlider>() }, { IndicatorType.Tail, new List<BaseSlider>() }
    };

    public abstract void Initialize(PlacementProvider provider);
    public abstract void UpdateState(Intersections.IntersectionHit hit, PlacementInputState inputState);
    public abstract void ShowVisual();
    public abstract void HideVisual();

    protected abstract void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint);
    protected virtual void HandlePlacementToData(PlacementInputState inputState) { }

    public abstract void Exit();
    public abstract void Apply();
    public virtual void Cancel() { }

    public abstract ObjectContainer StartDrag(GameObject draggedObject);
    public abstract void FinishDrag();
    protected virtual void HandleDragged() { }

    public virtual float GetContainerPosZ(ObjectContainer con) =>
        (con.ObjectData.SongBpmTime - Atsc.CurrentSongBpmTime) * EditorScaleController.EditorScale;
}

public abstract class BasePlacement<TObject, TContainer, TCollection> : BasePlacement
    where TObject : BaseObject
    where TContainer : ObjectContainer
    where TCollection : BeatmapObjectContainerCollection
{
    [Header("Data")] public TCollection ObjectContainerCollection;

    public TContainer PlacementVisualContainer;

    public TContainer DraggedObjectContainer;
    public TObject DraggedObjectData;

    public TObject OriginalDraggedObjectData;
    public TObject OriginalQueued;

    public TObject QueuedData; //Data that is not yet applied to the ObjectContainer.

    [Header("Implementation")] public bool ForceHeaderPlsIgnore;

    public event Action OnApplied; // this is an odd name

    public virtual void Start()
    {
        CreateVisual();
        HideVisual();
        QueuedData ??= GenerateOriginalData();
    }

    protected abstract TObject GenerateOriginalData();
    protected abstract BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts);

    public override void Initialize(PlacementProvider provider)
    {
        CreateVisual();
        HideVisual();
        QueuedData ??= GenerateOriginalData();
    }

    public override void UpdateState(
        Intersections.IntersectionHit hit,
        PlacementInputState inputState)
    {
        if (!AllowPlacement && !IsDragging)
        {
            if (!IsActive) return;
            HideVisual();
            State = PlacementState.Idle;
            return;
        }

        if (IsIdle) State = PlacementState.Active;

        if (inputState == PlacementInputState.Hover && !PlacementVisualContainer.gameObject.activeSelf) ShowVisual();

        if (BeatmapObjectContainerCollection.TrackFilterID != null && !ObjectContainerCollection.IgnoreTrackFilter)
            QueuedData.CustomTrack = BeatmapObjectContainerCollection.TrackFilterID;
        else
            QueuedData.CustomTrack = null;

        var (localPoint, jsonTime) = GetPositionAndTime(hit, inputState);
        RoundedJsonTime = jsonTime;
        QueuedData.JsonTime = jsonTime;

        SetTo360Tracks();
        HandleHitToPlacement(hit, localPoint);
        HandlePlacementToData(inputState);

        if (inputState == PlacementInputState.Hover || !IsDragging) return;
        TransferQueuedToDraggedObject(ref DraggedObjectData, QueuedData);
        if (DraggedObjectContainer != null) DraggedObjectContainer.UpdateGridPosition();
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var placementZ = SongBpmTime * EditorScaleController.EditorScale;
        var roundedPoint = new Vector3(Mathf.FloorToInt(localPoint.x), Mathf.FloorToInt(localPoint.y), placementZ);

        if (PrecisionPlacementController.IsEnabled && CanPrecisionPlacement)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedPoint = (Vector2)Vector2Int.FloorToInt((Vector2)localPoint * precision) / precision;
            roundedPoint.z = placementZ;
            PlacementVisualContainer.transform.localPosition = roundedPoint + (Vector3)GridOffset;
        }
        else
        {
            var minX = Bounds.min.x;
            var maxX = Bounds.max.x;

            var minY = Bounds.min.y;
            var maxY = Bounds.max.y;

            PlacementVisualContainer.transform.localPosition = new Vector3(
                    Mathf.Clamp(roundedPoint.x, 0, maxX - minX - 1),
                    Mathf.Clamp(roundedPoint.y, 0, maxY - minY - 1),
                    roundedPoint.z)
                + (Vector3)GridOffset;
        }
    }

    public override void ShowVisual() => PlacementVisualContainer.SafeSetActive(true);

    public override void HideVisual() => PlacementVisualContainer.SafeSetActive(false);

    protected virtual float GetDraggedObjectJsonTime() => DraggedObjectData.JsonTime;

    private (Vector3 localPoint, float jsonTime) GetPositionAndTime(
        Intersections.IntersectionHit hit,
        PlacementInputState inputState)
    {
        var currentJsonTime = inputState == PlacementInputState.DragAtTime
            ? GetDraggedObjectJsonTime()
            : Atsc.CurrentJsonTime;
        var snap = 1f / Atsc.GridMeasureSnapping;
        var offsetJsonTime = currentJsonTime
            - ((float)Math.Round(currentJsonTime / snap, MidpointRounding.AwayFromZero) * snap);

        var localPoint = PlacementTrack.InverseTransformPoint(hit.Point);

        localPoint.z = AdjustZScale
            ? (localPoint.z - BeatmapConstant.ZOffset) / BeatmapConstant.LaneSize
            : localPoint.z;
        var realTime = localPoint.z / EditorScaleController.EditorScale;
        if (hit.GameObject.transform.parent.name.Contains("Interface"))
        {
            realTime = PlacementTrack.InverseTransformPoint(hit.GameObject.transform.parent.position).z
                / EditorScaleController.EditorScale;
        }

        var hitPointJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(realTime);
        var jsonTime = (float)Math.Round((hitPointJsonTime - offsetJsonTime) / snap, MidpointRounding.AwayFromZero)
            * snap;
        if (!Atsc.IsPlaying) jsonTime += offsetJsonTime;

        return (localPoint, jsonTime);
    }

    public virtual void CreateVisual()
    {
        if (PlacementVisualContainer != null) return;

        PlacementVisualContainer = Instantiate(
                ObjectContainerPrefab,
                PlacementTrack)
            .GetComponent(typeof(TContainer)) as TContainer;
        PlacementVisualContainer.Setup();
        PlacementVisualContainer.Selected = false;

        foreach (var coll in PlacementVisualContainer.GetComponentsInChildren<IntersectionCollider>(true))
            Destroy(coll);
        if (PlacementVisualContainer.GetComponent<ObjectAnimator>() is ObjectAnimator animator)
            animator.enabled = false;

        PlacementVisualContainer.name = $"Hover {ObjectDataType}";
    }

    private void SetTo360Tracks()
    {
        if (!AssignTo360Tracks) return;
        var track = TracksManager.GetTrackAtTime(SongBpmTime);
        if (track == null) return;

        var localPos = PlacementVisualContainer.transform.localPosition;
        PlacementTrack = track.ObjectParentTransform;
        PlacementVisualContainer.transform.SetParent(track.ObjectParentTransform, false);
        PlacementVisualContainer.transform.localPosition = localPos;
        PlacementVisualContainer.transform.localEulerAngles = new Vector3(
            PlacementVisualContainer.transform.localEulerAngles.x,
            0,
            PlacementVisualContainer.transform.localEulerAngles.z);
    }

    public override void Apply()
    {
        if (QueuedData?.JsonTime >= 0
            && PlacementVisualContainer.gameObject.activeSelf)
        {
            HandleApply();
            OnApplied?.Invoke();
        }
    }

    public virtual void HandleApply()
    {
        ObjectContainerCollection.SpawnObject(QueuedData, out var conflicting);
        BeatmapActionContainer.AddAction(GenerateAction(QueuedData, conflicting));
        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.CustomData = null;
    }

    public override void Exit()
    {
        HideVisual();
        State = PlacementState.Idle;
    }

    // TODO(Bullet): Clean up implementations.
    protected virtual void TransferQueuedToDraggedObject(ref TObject dragged, TObject queued) { }

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = draggedObject.GetComponentInParent<TContainer>();
        // this does not need the last check
        if (con == null || con.ObjectData.ObjectType != ObjectDataType) return null;

        ObjectContainerCollection.SilentRemoveObject(con.ObjectData);

        DraggedObjectData = con.ObjectData as TObject;
        OriginalQueued = BeatmapFactory.Clone(QueuedData);
        OriginalDraggedObjectData = BeatmapFactory.Clone(con.ObjectData as TObject);
        QueuedData = BeatmapFactory.Clone(DraggedObjectData);
        DraggedObjectContainer = con;
        DraggedObjectContainer.Dragged = true;

        if (con is NoteContainer noteContainer)
        {
            var noteCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            noteCollection.ClearSpecialAngles(con.ObjectData);

            StartDragSliders(noteContainer);
        }

        IsDragging = true;
        return con;
    }

    public override void FinishDrag()
    {
        // Spawn our dragged object and delete anything that's overlapping.
        ObjectContainerCollection.SpawnObject(DraggedObjectData, out var conflicting);

        QueuedData = BeatmapFactory.Clone(OriginalQueued);
        var actions = new List<BeatmapAction>();
        // Don't queue an action if we didn't actually change anything
        if (DraggedObjectData.ToString() != OriginalDraggedObjectData.ToString())
        {
            if (conflicting.Count > 0)
            {
                actions.Add(
                    new BeatmapObjectModifiedWithConflictingAction(
                        DraggedObjectData,
                        DraggedObjectData,
                        OriginalDraggedObjectData,
                        conflicting,
                        "Modified via alt-click and drag."));
            }
            else
            {
                actions.Add(
                    new BeatmapObjectModifiedAction(
                        DraggedObjectData,
                        DraggedObjectData,
                        OriginalDraggedObjectData,
                        "Modified via alt-click and drag."));
            }

            SelectionController.OnSelectionChanged?.Invoke();
        }

        if (DraggedObjectContainer is NoteContainer)
        {
            var noteCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            noteCollection.RefreshSpecialAngles(DraggedObjectData, false, false);

            FinishSliderDrag(actions);
            ClearDraggedAttachedSliders();
        }

        if (actions.Count == 1)
            BeatmapActionContainer.AddAction(actions[0]);
        else if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(actions, true, true, "Modified via alt-click and drag"));
        }

        DraggedObjectContainer.Dragged = false;
        DraggedObjectContainer = null;
        HandleDragged();
        IsDragging = false;
    }

    private void StartDragSliders(NoteContainer noteContainer)
    {
        var noteData = noteContainer.NoteData;
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var arcData = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(arcData.JsonTime - noteData.JsonTime) < epsilon
                && arcData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(arcData.TailJsonTime - noteData.JsonTime) < epsilon
                && arcData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                OriginalDraggedAttachedSliderDatas[IndicatorType.Head].Add(BeatmapFactory.Clone(arcData));
                DraggedAttachedSliderDatas[IndicatorType.Head].Add(arcData);
                DraggedAttachedSliderContainers.Add(arcContainer.Value);
                arcCollection.SilentRemoveObject(arcData);
            }
            else if (isConnectedToTail)
            {
                OriginalDraggedAttachedSliderDatas[IndicatorType.Tail].Add(BeatmapFactory.Clone(arcData));
                DraggedAttachedSliderDatas[IndicatorType.Tail].Add(arcData);
                DraggedAttachedSliderContainers.Add(arcContainer.Value);
                arcCollection.SilentRemoveObject(arcData);
            }
        }

        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var chainData = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(chainData.JsonTime - noteData.JsonTime) < epsilon
                && chainData.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(chainData.TailJsonTime - noteData.JsonTime) < epsilon
                && chainData.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                OriginalDraggedAttachedSliderDatas[IndicatorType.Head].Add(BeatmapFactory.Clone(chainData));
                DraggedAttachedSliderDatas[IndicatorType.Head].Add(chainData);
                DraggedAttachedSliderContainers.Add(chainContainer.Value);
                chainCollection.SilentRemoveObject(chainData);
            }
            else if (isConnectedToTail)
            {
                OriginalDraggedAttachedSliderDatas[IndicatorType.Tail].Add(BeatmapFactory.Clone(chainData));
                DraggedAttachedSliderDatas[IndicatorType.Tail].Add(chainData);
                DraggedAttachedSliderContainers.Add(chainContainer.Value);
                chainCollection.SilentRemoveObject(chainData);
            }
        }

        foreach (var container in DraggedAttachedSliderContainers) container.Dragged = true;
    }

    private void FinishSliderDrag(List<BeatmapAction> actions)
    {
        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);

        for (var i = 0; i < DraggedAttachedSliderDatas[IndicatorType.Head].Count; i++)
        {
            var draggedSlider = DraggedAttachedSliderDatas[IndicatorType.Head][i];
            var originalDraggedSlider = OriginalDraggedAttachedSliderDatas[IndicatorType.Head][i];

            if (draggedSlider is BaseArc draggedArc)
                SpawnDraggedSlider(arcCollection, draggedArc, originalDraggedSlider, actions);
            else if (draggedSlider is BaseChain draggedChain)
                SpawnDraggedSlider(chainCollection, draggedChain, originalDraggedSlider, actions);
        }

        for (var i = 0; i < DraggedAttachedSliderDatas[IndicatorType.Tail].Count; i++)
        {
            var draggedSlider = DraggedAttachedSliderDatas[IndicatorType.Tail][i];
            var originalDraggedSlider = OriginalDraggedAttachedSliderDatas[IndicatorType.Tail][i];

            if (draggedSlider is BaseArc draggedArc)
                SpawnDraggedSlider(arcCollection, draggedArc, originalDraggedSlider, actions);
            else if (draggedSlider is BaseChain draggedChain)
                SpawnDraggedSlider(chainCollection, draggedChain, originalDraggedSlider, actions);
        }
    }

    private void SpawnDraggedSlider(
        BeatmapObjectContainerCollection sliderCollection,
        BaseSlider draggedSlider,
        BaseObject originalSlider,
        List<BeatmapAction> actions)
    {
        sliderCollection.SpawnObject(draggedSlider, out var conflictingArcs);

        // Don't queue an action if we didn't actually change anything
        if (draggedSlider.ToString() != originalSlider.ToString())
        {
            if (conflictingArcs.Count > 0)
            {
                actions.Add(
                    new BeatmapObjectModifiedWithConflictingAction(
                        draggedSlider,
                        draggedSlider,
                        originalSlider,
                        conflictingArcs,
                        "Modified via alt-click and drag."));
            }
            else
            {
                actions.Add(
                    new BeatmapObjectModifiedAction(
                        draggedSlider,
                        draggedSlider,
                        originalSlider,
                        "Modified via alt-click and drag."));
            }
        }
    }

    private void ClearDraggedAttachedSliders()
    {
        foreach (var container in DraggedAttachedSliderContainers) container.Dragged = false;
        DraggedAttachedSliderContainers.Clear();
        DraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        DraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
        OriginalDraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        OriginalDraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
    }
}
