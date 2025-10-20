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
    [SerializeField] public Transform ParentTrack;

    [Header("Dependencies")] [SerializeField]
    public CustomStandaloneInputModule CustomStandaloneInputModule;

    [SerializeField] public AudioTimeSyncController Atsc;

    [Header("360/90")] [SerializeField] public bool AssignTo360Tracks;
    [SerializeField] public TracksManager TracksManager;
    [SerializeField] public RotationCallbackController GridRotation;

    [Header("State")] public bool IsActive = true;
    public bool IsDragging;
    public float JsonTimeRounded;
    public Bounds Bounds;

    protected readonly List<ObjectContainer> DraggedAttachedSliderContainers = new();

    protected readonly Dictionary<IndicatorType, List<BaseSlider>> DraggedAttachedSliderDatas = new()
    {
        { IndicatorType.Head, new List<BaseSlider>() }, { IndicatorType.Tail, new List<BaseSlider>() }
    };

    protected readonly Dictionary<IndicatorType, List<BaseSlider>> OriginalDraggedAttachedSliderDatas = new()
    {
        { IndicatorType.Head, new List<BaseSlider>() }, { IndicatorType.Tail, new List<BaseSlider>() }
    };

    protected readonly Vector2 PrecisionOffset = new(-0.5f, -0.5f);
    protected readonly Vector2 VanillaOffset = new(-0.5f, -0.5f);

    public virtual bool CanClickAndDrag => true;
    public virtual bool CanPlace => BoxSelectionPlacementController.State == SelectionState.Idle;

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

    public abstract void Initialize(PlacementProvider provider);
    public abstract void UpdateState(Intersections.IntersectionHit hit, PlacementState state);

    public abstract void Exit();
    public abstract void Apply();
    public virtual void Cancel() { }

    public abstract ObjectContainer StartDrag(GameObject draggedObject);
    public abstract void FinishDrag();
    protected virtual void HandleDragged() { }

    public abstract void ShowVisual();
    public abstract void HideVisual();

    public virtual float GetContainerPosZ(ObjectContainer con) =>
        (con.ObjectData.SongBpmTime - Atsc.CurrentSongBpmTime) * EditorScaleController.EditorScale;
}

public abstract class BasePlacement<TObject, TContainer, TCollection> : BasePlacement
    where TObject : BaseObject
    where TContainer : ObjectContainer
    where TCollection : BeatmapObjectContainerCollection
{
    [Header("Data")] public TCollection ObjectContainerCollection;
    public TObject ObjectData;

    public TContainer PlacementVisualContainer;

    public TContainer DraggedObjectContainer;
    public TObject DraggedObjectData;

    public TObject OriginalDraggedObjectData;
    public TObject OriginalQueued;

    public TObject QueuedData; //Data that is not yet applied to the ObjectContainer.

    [Header("Implementation")] public bool ForceHeaderPlsIgnore;

    public virtual void Start() => QueuedData = GenerateOriginalData();

    public override void Initialize(PlacementProvider provider)
    {
        if (PlacementVisualContainer == null) RefreshVisuals();
        HideVisual();
    }

    public override void UpdateState(
        Intersections.IntersectionHit hit,
        PlacementState state)
    {
        if (!IsActive || !CanPlace)
        {
            HideVisual();
            return;
        }

        if (!PlacementVisualContainer.gameObject.activeSelf) ShowVisual();

        ObjectData = QueuedData;

        if (BeatmapObjectContainerCollection.TrackFilterID != null && !ObjectContainerCollection.IgnoreTrackFilter)
            QueuedData.CustomTrack = BeatmapObjectContainerCollection.TrackFilterID;
        else
            QueuedData.CustomTrack = null;

        CalculateTimes(hit, state, out var rawHit, out var jsonTime);
        rawHit += (Vector3)VanillaOffset;
        RoundedJsonTime = jsonTime;
        var placementZ = SongBpmTime * EditorScaleController.EditorScale;
        Update360Tracks();

        var roundedHit = new Vector3(Mathf.Round(rawHit.x), Mathf.Round(rawHit.y), placementZ);

        var localMax = ParentTrack.InverseTransformPoint(hit.Bounds.max);
        var localMin = ParentTrack.InverseTransformPoint(hit.Bounds.min);
        var farTopPoint = localMax.y;
        var farBottomPoint = localMin.y;

        var x = roundedHit.x; //Clamp values to prevent exceptions
        var y = roundedHit.y;
        PlacementVisualContainer.transform.localPosition = new Vector3(
            x,
            Mathf.Round(Mathf.Clamp(y, farBottomPoint, farTopPoint - 1)),
            roundedHit.z);

        QueuedData.JsonTime = jsonTime;
        UpdatePlacement(rawHit, roundedHit, state);

        if (state == PlacementState.Hover || QueuedData == null || !IsDragging) return;
        TransferQueuedToDraggedObject(ref DraggedObjectData, QueuedData);
        if (DraggedObjectContainer != null) DraggedObjectContainer.UpdateGridPosition();
    }

    public override void ShowVisual()
    {
        if (PlacementVisualContainer != null) PlacementVisualContainer.SafeSetActive(true);
    }

    public override void HideVisual()
    {
        if (PlacementVisualContainer != null) PlacementVisualContainer.SafeSetActive(false);
    }

    protected virtual float GetDraggedObjectJsonTime() => DraggedObjectData.JsonTime;

    private void CalculateTimes(
        Intersections.IntersectionHit hit,
        PlacementState state,
        out Vector3 rawHit,
        out float jsonTime)
    {
        var currentJsonTime = state == PlacementState.DragAtTime ? GetDraggedObjectJsonTime() : Atsc.CurrentJsonTime;
        var snap = 1f / Atsc.GridMeasureSnapping;
        var offsetJsonTime = currentJsonTime
            - ((float)Math.Round(currentJsonTime / snap, MidpointRounding.AwayFromZero) * snap);

        rawHit = ParentTrack.InverseTransformPoint(hit.Point);
        var realTime = rawHit.z / EditorScaleController.EditorScale;

        if (hit.GameObject.transform.parent.name.Contains("Interface"))
        {
            realTime = ParentTrack.InverseTransformPoint(hit.GameObject.transform.parent.position).z
                / EditorScaleController.EditorScale;
        }

        var hitPointJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(realTime);
        jsonTime = (float)Math.Round((hitPointJsonTime - offsetJsonTime) / snap, MidpointRounding.AwayFromZero)
            * snap;

        if (!Atsc.IsPlaying) jsonTime += offsetJsonTime;
    }

    public virtual void RefreshVisuals()
    {
        PlacementVisualContainer = Instantiate(
                ObjectContainerPrefab,
                ParentTrack)
            .GetComponent(typeof(TContainer)) as TContainer;
        PlacementVisualContainer.Setup();
        PlacementVisualContainer.OutlineVisible = false;

        foreach (var coll in PlacementVisualContainer.GetComponentsInChildren<IntersectionCollider>(true))
            Destroy(coll);
        if (PlacementVisualContainer.GetComponent<ObjectAnimator>() is ObjectAnimator animator)
            animator.enabled = false;

        PlacementVisualContainer.name = $"Hover {ObjectDataType}";
    }

    private void Update360Tracks()
    {
        if (!AssignTo360Tracks) return;
        var track = TracksManager.GetTrackAtTime(SongBpmTime);
        if (track == null) return;

        var localPos = PlacementVisualContainer.transform.localPosition;
        ParentTrack = track.ObjectParentTransform;
        PlacementVisualContainer.transform.SetParent(track.ObjectParentTransform, false);
        PlacementVisualContainer.transform.localPosition = localPos;
        PlacementVisualContainer.transform.localEulerAngles = new Vector3(
            PlacementVisualContainer.transform.localEulerAngles.x,
            0,
            PlacementVisualContainer.transform.localEulerAngles.z);
    }

    public override void Apply()
    {
        if (
            PlacementVisualContainer != null
            && QueuedData?.JsonTime >= 0
            && PlacementVisualContainer.gameObject.activeSelf)
            HandleApply();
    }

    public virtual void HandleApply()
    {
        ObjectData = QueuedData;
        ObjectContainerCollection.SpawnObject(ObjectData, out var conflicting);
        BeatmapActionContainer.AddAction(GenerateAction(ObjectData, conflicting));
        QueuedData = BeatmapFactory.Clone(QueuedData);
        QueuedData.CustomData = null;
    }

    protected abstract TObject GenerateOriginalData();
    protected abstract BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting);

    protected virtual void UpdatePlacement(
        Vector3 rawHit,
        Vector3 roundedHit,
        PlacementState state)
    {
    }

    public override void Exit() => HideVisual();

    // TODO(Bullet): Clean up implementations.
    protected virtual void TransferQueuedToDraggedObject(ref TObject dragged, TObject queued) { }

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = draggedObject.GetComponentInParent<TContainer>();
        if (con == null || con.ObjectData.ObjectType != ObjectDataType) return null;

        ObjectContainerCollection.SilentRemoveObject(con.ObjectData);

        DraggedObjectData = con.ObjectData as TObject;
        OriginalQueued = BeatmapFactory.Clone(QueuedData);
        OriginalDraggedObjectData = BeatmapFactory.Clone(con.ObjectData as TObject);
        QueuedData = BeatmapFactory.Clone(DraggedObjectData);
        DraggedObjectContainer = con;
        DraggedObjectContainer.Dragging = true;

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
        if (PlacementVisualContainer == null) return;

        // Spawn our dragged object and delete anything that's overlapping.
        ObjectContainerCollection.SpawnObject(DraggedObjectData, out var conflicting);

        QueuedData = BeatmapFactory.Clone(OriginalQueued);
        var actions = new List<BeatmapAction>();
        // Don't queue an action if we didn't actually change anything
        if (DraggedObjectData.ToString() != OriginalDraggedObjectData.ToString())
        {
            if (conflicting.Any())
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

        DraggedObjectContainer.Dragging = false;
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
            if (conflictingArcs.Any())
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
        DraggedAttachedSliderContainers.Clear();
        DraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        DraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
        OriginalDraggedAttachedSliderDatas[IndicatorType.Head].Clear();
        OriginalDraggedAttachedSliderDatas[IndicatorType.Tail].Clear();
    }
}
