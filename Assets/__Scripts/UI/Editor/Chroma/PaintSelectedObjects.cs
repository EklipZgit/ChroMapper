using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class PaintSelectedObjects : MonoBehaviour
{
    [SerializeField] private ColorPicker picker;
    public static TracksDefinitionSO TracksDefinition;

    public void Paint()
    {
        var allActions = new List<BeatmapAction>();
        foreach (var obj in SelectionController.SelectedObjects)
        {
            if (obj is BaseBpmEvent or BaseCustomEvent)
                continue; //These should probably not be colored.
            var beforePaint = BeatmapFactory.Clone(obj);
            if (DoPaint(obj))
            {
                // Restore the live object before submitting its edited clone so cached state keeps object identity.
                var edited = BeatmapFactory.Clone(obj);
                obj.Apply(beforePaint);
                // BaseEvent.Apply copies custom JSON without rebuilding parsed Chroma fields.
                obj.RefreshCustom();
                allActions.Add(new BeatmapObjectUpdatedAction(
                    edited,
                    obj,
                    "a",
                    true));
            }
        }

        if (allActions.Count == 0) return;

        // Capture affected pools before replacement actions update the selected object identities.
        var affectedObjectTypes = new HashSet<ObjectType>();
        foreach (var selectedObject in SelectionController.SelectedObjects)
            affectedObjectTypes.Add(selectedObject.ObjectType);
        // The live objects were restored above, so perform the collection to install the edited snapshots.
        BeatmapActionContainer.AddAction(
            new ActionCollectionAction(
                allActions,
                true,
                true,
                "Painted a selection of objects."),
            true);

        // Refresh visuals only after the edited snapshots become the authoritative live objects.
        foreach (var objectType in affectedObjectTypes)
            BeatmapObjectContainerCollection.GetCollectionForType(objectType).RefreshPool(true);

        // BeatmapObjectManager callbacks already update lightshow caches after the performed paint action.
    }

    private bool DoPaint(BaseObject obj)
    {
        if (obj is BaseEvent evt)
        {
            if (evt.Value == (int)LightValue.Off) return false; //Ignore painting Off events
            if (TracksDefinition.GetBasicOrDefault(evt.Type).Kind != BasicEventKind.Lights) return false; //Ignore non-light event
            if (evt.CustomLightGradient != null)
            {
                //Modify start color if we are painting a Chroma 2.0 gradient
                evt.CustomLightGradient.StartColor = picker.CurrentColor;
                return true;
            }
        }
        else if (obj is BaseBpmEvent or BaseCustomEvent)
        {
            return false; //These should not be colored.
        }

        obj.CustomColor = picker.CurrentColor;
        obj.WriteCustom();
        //Debug.Log($"[GLS-Paint] DoPaint on {obj.GetType().Name}: picker.CurrentColor={picker.CurrentColor}, CustomColor set to {obj.CustomColor}, CustomData after WriteCustom={obj.CustomData}");

        return true;
    }
}
