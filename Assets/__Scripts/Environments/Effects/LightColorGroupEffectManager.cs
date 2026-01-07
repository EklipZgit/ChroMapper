using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class LightColorGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<LightColorGroupEffectEntry> effectEntries = new();

    public Dictionary<int, LightColorGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        foreach (var effect in IdToEffect.Values)
        {
            effect.Atsc = atsc;
            effect.ColorScheme = colorScheme;
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

    public bool InsertData(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data)
    {
        if (!IdToEffect.TryGetValue(data.ID, out var effect)) return false;
        effect.InsertData(data);
        return true;
    }

    public bool InsertData(IEnumerable<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> data) =>
        data.GroupBy(x => x.ID).Aggregate(false, (current, d) => current | InsertData(d.Key, d));

    public bool InsertData(int type, IEnumerable<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> data)
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
        BaseLightColorEventBoxGroup<BaseLightColorEventBox> reference,
        BaseLightColorEventBoxGroup<BaseLightColorEventBox> original)
    {
        if (!IdToEffect.TryGetValue(original.ID, out var effect)) return false;
        effect.RemoveData(reference, original);

        return true;
    }

    public LightColorGroupEffect Register(int group, int count)
    {
        if (effectEntries.Any(x => x.Group == group)) return effectEntries.First(x => x.Group == group).Effect;
        var effect = gameObject.AddComponent<LightColorGroupEffect>();
        effect.Count = count;
        effectEntries.Add(new LightColorGroupEffectEntry { Group = group, Effect = effect });
        IdToEffect.Add(group, effect);
        return effect;
    }

    public void Register(LightController controllable)
    {
        if (controllable.Kind != LightController.LightKind.Group) return;
        IdToEffect[controllable.Type].Register(controllable);
    }
}

[Serializable]
public struct LightColorGroupEffectEntry
{
    public int Group;
    public LightColorGroupEffect Effect;
}
