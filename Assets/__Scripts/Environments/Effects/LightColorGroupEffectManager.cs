using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightColorGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<LightColorGroupEffectEntry> effectEntries = new();

    public Dictionary<int, LightColorGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public virtual void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        foreach (var effect in IdToEffect.Values)
        {
            effect.Atsc = atsc;
            effect.ColorScheme = colorScheme;
            effect.Initialize();
        }
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

    public void Register(int group, int id, LightController controllable) =>
        IdToEffect[group].Register(group, id, controllable);
}

[Serializable]
public struct LightColorGroupEffectEntry
{
    public int Group;
    public LightColorGroupEffect Effect;
}
