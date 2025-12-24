using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using UnityEngine;

public class LightRotationGroupEffectManager : MonoBehaviour
{
    [SerializeField] private List<LightRotationGroupEffectEntry> effectEntries = new();

    public Dictionary<int, LightRotationGroupEffect> IdToEffect = new();

    private void Awake() => IdToEffect = effectEntries.ToDictionary(x => x.Group, x => x.Effect);

    public virtual void Initialize(AudioTimeSyncController atsc)
    {
        foreach (var lightRotationGroupEffect in IdToEffect.Values)
        {
            lightRotationGroupEffect.Atsc = atsc;
            lightRotationGroupEffect.Initialize();
        }
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
