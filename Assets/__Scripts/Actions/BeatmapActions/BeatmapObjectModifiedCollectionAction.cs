using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Beatmap.Base;
using NetDataReader = LiteNetLib.Utils.NetDataReader;

/*
 * Alternative to ActionCollectionAction that removes all objects and then re adds them on undo/redo.
 * No modifying in-place should ensure map objects don't run into weird stacked/ghost issues. 
 */
public class BeatmapObjectModifiedCollectionAction : BeatmapAction
{
    public List<BaseObject> EditedObjects;
    public List<BaseObject> OriginalObjects;
    
    private readonly float firstBpmEventJsonTime;

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedCollectionAction() : base() { }

    public BeatmapObjectModifiedCollectionAction(List<BaseObject> editedObjects, List<BaseObject> originalObjects,
        string comment = "No comment.") : base(editedObjects.Concat(originalObjects), comment)
    {
        this.EditedObjects = editedObjects;
        this.OriginalObjects = originalObjects;

        firstBpmEventJsonTime = Data.OfType<BaseBpmEvent>().DefaultIfEmpty().Min(x => x?.JsonTime ?? -1f);
    }

    public override BaseObject DoesInvolveObject(BaseObject obj)
    {
        var involvedObject = EditedObjects.Find(x => x == obj);
        involvedObject ??= OriginalObjects.Find(x => x == obj);
        
        return involvedObject;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var glsEventCollection = BeginGlsEventReplacementBatch();
        foreach (var obj in EditedObjects)
        {
            DeleteObject(obj, false, obj is not BaseGLSEvent);
        }

        foreach (var obj in OriginalObjects)
        {
            SpawnObject(obj, false, false);
            
            if (!Networked)
            {
                SelectionController.Select(obj, true, true, false);
            }
        }

        if (firstBpmEventJsonTime >= 0)
        {
            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(firstBpmEventJsonTime);
        }
        
        RefreshPools(Data);
        RefreshEventAppearance();
        EndGlsEventReplacementBatch(
            glsEventCollection,
            OriginalObjects.OfType<BaseGLSEvent>(),
            "Restored GLS event collection.");
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var glsEventCollection = BeginGlsEventReplacementBatch();
        foreach (var obj in OriginalObjects)
        {
            DeleteObject(obj, false, obj is not BaseGLSEvent);
        }

        foreach (var obj in EditedObjects)
        {
            SpawnObject(obj, false, false);
            
            if (!Networked)
            {
                SelectionController.Select(obj, true, true, false);
            }
        }

        if (firstBpmEventJsonTime >= 0)
        {
            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(firstBpmEventJsonTime);
        }
        
        RefreshPools(Data);
        RefreshEventAppearance();
        EndGlsEventReplacementBatch(
            glsEventCollection,
            EditedObjects.OfType<BaseGLSEvent>(),
            "Modified GLS event collection.");
    }

    private GLSEventGridContainer BeginGlsEventReplacementBatch()
    {
        // GLS child events share a cache entry through their parent group, so delay that group's replacement.
        if (!EditedObjects.Any(obj => obj is BaseGLSEvent)) return null;
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<GLSEventGridContainer>(
            Beatmap.Enums.ObjectType.GLSEvent);
        // Unity collections need their overloaded null comparison before batch replacement starts.
        if (collection != null)
        {
            collection.BeginGroupReplacementBatch();
        }
        return collection;
    }

    private void EndGlsEventReplacementBatch(
        GLSEventGridContainer collection,
        IEnumerable<BaseGLSEvent> selectionSources,
        string message)
    {
        // The final replacement supplies the simulator with every edited child event in one cache update.
        // Unity collections need their overloaded null comparison before batch replacement ends.
        if (collection != null)
        {
            collection.EndGroupReplacementBatch(message);
            // Rebind selected inner nodes after the replacement action's default parent selection has completed.
            if (!Networked)
            {
                collection.RebindSelectionAfterBatch(selectionSources);
            }
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, EditedObjects);
        SerializeBeatmapObjectList(writer, OriginalObjects);
    }

    public override void Deserialize(NetDataReader reader)
    {
        EditedObjects = DeserializeBeatmapObjectList(reader).ToList();
        OriginalObjects = DeserializeBeatmapObjectList(reader).ToList();

        Data = EditedObjects.Concat(OriginalObjects);
    }
}
