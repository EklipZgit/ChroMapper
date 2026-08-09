using System;
using System.Linq;
using Beatmap.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeatmapRuntimeContext : MonoBehaviour
{
    public AudioTimeSyncController Atsc;
    public EnvironmentListSO EnvironmentList;

    [Header("Runtime")] public EnvironmentDescriptor Descriptor;
    public ColorSchemeSO ColorScheme;
    public TracksDefinitionSO TracksDefinition;

    public event Action OnEnvironmentUnloaded;
    public event Action<EnvironmentDescriptor> OnEnvironmentLoaded;
    public event Action<ColorSchemeSO> OnColorSchemeChanged;
    public event Action<TracksDefinitionSO> OnTracksDefinitionChanged;

    public void Start()
    {
        ColorScheme = ScriptableObject.CreateInstance<ColorSchemeSO>();
        TracksDefinition = ScriptableObject.CreateInstance<TracksDefinitionSO>();
    }

    public void SetEnvironment(EnvironmentDescriptor descriptor)
    {
        Descriptor = descriptor;
        if (Descriptor != null)
        {
            var listing = EnvironmentList.GetEnvironmentOrDefault(descriptor.ID);
            SetColorScheme(listing.ColorScheme);
            SetTracksDefinition(listing.TracksDefinition);
            Descriptor.Initialize(this);
            // TODO: also move this elsewhere
            if (BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData["_environmentRemoval"] != null)
            {
                var envRemoval = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData["_environmentRemoval"]
                    .AsArray;
                foreach (var marker in Descriptor.ChromaIDMarkers)
                {
                    foreach (var (_, id) in envRemoval)
                    {
                        if (!marker.ChromaID.Contains(id)) continue;
                        marker.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        NotifyEnvironment();
    }

    public void NotifyEnvironment()
    {
        if (Descriptor != null)
            OnEnvironmentLoaded?.Invoke(Descriptor);
        else
            OnEnvironmentUnloaded?.Invoke();
    }

    public void SetColorScheme(ColorSchemeSO colorScheme)
    {
        ColorScheme.Copy(colorScheme);
        // TODO: make a class that handles no event class that require direct assignment
        PointDataParsers.ColorScheme = colorScheme;
        NotifyColorScheme();
    }

    public void NotifyColorScheme() => OnColorSchemeChanged?.Invoke(ColorScheme);

    public void SetTracksDefinition(TracksDefinitionSO tracksDefinition)
    {
        TracksDefinition.Copy(tracksDefinition);
        // Share the active definition by reference so requirement checks can identify component-specific Basic Events.
        BeatSaberSongContainer.Instance.Map.RuntimeTracksDefinition = TracksDefinition;
        PaintSelectedObjects.TracksDefinition = tracksDefinition;
        NotifyTracksDefinition();
    }

    public void NotifyTracksDefinition() => OnTracksDefinitionChanged?.Invoke(TracksDefinition);
}
