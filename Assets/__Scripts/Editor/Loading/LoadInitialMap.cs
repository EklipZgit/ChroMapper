using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// why the hell so many thing depend on this, what's wrong with ya'll
public class LoadInitialMap : MonoBehaviour
{
    public static event Action OnLevelLoaded;

    [SerializeField] private RotationCallbackController rotationController;
    [SerializeField] private BeatmapRuntimeContext context;

    [Space] [SerializeField] private NoteGridContainer noteGridContainer;
    [SerializeField] private ObstacleGridContainer obstacleGridContainer;
    [SerializeField] private ArcGridContainer arcGridContainer;
    [SerializeField] private ChainGridContainer chainGridContainer;
    [SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private MapLoader loader;

    private void Awake() => SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());

    private void Start() => LoadedDifficultySelectController.OnLoadedDifficultyChanged += UpdatePlatformColors;

    private void OnDestroy() => LoadedDifficultySelectController.OnLoadedDifficultyChanged -= UpdatePlatformColors;

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        yield return new WaitUntil(() => context.Atsc.Initialized); // Wait until Start has been called

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
        var platform = context.EnvironmentList.GetEnvironmentOrDefault(envName);

        // if (customPlat)
        //     platform = CustomPlatformsLoader.Instance.LoadPlatform(info.CustomEnvironmentMetadata.Name, platform);

        SceneManager.LoadScene(platform.ID, LoadSceneMode.Additive);
        yield return new WaitUntil(() => SceneManager.GetSceneByName(platform.ID).isLoaded);

        var descriptor = FindAnyObjectByType<EnvironmentDescriptor>();

        PopulateColorsFromMapInfo();
        UpdateObjectContainerColors();

        context.SetEnvironment(descriptor);

        loader.UpdateMapData(BeatSaberSongContainer.Instance.Map);
        loader.HardRefresh();
        OnLevelLoaded?.Invoke();
    }

    public void PopulateColorsFromMapInfo()
    {
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;

        if (infoDifficulty.CustomColorLeft != null)
            context.ColorScheme.LeftNoteColor = infoDifficulty.CustomColorLeft.Value;
        if (infoDifficulty.CustomColorRight != null)
            context.ColorScheme.RightNoteColor = infoDifficulty.CustomColorRight.Value;

        if (infoDifficulty.CustomColorObstacle != null)
            context.ColorScheme.ObstacleColor = infoDifficulty.CustomColorObstacle.Value;

        if (infoDifficulty.CustomEnvColorLeft != null)
            context.ColorScheme.EnvironmentLeftColor =
                infoDifficulty.CustomEnvColorLeft.Value;
        if (infoDifficulty.CustomEnvColorRight != null)
            context.ColorScheme.EnvironmentRightColor =
                infoDifficulty.CustomEnvColorRight.Value;
        if (infoDifficulty.CustomEnvColorWhite != null)
            context.ColorScheme.EnvironmentWhiteColor =
                infoDifficulty.CustomEnvColorWhite.Value;

        if (infoDifficulty.CustomEnvColorBoostLeft != null)
            context.ColorScheme.EnvironmentLeftBoostColor =
                infoDifficulty.CustomEnvColorBoostLeft.Value;
        if (infoDifficulty.CustomEnvColorBoostRight != null)
            context.ColorScheme.EnvironmentRightBoostColor =
                infoDifficulty.CustomEnvColorBoostRight.Value;
        if (infoDifficulty.CustomEnvColorBoostWhite != null)
            context.ColorScheme.EnvironmentWhiteBoostColor =
                infoDifficulty.CustomEnvColorBoostWhite.Value;
    }

    private void UpdateObjectContainerColors()
    {
        var leftNoteColor = context.ColorScheme.LeftNoteColor;
        var rightNoteColor = context.ColorScheme.RightNoteColor;
        noteGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        arcGridContainer.UpdateColor(leftNoteColor, rightNoteColor);
        chainGridContainer.UpdateColor(leftNoteColor, rightNoteColor);

        obstacleGridContainer.UpdateColor(context.ColorScheme.ObstacleColor);

        eventGridContainer.UpdateColor(
            context.ColorScheme.EnvironmentLeftColor,
            context.ColorScheme.EnvironmentLeftBoostColor,
            context.ColorScheme.EnvironmentRightColor,
            context.ColorScheme.EnvironmentRightBoostColor,
            context.ColorScheme.EnvironmentWhiteColor,
            context.ColorScheme.EnvironmentWhiteBoostColor
        );
    }

    private void UpdatePlatformColors()
    {
        var previousColors = context.ColorScheme.Clone();

        PopulateColorsFromMapInfo();
        UpdateObjectContainerColors();

        // We only want to refresh pools if the colours have changed as refreshing is pretty expensive
        var currentColors = context.ColorScheme;

        var obstacleColorChanged = previousColors.ObstacleColor != currentColors.ObstacleColor;
        if (obstacleColorChanged) obstacleGridContainer.RefreshPool(true);

        var noteColorChanged = previousColors.RightNoteColor != currentColors.RightNoteColor
            || previousColors.LeftNoteColor != currentColors.LeftNoteColor;
        if (noteColorChanged)
        {
            noteGridContainer.RefreshPool(true);
            arcGridContainer.RefreshPool(true);
            chainGridContainer.RefreshPool(true);
        }

        var lightColorChanged = previousColors.EnvironmentRightColor != currentColors.EnvironmentRightColor
            || previousColors.EnvironmentLeftColor != currentColors.EnvironmentLeftColor
            || previousColors.EnvironmentWhiteColor != currentColors.EnvironmentWhiteColor
            || previousColors.EnvironmentRightBoostColor != currentColors.EnvironmentRightBoostColor
            || previousColors.EnvironmentLeftBoostColor != currentColors.EnvironmentLeftBoostColor
            || previousColors.EnvironmentWhiteBoostColor != currentColors.EnvironmentWhiteBoostColor;
        if (lightColorChanged) eventGridContainer.RefreshPool(true);

        context.NotifyColorScheme();
    }
}
