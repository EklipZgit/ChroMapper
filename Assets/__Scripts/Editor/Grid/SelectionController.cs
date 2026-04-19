using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Big boi master class for everything Selection.
/// </summary>
public class SelectionController : MonoBehaviour, CMInput.ISelectingActions, CMInput.IModifyingSelectionActions
{
    public static HashSet<BaseObject> SelectedObjects = new();
    public static HashSet<BaseObject> CopiedObjects = new();

    public static Action<BaseObject> OnObjectWasSelected;
    public static Action OnSelectionChanged;
    public static Action<IEnumerable<BaseObject>> OnSelectionPasted;

    private static SelectionController instance;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private BPMChangeGridContainer bpmChangesContainer;
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color copiedColor;
    [SerializeField] private TracksManager tracksManager;

    [Header("Basic Event")] [SerializeField]
    private EventPlacement eventPlacement;

    [SerializeField] private EventGridContainer eventGridContainer;

    [Header("GLS Group")] [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;
    [SerializeField] private GLSGroupColorPlacement glsGroupColorPlacement;
    [SerializeField] private GLSGroupColorGridContainer glsGroupColorGridContainer;
    [SerializeField] private GLSGroupRotationPlacement glsGroupRotationPlacement;
    [SerializeField] private GLSGroupRotationGridContainer glsGroupRotationGridContainer;
    [SerializeField] private GLSGroupTranslationPlacement glsGroupTranslationPlacement;
    [SerializeField] private GLSGroupTranslationGridContainer glsGroupTranslationGridContainer;
    [SerializeField] private GLSGroupFloatFXPlacement glsGroupFloatFXPlacement;
    [SerializeField] private GLSGroupFloatFXGridContainer glsGroupFloatFXGridContainer;

    [Header("GLS Event")] [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private GLSEventColorPlacement glsEventColorPlacement;
    [SerializeField] private GLSEventRotationPlacement glsEventRotationPlacement;
    [SerializeField] private GLSEventTranslationPlacement glsEventTranslationPlacement;
    [SerializeField] private GLSEventFloatFXPlacement glsEventFloatFXPlacement;
    [SerializeField] private GLSEventGridContainer glsEventGridContainer;

    [SerializeField] private CreateEventTypeLabels labels;
    private bool shiftInPlace;

    private bool shiftInTime;

    public static Color SelectedColor => instance.selectedColor;
    public static Color CopiedColor => instance.copiedColor;

    // TODO: perhaps this is useful elsewhere
    private static Dictionary<ObjectType, EditingMode> allowedObjectToEdit = new()
    {
        { ObjectType.Note, EditingMode.Gameplay },
        { ObjectType.Event, EditingMode.BasicEvent },
        { ObjectType.Obstacle, EditingMode.Gameplay },
        { ObjectType.CustomNote, EditingMode.Gameplay },
        { ObjectType.CustomEvent, EditingMode.Gameplay },
        { ObjectType.BpmChange, EditingMode.Gameplay },
        { ObjectType.Arc, EditingMode.Gameplay },
        { ObjectType.Chain, EditingMode.Gameplay },
        { ObjectType.Bookmark, EditingMode.Gameplay },
        { ObjectType.Waypoint, EditingMode.BasicEvent },
        { ObjectType.NJSEvent, EditingMode.Gameplay },
        { ObjectType.EnvironmentEnhancement, (EditingMode)0xff },
        { ObjectType.GLSColor, EditingMode.GLS },
        { ObjectType.GLSRotation, EditingMode.GLS },
        { ObjectType.GLSTranslation, EditingMode.GLS },
        { ObjectType.GLSFloatFx, EditingMode.GLS },
        { ObjectType.GLSEvent, EditingMode.EventBox }
    };

    // Use this for initialization
    private void Start()
    {
        instance = this;
        SelectedObjects.Clear();
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
    }

    private void OnDestroy() => editModeContext.OnEditModeChanged -= HandleEditModeChanged;

    private void HandleEditModeChanged(EditingMode mode) => DeselectAll();

    public void OnPaste(InputAction.CallbackContext context)
    {
        if (context.performed) Paste();
    }

    public void OnOverwritePaste(InputAction.CallbackContext context)
    {
        if (context.performed) Paste(true, true);
    }

    public void OnDeleteObjects(InputAction.CallbackContext context)
    {
        if (context.performed) Delete();
    }

    public void OnCopy(InputAction.CallbackContext context)
    {
        if (context.performed) Copy();
    }

    public void OnCut(InputAction.CallbackContext context)
    {
        if (context.performed) Copy(true);
    }

    public void OnShiftingMovement(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var movement = context.ReadValue<Vector2>();

        if (shiftInPlace) ShiftSelection(Mathf.RoundToInt(movement.x), Mathf.RoundToInt(movement.y));

        if (shiftInTime) MoveSelection(movement.y * (1f / atsc.GridMeasureSnapping));
    }

    public void OnActivateShiftinTime(InputAction.CallbackContext context) => shiftInTime = context.performed;

    public void OnActivateShiftinPlace(InputAction.CallbackContext context) => shiftInPlace = context.performed;

    public void OnDeselectAll(InputAction.CallbackContext context)
    {
        if (context.performed) DeselectAll();
    }

    private void RefreshMovedEventsAppearance(IEnumerable<BaseEvent> events)
    {
        if (!events.Any()) return;

        var eventContainer =
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
        eventContainer.MarkEventsToBeRelinked(events);
        eventContainer.LinkAllLightEvents();
        eventContainer.RefreshEventsAppearance(events);
    }

    #region Utils

    /// <summary>
    ///     Does the user have any selected objects?
    /// </summary>
    public static bool HasSelectedObjects() => SelectedObjects.Count > 0;

    /// <summary>
    ///     Does the user have any copied objects?
    /// </summary>
    public static bool HasCopiedObjects() => CopiedObjects.Count > 0;

    /// <summary>
    ///     Returns true if the given container is selected, and false if it's not.
    /// </summary>
    /// <param name="container">Container to check.</param>
    public static bool IsObjectSelected(BaseObject container) => SelectedObjects.Contains(container);

    /// <summary>
    ///     Given a list of generic objects, returns a bitmask of the groups that these objects belong to.
    /// </summary>
    /// <param name="objects">Enumerable group of objects</param>
    public static ObjectType GetObjectTypes(IEnumerable<BaseObject> objects) =>
        objects.Aggregate((ObjectType)0, (current, obj) => current | obj.ObjectType);

    public static ObjectType GetObjectTypesGrouped(IEnumerable<BaseObject> objects)
    {
        ObjectType grouping = 0;
        foreach (var obj in objects)
        {
            switch (obj.ObjectType)
            {
                case ObjectType.Note:
                case ObjectType.Obstacle:
                case ObjectType.CustomNote:
                case ObjectType.Arc:
                case ObjectType.Chain:
                    grouping |= ObjectType.Note
                        | ObjectType.Obstacle
                        | ObjectType.CustomNote
                        | ObjectType.Arc
                        | ObjectType.Chain;
                    break;
                case ObjectType.Event:
                case ObjectType.CustomEvent:
                    grouping |= ObjectType.Event | ObjectType.CustomEvent;
                    break;
                case ObjectType.BpmChange:
                    grouping |= ObjectType.BpmChange;
                    break;
                case ObjectType.NJSEvent:
                    grouping |= ObjectType.NJSEvent;
                    break;
                default:
                    grouping |= obj.ObjectType;
                    break;
            }
        }

        return grouping;
    }

    /// <summary>
    ///     Invokes a callback for all objects between a time by group
    /// </summary>
    /// <param name="start">Start time in beats</param>
    /// <param name="start">End time in beats</param>
    /// <param name="filterTypes">Which groups to include in the search</param>
    /// <param name="callback">Callback with an object container and the collection it belongs to</param>
    public static void ForEachObjectBetweenSongBpmTimeByGroup(
        float start,
        float end,
        ObjectType filterTypes,
        Action<BeatmapObjectContainerCollection, BaseObject> callback)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        for (var typeInt = 0; typeInt <= 32; typeInt++)
        {
            // Convert int to bitmask
            var type = (ObjectType)(1 << typeInt);
            if ((filterTypes & type) == 0) continue;

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(type);
            if (collection == null) continue;

            IEnumerable<BaseObject> objectsToCheck;

            // REVIEW: Considering a downcast appears to be necessary, I am not sure if
            //   a LoadedObjects (or similar) allocation is avoidable without a complete
            //   rewrite to this function.
            if (collection is ArcGridContainer or ChainGridContainer)
            {
                objectsToCheck = collection.LoadedObjects.Where(x =>
                    (start - epsilon < x.SongBpmTime && x.SongBpmTime < end + epsilon)
                    || (x.SongBpmTime < start + epsilon && start - epsilon < (x as BaseSlider).TailSongBpmTime));
            }
            else
            {
                objectsToCheck = collection.LoadedObjects.Where(x =>
                    start - epsilon < x.SongBpmTime && x.SongBpmTime < end + epsilon);
            }

            foreach (var toCheck in objectsToCheck) callback?.Invoke(collection, toCheck);
        }
    }

    #endregion

    #region Selection

    /// <summary>
    ///     Select an individual container.
    /// </summary>
    /// <param name="container">The container to select.</param>
    /// <param name="addsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    /// <param name="addActionEvent">If an action event to undo the selection should be made</param>
    public static void Select(
        BaseObject obj,
        bool addsToSelection = false,
        bool automaticallyRefreshes = true,
        bool addActionEvent = true)
    {
        if (!addsToSelection)
            DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);

        if (!collection.ContainsObject(obj)) return;

        SelectedObjects.Add(obj);
        if (collection.LoadedContainers.TryGetValue(obj, out var container))
        {
            container.SetOutlineColor(instance.selectedColor);
            container.Selected = true;
        }

        if (addActionEvent)
        {
            OnObjectWasSelected?.Invoke(obj);
            OnSelectionChanged?.Invoke();
        }
    }

    /// <summary>
    ///     Selects objects between 2 objects, sorted by group.
    /// </summary>
    /// <param name="first">The beatmap object at the one end of the selection.</param>
    /// <param name="second">The beatmap object at the other end of the selection</param>
    /// <param name="addsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    /// <param name="addActionEvent">If an action event to undo the selection should be made</param>
    public static void SelectBetween(
        BaseObject first,
        BaseObject second,
        bool addsToSelection = false,
        bool addActionEvent = true)
    {
        if (!addsToSelection)
            DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        if (first.SongBpmTime > second.SongBpmTime) (first, second) = (second, first);
        var types = GetObjectTypesGrouped(
            new[] { first, second });
        ForEachObjectBetweenSongBpmTimeByGroup(
            first.SongBpmTime,
            second.SongBpmTime,
            types,
            (collection, beatmapObject) =>
            {
                if (!SelectedObjects.Add(beatmapObject)) return;
                if (collection.LoadedContainers.TryGetValue(beatmapObject, out var container))
                {
                    container.SetOutlineColor(instance.selectedColor);
                    container.Selected = true;
                }

                if (addActionEvent) OnObjectWasSelected?.Invoke(beatmapObject);
            });
        if (addActionEvent) OnSelectionChanged?.Invoke();
    }

    /// <summary>
    ///     Deselects a container if it is currently selected
    /// </summary>
    /// <param name="obj">The container to deselect, if it has been selected.</param>
    public static void Deselect(BaseObject obj, bool removeActionEvent = true)
    {
        SelectedObjects.Remove(obj);
        if (BeatmapObjectContainerCollection
                .GetCollectionForType(obj.ObjectType)
                .LoadedContainers.TryGetValue(obj, out var container)
            && container != null)
            container.Selected = false;

        if (removeActionEvent) OnSelectionChanged?.Invoke();
    }

    /// <summary>
    ///     Deselect all selected objects.
    /// </summary>
    public static void DeselectAll(bool removeActionEvent = true)
    {
        foreach (var obj in SelectedObjects.ToArray()) Deselect(obj, false);
        if (removeActionEvent) OnSelectionChanged?.Invoke();
    }

    /// <summary>
    ///     Can be very taxing. Use sparringly.
    /// </summary>
    internal static void RefreshSelectionMaterial(bool triggersAction = true)
    {
        foreach (var data in SelectedObjects)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType);
            if (collection.LoadedContainers.TryGetValue(data, out var con))
            {
                con.SetOutlineColor(instance.selectedColor);
                con.Selected = true;
            }
        }
        //if (triggersAction) BeatmapActionContainer.AddAction(new SelectionChangedAction(SelectedObjects));
    }

    #endregion

    #region Manipulation

    /// <summary>
    ///     Deletes and clears the current selection.
    /// </summary>
    public void Delete(bool triggersAction = true)
    {
        IEnumerable<BaseObject> objects = SelectedObjects
            .Where(x =>
                (allowedObjectToEdit[x.ObjectType] & editModeContext.EditingMode) > 0)
            .ToArray();
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionDeletedAction(objects));
        DeselectAll();
        foreach (var con in objects)
            BeatmapObjectContainerCollection.GetCollectionForType(con.ObjectType).DeleteObject(con, false, false);
    }

    /// <summary>
    ///     Copies the current selection for later Pasting.
    /// </summary>
    /// <param name="cut">Whether or not to delete the original selection after copying them.</param>
    public void Copy(bool cut = false)
    {
        if (!HasSelectedObjects()) return;
        CopiedObjects.Clear();
        var firstJsonTime = SelectedObjects.OrderBy(x => x.JsonTime).First().JsonTime;
        foreach (var data in SelectedObjects)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType);
            if (collection.LoadedContainers.TryGetValue(data, out var con))
            {
                con.SetOutlineColor(instance.copiedColor);
                con.Selected = true;
            }

            var copy = BeatmapFactory.Clone(data);

            copy.JsonTime -= firstJsonTime;
            if (copy is BaseSlider slider) slider.TailJsonTime -= firstJsonTime;

            CopiedObjects.Add(copy);
        }

        if (cut) Delete();
    }

    /// <summary>
    ///     Pastes any copied objects into the map, selecting them immediately.
    /// </summary>
    public void Paste(bool triggersAction = true, bool overwriteSection = false)
    {
        var newObjects = GetNewObjects(CopiedObjects);
        if (newObjects.Count == 0) return; // nothing to paste, nothing to execute
        DeselectAll();

        // Set up stuff that we need
        var pasted = new List<BaseObject>();
        var collections = new Dictionary<ObjectType, BeatmapObjectContainerCollection>();

        // This first loop creates copy of the data to be pasted.
        foreach (var data in newObjects)
        {
            var currentJsonTime = atsc.CurrentJsonTime;
            data.JsonTime = currentJsonTime + data.JsonTime;
            if (data is BaseSlider slider) slider.TailJsonTime = currentJsonTime + slider.TailJsonTime;

            if (!collections.TryGetValue(data.ObjectType, out var collection))
            {
                collection = BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType);
                collections.Add(data.ObjectType, collection);
            }

            pasted.Add(data);
        }

        var totalRemoved = new List<BaseObject>();

        // We remove conflicting objects with our to-be-pasted objects.
        foreach (var (objectType, collection) in collections)
        {
            collection.RemoveConflictingObjects(pasted.Where(x => x.ObjectType == objectType), out var conflicting);
            totalRemoved.AddRange(conflicting);
        }

        // While we're at it, we will also overwrite the entire section if we have to.
        if (overwriteSection)
        {
            var start = pasted.First().SongBpmTime;
            var end = pasted.First().SongBpmTime;
            foreach (var beatmapObject in pasted)
            {
                if (start > beatmapObject.SongBpmTime) start = beatmapObject.SongBpmTime;
                if (end < beatmapObject.SongBpmTime) end = beatmapObject.SongBpmTime;
            }

            var types = GetObjectTypes(pasted);
            var toRemove = new List<(BeatmapObjectContainerCollection, BaseObject)>();
            ForEachObjectBetweenSongBpmTimeByGroup(
                start,
                end,
                types,
                (collection, beatmapObject) =>
                {
                    if (pasted.Contains(beatmapObject)) return;
                    toRemove.Add((collection, beatmapObject));
                });
            foreach (var (collection, beatmapObject) in toRemove)
            {
                collection.DeleteObject(beatmapObject, false, inCollectionOfDeletes: true);
                totalRemoved.Add(beatmapObject);
            }
        }

        // We then spawn our pasted objects into the map and select them.
        foreach (var data in pasted)
        {
            collections[data.ObjectType].SpawnObject(data, false, false, true);
            Select(data, true, false, false);
        }

        RefreshMovedEventsAppearance(SelectedObjects.OfType<BaseEvent>());

        foreach (var collection in collections.Values)
        {
            collection.RefreshPool();

            if (collection is BPMChangeGridContainer con) con.RefreshModifiedBeat();
        }

        if (newObjects.Any(x => x is BaseEvent e && e.IsLaneRotationEvent())) tracksManager.RefreshTracks();
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionPastedAction(pasted, totalRemoved));
        OnSelectionPasted?.Invoke(pasted);
        OnSelectionChanged?.Invoke();

        if (eventPlacement.ObjectContainerCollection.PropagationEditing != EventGridContainer.PropMode.Off)
        {
            eventPlacement.ObjectContainerCollection.PropagationEditing =
                eventPlacement.ObjectContainerCollection.PropagationEditing;
        }

        Debug.Log("Pasted!");
    }

    // not so elegant but this will do for now
    private HashSet<BaseObject> GetNewObjects(HashSet<BaseObject> copiedObjects)
    {
        var selectedType = 0;
        var newObjects = copiedObjects
            .Where(x => x != null)
            .Where(x => (editModeContext.EditingMode & allowedObjectToEdit[x.ObjectType]) > 0)
            .Select(x =>
            {
                selectedType |= (int)x.ObjectType;
                return BeatmapFactory.Clone(x);
            })
            .ToHashSet();

        if ((selectedType & (int)ObjectType.Event) > 0) return TryGetModifiedEventOnLanePaste(newObjects);

        var glsMask = (int)ObjectType.GLSColor
            | (int)ObjectType.GLSRotation
            | (int)ObjectType.GLSTranslation
            | (int)ObjectType.GLSFloatFx;
        if ((selectedType & glsMask) > 0) return TryGetModifiedGLSGroupOnLanePaste(newObjects);

        if ((selectedType & (int)ObjectType.GLSEvent) > 0) return TryGetModifiedGLSEventOnLanePaste(newObjects);

        return newObjects;
    }

    private HashSet<BaseObject> TryGetModifiedEventOnLanePaste(HashSet<BaseObject> newObjects)
    {
        if (eventPlacement.IsIdle || eventPlacement.QueuedData == null) return newObjects;

        var copiedEvents = new HashSet<BaseObject>();

        var expectedType = -1;
        var first = true;
        var isSingleIds = true;
        int[] lightIds = null;
        var minId = int.MaxValue;
        var hasNullId = false;

        var offsetTime = eventPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
        foreach (var obj in newObjects)
        {
            if (obj is not BaseEvent) return newObjects;
            var ev = (BaseEvent)BeatmapFactory.Clone(obj);
            if (first) expectedType = ev.Type;
            if (ev.Type != expectedType) return newObjects;

            ev.Type = eventPlacement.QueuedData.Type;
            ev.JsonTime += offsetTime;

            if (first) lightIds = ev.CustomLightID;

            if (ev.CustomLightID != null)
            {
                minId = Math.Min(ev.CustomLightID.Min(), minId);

                if (!first && (lightIds == null || ev.CustomLightID.Length != lightIds.Length)) isSingleIds = false;

                if (!first
                    && lightIds != null
                    && ev.CustomLightID.Length == lightIds.Length
                    && !lightIds.OrderBy(s => s).SequenceEqual(ev.CustomLightID.OrderBy(s => s)))
                    isSingleIds = false;
            }
            else
                hasNullId = true;

            first = false;
            copiedEvents.Add(ev);
        }

        switch (eventGridContainer.PropagationEditing)
        {
            case EventGridContainer.PropMode.Prop when isSingleIds:
            case EventGridContainer.PropMode.Light when hasNullId && isSingleIds:
                {
                    foreach (var ev in copiedEvents.Cast<BaseEvent>())
                    {
                        ev.Type = eventGridContainer.EventTypeToPropagate;
                        ev.CustomLightID = eventPlacement.QueuedData.CustomLightID;
                    }

                    break;
                }
            case EventGridContainer.PropMode.Light when !hasNullId:
                {
                    foreach (var ev in copiedEvents.Cast<BaseEvent>())
                    {
                        ev.Type = eventGridContainer.EventTypeToPropagate;
                        if (eventPlacement.QueuedData.CustomLightID == null)
                        {
                            ev.CustomLightID = null;
                            continue;
                        }

                        for (var i = 0; i < ev.CustomLightID.Length; i++)
                        {
                            ev.CustomLightID[i] =
                                ev.CustomLightID[i] - minId + eventPlacement.QueuedData.CustomLightID[0];
                        }
                    }

                    break;
                }
            case EventGridContainer.PropMode.Off:
            default:
                break;
        }

        return copiedEvents;
    }

    // it got really ridiculous
    private HashSet<BaseObject> TryGetModifiedGLSGroupOnLanePaste(HashSet<BaseObject> newObjects)
    {
        var groups = newObjects
            .Cast<BaseEventBoxGroup>()
            .Select(x => beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(x.ID).Group)
            .Distinct()
            .ToList();
        if (groups.Count != 1) return new HashSet<BaseObject>();

        var oldIdToOrder = beatmapRuntimeContext
            .TracksDefinition.Gls.Values
            .Where(x => groups[0] == x.Group)
            .Select((x, i) => (x, i))
            .ToDictionary(x => x.x.ID, x => x.i);
        var newIdToOrder = beatmapRuntimeContext
            .TracksDefinition.Gls.Values
            .Where(x => glsGroupGridProvider.CurrentGroup == x.Group)
            .Select((x, i) => (x, i))
            .ToDictionary(x => x.x.ID, x => x.i);
        var newOrderToId = beatmapRuntimeContext
            .TracksDefinition.Gls.Values
            .Where(x => glsGroupGridProvider.CurrentGroup == x.Group)
            .Select((x, i) => (x, i))
            .ToDictionary(x => x.i, x => x.x.ID);

        var minOrder = newObjects.Cast<BaseEventBoxGroup>().Select(x => oldIdToOrder[x.ID]).Min();

        var offsetTime = 0f;
        var offsetOrder = 0;
        if (!glsGroupColorPlacement.IsIdle && glsGroupColorPlacement.QueuedData != null)
        {
            offsetTime = glsGroupColorPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = newIdToOrder[glsGroupColorPlacement.QueuedData.ID] - minOrder;
        }
        else if (!glsGroupRotationPlacement.IsIdle && glsGroupRotationPlacement.QueuedData != null)
        {
            offsetTime = glsGroupRotationPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = newIdToOrder[glsGroupRotationPlacement.QueuedData.ID] - minOrder;
        }
        else if (!glsGroupTranslationPlacement.IsIdle && glsGroupTranslationPlacement.QueuedData != null)
        {
            offsetTime = glsGroupTranslationPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = newIdToOrder[glsGroupTranslationPlacement.QueuedData.ID] - minOrder;
        }
        else if (!glsGroupFloatFXPlacement.IsIdle && glsGroupFloatFXPlacement.QueuedData != null)
        {
            offsetTime = glsGroupFloatFXPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = newIdToOrder[glsGroupFloatFXPlacement.QueuedData.ID] - minOrder;
        }

        var newGls = new HashSet<BaseObject>();
        foreach (var obj in newObjects.Cast<BaseEventBoxGroup>())
        {
            if (!newOrderToId.TryGetValue(oldIdToOrder[obj.ID] + offsetOrder, out var newId)) continue;
            switch (obj)
            {
                case BaseLightColorEventBoxGroup:
                    if (!beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(newId).ColorTrack) continue;
                    break;
                case BaseLightRotationEventBoxGroup:
                    if (!beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(newId).RotationTracks.Any(x => x))
                        continue;
                    break;
                case BaseLightTranslationEventBoxGroup:
                    if (!beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(newId).TranslationTracks.Any(x => x))
                        continue;
                    break;
                case BaseVfxEventEventBoxGroup:
                    if (!beatmapRuntimeContext.TracksDefinition.GetGlsOrDefault(newId).FloatFXTrack) continue;
                    break;
            }

            obj.ID = newId;
            obj.JsonTime += offsetTime;
            newGls.Add(obj);
        }

        return newGls;
    }

    private HashSet<BaseObject> TryGetModifiedGLSEventOnLanePaste(HashSet<BaseObject> newObjects)
    {
        var firstObject = newObjects.First();

        var context = glsEventGridProvider.GroupContext;
        if ((firstObject is BaseLightColorBase && context is not BaseLightColorEventBoxGroup)
            || (firstObject is BaseLightRotationBase && context is not BaseLightRotationEventBoxGroup)
            || (firstObject is BaseLightTranslationBase && context is not BaseLightTranslationEventBoxGroup)
            || (firstObject is BaseFxEventFloat && context is not BaseVfxEventEventBoxGroup))
            return new HashSet<BaseObject>();

        var newGroup = BeatmapFactory.Clone(context);

        var minOrder = newObjects.Cast<BaseGLSEvent>().Select(x => x.BoxIndex).Min();

        var offsetTime = 0f;
        var offsetOrder = 0;
        if (!glsEventColorPlacement.IsIdle && glsEventColorPlacement.QueuedData != null)
        {
            offsetTime = glsEventColorPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = glsEventColorPlacement.QueuedData.BoxIndex - minOrder;
        }
        else if (!glsEventRotationPlacement.IsIdle && glsEventRotationPlacement.QueuedData != null)
        {
            offsetTime = glsEventRotationPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = glsEventRotationPlacement.QueuedData.BoxIndex - minOrder;
        }
        else if (!glsEventTranslationPlacement.IsIdle && glsEventTranslationPlacement.QueuedData != null)
        {
            offsetTime = glsEventTranslationPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = glsEventTranslationPlacement.QueuedData.BoxIndex - minOrder;
        }
        else if (!glsEventFloatFXPlacement.IsIdle && glsEventFloatFXPlacement.QueuedData != null)
        {
            offsetTime = glsEventFloatFXPlacement.QueuedData.JsonTime - atsc.CurrentJsonTime;
            offsetOrder = glsEventFloatFXPlacement.QueuedData.BoxIndex - minOrder;
        }

        // i have never been so disgusted by this
        foreach (var obj in newObjects.Cast<BaseGLSEvent>())
        {
            var boxIndex = obj.BoxIndex + offsetOrder;
            if (boxIndex < 0) continue;
            obj.JsonTime += atsc.CurrentJsonTime + offsetTime;
            if (obj.JsonTime < newGroup.JsonTime) continue;
            obj.RelativeJsonTime = obj.JsonTime - newGroup.JsonTime;
            switch (newGroup)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    if (boxIndex >= lcebg.Boxes.Count) continue;
                    lcebg.Boxes[boxIndex].Events =
                        lcebg
                            .Boxes[boxIndex]
                            .Events.Where(x => x.CompareTo(obj) != 0)
                            .Append(obj as BaseLightColorBase)
                            .OrderBy(x => x.RelativeJsonTime)
                            .ToArray();
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    if (boxIndex >= lrebg.Boxes.Count) continue;
                    lrebg.Boxes[boxIndex].Events =
                        lrebg
                            .Boxes[boxIndex]
                            .Events.Where(x => x.CompareTo(obj) != 0)
                            .Append(obj as BaseLightRotationBase)
                            .OrderBy(x => x.RelativeJsonTime)
                            .ToArray();
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    if (boxIndex >= ltebg.Boxes.Count) continue;
                    ltebg.Boxes[boxIndex].Events =
                        ltebg
                            .Boxes[boxIndex]
                            .Events.Where(x => x.CompareTo(obj) != 0)
                            .Append(obj as BaseLightTranslationBase)
                            .OrderBy(x => x.RelativeJsonTime)
                            .ToArray();
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    if (boxIndex >= ffebg.Boxes.Count) continue;
                    ffebg.Boxes[boxIndex].Events =
                        ffebg
                            .Boxes[boxIndex]
                            .Events.Where(x => x.CompareTo(obj) != 0)
                            .Append(obj as BaseFxEventFloat)
                            .OrderBy(x => x.RelativeJsonTime)
                            .ToArray();
                    break;
            }
        }

        newGroup.JsonTime -= atsc.CurrentJsonTime;

        return new HashSet<BaseObject> { BeatmapFactory.Clone(newGroup) };
    }

    public void MoveSelection(float beats, bool snapObjects = false)
    {
        var originalObjects = new List<BaseObject>();
        var editedObjects = new List<BaseObject>();

        foreach (var original in SelectedObjects)
        {
            var edited = BeatmapFactory.Clone(original);

            edited.JsonTime += beats;
            if (snapObjects)
            {
                edited.JsonTime = Mathf.Round(beats / (1f / atsc.GridMeasureSnapping))
                    * (1f / atsc.GridMeasureSnapping);
            }

            if (edited is BaseSlider slider)
            {
                slider.TailJsonTime += beats;
                if (snapObjects)
                {
                    slider.TailJsonTime = Mathf.Round(beats / (1f / atsc.GridMeasureSnapping))
                        * (1f / atsc.GridMeasureSnapping);
                }
            }

            editedObjects.Add(edited);
            originalObjects.Add(original);
        }

        RefreshMovedEventsAppearance(SelectedObjects.OfType<BaseEvent>());
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedCollectionAction(
                editedObjects,
                originalObjects,
                "Shifted a selection of objects."),
            true);
    }

    public void ShiftSelection(int leftRight, int upDown)
    {
        var editedObjects = SelectedObjects
            .AsParallel()
            .Select(original =>
            {
                var edited = BeatmapFactory.Clone(original);
                if (edited is BaseNote note)
                {
                    if (note.CustomCoordinate != null && note.CustomCoordinate.IsArray)
                        ShiftCustomCoordinates(note, leftRight, upDown);
                    else
                    {
                        var outsideVanillaBounds = false;
                        if (note.PosX >= 1000)
                        {
                            note.PosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (note.PosX < 1000) note.PosX = 1000;
                        }
                        else if (note.PosX <= -1000)
                        {
                            note.PosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (note.PosX > -1000) note.PosX = -1000;
                        }
                        else
                        {
                            note.PosX += leftRight;
                            if (Settings.Instance.VanillaOnlyShift)
                                note.PosX = Mathf.Clamp(note.PosX, 0, 3);
                            else if (note.PosX < 0 || note.PosX > 3) outsideVanillaBounds = true;
                        }

                        note.PosY += upDown;
                        if (Settings.Instance.VanillaOnlyShift)
                            note.PosY = Mathf.Clamp(note.PosY, 0, 2);
                        else if (note.PosY < 0 || note.PosY > 2) outsideVanillaBounds = true;

                        if (outsideVanillaBounds)
                        {
                            note.CustomCoordinate = new Vector2(note.PosX - 2f, note.PosY);
                            note.PosX = note.PosY = 0;
                        }
                    }
                }
                else if (edited is BaseObstacle obstacle)
                {
                    if (obstacle.CustomCoordinate != null && obstacle.CustomCoordinate.IsArray)
                        ShiftCustomCoordinates(obstacle, leftRight, upDown);
                    else
                    {
                        if (obstacle.PosX >= 1000)
                        {
                            obstacle.PosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (obstacle.PosX < 1000) obstacle.PosX = 1000;
                        }
                        else if (obstacle.PosX <= -1000)
                        {
                            obstacle.PosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (obstacle.PosX > -1000) obstacle.PosX = -1000;
                        }
                        else
                            obstacle.PosX += leftRight;
                    }
                }
                else if (edited is BaseEvent e)
                {
                    var events = eventPlacement.ObjectContainerCollection;
                    if (eventPlacement.ObjectContainerCollection.PropagationEditing
                        == EventGridContainer.PropMode.Light)
                    {
                        var max = events.TypeToManager[events.EventTypeToPropagate]
                                .LaneToLightID
                                .Count
                            - 1;

                        var curLane = e.CustomLightID != null
                            ? labels.LightIDToLane(e.Type, e.CustomLightID[0])
                            : -1;
                        var newLane = Math.Min(curLane + leftRight, max);
                        if (newLane < 0)
                            e.CustomLightID = null;
                        else
                        {
                            var newId = labels.LaneToLightID(e.Type, newLane);
                            e.CustomLightID = new[] { newId };
                        }
                    }
                    else if (eventPlacement.ObjectContainerCollection.PropagationEditing
                        == EventGridContainer.PropMode.Prop)
                    {
                        var oldId = (e.CustomLightID != null
                                ? labels.LightIdsToPropId(events.EventTypeToPropagate, e.CustomLightID)
                                : null)
                            ?? -1;
                        var max = events.TypeToManager[events.EventTypeToPropagate]
                            .LaneToLightIDs
                            .Count;
                        var newId = Math.Min(oldId + leftRight, max - 1);

                        if (newId < 0)
                            e.CustomLightID = null;
                        else
                            e.CustomLightID = labels.PropIdToLightIds(events.EventTypeToPropagate, newId);
                    }
                    else
                    {
                        var oldType = e.Type;

                        var modified = labels.EventTypeToLaneId(e.Type);

                        modified += leftRight;

                        if (modified < 0) modified = 0;

                        var laneCount = labels.MaxLaneId();

                        if (modified > laneCount) modified = laneCount;

                        e.Type = labels.LaneIdToEventType(modified);

                        if (e.CustomLightID != null)
                        {
                            var editorID = labels.LightIDToLane(oldType, e.CustomLightID[0]);
                            e.CustomLightID = new[] { labels.LaneToLightID(e.Type, editorID) };
                        }

                        if (e.CustomLightID is { Length: 0 }) e.CustomLightID = null;
                    }

                    if (original.CustomData?.Count <= 0) original.CustomData = null;
                }
                else if (edited is BaseSlider slider)
                {
                    var headOutsideVanillaBounds = false;
                    if (slider.CustomCoordinate != null && slider.CustomCoordinate.IsArray)
                        ShiftCustomCoordinates(slider, leftRight, upDown);
                    else
                    {
                        if (slider.PosX >= 1000)
                        {
                            slider.PosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (slider.PosX < 1000) slider.PosX = 1000;
                        }
                        else if (slider.PosX <= -1000)
                        {
                            slider.PosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (slider.PosX > -1000) slider.PosX = -1000;
                        }
                        else
                        {
                            slider.PosX += leftRight;
                            if (Settings.Instance.VanillaOnlyShift)
                                slider.PosX = Mathf.Clamp(slider.PosX, 0, 3);
                            else if (slider.PosY < 0 || slider.PosY > 2) headOutsideVanillaBounds = true;
                        }

                        slider.PosY += upDown;
                        if (Settings.Instance.VanillaOnlyShift)
                            slider.PosY = Mathf.Clamp(slider.PosY, 0, 2);
                        else if (slider.PosY < 0 || slider.PosY > 2) headOutsideVanillaBounds = true;

                        if (headOutsideVanillaBounds)
                        {
                            slider.CustomCoordinate = new Vector2(slider.PosX + 1f, slider.PosY);
                            slider.PosX = slider.PosY = 0;
                        }
                    }

                    var tailOutsideVanillaBounds = false;
                    if (slider.CustomTailCoordinate != null && slider.CustomTailCoordinate.IsArray)
                        ShiftCustomTailCoordinates(slider, leftRight, upDown);
                    else
                    {
                        if (slider.TailPosX >= 1000)
                        {
                            slider.TailPosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (slider.TailPosX < 1000) slider.TailPosX = 1000;
                        }
                        else if (slider.TailPosX <= -1000)
                        {
                            slider.TailPosX += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                            if (slider.TailPosX > -1000) slider.TailPosX = -1000;
                        }
                        else
                        {
                            slider.TailPosX += leftRight;
                            if (Settings.Instance.VanillaOnlyShift)
                                slider.TailPosX = Mathf.Clamp(slider.TailPosX, 0, 3);
                        }

                        slider.TailPosY += upDown;
                        if (Settings.Instance.VanillaOnlyShift)
                            slider.TailPosY = Mathf.Clamp(slider.TailPosY, 0, 2);
                        else if (slider.PosY < 0 || slider.PosY > 2) tailOutsideVanillaBounds = true;

                        if (tailOutsideVanillaBounds)
                        {
                            slider.CustomTailCoordinate = new Vector2(slider.TailPosX + 1f, slider.TailPosY);
                            slider.TailPosX = slider.TailPosY = 0;
                        }
                    }
                }

                edited.SaveCustom();

                return edited;
            })
            .ToList();

        var originalObjects = SelectedObjects.ToList();

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedCollectionAction(
                editedObjects,
                originalObjects,
                "Shifted a selection of objects."),
            true);
        tracksManager.RefreshTracks();
    }

    private void ShiftCustomCoordinates(BaseGrid gridObject, int leftRight, int upDown)
    {
        var position = new Vector2(gridObject.PosX - 2f, gridObject.PosY);
        if (gridObject.CustomCoordinate[0].IsNumber) position.x = gridObject.CustomCoordinate[0];
        if (gridObject.CustomCoordinate[1].IsNumber) position.y = gridObject.CustomCoordinate[1];

        gridObject.CustomCoordinate = new Vector2(
            position.x + (1f / atsc.GridMeasureSnapping * leftRight),
            position.y + (1f / atsc.GridMeasureSnapping * upDown));
    }

    private void ShiftCustomTailCoordinates(BaseSlider slider, int leftRight, int upDown)
    {
        var tailPosition = new Vector2(slider.TailPosX - 2f, slider.TailPosY);
        if (slider.CustomTailCoordinate[0].IsNumber) tailPosition.x = slider.CustomTailCoordinate[0];
        if (slider.CustomTailCoordinate[1].IsNumber) tailPosition.y = slider.CustomTailCoordinate[1];

        slider.CustomTailCoordinate = new Vector2(
            tailPosition.x + (1f / atsc.GridMeasureSnapping * leftRight),
            tailPosition.y + (1f / atsc.GridMeasureSnapping * upDown));
    }

    #endregion
}
