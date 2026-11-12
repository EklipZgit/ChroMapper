using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RefreshMapController : MonoBehaviour, CMInput.IRefreshMapActions
{
    [SerializeField] private MapLoader loader;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Material cancelFontMaterial;
    [SerializeField] private Material moreOptionsFontMaterial;
    [SerializeField] private Material thingYouCanRefreshFontMaterial;
    private LoadInitialMap initialMapLoader;

    private void Awake() => initialMapLoader = FindAnyObjectByType<LoadInitialMap>();

    public void OnRefreshMap(InputAction.CallbackContext context)
    {
        if (context.performed) InitiateRefreshConversation();
    }

    public void InitiateRefreshConversation() =>
        PersistentUI.Instance.ShowDialogBox(
            "Mapper",
            "refreshmap",
            HandleFirstLayerConversation,
            new[]
            {
                "refreshmap.notes",
                "refreshmap.walls",
                "refreshmap.events",
                "refreshmap.other",
                "refreshmap.full",
                "refreshmap.cancel"
            },
            new[]
            {
                thingYouCanRefreshFontMaterial,
                thingYouCanRefreshFontMaterial,
                thingYouCanRefreshFontMaterial,
                thingYouCanRefreshFontMaterial,
                thingYouCanRefreshFontMaterial,
                cancelFontMaterial
            });

    private void HandleFirstLayerConversation(int res)
    {
        switch (res)
        {
            case 0:
                RefreshMap(true, false, false, false, false);
                break;
            case 1:
                RefreshMap(false, true, false, false, false);
                break;
            case 2:
                RefreshMap(false, false, true, false, false);
                break;
            case 3:
                RefreshMap(false, false, false, true, false);
                break;
            case 4:
                RefreshMap(false, false, false, false, true);
                break;
        }
    }

    private void RefreshMap(bool notes, bool obstacles, bool events, bool others, bool full) =>
        StartCoroutine(ReloadMap(notes, obstacles, events, others, full));

    private IEnumerator ReloadMap(bool notes, bool obstacles, bool events, bool others, bool full)
    {
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        var info = BeatSaberSongContainer.Instance.Info;
        var reloaded = BeatSaberSongUtils.GetMapFromInfoFiles(info, infoDifficulty);

        if (full)
            BeatSaberSongContainer.Instance.Map = reloaded;
        else
        {
            var current = BeatSaberSongContainer.Instance.Map;
            current.BpmEvents = reloaded.BpmEvents;
            if (notes)
            {
                current.Notes = reloaded.Notes;
                current.Arcs = reloaded.Arcs;
                current.Chains = reloaded.Chains;
            }

            if (obstacles)
                current.Obstacles = reloaded.Obstacles;

            if (events)
                current.Events = reloaded.Events;

            if (others)
            {
                current.CustomEvents = reloaded.CustomEvents;
                current.PointDefinitions = reloaded.PointDefinitions;
                current.Materials = reloaded.Materials;
                current.EnvironmentEnhancements = reloaded.EnvironmentEnhancements;
            }

        }

        // Partial reloads retain unselected collections. Bind their timing and future refreshes to the merged map.
        var map = BeatSaberSongContainer.Instance.Map;
        loader.UpdateMapData(map);
        map.RecomputeAllObjectSongBpmTimes();
        // Reload the environment as well so removed lights and cloned rings cannot survive in its cached effect targets.
        if (full || others)
        {
            yield return initialMapLoader.ReloadCurrentMapInPlace();
            yield break;
        }

        var currentSongBpmTime = atsc.CurrentSongBpmTime;
        var resumePlayback = atsc.IsPlaying;
        // Partial object reloads also need stopped-seek callbacks to rebuild their pools.
        if (resumePlayback)
            atsc.TogglePlaying();

        atsc.MoveToSongBpmTime(0);
        loader.LoadObjects(map.BpmEvents);
        if (notes)
        {
            loader.LoadObjects(map.Notes);
            loader.LoadObjects(map.Arcs);
            loader.LoadObjects(map.Chains);
        }

        if (obstacles)
            loader.LoadObjects(map.Obstacles);

        if (events)
            loader.LoadObjects(map.Events);

        BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(0f);
        tracksManager.RefreshTracks();
        atsc.MoveToSongBpmTime(currentSongBpmTime);
        if (resumePlayback)
            atsc.TogglePlaying();
    }
}
