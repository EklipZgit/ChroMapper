using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using UnityEngine;

public class LightTranslationGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<LightTranslationGroupEffectEntry> effectEntries = new();

    public Dictionary<int, LightTranslationGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public virtual void Initialize(AudioTimeSyncController atsc)
    {
        foreach (var LightTranslationGroupEffect in IdToEffect.Values)
        {
            LightTranslationGroupEffect.Atsc = atsc;
            LightTranslationGroupEffect.Initialize();
        }
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
