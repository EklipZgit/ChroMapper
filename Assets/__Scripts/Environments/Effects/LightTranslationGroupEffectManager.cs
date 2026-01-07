using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class LightTranslationGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<LightTranslationGroupEffectEntry> effectEntries = new();

    public Dictionary<int, LightTranslationGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public void Initialize(AudioTimeSyncController atsc)
    {
        foreach (var LightTranslationGroupEffect in IdToEffect.Values)
        {
            LightTranslationGroupEffect.Atsc = atsc;
            LightTranslationGroupEffect.Initialize();
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

    public bool InsertData(BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> data)
    {
        if (!IdToEffect.TryGetValue(data.ID, out var effect)) return false;
        effect.InsertData(data);
        return true;
    }

    public bool InsertData(IEnumerable<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>> data) =>
        data.GroupBy(x => x.ID).Aggregate(false, (current, d) => current | InsertData(d.Key, d));

    public bool InsertData(int type, IEnumerable<BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox>> data)
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
        BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> reference,
        BaseLightTranslationEventBoxGroup<BaseLightTranslationEventBox> original)
    {
        if (!IdToEffect.TryGetValue(original.ID, out var effect)) return false;
        effect.RemoveData(reference, original);

        return true;
    }

    public void Register(int group, int count, Vector2[] translationLimits, Vector2[] distributionLimits)
    {
        if (effectEntries.Any(x => x.Group == group)) return;
        var effect = gameObject.AddComponent<LightTranslationGroupEffect>();
        effect.Count = count;
        effect.TranslationLimits = translationLimits;
        effect.DistributionLimits = distributionLimits;
        effectEntries.Add(new LightTranslationGroupEffectEntry { Group = group, Effect = effect });
        IdToEffect.Add(group, effect);
    }

    public void Register(int group, int id, Axis axis, bool mirrored, Transform tr) =>
        IdToEffect[group].Register(id, axis, mirrored, tr);
}

[Serializable]
public struct LightTranslationGroupEffectEntry
{
    public int Group;
    public LightTranslationGroupEffect Effect;
}
