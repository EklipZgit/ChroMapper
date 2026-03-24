using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class GLSManager : BeatmapObjectManager<BaseEventBoxGroup>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    [SerializeField] private LightshowController lightshowController;
    [SerializeField] private GLSEventGridProvider eventGridProvider;

    public override void Refresh()
    {
    }

    public override void UpdateTime()
    {
        if (lightshowController.Mode != LightshowMode.Full) return;
        UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        foreach (var manager in
            Context.Descriptor.BasicEventEffectManager.EventTypeToEffects.Values.SelectMany(managers =>
                managers))
            manager.UpdateTime(isPlaying, time);
    }

    // TODO: probably do more generic on descriptor side
    protected override bool AddData(IEnumerable<BaseEventBoxGroup> data)
    {
        var mark = false;
        foreach (var ebg in data)
        {
            switch (ebg)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.InsertData(lcebg);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.InsertData(lrebg);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.InsertData(ltebg);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.InsertData(ffebg);
                    break;
            }
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseEventBoxGroup reference, BaseEventBoxGroup original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            if (eventGridProvider.GroupContext == reference) eventGridProvider.MarkRemove = true;
            switch (reference)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.RemoveData(
                        lcebg,
                        original as BaseLightColorEventBoxGroup);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.RemoveData(
                        lrebg,
                        original as BaseLightRotationEventBoxGroup);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.RemoveData(
                        ltebg,
                        original as BaseLightTranslationEventBoxGroup);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.RemoveData(
                        ffebg,
                        original as BaseVfxEventEventBoxGroup);
                    break;
            }
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEventBoxGroup> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (eventGridProvider.GroupContext == d) eventGridProvider.MarkRemove = true;
            switch (d)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.RemoveData(lcebg, lcebg);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.RemoveData(lrebg, lrebg);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.RemoveData(ltebg, ltebg);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.RemoveData(ffebg, ffebg);
                    break;
            }
        }

        return mark;
    }
}
