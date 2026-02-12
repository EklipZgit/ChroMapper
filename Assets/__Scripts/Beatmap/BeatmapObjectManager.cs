using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public abstract class BeatmapObjectManager : MonoBehaviour, IBeatmapUpdate
{
    [SerializeField] protected BeatmapRuntimeContext Context;

    public abstract void Refresh();
    public abstract void UpdateTime();
    public abstract void UpdateTime(bool isPlaying, float beatTime);
}

public abstract class BeatmapObjectManager<T> : BeatmapObjectManager where T : BaseObject
{
    protected virtual bool AllowAction => true;

    protected virtual void Awake()
    {
        BeatmapActionContainer.OnActionCreated += HandleActionRedo;
        BeatmapActionContainer.OnActionRedo += HandleActionRedo;
        BeatmapActionContainer.OnActionUndo += HandleActionUndo;
    }

    protected virtual void OnDestroy()
    {
        BeatmapActionContainer.OnActionCreated -= HandleActionRedo;
        BeatmapActionContainer.OnActionRedo -= HandleActionRedo;
        BeatmapActionContainer.OnActionUndo -= HandleActionUndo;
    }

    protected abstract bool AddData(IEnumerable<T> data);
    protected abstract bool RemoveData(IEnumerable<(T reference, T original)> data);
    protected abstract bool RemoveData(IEnumerable<T> data);

    private void HandleActionRedo(BeatmapAction action)
    {
        if (!AllowAction) return;
        if (!HandleActionEventRedoNoNotify(action) || Context.Atsc.IsPlaying) return;
        UpdateTime();
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
        var b = RemoveData(
            action
                .RemovedConflictObjects
                .Where(d => d is T)
                .Cast<T>());
        b = AddData(
                action
                    .Data.Where(d => d is T)
                    .Cast<T>())
            || b;
        return b;
    }

    private bool HandleSelectionDeletedActionRedo(SelectionDeletedAction action) =>
        RemoveData(
            action
                .Data.Where(d => d is T)
                .Cast<T>());

    private bool HandleSelectionPastedActionRedo(SelectionPastedAction action)
    {
        var b = RemoveData(
            action
                .Removed
                .Where(d => d is T)
                .Cast<T>());
        b = AddData(
                action
                    .Data.Where(d => d is T)
                    .Cast<T>())
            || b;
        return b;
    }

    private bool HandleStrobeGeneratorGenerationActionRedo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveData(
            action
                .ConflictingData
                .Where(d => d is T)
                .Cast<T>());
        return AddData(
                action
                    .Data.Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleDeletionActionRedo(BeatmapObjectDeletionAction action) =>
        RemoveData(
            action
                .Data.Where(d => d is T)
                .Cast<T>());

    private bool HandleModifiedActionRedo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        return AddData(
                new List<BaseObject> { action.EditedObject }
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleModifiedCollectionActionRedo(BeatmapObjectModifiedCollectionAction action)
    {
        var b = RemoveData(
            action
                .OriginalObjects
                .Where(d => d is T)
                .Cast<T>());
        return AddData(
                action
                    .EditedObjects
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleModifiedWithConflictingActionRedo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        b = RemoveData(
                action
                    .ConflictingObjects
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
        return AddData(
                new List<BaseObject> { action.EditedObject }
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private void HandleActionUndo(BeatmapAction action)
    {
        if (!AllowAction) return;
        if (!HandleActionEventUndoNoNotify(action) || Context.Atsc.IsPlaying) return;
        UpdateTime();
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
        var b = RemoveData(
            action
                .Data
                .Where(d => d is T)
                .Cast<T>());
        return AddData(
                action
                    .RemovedConflictObjects
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleSelectionDeletedActionUndo(SelectionDeletedAction action) =>
        AddData(
            action
                .Data
                .Where(d => d is T)
                .Cast<T>());

    private bool HandleSelectionPastedActionUndo(SelectionPastedAction action)
    {
        var b = RemoveData(
            action
                .Data
                .Where(d => d is T)
                .Cast<T>());
        return AddData(
                action
                    .Removed
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleStrobeGeneratorGenerationActionUndo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveData(
            action
                .Data
                .Where(d => d is T)
                .Cast<T>());
        return AddData(
                action
                    .ConflictingData.Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleDeletionActionUndo(BeatmapObjectDeletionAction action) =>
        AddData(
            action
                .Data.Where(d => d is T)
                .Cast<T>());

    private bool HandleModifiedActionUndo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.EditedObject, action.EditedData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        return AddData(
                new List<BaseObject> { action.OriginalObject }
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleModifiedCollectionActionUndo(BeatmapObjectModifiedCollectionAction action)
    {
        var b = RemoveData(
            action
                .EditedObjects
                .Where(d => d is T)
                .Cast<T>());
        return AddData(
                action
                    .OriginalObjects
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }

    private bool HandleModifiedWithConflictingActionUndo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.EditedObject, action.EditedData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        b = AddData(
                new List<BaseObject> { action.OriginalObject }
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
        return AddData(
                action
                    .ConflictingObjects
                    .Where(d => d is T)
                    .Cast<T>())
            || b;
    }
}
