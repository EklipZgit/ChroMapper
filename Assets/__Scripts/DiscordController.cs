using System;
using Discord;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordController : MonoBehaviour
{
    public static bool IsActive = true;
    public static ImageManager ImageManager = null;
    public static UserManager UserManager = null;

    public ActivityManager ActivityManager;
    public Discord.Discord Discord;

    private Activity activity;

    [SerializeField] private EnvironmentListSO environmentList;
    [SerializeField] private TextAsset clientIDTextAsset;

    // Start is called before the first frame update
    private void Start()
    {
        if (!Settings.Instance.DiscordRPCEnabled)
        {
            IsActive = false;
            return;
        }

        try
        {
            if (long.TryParse(clientIDTextAsset.text, out var discordClientID)
                && Application.internetReachability != NetworkReachability.NotReachable)
            {
                Discord = new Discord.Discord(discordClientID, (ulong)CreateFlags.NoRequireDiscord);
                ImageManager = Discord.GetImageManager();
                UserManager = Discord.GetUserManager();
                ActivityManager = Discord.GetActivityManager();
                ActivityManager.ClearActivity(res => { });
                SceneManager.activeSceneChanged += SceneUpdated;
                LoadedDifficultySelectController.OnLoadedDifficultyChanged += OnLoadedDifficultyChanged;
            }
            else
            {
                HandleException("No internet connection, or invalid Client ID.");
            }
        }
        catch (ResultException result)
        {
            HandleException($"{result.Message} (Perhaps Discord is not open?)");
        }
        catch (DllNotFoundException e)
        {
            HandleException($"{e.Message} Dll missing?");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        try
        {
            if (IsActive) Discord?.RunCallbacks();
        }
        catch (ResultException resultException)
        {
            HandleException(resultException.Message);
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneUpdated;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= OnLoadedDifficultyChanged;
    }

    private void OnApplicationQuit() => Discord?.Dispose();

    private void OnLoadedDifficultyChanged()
    {
        var diff = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        activity.State = $"{diff.Characteristic} {diff.Difficulty}";
        UpdatePresence();
    }

    private void SceneUpdated(Scene from, Scene to)
    {
        var details = "Invalid!";
        var state = "";
        var assets = new ActivityAssets
        {
            SmallImage = "newlogo",
            SmallText = $"ChroMapper v{Application.version}",
            LargeImage = "newlogo_glow",
            LargeText = "In Menus"
        };
        var timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        switch (to.name)
        {
            case "00_FirstBoot":
                details = "Selecting install folder...";
                break;
            case "01_SongSelectMenu":
                details = "Viewing song list.";
                break;
            case "02_SongEditMenu":
                details = BeatSaberSongContainer.Instance.Info.SongName;
                state = "Viewing song info.";
                break;
            case "03_Mapper":
                var songContainer = BeatSaberSongContainer.Instance;

                var info = songContainer.Info;
                var diff = songContainer.MapDifficultyInfo;
                var envName = EnvironmentInfoHelper.GetCurrentEnvironment();

                details = $"Editing {info.SongName}";
                state = $"{diff.Characteristic} {diff.Difficulty}";

                // i hate discord for enforcing lowercase image keys
                assets.LargeImage = envName.ToLower();
                assets.LargeText = environmentList.GetEnvironmentOrDefault(envName).Name;
                break;
            case "04_Options":
                details = "Editing ChroMapper options";
                break;
        }

        activity = new Activity
        {
            Details = details,
            State = state,
            Timestamps = new() { Start = timestamp },
            Assets = assets
        };

        UpdatePresence();
    }

    private void UpdatePresence()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable) return;

        ActivityManager?.UpdateActivity(
            activity,
            res =>
            {
                if (res == Result.Ok)
                    Debug.Log("Discord Presence updated!");
                else
                    Debug.LogWarning($"Discord Presence failed! {res}");
            });
    }

    private void HandleException(string msg)
    {
        PersistentUI.Instance.ShowDialogBox(
            "PersistentUI",
            "discord.error",
            null,
            PersistentUI.DialogBoxPresetType.Ok,
            new object[] { msg });
        IsActive = false;
    }
}
