using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private TracksManager manager;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    [Space] [SerializeField] private Transform containerCollectionsContainer;

    private BaseDifficulty map;

    // Track/fog/bloom animators push into environment-scene targets every frame, so environment teardown
    // must quiesce them first; otherwise the unload window NREs on the null Descriptor or destroyed targets.
    public void ResetAnimationTracks() => manager.ResetAnimationTracks();

    public void UpdateMapData(BaseDifficulty m)
    {
        map = m;
        map.ConvertCustomBpmToOfficial();
    }

    public void HardRefresh()
    {
        var perfSw = System.Diagnostics.Stopwatch.StartNew();
        // WorldCavesInEnvironmentTest reloads the same map within one session; named animation tracks persist
        // across loads, so reset them to fresh-load state (the game rebuilds all track state per map load) before
        // the new map's custom events and environment enhancements spawn.
        manager.ResetAnimationTracks();

        LoadObjects(map.BpmEvents);

        if (Settings.Instance.Load_Others)
        {
            perfSw.Restart();
            LoadObjects(map.CustomEvents);
            Debug.Log($"[Perf] HardRefresh: CustomEvents took {perfSw.ElapsedMilliseconds}ms");
            perfSw.Restart();
            // EnvironmentEnhancementLoadOrderTest: Chroma applies this authored instruction
            // array sequentially; it must bypass the timed-object loader and its sort.
            LoadEnvironmentEnhancements(map.EnvironmentEnhancements);
            Debug.Log($"[Perf] HardRefresh: EnvironmentEnhancements took {perfSw.ElapsedMilliseconds}ms");
        }

        perfSw.Restart();
        if (Settings.Instance.Load_Notes)
        {
            LoadObjects(map.Notes);
            LoadObjects(map.Arcs);
            LoadObjects(map.Chains);
        }

        if (Settings.Instance.Load_Obstacles) LoadObjects(map.Obstacles);
        if (Settings.Instance.Load_Events)
        {
            LoadObjects(map.Events);
            LoadObjects(map.LightColorEventBoxGroups);
            LoadObjects(map.LightRotationEventBoxGroups);
            LoadObjects(map.LightTranslationEventBoxGroups);
            LoadObjects(map.VfxEventBoxGroups);
        }

        if (Settings.Instance.Load_Notes || Settings.Instance.Load_Obstacles)
        {
            LoadObjects(map.NJSEvents);
            LoadObjects(map.RotationEvents);
        }
        Debug.Log($"[Perf] HardRefresh: remaining LoadObjects took {perfSw.ElapsedMilliseconds}ms");

        perfSw.Restart();
        manager.RefreshTracks();
        Debug.Log($"[Perf] HardRefresh: RefreshTracks took {perfSw.ElapsedMilliseconds}ms");
    }

    // RestoringEditorCursorAfterCloningRingDoesNotUsePreCloneRotationSnapshot requires movement snapshots to observe
    // every ring appended by environment enhancement spawning before AudioTimeSyncController renders saved map time.
    public void HardRefreshBeforeEditorStateRestore(EnvironmentDescriptor descriptor)
    {
        HardRefresh();

        if (Settings.Instance.Load_Others && map.EnvironmentEnhancements.Count > 0)
        {
            var perfSw = System.Diagnostics.Stopwatch.StartNew();
            descriptor.Reinitialize();
            Debug.Log($"[Perf] HardRefresh: descriptor.Reinitialize took {perfSw.ElapsedMilliseconds}ms");
        }
    }

    public void LoadObjects<T>(List<T> objects) where T : BaseObject
    {
        var collection =
            BeatmapObjectContainerCollection.GetCollectionForType<BeatmapObjectContainerCollection<T>, T>();

        if (collection == null) return;

        // WorldCavesInEnvironmentTest's runway: List<T>.Sort is unstable, so custom events whose CompareTo
        // ties (same JsonTime + Type, e.g. a beat-0 696969 hide AnimateTrack followed by the beat-0 show on
        // the same track) reached CustomEventGridContainer.AddCustomEvent in scrambled order and the wrong
        // event won the track property. Heck fires custom events in beatmap file order, so equal-key objects
        // must keep file order here; OrderBy's sort is stable. The result is copied back into the same list
        // because collection.MapObjects aliases the map's own list (SongBoundaryTest's spawned BPM events
        // must stay visible to BaseDifficulty.SongBpmTimeToJsonTime).
        var sorted = objects.OrderBy(it => it).ToList();
        objects.Clear();
        objects.AddRange(sorted);

        collection.MapObjects = objects;

        if (objects is List<BaseEvent> eventsList)
        {
            var events = collection as EventGridContainer;
            // Build and filter the boost lookup index in one load pass without retaining a linear-scan list.
            events.LoadBoostEvents(eventsList);
            events.AllBpmEvents = eventsList.FindAll(it => it.IsBpmEvent());

            events.LinkAllLightEvents();
            events.LinkRingEvents();
        }

        if (objects is List<BaseCustomEvent> customEventsList)
        {
            var perfSw = System.Diagnostics.Stopwatch.StartNew();
            var events = collection as CustomEventGridContainer;
            events.LoadAll();
            Debug.Log($"[Perf] LoadObjects: CustomEvents LoadAll took {perfSw.ElapsedMilliseconds}ms");
        }

        var poolSw = System.Diagnostics.Stopwatch.StartNew();
        collection.RefreshPool(true);
        Debug.Log($"[Perf] LoadObjects: RefreshPool({typeof(T).Name} x{objects.Count}) took {poolSw.ElapsedMilliseconds}ms");
    }

    // EnvironmentEnhancementLoadOrderTest: environment enhancements are Chroma commands,
    // not beat-indexed objects. Keep their array order while reusing the geometry collection
    // solely for rendering and selection; a source transform can affect every later clone.
    public void LoadEnvironmentEnhancements(List<BaseEnvironmentEnhancement> instructions)
    {
        var collection = BeatmapObjectContainerCollection
            .GetCollectionForType<GeometryGridContainer, BaseEnvironmentEnhancement>();
        if (collection == null) return;

        collection.MapObjects = instructions;
        if (beatmapRuntimeContext.Descriptor != null)
            beatmapRuntimeContext.Descriptor.BloomFogParams.ResetToDefaults();
        beatmapRuntimeContext.NotifyEnvironment();

        var poolSw = System.Diagnostics.Stopwatch.StartNew();
        collection.RefreshPool(true);
        Debug.Log($"[Perf] LoadEnvironmentEnhancements: RefreshPool(x{instructions.Count}) took {poolSw.ElapsedMilliseconds}ms");
    }
}
