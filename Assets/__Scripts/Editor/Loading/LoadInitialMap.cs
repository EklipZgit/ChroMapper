using System;
using System.Collections;
using Beatmap.Containers;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// why the hell so many thing depend on this, what's wrong with ya'll
public class LoadInitialMap : MonoBehaviour
{
    public static event Action<PlatformDescriptor> OnPlatformLoaded;
    public static event Action<PlatformColorScheme> OnPlatformColorsRefreshed;
    public static event Action OnLevelLoaded;
    public static PlatformDescriptor Platform;
    public static readonly Vector3 PlatformOffset = new Vector3(0, -0.5f, -1.5f);

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private RotationCallbackController rotationController;

    [FormerlySerializedAs("notesContainer")] [Space] [SerializeField]
    private NoteGridContainer noteGridContainer;

    [FormerlySerializedAs("obstaclesContainer")] [SerializeField]
    private ObstacleGridContainer obstacleGridContainer;

    [FormerlySerializedAs("arcsContainer")] [SerializeField]
    private ArcGridContainer arcGridContainer;

    [FormerlySerializedAs("chainsContainer")] [SerializeField]
    private ChainGridContainer chainGridContainer;

    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private MapLoader loader;

    [SerializeField] private EnvironmentListSO environmentList;

    private void Awake() => SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());

    private void Start() => LoadedDifficultySelectController.OnLoadedDifficultyChanged += UpdatePlatformColors;

    private void OnDestroy() => LoadedDifficultySelectController.OnLoadedDifficultyChanged -= UpdatePlatformColors;

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => atsc.Initialized); //Wait until Start has been called

        var info = BeatSaberSongContainer.Instance.Info; //Grab songe data
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        //Set up some local variables
        var envName = info.EnvironmentNames[infoDifficulty.EnvironmentNameIndex];
        var customPlat = false;
        var directional = false;

        //Grab platform by name (Official or Custom)
        // if (!string.IsNullOrEmpty(info.CustomEnvironmentMetadata.Name))
        // {
        //     if (CustomPlatformsLoader
        //             .Instance.GetAllEnvironmentIds()
        //             .IndexOf(info.CustomEnvironmentMetadata.Name)
        //         >= 0)
        //     {
        //         customPlat = true;
        //     }
        // }

        //Instantiate platform, grab descriptor
        var platform = environmentList.LookupID.TryGetValue(envName, out var value)
            ? value
            : environmentList.LookupID["DefaultEnvironment"];

        // if (customPlat)
        //     platform = CustomPlatformsLoader.Instance.LoadPlatform(info.CustomEnvironmentMetadata.Name, platform);

        SceneManager.LoadScene(platform.ID, LoadSceneMode.Additive);
        yield return new WaitUntil(() => SceneManager.GetSceneByName(platform.ID).isLoaded);

        var descriptor = FindAnyObjectByType<PlatformDescriptor>();
        EventContainer.ModifyTypeMode = descriptor.SortMode; //Change sort mode

        PopulateColorsFromMapInfo(descriptor);
        UpdateObjectContainerColors(descriptor.ColorScheme);

        OnPlatformLoaded.Invoke(descriptor); //Trigger event for classes that use the platform
        Platform = descriptor;

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        loader.HardRefresh();
        OnLevelLoaded?.Invoke();
    }

    public static void NotifyPlatformLoaded() => OnPlatformLoaded?.Invoke(Platform);

    public static void PopulateColorsFromMapInfo(PlatformDescriptor platformDescriptor)
    {
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        platformDescriptor.ColorScheme = platformDescriptor.DefaultColorScheme.Clone();

        if (infoDifficulty.CustomColorLeft != null)
            platformDescriptor.ColorScheme.RedNoteColor = infoDifficulty.CustomColorLeft.Value;
        if (infoDifficulty.CustomColorRight != null)
            platformDescriptor.ColorScheme.BlueNoteColor = infoDifficulty.CustomColorRight.Value;

        if (infoDifficulty.CustomColorObstacle != null)
            platformDescriptor.ColorScheme.ObstacleColor = infoDifficulty.CustomColorObstacle.Value;

        if (infoDifficulty.CustomEnvColorLeft != null)
            platformDescriptor.ColorScheme.RedColor = infoDifficulty.CustomEnvColorLeft.Value;
        if (infoDifficulty.CustomEnvColorRight != null)
            platformDescriptor.ColorScheme.BlueColor = infoDifficulty.CustomEnvColorRight.Value;
        if (infoDifficulty.CustomEnvColorWhite != null)
            platformDescriptor.ColorScheme.WhiteColor = infoDifficulty.CustomEnvColorWhite.Value;

        if (infoDifficulty.CustomEnvColorBoostLeft != null)
            platformDescriptor.ColorScheme.RedBoostColor = infoDifficulty.CustomEnvColorBoostLeft.Value;
        if (infoDifficulty.CustomEnvColorBoostRight != null)
            platformDescriptor.ColorScheme.BlueBoostColor = infoDifficulty.CustomEnvColorBoostRight.Value;
        if (infoDifficulty.CustomEnvColorBoostWhite != null)
            platformDescriptor.ColorScheme.WhiteBoostColor = infoDifficulty.CustomEnvColorBoostWhite.Value;
    }

    private void UpdateObjectContainerColors(PlatformColorScheme platformColorScheme)
    {
        var leftNoteColor = platformColorScheme.RedNoteColor;
        var rightNoteColor = platformColorScheme.BlueNoteColor;
        noteGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        arcGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        chainGridContainer.UpdateColor(leftNoteColor, rightNoteColor);

        obstacleGridContainer.UpdateColor(platformColorScheme.ObstacleColor);

        eventGridContainer.UpdateColor(
            platformColorScheme.RedColor,
            platformColorScheme.RedBoostColor,
            platformColorScheme.BlueColor,
            platformColorScheme.BlueBoostColor,
            platformColorScheme.WhiteColor,
            platformColorScheme.WhiteBoostColor
        );
    }

    private void UpdatePlatformColors()
    {
        var previousColors = Platform.ColorScheme.Clone();

        PopulateColorsFromMapInfo(Platform);
        UpdateObjectContainerColors(Platform.ColorScheme);

        // We only want to refresh pools if the colours have changed as refreshing is pretty expensive
        var currentColors = Platform.ColorScheme;

        var obstacleColorChanged = previousColors.ObstacleColor != currentColors.ObstacleColor;
        if (obstacleColorChanged)
        {
            obstacleGridContainer.RefreshPool(true);
        }

        var noteColorChanged = previousColors.BlueNoteColor != currentColors.BlueNoteColor
            || previousColors.RedNoteColor != currentColors.RedNoteColor;
        if (noteColorChanged)
        {
            noteGridContainer.RefreshPool(true);
            arcGridContainer.RefreshPool(true);
            chainGridContainer.RefreshPool(true);
        }

        var lightColorChanged = previousColors.BlueColor != currentColors.BlueColor
            || previousColors.RedColor != currentColors.RedColor
            || previousColors.WhiteColor != currentColors.WhiteColor
            || previousColors.BlueBoostColor != currentColors.BlueBoostColor
            || previousColors.RedBoostColor != currentColors.RedBoostColor
            || previousColors.WhiteBoostColor != currentColors.WhiteBoostColor;
        if (lightColorChanged)
        {
            eventGridContainer.RefreshPool(true);
        }

        OnPlatformColorsRefreshed?.Invoke(Platform.ColorScheme);
    }
}
