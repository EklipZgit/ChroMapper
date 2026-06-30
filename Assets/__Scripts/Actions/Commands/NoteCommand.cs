using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class NoteCommand
{
    public static void SetCutDirection(BaseNote baseNote, int cutDirection)
    {
        var newNote = BeatmapFactory.Clone(baseNote);
        ToggleDiagonalAngleOffset(newNote, cutDirection);
        newNote.CutDirection = cutDirection;

        var actions = new List<BeatmapAction>
        {
            new BeatmapObjectUpdatedAction(
                newNote,
                baseNote,
                "Update Note Direction",
                true,
                ActionMergeType.NoteDirectionChange)
        };
        UpdateAttachedSlidersDirection(newNote, actions);

        if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(
                    actions,
                    true,
                    false,
                    "Update Note Direction",
                    ActionMergeType.NoteDirectionChange),
                true);
            SelectionController.OnSelectionChanged?.Invoke();
        }
        else
            BeatmapActionContainer.AddAction(actions[0], true);
    }
    
    private static void ToggleDiagonalAngleOffset(BaseNote note, int newCutDirection)
    {
        if (note.CutDirection == (int)NoteCutDirection.Any
            && newCutDirection == (int)NoteCutDirection.Any
            && note.AngleOffset != 45)
            note.AngleOffset = 45;
        else
            note.AngleOffset = 0;
    }
    
    private static void UpdateAttachedSlidersDirection(BaseNote noteData, ICollection<BeatmapAction> actions)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var originalArc = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(originalArc.JsonTime - noteData.JsonTime) < epsilon
                && originalArc.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(originalArc.TailJsonTime - noteData.JsonTime) < epsilon
                && originalArc.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var newArc = BeatmapFactory.Clone(originalArc);
                newArc.CutDirection = noteData.CutDirection;

                actions.Add(
                    new BeatmapObjectUpdatedAction(
                        newArc,
                        originalArc,
                        keepSelection: true,
                        mergeType: ActionMergeType.NoteDirectionChange));
            }
            else if (isConnectedToTail)
            {
                var newArc = BeatmapFactory.Clone(originalArc);
                newArc.TailCutDirection = noteData.CutDirection;

                actions.Add(
                    new BeatmapObjectUpdatedAction(
                        newArc,
                        originalArc,
                        keepSelection: true,
                        mergeType: ActionMergeType.NoteDirectionChange));
            }
        }

        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var originalChain = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(originalChain.JsonTime - noteData.JsonTime) < epsilon
                && originalChain.GetPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var newChain = BeatmapFactory.Clone(originalChain);
                newChain.CutDirection = noteData.CutDirection;

                actions.Add(
                    new BeatmapObjectUpdatedAction(
                        newChain,
                        originalChain,
                        keepSelection: true,
                        mergeType: ActionMergeType.NoteDirectionChange));
            }
        }
    }

    public static void SetAngleOffset(BaseNote baseNote, int angleOffset)
    {
        var newNote = BeatmapFactory.Clone(baseNote);
        newNote.AngleOffset = angleOffset;
        
        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newNote,
                baseNote,
                "Update Note Precise Direction",
                mergeType: ActionMergeType.NotePreciseDirectionTweak),
            true);
        SelectionController.OnSelectionChanged?.Invoke();
    }
    
    public static void InvertColor(BaseNote baseNote)
    {
        if (baseNote.Type == (int)NoteType.Bomb) return;

        var newNote = BeatmapFactory.Clone(baseNote);
        var newType = baseNote.Type == (int)NoteType.Red
            ? (int)NoteType.Blue
            : (int)NoteType.Red;
        newNote.Type = newType;

        var actions = new List<BeatmapAction> { new BeatmapObjectUpdatedAction(newNote, baseNote) };

        InvertAttachedSliders(newNote, actions);
        
        if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(
                    actions,
                    true,
                    false,
                    "Invert note"),
                true);
            SelectionController.OnSelectionChanged?.Invoke();
        }
        else
            BeatmapActionContainer.AddAction(actions[0], true);
    }

    private static void InvertAttachedSliders(BaseNote noteData, ICollection<BeatmapAction> actions)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;

        var arcCollection = BeatmapObjectContainerCollection.GetCollectionForType<ArcGridContainer>(ObjectType.Arc);
        foreach (var arcContainer in arcCollection.LoadedContainers)
        {
            var originalArc = arcContainer.Key as BaseArc;
            var isConnectedToHead = Mathf.Abs(originalArc.JsonTime - noteData.JsonTime) < epsilon
                && originalArc.GetPosition() == noteData.GetPosition();
            var isConnectedToTail = Mathf.Abs(originalArc.TailJsonTime - noteData.JsonTime) < epsilon
                && originalArc.GetTailPosition() == noteData.GetPosition();
            if (isConnectedToHead || isConnectedToTail)
            {
                var newArc = BeatmapFactory.Clone(originalArc);
                newArc.Color = noteData.Color;

                actions.Add(new BeatmapObjectUpdatedAction(newArc, originalArc));
            }
        }

        var chainCollection =
            BeatmapObjectContainerCollection.GetCollectionForType<ChainGridContainer>(ObjectType.Chain);
        foreach (var chainContainer in chainCollection.LoadedContainers)
        {
            var originalChain = chainContainer.Key as BaseChain;
            var isConnectedToHead = Mathf.Abs(originalChain.JsonTime - noteData.JsonTime) < epsilon
                && originalChain.GetPosition() == noteData.GetPosition();
            if (isConnectedToHead)
            {
                var newChain = BeatmapFactory.Clone(originalChain);
                newChain.Color = noteData.Color;

                actions.Add(new BeatmapObjectUpdatedAction(newChain, originalChain));
            }
        }
    }
}

