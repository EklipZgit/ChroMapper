using System;
using System.Collections.Generic;
using System.Linq;
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

    public FloatFxGroupEffect Register(int group, int count)
    {
        if (effectEntries.Any(x => x.Group == group)) return effectEntries.First(x => x.Group == group).Effect;
        var effect = gameObject.AddComponent<FloatFxGroupEffect>();
        effect.Count = count;
        effectEntries.Add(new FloatFxGroupEffectEntry { Group = group, Effect = effect });
        IdToEffect.Add(group, effect);
        return effect;
    }

    public void Register(int group, int id, GameObject go) => IdToEffect[group].Register(id);
}

[Serializable]
public struct FloatFxGroupEffectEntry
{
    public int Group;
    public FloatFxGroupEffect Effect;
}
