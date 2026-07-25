using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

/// <summary>
/// Manages GLS (Grouped Light System) event box groups in the renderer.
/// IMPORTANT: GLS event groups are cached in StateChunksContainer buckets based on their StartTime (SongBpmTime).
/// When an event group's time changes (e.g., when moved via cut/paste), the cached state must be properly updated
/// to reflect the new time, otherwise the renderer will continue showing lights at the old location.
/// 
/// CRITICAL: Any modification to an event group's JsonTime must go through the StateManager's RemoveData/InsertData
/// mechanism to ensure the state cache is properly updated. The StateChunksContainer uses bucket indices based on
/// StartTime for performance, so time changes require the state to be removed from the old bucket and re-inserted
/// into the new bucket.
/// </summary>
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
        foreach (var d in data)
        {
            if (eventGridProvider.LastContext != null && eventGridProvider.LastContext.IsConflictingWith(d))
                eventGridProvider.GroupContext = d;
            switch (d)
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
            if (eventGridProvider.GroupContext == reference) eventGridProvider.MarkRemove();

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
            if (eventGridProvider.GroupContext == d) eventGridProvider.MarkRemove();
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

    /// <summary>
    /// Updates data when an event group is modified (e.g., moved via cut/paste).
    /// IMPORTANT: When an event group's JsonTime changes, the cached state's StartTime must be updated
    /// to reflect the new time. This is handled by removing the old state and inserting a new state,
    /// which ensures the state is in the correct bucket for time-based lookups.
    /// 
    /// CRITICAL: This method is called by BeatmapObjectUpdatedAction when event groups are moved.
    /// The RemoveData/InsertData pattern ensures the state cache is properly synchronized.
    /// </summary>
    protected override bool UpdateData(IEnumerable<(BaseEventBoxGroup reference, BaseEventBoxGroup original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            if (eventGridProvider.GroupContext == reference) eventGridProvider.MarkRemove();

            // Remove the old state (using original time to find it in the cache)
            switch (original)
            {
                case BaseLightColorEventBoxGroup lcebg:
                    mark |= Context.Descriptor.LightColorGroupEffectManager.RemoveData(
                        reference as BaseLightColorEventBoxGroup,
                        lcebg);
                    break;
                case BaseLightRotationEventBoxGroup lrebg:
                    mark |= Context.Descriptor.LightRotationGroupEffectManager.RemoveData(
                        reference as BaseLightRotationEventBoxGroup,
                        lrebg);
                    break;
                case BaseLightTranslationEventBoxGroup ltebg:
                    mark |= Context.Descriptor.LightTranslationGroupEffectManager.RemoveData(
                        reference as BaseLightTranslationEventBoxGroup,
                        ltebg);
                    break;
                case BaseVfxEventEventBoxGroup ffebg:
                    mark |= Context.Descriptor.FloatFxGroupEffectManager.RemoveData(
                        reference as BaseVfxEventEventBoxGroup,
                        ffebg);
                    break;
            }

            // Insert the new state (using the updated time)
            switch (reference)
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
}
