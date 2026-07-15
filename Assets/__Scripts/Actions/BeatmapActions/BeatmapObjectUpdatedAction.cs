using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

public class BeatmapObjectUpdatedAction : BeatmapAction, IMergeableAction
{
    public BaseObject EditedObject;
    public BaseObject OriginalObject;

    public BaseObject PreMergeOriginalObject;

    public ActionMergeType MergeType { get; set; }
    public int MergeCount { get; set; }

    private bool addToSelection;

    // This constructor is needed for United Mapping
    public BeatmapObjectUpdatedAction() : base() { }

    public BeatmapObjectUpdatedAction(
        BaseObject editedObject,
        BaseObject originalObject,
        string comment = "No comment.",
        bool keepSelection = false,
        ActionMergeType mergeType = ActionMergeType.None)
        : base(new[] { editedObject, originalObject }, comment)
    {
        EditedObject = editedObject;
        OriginalObject = originalObject;
        addToSelection = keepSelection;
        MergeType = mergeType;
    }

    public IMergeableAction TryMerge(IMergeableAction previous)
    {
        return CanMerge(previous) ? DoMerge(previous) : null;
    }

    public bool CanMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectUpdatedAction previousAction) return false;
        return MergeType != ActionMergeType.None
            && previous.MergeType == MergeType
            && OriginalObject == previousAction.EditedObject;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectUpdatedAction previousAction) return null;
        var merged = new BeatmapObjectUpdatedAction(
            EditedObject,
            previousAction.OriginalObject,
            Comment,
            addToSelection,
            MergeType);

        merged.MergeCount = previousAction.MergeCount + 1;
        merged.Comment += $" ({merged.MergeCount}x merged)";
        merged.PreMergeOriginalObject = OriginalObject;

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == EditedObject ? OriginalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        DeleteObject(EditedObject, false);
        SpawnObject(OriginalObject);
        if (!addToSelection) SelectionController.DeselectAll();
        RefreshPools(Data);

        if (!Networked)
        {
            SelectionController.Select(OriginalObject, addToSelection, true, !inCollection);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (Networked && MergeCount > 0)
        {
            /*
             * Since actions over the network come merged, we use the pre-merge data to correctly remove object
             * e.g.
             * PC 1 edits object A to B
             * PC 2 receives edit Action A to B
             * PC 1 edits objects B to C -> Merges into A to C
             * PC 2 receives edit Action A to C (with preMerge original data B)
             */
            DeleteObject(PreMergeOriginalObject, false);

            // We've now handled the intermediate data, now treat it as a non-merged action so undos and redos work 
            MergeCount = 0;
        }
        else
        {
            DeleteObject(OriginalObject, false);
        }

        SpawnObject(EditedObject, false, !inCollection);
        if (!addToSelection) SelectionController.DeselectAll();

        // Don't think refresh pools is necessary
        // RefreshPools(Data);

        if (!Networked)
        {
            SelectionController.Select(EditedObject, addToSelection, true, !inCollection);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(EditedObject);
        writer.PutBeatmapObject(OriginalObject);

        writer.Put(MergeCount);
        if (MergeCount > 0)
        {
            writer.PutBeatmapObject(PreMergeOriginalObject);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        EditedObject = BeatmapFactory.Clone(reader.GetBeatmapObject());
        OriginalObject = BeatmapFactory.Clone(reader.GetBeatmapObject());

        MergeCount = reader.GetInt();
        if (MergeCount > 0)
        {
            PreMergeOriginalObject = reader.GetBeatmapObject();
        }

        Data = new[] { EditedObject, OriginalObject };
    }
}
