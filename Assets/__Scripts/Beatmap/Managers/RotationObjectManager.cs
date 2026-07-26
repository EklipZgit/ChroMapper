using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using ZLinq;

public class RotationObjectManager : BeatmapObjectManager<BaseObject>
{
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private LaneRotationProvider provider;
    [SerializeField] private GridChild gridChild;

    private readonly string[] enabledCharacteristics = { "360Degree", "90Degree", "Lawless" };

    protected override void Awake()
    {
        base.Awake();
        LoadInitialMap.OnLevelLoaded += Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += Refresh;
    }

    protected void Start()
    {
        if (BeatSaberSongContainer.Instance.Map.MajorVersion < 4
            && (BeatSaberSongContainer.Instance.Map.RotationEvents.Count > 0
                || enabledCharacteristics.Contains(BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic)))
        {
            if (Settings.Instance.Reminder_Loading360Levels)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "PersistentUI",
                    "360warning",
                    Handle360LevelReminder,
                    PersistentUI.DialogBoxPresetType.OkIgnore);
            }
        }

        Settings.NotifyBySettingName("Rotation360FollowNote", HandleFollowChanged);
        Settings.NotifyBySettingName("Rotation360FollowBomb", HandleFollowChanged);
        Settings.NotifyBySettingName("Rotation360FollowWall", HandleFollowChanged);
        Settings.NotifyBySettingName("Rotation360FollowArc", HandleFollowChanged);
        Settings.NotifyBySettingName("Rotation360FollowChain", HandleFollowChanged);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= Refresh;
        Context.Atsc.OnTimeChangedEarly -= UpdateTime;

        Settings.ClearSettingNotifications("Rotation360FollowNote");
        Settings.ClearSettingNotifications("Rotation360FollowBomb");
        Settings.ClearSettingNotifications("Rotation360FollowWall");
        Settings.ClearSettingNotifications("Rotation360FollowArc");
        Settings.ClearSettingNotifications("Rotation360FollowChain");
    }

    private void HandleFollowChanged(object _) => Refresh();

    public override void Refresh()
    {
        if (BeatSaberSongContainer.Instance.Map.MajorVersion < 4
            && (BeatSaberSongContainer.Instance.Map.RotationEvents.Count > 0
                || enabledCharacteristics.Contains(BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic)))
            gridChild.Hide = false;
        else
            gridChild.Hide = true;

        Context.Atsc.OnTimeChangedEarly -= UpdateTime;

        provider.Initialize();
        switch (BeatSaberSongContainer.Instance.Map.MajorVersion)
        {
            case >= 4:
                if (Settings.Instance.Rotation360FollowNote)
                {
                    foreach (var o in BeatSaberSongContainer
                        .Instance.Map.Notes.AsValueEnumerable()
                        .Where(x => x.Type != 3))
                        provider.InsertData(o);
                }

                if (Settings.Instance.Rotation360FollowBomb)
                {
                    foreach (var o in BeatSaberSongContainer
                        .Instance.Map.Notes.AsValueEnumerable()
                        .Where(x => x.Type == 3))
                        provider.InsertData(o);
                }

                if (Settings.Instance.Rotation360FollowArc)
                {
                    foreach (var o in BeatSaberSongContainer.Instance.Map.Arcs) provider.InsertData(o);
                }

                if (Settings.Instance.Rotation360FollowChain)
                {
                    foreach (var o in BeatSaberSongContainer.Instance.Map.Chains) provider.InsertData(o);
                }

                if (Settings.Instance.Rotation360FollowWall)
                {
                    foreach (var o in BeatSaberSongContainer.Instance.Map.Obstacles) provider.InsertData(o);
                }

                break;
            case < 4:
                foreach (var o in BeatSaberSongContainer.Instance.Map.RotationEvents) provider.InsertData(o);
                break;
        }

        Context.Atsc.OnTimeChangedEarly += UpdateTime;
    }

    private static void Handle360LevelReminder(int res) => Settings.Instance.Reminder_Loading360Levels = res == 0;

    public override void UpdateTime()
    {
        if (BeatSaberSongContainer.Instance.Map.MajorVersion < 4) tracksManager.RefreshTracks();
        UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(bool isPlaying, float beatTime) => provider.UpdateTime(isPlaying, beatTime);

    private static bool FilterObjectRotation(BaseObject data)
    {
        switch (BeatSaberSongContainer.Instance.Map.MajorVersion)
        {
            case >= 4 when data is BaseGrid:
                if ((!Settings.Instance.Rotation360FollowNote && data is BaseNote note && note.Type != 3)
                    || (!Settings.Instance.Rotation360FollowBomb && data is BaseNote bomb && bomb.Type == 3)
                    || (!Settings.Instance.Rotation360FollowArc && data is BaseArc)
                    || (!Settings.Instance.Rotation360FollowChain && data is BaseChain)
                    || (!Settings.Instance.Rotation360FollowWall && data is BaseObstacle))
                    return false;
                break;
            case >= 4:
            case < 4 when data is not BaseRotationEvent:
                return false;
        }

        return true;
    }

    private static bool FilterObjectRotationPair((BaseObject reference, BaseObject original) data) =>
        FilterObjectRotation(data.reference);

    protected override bool AddData(IEnumerable<BaseObject> data)
    {
        var mark = false;
        foreach (var d in data.AsValueEnumerable().Where(FilterObjectRotation))
        {
            provider.InsertData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseObject reference, BaseObject original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data.AsValueEnumerable().Where(FilterObjectRotationPair))
        {
            provider.RemoveData(reference, original);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseObject> data)
    {
        var mark = false;
        foreach (var d in data.AsValueEnumerable().Where(FilterObjectRotation))
        {
            provider.RemoveData(d, d);
            mark = true;
        }

        return mark;
    }

}
