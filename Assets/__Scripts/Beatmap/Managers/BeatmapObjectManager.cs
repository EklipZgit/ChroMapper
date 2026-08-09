using System;
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
        BeatmapActionContainer.OnActionCreated += HandleActionCreated;
        BeatmapActionContainer.OnActionRedo += HandleActionRedo;
        BeatmapActionContainer.OnActionUndo += HandleActionUndo;
    }

    protected virtual void OnDestroy()
    {
        BeatmapActionContainer.OnActionCreated -= HandleActionCreated;
        BeatmapActionContainer.OnActionRedo -= HandleActionRedo;
        BeatmapActionContainer.OnActionUndo -= HandleActionUndo;
    }

    protected abstract bool AddData(IEnumerable<T> data);
    protected abstract bool RemoveData(IEnumerable<(T reference, T original)> data);
    protected abstract bool RemoveData(IEnumerable<T> data);

    private void HandleActionCreated(BeatmapAction action)
    {
        if (!AllowAction) return;
        if (!HandleActionEventCreatedNoNotify(action) || Context.Atsc.IsPlaying) return;
        UpdateTime();
    }

    private bool HandleActionEventCreatedNoNotify(BeatmapAction action)
    {
        return action switch
        {
            ActionCollectionAction actionCollectionAction =>
                HandleEveryCollectionAction(actionCollectionAction, HandleActionEventCreatedNoNotify),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionCreated(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction =>
                HandleSelectionDeletedActionCreated(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionCreated(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionCreated(
                    strobeGeneratorGenerationAction),
            BeatmapGLSEventBoxModifiedAction beatmapGLSEventBoxModifiedAction => HandleGLSEventBoxModifiedActionCreated(
                beatmapGLSEventBoxModifiedAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionCreated(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionCreated(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionCreated(beatmapObjectModifiedAction),
            BeatmapObjectUpdatedAction beatmapObjectUpdatedAction =>
                HandleUpdatedActionCreated(beatmapObjectUpdatedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionCreated(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    private bool HandlePlacementActionCreated(BeatmapObjectPlacementAction action)
    {
        var b = RemoveData(action.RemovedConflictObjects.OfType<T>());
        b = AddData(action.Data.OfType<T>()) || b;
        return b;
    }

    private bool HandleGLSEventBoxModifiedActionCreated(BeatmapGLSEventBoxModifiedAction action)
    {
        var b = RemoveData(
            action.PreMergeOriginalData is T preBaseObject ? new[] { preBaseObject } :
            action.OriginalObject is T baseObject          ? new[] { baseObject } : Enumerable.Empty<T>());
        b = AddData(action.Data.OfType<T>()) || b;
        return b;
    }

    private bool HandleSelectionDeletedActionCreated(SelectionDeletedAction action) =>
        RemoveData(action.Data.OfType<T>());

    private bool HandleSelectionPastedActionCreated(SelectionPastedAction action)
    {
        var b = RemoveData(action.Removed.OfType<T>());
        return AddData(action.Data.OfType<T>()) || b;
    }

    private bool HandleStrobeGeneratorGenerationActionCreated(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveData(action.ConflictingData.OfType<T>());
        return AddData(action.Data.OfType<T>()) || b;
    }

    private bool HandleDeletionActionCreated(BeatmapObjectDeletionAction action) => RemoveData(action.Data.OfType<T>());

    private bool HandleUpdatedActionCreated(BeatmapObjectUpdatedAction action)
    {
        var b = RemoveData(action.OriginalObject is T baseObject ? new[] { baseObject } : Enumerable.Empty<T>());
        return AddData(new List<BaseObject> { action.EditedObject }.OfType<T>()) || b;
    }

    private bool HandleModifiedActionCreated(BeatmapObjectModifiedAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        return AddData(new List<BaseObject> { action.EditedObject }.OfType<T>()) || b;
    }

    private bool HandleModifiedCollectionActionCreated(BeatmapObjectModifiedCollectionAction action)
    {
        // Replacing C selected objects avoids BasicEventManager's former O(M) whole-map simulation rebuild.
        var b = RemoveData(action.OriginalObjects.OfType<T>());
        return AddData(action.EditedObjects.OfType<T>()) || b;
    }

    private bool HandleModifiedWithConflictingActionCreated(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        b = RemoveData(action.ConflictingObjects.OfType<T>()) || b;
        return AddData(new List<BaseObject> { action.EditedObject }.OfType<T>()) || b;
    }

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
            ActionCollectionAction actionCollectionAction =>
                HandleEveryCollectionAction(actionCollectionAction, HandleActionEventRedoNoNotify),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionRedo(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction => HandleSelectionDeletedActionRedo(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionRedo(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionRedo(
                    strobeGeneratorGenerationAction),
            BeatmapGLSEventBoxModifiedAction beatmapGLSEventBoxModifiedAction => HandleGLSEventBoxModifiedActionRedo(
                beatmapGLSEventBoxModifiedAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionRedo(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionRedo(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionRedo(beatmapObjectModifiedAction),
            BeatmapObjectUpdatedAction beatmapObjectUpdatedAction =>
                HandleUpdatedActionRedo(beatmapObjectUpdatedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionRedo(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    private bool HandlePlacementActionRedo(BeatmapObjectPlacementAction action)
    {
        var b = RemoveData(action.RemovedConflictObjects.OfType<T>());
        b = AddData(action.Data.OfType<T>()) || b;
        return b;
    }

    private bool HandleGLSEventBoxModifiedActionRedo(BeatmapGLSEventBoxModifiedAction action)
    {
        var b = RemoveData(action.OriginalObject is T baseObject ? new[] { baseObject } : Enumerable.Empty<T>());
        b = AddData(action.Data.OfType<T>()) || b;
        return b;
    }

    private bool HandleSelectionDeletedActionRedo(SelectionDeletedAction action) => RemoveData(action.Data.OfType<T>());

    private bool HandleSelectionPastedActionRedo(SelectionPastedAction action)
    {
        var b = RemoveData(action.Removed.OfType<T>());
        return AddData(action.Data.OfType<T>()) || b;
    }

    private bool HandleStrobeGeneratorGenerationActionRedo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveData(action.ConflictingData.OfType<T>());
        return AddData(action.Data.OfType<T>()) || b;
    }

    private bool HandleDeletionActionRedo(BeatmapObjectDeletionAction action) => RemoveData(action.Data.OfType<T>());

    private bool HandleUpdatedActionRedo(BeatmapObjectUpdatedAction action)
    {
        var b = RemoveData(action.OriginalObject is T baseObject ? new[] { baseObject } : Enumerable.Empty<T>());
        return AddData(new List<BaseObject> { action.EditedObject }.OfType<T>()) || b;
    }

    private bool HandleModifiedActionRedo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        return AddData(new List<BaseObject> { action.EditedObject }.OfType<T>()) || b;
    }

    private bool HandleModifiedCollectionActionRedo(BeatmapObjectModifiedCollectionAction action)
    {
        // Replacing C selected objects avoids BasicEventManager's former O(M) whole-map simulation rebuild.
        var b = RemoveData(action.OriginalObjects.OfType<T>());
        return AddData(action.EditedObjects.OfType<T>()) || b;
    }

    private bool HandleModifiedWithConflictingActionRedo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.OriginalObject, action.OriginalData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        b = RemoveData(action.ConflictingObjects.OfType<T>()) || b;
        return AddData(new List<BaseObject> { action.EditedObject }.OfType<T>()) || b;
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
            ActionCollectionAction actionCollectionAction =>
                HandleEveryCollectionAction(actionCollectionAction, HandleActionEventUndoNoNotify),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionUndo(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction => HandleSelectionDeletedActionUndo(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionUndo(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionUndo(
                    strobeGeneratorGenerationAction),
            BeatmapGLSEventBoxModifiedAction beatmapGLSEventBoxModifiedAction => HandleGLSEventBoxModifiedActionUndo(
                beatmapGLSEventBoxModifiedAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionUndo(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionUndo(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionUndo(beatmapObjectModifiedAction),
            BeatmapObjectUpdatedAction beatmapObjectUpdatedAction =>
                HandleUpdatedActionUndo(beatmapObjectUpdatedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionUndo(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    // Process every child because Any() short-circuiting leaves later objects absent from dependent manager caches.
    private static bool HandleEveryCollectionAction(
        ActionCollectionAction collection,
        Func<BeatmapAction, bool> handler)
    {
        var handled = false;
        foreach (var action in collection.Actions)
        {
            handled = handler(action) || handled;
        }

        return handled;
    }

    private bool HandlePlacementActionUndo(BeatmapObjectPlacementAction action)
    {
        var b = RemoveData(action.Data.OfType<T>());
        return AddData(action.RemovedConflictObjects.OfType<T>()) || b;
    }

    private bool HandleGLSEventBoxModifiedActionUndo(BeatmapGLSEventBoxModifiedAction action)
    {
        var b = RemoveData(action.Data.OfType<T>());
        return AddData(action.OriginalObject is T baseObject ? new[] { baseObject } : Enumerable.Empty<T>()) || b;
    }

    private bool HandleSelectionDeletedActionUndo(SelectionDeletedAction action) => AddData(action.Data.OfType<T>());

    private bool HandleSelectionPastedActionUndo(SelectionPastedAction action)
    {
        var b = RemoveData(action.Data.OfType<T>());
        return AddData(action.Removed.OfType<T>()) || b;
    }

    private bool HandleStrobeGeneratorGenerationActionUndo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveData(action.Data.OfType<T>());
        return AddData(action.ConflictingData.OfType<T>()) || b;
    }

    private bool HandleDeletionActionUndo(BeatmapObjectDeletionAction action) => AddData(action.Data.OfType<T>());

    private bool HandleUpdatedActionUndo(BeatmapObjectUpdatedAction action)
    {
        var b = RemoveData(action.EditedObject is T baseObject ? new[] { baseObject } : Enumerable.Empty<T>());
        return AddData(new List<BaseObject> { action.OriginalObject }.OfType<T>()) || b;
    }

    private bool HandleModifiedActionUndo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.EditedObject, action.EditedData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        return AddData(new List<BaseObject> { action.OriginalObject }.OfType<T>()) || b;
    }

    private bool HandleModifiedCollectionActionUndo(BeatmapObjectModifiedCollectionAction action)
    {
        // Restoring C selected objects avoids BasicEventManager's former O(M) whole-map simulation rebuild.
        var b = RemoveData(action.EditedObjects.OfType<T>());
        return AddData(action.OriginalObjects.OfType<T>()) || b;
    }

    private bool HandleModifiedWithConflictingActionUndo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveData(
            new List<(BaseObject, BaseObject)> { (action.EditedObject, action.EditedData) }
                .Where(d => d is { Item1: T, Item2: T })
                .Select(d => (d.Item1 as T, d.Item2 as T)));
        b = AddData(new List<BaseObject> { action.OriginalObject }.OfType<T>()) || b;
        return AddData(action.ConflictingObjects.OfType<T>()) || b;
    }
}
