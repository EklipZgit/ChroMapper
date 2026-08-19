using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventEffectManager : MonoBehaviour
{
    [SerializeField] private List<BasicEventStateManagerEntry> effectEntries = new();

    public readonly Dictionary<int, List<StateManager<BaseEvent>>> EventTypeToEffects = new();
    public readonly List<StateManager<BaseEvent>> Effects = new();

    private void Awake()
    {
        foreach (var managers in effectEntries.OrderBy(x => x.Type).GroupBy(x => x.Type))
            EventTypeToEffects.Add(managers.First().Type, managers.Select(x => x.Manager).ToList());

        // Environment scenes generated before the snapshot conversion still serialize
        // callback-manager references; migrate them without requiring scene regeneration.
        MigrateLegacyMovementEffects();

        // Combined movement managers can be registered under several event types, but
        // their shared timeline must still update and initialize only once per frame.
        foreach (var entry in effectEntries)
        {
            if (!Effects.Contains(entry.Manager))
                Effects.Add(entry.Manager);
        }
    }

    private void MigrateLegacyMovementEffects()
    {
        var legacyRotations = effectEntries
            .Select(entry => entry.Manager)
            .OfType<LightRotationEffect>()
            .Distinct()
            .ToList();

        foreach (var visual in FindObjectsOfType<LightRotation>(true))
        {
            if (visual.Effect == null || !legacyRotations.Contains(visual.Effect))
                continue;

            var effect = visual.gameObject.AddComponent<LightRotationEffect>();
            effect.Visual = visual;
            effect.SpeedMultiplier = visual.SpeedMultiplier;
            Register(visual.Effect.ID, effect);
        }

        foreach (var visual in FindObjectsOfType<LightPairRotation>(true))
        {
            if (visual.LeftEffect == null && visual.RightEffect == null)
                continue;

            var effect = visual.gameObject.AddComponent<LightPairRotationEffect>();
            effect.Visual = visual;
            RegisterLegacyPairTypes(effect, visual.LeftEffect, visual.RightEffect, visual.SwitchEffect);
        }

        foreach (var visual in FindObjectsOfType<LightPairSinMove>(true))
        {
            if (visual.LeftEffect == null && visual.RightEffect == null)
                continue;

            var effect = visual.gameObject.AddComponent<LightPairSinMoveEffect>();
            effect.Visual = visual;
            RegisterLegacyPairTypes(effect, visual.LeftEffect, visual.RightEffect, visual.SwitchEffect);
        }

        foreach (var visual in FindObjectsOfType<Movement>(true))
        {
            if (visual.Effect == null)
                continue;

            var effect = visual.gameObject.AddComponent<MovementEffect>();
            effect.Visual = visual;
            Register(visual.Effect.ID, effect);
        }

        // The old rotation managers existed only to broadcast callback state to these
        // visuals; after migration they would compute duplicate timelines with no visual.
        foreach (var legacy in legacyRotations)
        {
            foreach (var effects in EventTypeToEffects.Values)
                effects.Remove(legacy);
            effectEntries.RemoveAll(entry => entry.Manager == legacy);
            legacy.enabled = false;
        }
    }

    private void RegisterLegacyPairTypes(
        LightPairRotationEffect effect,
        LightRotationEffect left,
        LightRotationEffect right,
        GenericCallbackEventEffect switchEffect)
    {
        if (left != null)
        {
            effect.LeftEventType = left.ID;
            Register(left.ID, effect);
        }
        if (right != null)
        {
            effect.RightEventType = right.ID;
            Register(right.ID, effect);
        }
        if (switchEffect != null)
        {
            effect.SwitchEventType = switchEffect.ID;
            Register(switchEffect.ID, effect);
        }
    }

    private void RegisterLegacyPairTypes(
        LightPairSinMoveEffect effect,
        LightRotationEffect left,
        LightRotationEffect right,
        GenericCallbackEventEffect switchEffect)
    {
        if (left != null)
        {
            effect.LeftEventType = left.ID;
            Register(left.ID, effect);
        }
        if (right != null)
        {
            effect.RightEventType = right.ID;
            Register(right.ID, effect);
        }
        if (switchEffect != null)
        {
            effect.SwitchEventType = switchEffect.ID;
            Register(switchEffect.ID, effect);
        }
    }

    public void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        foreach (var manager in Effects)
        {
            manager.Atsc = atsc;
            switch (manager)
            {
                case BasicLightEffect blm:
                    blm.ColorScheme = colorScheme;
                    break;
                case ColorBoostEffect cbm:
                    cbm.ColorScheme = colorScheme;
                    break;
                case TrackLaneRingsRotationEffect tlrre:
                    if (tlrre.Visual != null && tlrre.Visual.Manager != null)
                        tlrre.Visual.Manager.Atsc = atsc;
                    break;
                case TrackLaneRingsPositionEffect tlrpe:
                    if (tlrpe.Visual != null && tlrpe.Visual.RingManager != null)
                        tlrpe.Visual.RingManager.Atsc = atsc;
                    break;
            }

            manager.Initialize();
        }
    }

    public void Reinitialize()
    {
        foreach (var manager in Effects) manager.Initialize();
    }

    public void Refresh()
    {
        foreach (var manager in Effects) manager.Refresh();
    }

    public bool InsertData(BaseEvent data)
    {
        // Missing event types are common during bulk load; avoid allocating an empty list
        // for every event that has no environment movement consumer.
        if (!EventTypeToEffects.TryGetValue(data.Type, out var effects))
            return false;

        foreach (var effect in effects)
        {
            // Ring snapshots fail only after collection actions, so retain the exact event and
            // effect dispatch that constructed each candidate state for the next live trace.
            if (effect is TrackLaneRingsRotationEffect || effect is TrackLaneRingsPositionEffect)
            {
                Debug.Log(
                    $"RINGSTATE insert effect={effect.name} type={data.Type} "
                    + $"beat={data.JsonTime:R} songBeat={data.SongBpmTime:R} value={data.Value}.");
            }

            effect.InsertData(data);
        }

        return effects.Count > 0;
    }

    public bool InsertData(IEnumerable<BaseEvent> data)
    {
        // Collection edits are usually small; dispatch each event directly to avoid GroupBy and per-type ToList allocations.
        var marked = false;
        foreach (var evt in data)
        {
            marked |= InsertData(evt);
        }

        return marked;
    }

    public bool InsertData(int type, IEnumerable<BaseEvent> data)
    {
        var marked = false;
        if (!EventTypeToEffects.TryGetValue(type, out var effects))
        {
            return false;
        }

        // Preserve one-pass enumerable consumption without materializing data for every effect manager.
        foreach (var evt in data)
        {
            foreach (var effect in effects)
            {
                effect.InsertData(evt);
                marked = true;
            }
        }

        return marked;
    }

    public bool RemoveData(BaseEvent reference, BaseEvent original)
    {
        // Removal follows the same allocation-free dispatch path as insertion.
        if (!EventTypeToEffects.TryGetValue(original.Type, out var effects))
            return false;

        foreach (var effect in effects)
        {
            // Pair removal with the insertion trace so paste, undo, and replacement actions
            // reveal whether a broken snapshot is retained or rebuilt from the right predecessor.
            if (effect is TrackLaneRingsRotationEffect || effect is TrackLaneRingsPositionEffect)
            {
                Debug.Log(
                    $"RINGSTATE remove effect={effect.name} type={original.Type} "
                    + $"referenceBeat={reference.JsonTime:R} originalBeat={original.JsonTime:R} "
                    + $"value={original.Value}.");
            }

            effect.RemoveData(reference, original);
        }

        return effects.Count > 0;
    }

    public T GetEffect<T>(int type) where T : StateManager<BaseEvent> =>
        EventTypeToEffects.TryGetValue(type, out var list) ? list.FirstOrDefault(x => x is T) as T : null;

    public IEnumerable<(int type, StateManager<BaseEvent> effect)> GetEffects() =>
        EventTypeToEffects.SelectMany(effects => effects.Value.Select(m => (effects.Key, m)));

    public IEnumerable<(int type, T effect)> GetEffects<T>() where T : StateManager<BaseEvent> =>
        GetEffects().Where(m => m.effect is T).Select(m => (m.type, m.effect as T));

    public T Register<T>(int type) where T : StateManager<BaseEvent>
    {
        var comp = gameObject.AddComponent<T>();
        return Register(type, comp);
    }

    public T GetOrRegister<T>(int type) where T : StateManager<BaseEvent>
    {
        var comp = GetEffect<T>(type);
        return comp == null ? Register<T>(type) : comp;
    }

    public T Register<T>(int type, T comp) where T : StateManager<BaseEvent>
    {
        AddToEntry(type, comp);
        comp.ID = type;
        return comp;
    }

    private void AddToEntry(int type, StateManager<BaseEvent> comp)
    {
        if (EventTypeToEffects.ContainsKey(type) && EventTypeToEffects[type].Contains(comp)) return;
        effectEntries.Add(new() { Type = type, Manager = comp });
        EventTypeToEffects.TryAdd(type, new List<StateManager<BaseEvent>>());
        EventTypeToEffects[type].Add(comp);
        if (!Effects.Contains(comp))
            Effects.Add(comp);
    }

    public void Register(LightController controller, bool strict = true)
    {
        if (effectEntries.Exists(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect))
        {
            var manager = effectEntries
                .First(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect)
                .Manager as BasicLightEffect;
            manager!.Register(controller, strict);
        }
        else
            Debug.LogError("Could not find manager for type " + controller.Type);
    }

    public void Unregister(LightController controller)
    {
        if (effectEntries.Exists(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect))
        {
            var manager = effectEntries
                .First(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect)
                .Manager as BasicLightEffect;
            manager!.Unregister(controller);
        }
        else
            Debug.LogError("Could not find manager for type " + controller.Type);
    }
}

[Serializable]
public class BasicEventStateManagerEntry
{
    public int Type;
    public StateManager<BaseEvent> Manager;
}
