using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapEventInputController : BeatmapInputController<EventContainer>, CMInput.IEventObjectsActions
{
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    private TracksDefinitionSO trackDefinition;

    private void Start() => beatmapRuntimeContext.OnTracksDefinitionChanged += HandleTrackDefinitionChanged;

    private void OnDestroy() => beatmapRuntimeContext.OnTracksDefinitionChanged -= HandleTrackDefinitionChanged;

    private void HandleTrackDefinitionChanged(TracksDefinitionSO obj) => trackDefinition = obj;

    public void OnInvertEventValue(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsMouseInWindow
            || !context.performed)
            return;

        RaycastFirstObject(out var e);
        if (e != null && !e.Dragged) InvertEvent(e);
    }

    public void OnTweakEventMain(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged || !context.performed) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        TweakMain(e, modifier);
    }

    public void OnTweakEventAlternative(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        RaycastFirstObject(out var e);
        if (e == null || e.Dragged || !context.performed) return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        TweakAlternative(e, modifier);
    }

    public void InvertEvent(EventContainer e)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);
        if (e.EventData.IsLaneRotationEvent())
        {
            e.EventData.Rotation *= -1;
            tracksManager.RefreshTracks();
        }
        else if (e.EventData.IsColorBoostEvent())
        {
            e.EventData.Value = e.EventData.Value > 0 ? 0 : 1;
        }
        else if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind != BasicEventKind.Lights)
        {
            return;
        }
        else
        {
            switch (e.EventData.Value)
            {
                case > 0 and <= 8:
                    e.EventData.Value += 4;
                    break;
                case > 8 and <= 12:
                    e.EventData.Value -= 8; // white to blue
                    break;
            }

            RefreshPrevEventContainer(e);
        }

        eventAppearanceSo.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    protected override bool GetComponentFromTransform(GameObject t, out EventContainer obj) =>
        t.transform.parent.TryGetComponent(out obj);

    // for event that frequently gets changed
    public void TweakMain(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            var prec = scrollPrecisionController.GetCurrentBrightnessPrecision() / 100f;
            var value = Mathf.Round((e.EventData.FloatValue + (modifier * prec)) * 1_000f) / 1_000f;
            e.EventData.FloatValue = Mathf.Max(0f, value);

            RefreshPrevEventContainer(e);
        }
        else if (e.EventData.IsLaneRotationEvent())
        {
            var prec = scrollPrecisionController.GetCurrentRotationPrecision();
            var value = Mathf.Round((e.EventData.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
            e.EventData.Rotation += value;
            tracksManager.RefreshTracks();
        }
        else if (e.EventData.IsColorBoostEvent())
        {
            e.EventData.Value = e.EventData.Value == 0 ? 1 : 0;
        }
        else if (e.EventData.IsBpmEvent())
        {
            e.EventData.FloatValue += modifier;
            if (e.EventData.FloatValue < 1) e.EventData.FloatValue = 1;
        }
        else
        {
            e.EventData.Value += modifier;
            if (e.EventData.Value < 0) e.EventData.Value = 0;
        }

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearanceSo.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                e.ObjectData,
                e.ObjectData,
                original,
                mergeType: ActionMergeType.EventMainTweak));
    }

    // for event that occasionally gets changed
    public void TweakAlternative(EventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        if (trackDefinition.GetBasicOrDefault(e.EventData.Type).Kind == BasicEventKind.Lights)
        {
            e.EventData.Value += modifier;

            if (e.EventData.Value < 0) e.EventData.Value = 0;
            if (e.EventData.Value > 12) e.EventData.Value = 12;
            if (e.EventData.CompareTo(original) == 0) return;

            RefreshPrevEventContainer(e);
        }
        else if (e.EventData.IsLaneRotationEvent())
        {
            var prec = scrollPrecisionController.GetCurrentRotationPrecision();
            var value = Mathf.Round((e.EventData.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
            e.EventData.Rotation += value;
            tracksManager.RefreshTracks();
        }

        eventAppearanceSo.SetAppearance(e, trackDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                e.ObjectData,
                e.ObjectData,
                original,
                mergeType: ActionMergeType.EventAltTweak));
    }

    private void RefreshPrevEventContainer(EventContainer e)
    {
        var prevEvent = e.EventData.Prev;
        if (prevEvent != null)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (collection.LoadedContainers.TryGetValue(prevEvent, out var container))
                (container as EventContainer).RefreshAppearance();
        }
    }
}
