using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NoteLanesController : MonoBehaviour
{
    [SerializeField] private GridLane gridLane;
    public static int LaneCount = 4;

    private void Start()
    {
        Settings.NotifyBySettingName("NoteLanes", UpdateNoteLanes);
        UpdateNoteLanes(LaneCount);
        if (Settings.NonPersistentSettings.ContainsKey("NoteLanes")) Settings.NonPersistentSettings["NoteLanes"] = 4;
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("NoteLanes");

    public void UpdateNoteLanes(object value)
    {
        var noteLanesText = value.ToString();
        if (!int.TryParse(noteLanesText, out var noteLanes)) return;
        if (noteLanes < 1) return;
        LaneCount = gridLane.Lane = noteLanes;
    }
}
