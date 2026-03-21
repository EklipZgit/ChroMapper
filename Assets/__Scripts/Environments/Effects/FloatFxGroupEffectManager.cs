using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class FloatFxGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<FloatFxGroupEffectEntry> effectEntries = new();

    public Dictionary<int, FloatFxGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public virtual void Initialize(AudioTimeSyncController atsc)
    {
        foreach (var effect in IdToEffect.Values)
        {
            effect.Atsc = atsc;
            effect.Initialize();
        }
    }

    public void Reinitialize()
    {
        foreach (var effect in IdToEffect.Values) effect.Initialize();
    }

    public void Refresh()
    {
        foreach (var effect in IdToEffect.Values) effect.Refresh();
    }

    public bool InsertData(BaseVfxEventEventBoxGroup data)
    {
        if (!IdToEffect.TryGetValue(data.ID, out var effect)) return false;
        effect.InsertData(data);
        return true;
    }

    public bool InsertData(IEnumerable<BaseVfxEventEventBoxGroup> data) =>
        data.GroupBy(x => x.ID).Aggregate(false, (current, d) => current | InsertData(d.Key, d));

    public bool InsertData(int type, IEnumerable<BaseVfxEventEventBoxGroup> data)
    {
        data = data.ToList();
        if (!IdToEffect.TryGetValue(type, out var effect)) return false;

        var marked = false;
        foreach (var evt in data)
        {
            effect.InsertData(evt);
            marked = true;
        }

        return marked;
    }

    public bool RemoveData(BaseVfxEventEventBoxGroup reference, BaseVfxEventEventBoxGroup original)
    {
        if (!IdToEffect.TryGetValue(original.ID, out var effect)) return false;
        effect.RemoveData(reference, original);

        return true;
    }

    public FloatFxGroupEffect Register(int group, int count, bool trigger)
    {
        if (effectEntries.Any(x => x.Group == group)) return effectEntries.First(x => x.Group == group).Effect;
        var effect = gameObject.AddComponent<FloatFxGroupEffect>();
        effect.ID = group;
        effect.Count = count;
        effect.Trigger = trigger;
        effectEntries.Add(new FloatFxGroupEffectEntry { Group = group, Effect = effect });
        IdToEffect.Add(group, effect);
        return effect;
    }

    public void Register(int group, int id, FxTarget target) => IdToEffect[group].Register(id, target);
}

[Serializable]
public struct FloatFxGroupEffectEntry
{
    public int Group;
    public FloatFxGroupEffect Effect;
}
