using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NoteLanesController : MonoBehaviour
{
    [SerializeField] private GridLane gridLane;

    private void Start()
    {
        Settings.NotifyBySettingName("NoteLanes", UpdateNoteLanes);
        UpdateNoteLanes(4);
        if (Settings.NonPersistentSettings.ContainsKey("NoteLanes")) Settings.NonPersistentSettings["NoteLanes"] = 4;
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("NoteLanes");

    public void UpdateNoteLanes(object value)
    {
        var noteLanesText = value.ToString();
        if (!int.TryParse(noteLanesText, out var noteLanes)) return;
        if (noteLanes < 1) return;
        gridLane.Size = noteLanes;
    }
}
