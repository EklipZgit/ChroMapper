using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class RotationCallbackController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventGridContainer eventGridContainer;
    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;

    private readonly string[] enabledCharacteristics = { "360Degree", "90Degree", "Lawless" };

    public event Action<bool, float> OnRotationChanged; //Natural, degrees
    public bool IsActive { get; private set; }
    public BaseEvent LatestRotationEvent { get; private set; }

    private float rotation;
    public float Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            OnRotationChanged?.Invoke(false, rotation);
        }
    }

    internal void Start()
    {
        var infoDifficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        IsActive = enabledCharacteristics.Contains(infoDifficulty.Characteristic);
        if (IsActive && Settings.Instance.Reminder_Loading360Levels)
        {
            PersistentUI.Instance.ShowDialogBox(
                "PersistentUI",
                "360warning",
                Handle360LevelReminder,
                PersistentUI.DialogBoxPresetType.OkIgnore);
        }

        interfaceCallback.OnEventPassedThreshold += OnEventPassedThreshold;
        atsc.OnPlayToggled += PlayToggle;
        atsc.OnTimeChanged += OnTimeChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void OnDestroy()
    {
        interfaceCallback.OnEventPassedThreshold -= OnEventPassedThreshold;
        atsc.OnPlayToggled -= PlayToggle;
        atsc.OnTimeChanged -= OnTimeChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    private void UpdateRotateTrack(object obj)
    {
        if (Settings.Instance.RotateTrack) return;
        OnRotationChanged?.Invoke(false, 0);
    }

    private void Handle360LevelReminder(int res) => Settings.Instance.Reminder_Loading360Levels = res == 0;

    private void OnTimeChanged()
    {
        if (atsc.IsPlaying) return;
        PlayToggle(false);
    }

    private void PlayToggle(bool isPlaying)
    {
        if (!IsActive) return;
        var jsonTime = atsc.CurrentJsonTime;

        var span = eventGridContainer.AllRotationEvents.AsSpan();
        var result = span.BinarySearchBy(jsonTime, e => e.JsonTime);
        var idx = result >= 0 ? result : ~result;

        // Continue marching forward until JsonTime reaches current time or beyond
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        while (idx < span.Length && span[idx].JsonTime <= jsonTime - epsilon) idx++;

        rotation = 0;

        if (idx > 0)
        {
            for (var i = 0; i < idx; i++) rotation += span[i].Rotation;
            LatestRotationEvent = span[idx - 1];
        }
        else
            LatestRotationEvent = null;

        OnRotationChanged?.Invoke(false, rotation);
    }

    private void OnEventPassedThreshold(bool initial, int index, BaseObject obj)
    {
        if (!IsActive) return;
        if (obj is not BaseEvent e) return;
        if (!e.IsLaneRotationEvent()) return;
        if (e == LatestRotationEvent) return;

        rotation += e.Rotation;
        LatestRotationEvent = e;
        OnRotationChanged?.Invoke(true, rotation);
    }
}
