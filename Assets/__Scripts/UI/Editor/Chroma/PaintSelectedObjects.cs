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
        var selectedObjects = new List<BaseObject>(SelectionController.SelectedObjects);
        var selectedGlsColorEvents = GLSEventLookupIndex.GroupSelectedEvents(selectedObjects);
        foreach (var obj in selectedObjects)
        {
            // GLS children are replaced through their parent group below, preventing each painted node from invalidating the next selection.
            if (obj is BaseGLSEvent)
            {
                continue;
            }

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

        foreach (var (originalGroup, selectedEvents) in selectedGlsColorEvents)
        {
            var editedGroup = BeatmapFactory.Clone(originalGroup);
            var eventLookup = new GLSEventLookupIndex(originalGroup);
            var paintedEventCount = 0;
            foreach (var selectedEvent in selectedEvents)
            {
                // Chroma color is supported by color nodes only; rotation, translation, and FloatFX nodes have no color payload.
                if (selectedEvent is not BaseLightColorBase
                    || !eventLookup.TryGetCloneEvent(selectedEvent, editedGroup, out _, out var editedEvent)
                    || editedEvent is not BaseLightColorBase editedColorEvent)
                {
                    continue;
                }

                editedColorEvent.CustomColor = picker.CurrentColor;
                editedColorEvent.WriteCustom();
                paintedEventCount++;
            }

            if (paintedEventCount == 0)
            {
                continue;
            }

            // One parent replacement preserves every child identity until all selected nodes have been painted.
            allActions.Add(new BeatmapGLSEventBoxModifiedAction(
                editedGroup,
                originalGroup,
                "Painted GLS event box group."));
        }

        if (allActions.Count == 0) return;

        // The live objects were restored above, so perform the collection to install the edited snapshots.
        BeatmapActionContainer.AddAction(
            new ActionCollectionAction(
                allActions,
                true,
                true,
                "Painted a selection of objects."),
            true);
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
