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
                allActions.Add(new BeatmapObjectUpdatedAction(
                    edited,
                    obj,
                    "a",
                    true));
            }
        }

        if (allActions.Count == 0) return;

        foreach (var unique in SelectionController.SelectedObjects.DistinctBy(x => x.ObjectType))
            BeatmapObjectContainerCollection.GetCollectionForType(unique.ObjectType).RefreshPool(true);

        FindAnyObjectByType<LightshowController>()?.RefreshLightshow();

        BeatmapActionContainer.AddAction(
            new ActionCollectionAction(
                allActions,
                true,
                true,
                "Painted a selection of objects."));
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
