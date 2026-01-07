using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class LightRotationGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<LightRotationGroupEffectEntry> effectEntries = new();

    public Dictionary<int, LightRotationGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public void Initialize(AudioTimeSyncController atsc)
    {
        foreach (var lightRotationGroupEffect in IdToEffect.Values)
        {
            lightRotationGroupEffect.Atsc = atsc;
            lightRotationGroupEffect.Initialize();
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

    public bool InsertData(BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data)
    {
        if (!IdToEffect.TryGetValue(data.ID, out var effect)) return false;
        effect.InsertData(data);
        return true;
    }

    public bool InsertData(IEnumerable<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>> data) =>
        data.GroupBy(x => x.ID).Aggregate(false, (current, d) => current | InsertData(d.Key, d));

    public bool InsertData(int type, IEnumerable<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>> data)
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

    public bool RemoveData(
        BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> reference,
        BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> original)
    {
        if (!IdToEffect.TryGetValue(original.ID, out var effect)) return false;
        effect.RemoveData(reference, original);

        return true;
    }

    public void Register(int group, int count)
    {
        if (effectEntries.Any(x => x.Group == group)) return;
        var effect = gameObject.AddComponent<LightRotationGroupEffect>();
        effect.Count = count;
        effectEntries.Add(new LightRotationGroupEffectEntry { Group = group, Effect = effect });
        IdToEffect.Add(group, effect);
    }

    public void Register(int group, int id, Axis axis, bool mirrored, Transform tr) =>
        IdToEffect[group].Register(id, axis, mirrored, tr);
}

[Serializable]
public struct LightRotationGroupEffectEntry
{
    public int Group;
    public LightRotationGroupEffect Effect;
}
