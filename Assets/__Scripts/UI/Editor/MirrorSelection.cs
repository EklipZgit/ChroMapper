using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;

public class MirrorSelection : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CreateEventTypeLabels labels;

    private readonly Dictionary<int, int> cutDirectionToMirrored = new()
    {
        { (int)NoteCutDirection.DownLeft, (int)NoteCutDirection.DownRight },
        { (int)NoteCutDirection.DownRight, (int)NoteCutDirection.DownLeft },
        { (int)NoteCutDirection.UpLeft, (int)NoteCutDirection.UpRight },
        { (int)NoteCutDirection.UpRight, (int)NoteCutDirection.UpLeft },
        { (int)NoteCutDirection.Right, (int)NoteCutDirection.Left },
        { (int)NoteCutDirection.Left, (int)NoteCutDirection.Right }
    };

    // Mirror the lane filter in place so existing GLS event references remain valid.
    private void MirrorEventBoxGroupPositions(BaseLightColorEventBoxGroup group) => MirrorEventBoxGroupPositions(group.ReadOnlyBoxes);

    // Mirror the lane filter in place so existing GLS event references remain valid.
    private void MirrorEventBoxGroupPositions(BaseLightRotationEventBoxGroup group) => MirrorEventBoxGroupPositions(group.ReadOnlyBoxes);

    // Mirror the lane filter in place so existing GLS event references remain valid.
    private void MirrorEventBoxGroupPositions(BaseLightTranslationEventBoxGroup group) => MirrorEventBoxGroupPositions(group.ReadOnlyBoxes);

    // Mirror the lane filter in place so existing GLS event references remain valid.
    private void MirrorEventBoxGroupPositions(BaseVfxEventEventBoxGroup group) => MirrorEventBoxGroupPositions(group.ReadOnlyBoxes);

    // Change filters instead of swapping boxes because events retain their BoxIndex and EventBoxData references.
    private void MirrorEventBoxGroupPositions(IReadOnlyList<BaseEventBox> boxes)
    {
        if (boxes.Count <= 1) return;

        int laneCount = boxes.Count;
        foreach (var box in boxes)
        {
            MirrorIndexFilter(box.IndexFilter, laneCount);
        }
    }

    private void MirrorIndexFilter(BaseIndexFilter filter, int totalLanes = 10)
    {
        if (filter == null) return;

        // Mirror the ID based on the filter type
        // For Division: Param1 is the ID (0-indexed)
        // For StepAndOffset: Param0 is the ID (0-indexed)
        int id;
        if (filter.Type == (int)IndexFilterType.Division)
        {
            id = filter.Param1;
            // Mirror the ID
            int mirroredId = (int)Mathf.Repeat(-id - 1, totalLanes);
            filter.Param1 = mirroredId;
        }
        else if (filter.Type == (int)IndexFilterType.StepAndOffset)
        {
            id = filter.Param0;
            // Mirror the ID
            int mirroredId = (int)Mathf.Repeat(-id - 1, totalLanes);
            filter.Param0 = mirroredId;
        }
    }

    public void MirrorTime()
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        var ordered = SelectionController.SelectedObjects.OrderByDescending(x => x.JsonTime);
        var orderedSliders = ordered.Where(x => x is BaseSlider);
        var maxTailJsonTime = orderedSliders.Any()
            ? orderedSliders.Max(x => (x as BaseSlider).TailJsonTime)
            : float.MinValue;

        var end = Mathf.Max(ordered.First().JsonTime, maxTailJsonTime);
        var start = ordered.Last().JsonTime;
        var allActions = new List<BeatmapAction>();
        foreach (var con in SelectionController.SelectedObjects)
        {
            var edited = BeatmapFactory.Clone(con);
            edited.JsonTime = start + (end - con.JsonTime);

            if (edited is BaseSlider edittedSlider && con is BaseSlider slider)
            {
                edittedSlider.TailJsonTime = start + (end - slider.TailJsonTime);
                edittedSlider.SwapHeadAndTail();
            }

            allActions.Add(new BeatmapObjectModifiedAction(edited, con, con, "e", true));
        }

        var actionCollection =
            new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects in time.");
        BeatmapActionContainer.AddAction(actionCollection, true);
    }

    // Rebuild each affected GLS group once so replay does not spawn individual nodes through ReplaceGroup.
    private List<BeatmapAction> CreateMirroredGlsActions(
        bool moveNotes,
        List<BaseGLSEvent> mirroredSelectedGlsEvents)
    {
        var actions = new List<BeatmapAction>();
        foreach (var grouping in SelectionController.SelectedObjects.OfType<BaseGLSEvent>().GroupBy(evt => evt.EventBoxGroupData))
        {
            var originalGroup = grouping.Key;
            var editedGroup = BeatmapFactory.Clone(originalGroup);
            var editedEventsByBox = editedGroup.ReadOnlyBoxes
                .Select(box => box.ReadOnlyEvents.ToList())
                .ToList();
            int laneCount = editedEventsByBox.Count;

            var selectedEvents = grouping
                .Select(originalEvent =>
                {
                    int sourceIndex = originalEvent.BoxIndex;
                    int eventIndex = sourceIndex >= 0 && sourceIndex < laneCount
                        ? originalEvent.EventBoxData.ReadOnlyEvents.ToList().IndexOf(originalEvent)
                        : -1;
                    var editedEvent = eventIndex >= 0 && eventIndex < editedEventsByBox[sourceIndex].Count
                        ? editedEventsByBox[sourceIndex][eventIndex]
                        : null;
                    return (originalEvent, sourceIndex, editedEvent);
                })
                .Where(item => item.editedEvent != null)
                .ToList();

            foreach (var (_, sourceIndex, editedEvent) in selectedEvents)
            {
                editedEventsByBox[sourceIndex].Remove(editedEvent);
                int destinationIndex = moveNotes ? laneCount - 1 - sourceIndex : sourceIndex;
                editedEventsByBox[destinationIndex].Add(editedEvent);

                if (editedEvent is BaseLightColorBase colorEvent)
                {
                    colorEvent.Color = (colorEvent.Color + 1) % 3;
                }

                if (editedEvent is BaseLightRotationBase rotationEvent)
                {
                    // Physical lane mirroring already supplies the reflection; do not invert GLS rotation as well.
                    if (!moveNotes) rotationEvent.Rotation *= -1f;
                }

                // Verify every GLS payload survives the clone/lane rebuild before serialization.
                Debug.Log($"[MirrorSelection] GLS mirror payload type={editedEvent.GetType().Name} json={editedEvent.ToJson()}");

                mirroredSelectedGlsEvents.Add(editedEvent);
            }

            for (var boxIndex = 0; boxIndex < editedGroup.ReadOnlyBoxes.Count; boxIndex++)
            {
                var box = editedGroup.ReadOnlyBoxes[boxIndex];
                box.SetEvents(editedEventsByBox[boxIndex].ToArray());
                foreach (var evt in box.ReadOnlyEvents)
                {
                    evt.EventBoxData = box;
                    evt.EventBoxGroupData = editedGroup;
                    evt.BoxIndex = boxIndex;
                    evt.JsonTime = editedGroup.JsonTime + evt.RelativeJsonTime;
                }
            }

            editedGroup.SaveCustom();
            Debug.Log($"[MirrorSelection] GLS mirrored group serialized json={editedGroup.ToJson()}");
            actions.Add(new BeatmapGLSEventBoxModifiedAction(
                editedGroup,
                originalGroup,
                "Mirrored GLS events."));
        }

        return actions;
    }

    public void Mirror(bool moveNotes = true)
    {
        Debug.Log($"[MirrorSelection] Mirror invoked moveNotes={moveNotes} selected={SelectionController.SelectedObjects.Count}");
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        var mirroredSelectedGlsEvents = new List<BaseGLSEvent>();
        var glsActions = CreateMirroredGlsActions(moveNotes, mirroredSelectedGlsEvents);
        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
        var originalObjects = new List<BaseObject>();
        var editedObjects = new List<BaseObject>();
        foreach (var original in SelectionController.SelectedObjects.Where(obj => obj is not BaseGLSEvent))
        {
            var edited = BeatmapFactory.Clone(original);
            Debug.Log($"[MirrorSelection] Processing {original.GetType().Name} time={original.JsonTime} edited={edited?.GetType().Name}");
            if (edited is BaseObstacle obstacle && moveNotes)
            {
                var precisionWidth = obstacle.Width >= 1000;
                var state = obstacle.PosX;

                if (obstacle.CustomCoordinate != null && obstacle.CustomCoordinate.IsArray)
                {
                    var oldPosition = obstacle.CustomCoordinate.ReadVector2();

                    var flipped = new Vector2(oldPosition.x * -1, oldPosition.y);

                    var customSize = obstacle.CustomSize;
                    if (customSize != null && customSize.IsArray && customSize[0].IsNumber)
                    {
                        flipped.x -= customSize[0].AsFloat;
                    }
                    else
                    {
                        flipped.x -= obstacle.Width;
                    }

                    obstacle.CustomCoordinate = flipped;
                }

                if (obstacle.CustomLocalRotation != null)
                {
                    if (obstacle.CustomLocalRotation.IsNumber)
                    {
                        obstacle.CustomLocalRotation = -obstacle.CustomLocalRotation.AsFloat;
                    }
                    else if (obstacle.CustomLocalRotation is JSONArray rot)
                    {
                        if (rot.Count > 1)
                        {
                            rot[1] = -rot[1].AsFloat;
                        }

                        if (rot.Count > 2)
                        {
                            rot[2] = -rot[2].AsFloat;
                        }
                    }
                }

                if (obstacle.CustomWorldRotation != null)
                {
                    if (obstacle.CustomWorldRotation.IsNumber)
                    {
                        obstacle.CustomWorldRotation = -obstacle.CustomWorldRotation.AsFloat;
                    }
                    else if (obstacle.CustomWorldRotation is JSONArray rot)
                    {
                        if (rot.Count > 1)
                        {
                            rot[1] = -rot[1].AsFloat;
                        }

                        if (rot.Count > 2)
                        {
                            rot[2] = -rot[2].AsFloat;
                        }
                    }
                }

                if (state >= 1000 || state <= -1000 || precisionWidth) // precision lineIndex
                {
                    var newIndex = state;
                    if (newIndex <= -1000) // normalize index values, we'll fix them later
                        newIndex += 1000;
                    else if (newIndex >= 1000)
                        newIndex -= 1000;
                    else
                        newIndex *= 1000; //convert lineIndex to precision if not already
                    newIndex = ((newIndex - 2000) * -1) + 2000; //flip lineIndex

                    var newWidth = obstacle.Width; //normalize wall width
                    if (newWidth < 1000)
                        newWidth *= 1000;
                    else
                        newWidth -= 1000;
                    newIndex -= newWidth;

                    if (newIndex < 0)
                        //this is where we fix them
                        newIndex -= 1000;
                    else
                        newIndex += 1000;
                    obstacle.PosX = newIndex;
                }
                else // state > -1000 || state < 1000 assumes no precision width
                {
                    var mirrorLane = ((state - 2) * -1) + 2; //flip lineIndex
                    obstacle.PosX = mirrorLane - obstacle.Width; //adjust for wall width
                }
            }
            else if (edited is BaseNote note)
            {
                if (moveNotes)
                {
                    note.AngleOffset *= -1;

                    // NE Precision rotation
                    if (note.CustomCoordinate != null && note.CustomCoordinate.IsArray)
                    {
                        var oldPosition = note.CustomCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        note.CustomCoordinate = flipped;
                    }

                    // NE precision cut direction
                    if (note.CustomDirection != null)
                    {
                        var cutDirection = note.CustomDirection;
                        note.CustomDirection = cutDirection * -1;
                    }

                    if (note.CustomLocalRotation != null)
                    {
                        if (note.CustomLocalRotation.IsNumber)
                        {
                            note.CustomLocalRotation = -note.CustomLocalRotation.AsFloat;
                        }
                        else if (note.CustomLocalRotation is JSONArray rot)
                        {
                            if (rot.Count > 1)
                            {
                                rot[1] = -rot[1].AsFloat;
                            }

                            if (rot.Count > 2)
                            {
                                rot[2] = -rot[2].AsFloat;
                            }
                        }
                    }

                    if (note.CustomWorldRotation != null)
                    {
                        if (note.CustomWorldRotation.IsNumber)
                        {
                            note.CustomWorldRotation = -note.CustomWorldRotation.AsFloat;
                        }
                        else if (note.CustomWorldRotation is JSONArray rot)
                        {
                            if (rot.Count > 1)
                            {
                                rot[1] = -rot[1].AsFloat;
                            }

                            if (rot.Count > 2)
                            {
                                rot[2] = -rot[2].AsFloat;
                            }
                        }
                    }

                    var state = note.PosX; // flip line index
                    if (state > 3 || state < 0) // precision case
                    {
                        var newIndex = state;
                        if (newIndex <= -1000) // normalize index values, we'll fix them later
                            newIndex += 1000;
                        else if (newIndex >= 1000) newIndex -= 1000;

                        newIndex = ((newIndex - 1500) * -1) + 1500; //flip lineIndex

                        if (newIndex < 0) //this is where we fix them
                            newIndex -= 1000;
                        else
                            newIndex += 1000;

                        note.PosX = newIndex;
                    }
                    else
                    {
                        var mirrorLane = (int)(((state - 1.5f) * -1) + 1.5f);
                        note.PosX = mirrorLane;
                    }
                }

                //flip colors
                if (note.Type != (int)NoteType.Bomb)
                {
                    note.Type = note.Type == (int)NoteType.Red
                        ? (int)NoteType.Blue
                        : (int)NoteType.Red;

                    //flip cut direction horizontally
                    if (moveNotes && cutDirectionToMirrored.ContainsKey(note.CutDirection))
                        note.CutDirection = cutDirectionToMirrored[note.CutDirection];
                }
            }
            else if (edited is BaseEvent e)
            {
                var mirroredPhysically = false;
                Debug.Log($"[MirrorSelection] Basic event before type={e.Type} value={e.Value} lightId={string.Join(",", e.CustomLightID ?? System.Array.Empty<int>())} propMode={events.PropagationEditing} targetType={events.EventTypeToPropagate}");
                // Ring rotation and zoom use value inversion only when no physical lane mirror is requested.
                // Read current environment metadata directly so mirroring cannot retain stale track capabilities.
                var components = beatmapRuntimeContext.TracksDefinition.GetBasicOrDefault(e.Type).Components;
                var isRingRotation = components.HasFlag(BasicEventComponent.RingRotation);
                // SmoothStepRingZoom only applies to The Second's legacy ring right now.
                var isRingZoom = components.HasFlag(BasicEventComponent.RingZoom)
                    || components.HasFlag(BasicEventComponent.SmoothStepRingZoom);
                if (isRingRotation || isRingZoom)
                {
                    if (!moveNotes)
                    {
                        if (isRingRotation && e.CustomRingRotation.HasValue)
                            e.CustomRingRotation = -e.CustomRingRotation.Value;
                        else if (isRingZoom && e.CustomStep.HasValue)
                            e.CustomStep = -e.CustomStep.Value;
                    }

                    continue;
                }

                // In the normal basic-event view, mirror the event's visible lane by changing its event type.
                if (moveNotes && events.PropagationEditing == EventGridContainer.PropMode.Off)
                {
                    e.Type = labels.MirroredEventType(e);
                    mirroredPhysically = true;
                    Debug.Log($"[MirrorSelection] Basic event visible-lane mirror result type={e.Type}");
                }

                if (beatmapRuntimeContext.TracksDefinition.GetBasicOrDefault(e.Type).Kind != BasicEventKind.Lights)
                {
                    Debug.Log($"[MirrorSelection] Basic event skipped: type={e.Type} is not a light track");
                    continue;
                }
                if (moveNotes
                    && e.IsPropagation
                    && e.CustomLightID != null
                    && events.EventTypeToPropagate == e.Type
                    && events.PropagationEditing == EventGridContainer.PropMode.Prop)
                {
                    var idx = labels.LightIDsToPropID(e.Type, e.CustomLightID);
                    var mirroredIdx = (int)Mathf.Repeat(-idx - 1, events.EventTypePropagationSize);
                    e.CustomLightID = labels.PropIdToLightIds(e.Type, mirroredIdx);
                }
                // Physical mirroring changes lane/type or light ID only; color/value mirroring is separate.
                if (moveNotes && e.CustomLightID != null && events.PropagationEditing == EventGridContainer.PropMode.Light)
                {
                    var idx = labels.LightIDsToVisibleLane(e.Type, e.CustomLightID);
                    Debug.Log($"[MirrorSelection] Physical light-ID mirror lookup type={e.Type} ids={string.Join(",", e.CustomLightID)} visibleLane={idx} laneCount={events.EventTypePropagationSize}");
                    if (idx >= 0)
                    {
                        var mirroredIdx = (int)Mathf.Repeat(-idx - 1, events.EventTypePropagationSize);
                        // Resolve the target by the displayed lane mapping; LaneToLightID is not the inverse
                        // of LightIDToLane for environments with hidden/non-contiguous IDs.
                        var mirroredId = Enumerable.Range(0, events.EventTypePropagationSize)
                            .FirstOrDefault(id => labels.LightIDToLane(e.Type, id) == mirroredIdx);
                        e.CustomLightID = new[] { mirroredId };
                        mirroredPhysically = true;
                        Debug.Log($"[MirrorSelection] Physical light-ID mirror result visibleLane={idx} mirroredLane={mirroredIdx} id={e.CustomLightID[0]}");
                    }
                }
                else if (!moveNotes)
                {
                    if (e.CustomLightGradient != null)
                        (e.CustomLightGradient.StartColor, e.CustomLightGradient.EndColor) =
                            (e.CustomLightGradient.EndColor, e.CustomLightGradient.StartColor);
                    if (e.Value > 0 && e.Value <= 4) e.Value += 4;
                    else if (e.Value > 4 && e.Value <= 8) e.Value += 4;
                    else if (e.Value > 8 && e.Value <= 12) e.Value -= 8;
                }

                Debug.Log($"[MirrorSelection] Basic event after type={e.Type} value={e.Value} lightId={string.Join(",", e.CustomLightID ?? System.Array.Empty<int>())} physical={mirroredPhysically}");
            }
            else if (edited is BaseRotationEvent r)
            {
                r.Rotation *= -1;
                tracksManager.RefreshTracks();
            }
            else if (edited is BaseArc arc)
            {
                if (moveNotes)
                {
                    if (arc.CustomCoordinate != null && arc.CustomCoordinate.IsArray)
                    {
                        var oldPosition = arc.CustomCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        arc.CustomCoordinate = flipped;
                    }

                    if (arc.CustomTailCoordinate != null && arc.CustomTailCoordinate.IsArray)
                    {
                        var oldPosition = arc.CustomTailCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        arc.CustomTailCoordinate = flipped;
                    }

                    arc.PosX = Mathf.RoundToInt(((arc.PosX - 1.5f) * -1) + 1.5f);
                    if (cutDirectionToMirrored.ContainsKey(arc.CutDirection))
                        arc.CutDirection = cutDirectionToMirrored[arc.CutDirection];

                    arc.TailPosX = Mathf.RoundToInt(((arc.TailPosX - 1.5f) * -1) + 1.5f);
                    if (cutDirectionToMirrored.ContainsKey(arc.TailCutDirection))
                        arc.TailCutDirection = cutDirectionToMirrored[arc.TailCutDirection];

                    if (arc.MidAnchorMode > 0 && arc.MidAnchorMode < 3)
                    {
                        arc.MidAnchorMode = arc.MidAnchorMode == (int)SliderMidAnchorMode.Clockwise
                            ? (int)SliderMidAnchorMode.CounterClockwise
                            : (int)SliderMidAnchorMode.Clockwise;
                    }
                }

                arc.Color = arc.Color == (int)NoteType.Red
                    ? (int)NoteType.Blue
                    : (int)NoteType.Red;
            }
            else if (edited is BaseChain chain)
            {
                if (moveNotes)
                {
                    // NE Precision rotation
                    if (chain.CustomCoordinate != null && chain.CustomCoordinate.IsArray)
                    {
                        var oldPosition = chain.CustomCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        chain.CustomCoordinate = flipped;
                    }

                    if (chain.CustomTailCoordinate != null && chain.CustomTailCoordinate.IsArray)
                    {
                        var oldPosition = chain.CustomTailCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        chain.CustomTailCoordinate = flipped;
                    }

                    chain.PosX = Mathf.RoundToInt(((chain.PosX - 1.5f) * -1) + 1.5f);
                    if (cutDirectionToMirrored.ContainsKey(chain.CutDirection))
                        chain.CutDirection = cutDirectionToMirrored[chain.CutDirection];

                    chain.TailPosX = Mathf.RoundToInt(((chain.TailPosX - 1.5f) * -1) + 1.5f);
                }

                chain.Color = chain.Color == (int)NoteType.Red
                    ? (int)NoteType.Blue
                    : (int)NoteType.Red;
            }
            // Mirror selected GLS inner nodes by changing their lane index instead of moving box objects.
            else if (edited is BaseGLSEvent glsEvent && original is BaseGLSEvent originalGlsEvent && moveNotes)
            {
                int laneCount = originalGlsEvent.EventBoxGroupData?.ReadOnlyBoxes.Count ?? 0;
                if (laneCount > 1 && originalGlsEvent.BoxIndex >= 0 && originalGlsEvent.BoxIndex < laneCount)
                {
                    glsEvent.BoxIndex = laneCount - 1 - originalGlsEvent.BoxIndex;
                }
            }
            else if (edited is BaseLightColorEventBoxGroup lcebg)
            {
                // Mirror the box positions within the group (swap lane indices)
                MirrorEventBoxGroupPositions(lcebg);
                // Cycle colors (red/blue/white)
                foreach (var evt in lcebg.Boxes.SelectMany(box => box.Events)) evt.Color = (evt.Color + 1) % 3;
            }
            else if (edited is BaseLightRotationEventBoxGroup lrebg)
            {
                // Mirror the box positions within the group and invert every rotation node for horizontal mirroring.
                MirrorEventBoxGroupPositions(lrebg);
                foreach (var evt in lrebg.Boxes.SelectMany(box => box.Events))
                {
                    evt.Rotation *= -1f;
                }
            }
            else if (edited is BaseLightTranslationEventBoxGroup ltebg)
            {
                // Mirror the box positions within the group (swap lane indices)
                MirrorEventBoxGroupPositions(ltebg);
            }
            else if (edited is BaseVfxEventEventBoxGroup ffebg)
            {
                // Mirror the box positions within the group (swap lane indices)
                MirrorEventBoxGroupPositions(ffebg);
            }

            edited.SaveCustom();

            editedObjects.Add(edited);
            originalObjects.Add(original);
        }

        // Keep GLS group actions separate from ordinary object collection replacement to avoid nested GLS spawns.
        var actions = new List<BeatmapAction>(glsActions);
        if (editedObjects.Count > 0)
        {
            actions.Add(new BeatmapObjectModifiedCollectionAction(
                editedObjects,
                originalObjects,
                "Mirrored a selection of objects."));
        }

        if (actions.Count > 0)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(actions, true, true, "Mirrored a selection of objects."),
                true);
        }

        // Group replacement clears selection; restore mirrored GLS nodes without adding selection history entries.
        foreach (var mirroredSelectedGlsEvent in mirroredSelectedGlsEvents)
        {
            SelectionController.Select(mirroredSelectedGlsEvent, true, false, false);
        }

        if (mirroredSelectedGlsEvents.Count > 0)
        {
            SelectionController.OnSelectionChanged?.Invoke();
        }
    }
}
