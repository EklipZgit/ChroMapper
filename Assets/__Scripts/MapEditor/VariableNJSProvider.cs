using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class VariableNJSProvider : StateManager<VariableNJSStateData, BaseNJSEvent>
{
    private float baseNjs;
    public float CurrentNjs = 10f;

    private bool init;

    private readonly VariableNJSStateChunksContainer stateChunksContainer = new();

    private void Awake()
    {
        BeatmapActionContainer.ActionCreatedEvent += HandleActionEventRedo;
        BeatmapActionContainer.ActionRedoEvent += HandleActionEventRedo;
        BeatmapActionContainer.ActionUndoEvent += HandleActionEventUndo;
        LoadInitialMap.LevelLoadedEvent += HandleLevelLoaded;
    }

    public void OnDestroy()
    {
        BeatmapActionContainer.ActionCreatedEvent -= HandleActionEventRedo;
        BeatmapActionContainer.ActionRedoEvent -= HandleActionEventRedo;
        BeatmapActionContainer.ActionUndoEvent -= HandleActionEventUndo;
        LoadInitialMap.LevelLoadedEvent -= HandleLevelLoaded;
        Atsc.TimeChangedEarly -= HandleTimeChangedEarly;
    }

    private void HandleLevelLoaded()
    {
        Initialize();
        baseNjs = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        CurrentNjs = baseNjs;
        BuildFromData(BeatSaberSongContainer.Instance.Map.NJSEvents);

        if (init) return;
        Atsc.TimeChangedEarly += HandleTimeChangedEarly;
        init = true;
    }

    private void HandleTimeChangedEarly() => UpdateTime(Atsc.CurrentSongBpmTime);

    public override void Initialize()
    {
        InitializeStates(
            stateChunksContainer,
            CreateState(new BaseNJSEvent { UsePrevious = 1 }),
            CreateState(new BaseNJSEvent { UsePrevious = 1 }));
    }

    public override void UpdateTime(float time)
    {
        stateChunksContainer.IsCurrentOrFindState(time, Atsc.IsPlaying);

        var currentState = stateChunksContainer.CurrentState;
        var normalizedTime = (time - currentState.StartTime) / (currentState.EndTime - currentState.StartTime);
        CurrentNjs = Mathf.Max(
            baseNjs
            + Mathf.Lerp(
                currentState.RelativeNjs,
                currentState.NextRelativeNjs,
                currentState.Easing(normalizedTime)),
            0.01f);
    }

    public override void BuildFromData(IEnumerable<BaseNJSEvent> data)
    {
        foreach (var evt in data) InsertData(evt);
    }

    protected override void OnInsertUpdateToPreviousState(VariableNJSStateData newState, VariableNJSStateData prevState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.NextRelativeNjs = newState.Base.UsePrevious == 1 ? prevState.RelativeNjs : newState.RelativeNjs;
        var easingId = newState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => newState.Base.Easing
        };
        prevState.Easing = Easing.FromID(easingId);
    }

    protected override void OnInsertUpdateFromNextState(VariableNJSStateData newState, VariableNJSStateData nextState)
    {
        base.OnInsertUpdateFromNextState(newState, nextState);
        newState.NextRelativeNjs = nextState.Base.UsePrevious == 1 ? newState.RelativeNjs : nextState.RelativeNjs;
        var easingId = nextState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => nextState.Base.Easing
        };
        newState.Easing = Easing.FromID(easingId);
    }

    public override void InsertData(BaseNJSEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.RelativeNjs = data.RelativeNJS;

        HandleInsertState(stateChunksContainer, state);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        VariableNJSStateData currState,
        VariableNJSStateData prevState,
        VariableNJSStateData nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);
        prevState.NextRelativeNjs = nextState.Base.UsePrevious == 1 ? prevState.RelativeNjs : nextState.RelativeNjs;
        var easingId = nextState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => nextState.Base.Easing
        };
        prevState.Easing = Easing.FromID(easingId);
    }

    public override void RemoveData(BaseNJSEvent data)
    {
        var state = HandleRemoveState(stateChunksContainer, data);
        if (state == stateChunksContainer.CurrentState) stateChunksContainer.SetStateAt(data.SongBpmTime);
    }

    public override void Reset()
    {
    }

    protected override VariableNJSStateData CreateState(BaseNJSEvent data) => new(data);

    private bool AddEvents(IEnumerable<BaseNJSEvent> events)
    {
        var mark = false;
        foreach (var data in events)
        {
            InsertData(data);
            mark = true;
        }

        return mark;
    }

    private bool RemoveEvents(IEnumerable<(BaseNJSEvent reference, BaseNJSEvent original)> events)
    {
        var mark = false;
        foreach (var (reference, _) in events)
        {
            RemoveData(reference);
            mark = true;
        }

        return mark;
    }

    private bool RemoveEvents(IEnumerable<BaseNJSEvent> events)
    {
        var mark = false;
        foreach (var data in events)
        {
            RemoveData(data);
            mark = true;
        }

        return mark;
    }

    private void HandleActionEventRedo(BeatmapAction action)
    {
        if (!HandleActionEventRedoNoNotify(action) || Atsc.IsPlaying) return;
        UpdateTime(Atsc.CurrentSongBpmTime);
    }

    private bool HandleActionEventRedoNoNotify(BeatmapAction action)
    {
        return action switch
        {
            ActionCollectionAction actionCollectionAction => actionCollectionAction
                .Actions.ToArray()
                .Select(HandleActionEventRedoNoNotify)
                .Any(),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionRedo(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction => HandleSelectionDeletedActionRedo(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionRedo(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionRedo(
                    strobeGeneratorGenerationAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionRedo(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionRedo(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionRedo(beatmapObjectModifiedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionRedo(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    private bool HandlePlacementActionRedo(BeatmapObjectPlacementAction action)
    {
        var b = RemoveEvents(
            action
                .RemovedConflictObjects
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        b = AddEvents(
                action
                    .Data.Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
        return b;
    }

    private bool HandleSelectionDeletedActionRedo(SelectionDeletedAction action) =>
        RemoveEvents(
            action
                .Data.Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());

    private bool HandleSelectionPastedActionRedo(SelectionPastedAction action)
    {
        var b = RemoveEvents(
            action
                .Removed
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        b = AddEvents(
                action
                    .Data.Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
        return b;
    }

    private bool HandleStrobeGeneratorGenerationActionRedo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveEvents(
            action
                .ConflictingData
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        return AddEvents(
                action
                    .Data.Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleDeletionActionRedo(BeatmapObjectDeletionAction action) =>
        RemoveEvents(
            action
                .Data.Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());

    private bool HandleModifiedActionRedo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveEvents(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: BaseNJSEvent, Item2: BaseNJSEvent })
                .Select(d => (d.Item1 as BaseNJSEvent, d.Item2 as BaseNJSEvent)));
        return AddEvents(
                new List<BaseObject> { action.EditedObject }
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleModifiedCollectionActionRedo(BeatmapObjectModifiedCollectionAction action)
    {
        var b = RemoveEvents(
            action
                .OriginalObjects
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        return AddEvents(
                action
                    .EditedObjects
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleModifiedWithConflictingActionRedo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveEvents(
            new List<BaseObject> { action.OriginalObject }
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        b = RemoveEvents(
                action
                    .ConflictingObjects
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
        return AddEvents(
                new List<BaseObject> { action.EditedObject }
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private void HandleActionEventUndo(BeatmapAction action)
    {
        if (!HandleActionEventUndoNoNotify(action) || Atsc.IsPlaying) return;
        UpdateTime(Atsc.CurrentSongBpmTime);
    }

    private bool HandleActionEventUndoNoNotify(BeatmapAction action)
    {
        return action switch
        {
            ActionCollectionAction actionCollectionAction => actionCollectionAction
                .Actions.ToArray()
                .Select(HandleActionEventUndoNoNotify)
                .Any(),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionUndo(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction => HandleSelectionDeletedActionUndo(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionUndo(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionUndo(
                    strobeGeneratorGenerationAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionUndo(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionUndo(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionUndo(beatmapObjectModifiedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionUndo(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    private bool HandlePlacementActionUndo(BeatmapObjectPlacementAction action)
    {
        var b = RemoveEvents(
            action
                .Data
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        return AddEvents(
                action
                    .RemovedConflictObjects
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleSelectionDeletedActionUndo(SelectionDeletedAction action) =>
        AddEvents(
            action
                .Data
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());

    private bool HandleSelectionPastedActionUndo(SelectionPastedAction action)
    {
        var b = RemoveEvents(
            action
                .Data
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        return AddEvents(
                action
                    .Removed
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleStrobeGeneratorGenerationActionUndo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveEvents(
            action
                .Data
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        return AddEvents(
                action
                    .ConflictingData.Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleDeletionActionUndo(BeatmapObjectDeletionAction action) =>
        AddEvents(
            action
                .Data.Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());

    private bool HandleModifiedActionUndo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveEvents(
            new List<(BaseObject, BaseObject)> { (action.EditedObject, action.EditedData) }
                .Where(d => d is { Item1: BaseNJSEvent, Item2: BaseNJSEvent })
                .Select(d => (d.Item1 as BaseNJSEvent, d.Item2 as BaseNJSEvent)));
        return AddEvents(
                new List<BaseObject> { action.OriginalObject }
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleModifiedCollectionActionUndo(BeatmapObjectModifiedCollectionAction action)
    {
        var b = RemoveEvents(
            action
                .EditedObjects
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        return AddEvents(
                action
                    .OriginalObjects
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }

    private bool HandleModifiedWithConflictingActionUndo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveEvents(
            new List<BaseObject> { action.EditedObject }
                .Where(d => d is BaseNJSEvent)
                .Cast<BaseNJSEvent>());
        b = AddEvents(
                new List<BaseObject> { action.OriginalObject }
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
        return AddEvents(
                action
                    .ConflictingObjects
                    .Where(d => d is BaseNJSEvent)
                    .Cast<BaseNJSEvent>())
            || b;
    }
}
