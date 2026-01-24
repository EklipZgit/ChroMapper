using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeatmapRuntimeContext : MonoBehaviour
{
    public AudioTimeSyncController Atsc;
    public EnvironmentListSO EnvironmentList;

    [Header("Runtime")] public EnvironmentDescriptor Descriptor;
    public ColorSchemeSO ColorScheme;
    public TracksDefinitionSO TracksDefinition;

    public event Action<EnvironmentDescriptor> OnEnvironmentChanged;
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
            if (BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData["_environmentRemoval"] != null)
            {
                var envRemoval = BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomData["_environmentRemoval"]
                    .AsArray;
                foreach (var go in Descriptor.gameObject.scene.GetRootGameObjects())
                    SearchAndDeactivate(go);

                void SearchAndDeactivate(GameObject go)
                {
                    var c = go.GetComponent<ChromaIDMarker>();
                    if (c != null)
                    {
                        foreach (var (_, value) in envRemoval)
                        {
                            if (!c.ChromaID.Contains(value)) continue;
                            go.SetActive(false);
                            return;
                        }
                    }

                    foreach (Transform child in go.transform) SearchAndDeactivate(child.gameObject);
                }
            }
        }

        NotifyEnvironment();
    }

    public void NotifyEnvironment() => OnEnvironmentChanged?.Invoke(Descriptor);

    public void SetColorScheme(ColorSchemeSO colorScheme)
    {
        ColorScheme.Copy(colorScheme);
        NotifyColorScheme();
    }

    public void NotifyColorScheme() => OnColorSchemeChanged?.Invoke(ColorScheme);

    public void SetTracksDefinition(TracksDefinitionSO tracksDefinition)
    {
        TracksDefinition.Copy(tracksDefinition);
        NotifyTracksDefinition();
    }

    public void NotifyTracksDefinition() => OnTracksDefinitionChanged?.Invoke(TracksDefinition);
}
