using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GLSGroupGridProvider : MonoBehaviour, CMInput.IGLSGroupTabsActions
{
    public event Action<string> OnGroupChanged;

    [Header("Dependencies")] [SerializeField]
    private AudioTimeSyncController atsc;

    [SerializeField] private BeatmapRuntimeContext beatmapContext;
    [SerializeField] private EditModeContext editContext;

    [Header("Prefab")] [SerializeField] private GLSEventTrack trackPrefab;
    [SerializeField] private Transform targetGrid;

    private readonly Stack<GLSEventTrack> reuseTracks = new();
    public readonly Dictionary<int, GLSEventTrack> Tracks = new();
    private readonly Dictionary<string, List<int>> groupToIdList = new();
    private readonly List<string> groupList = new();
    private int currentGroup;

    private void Start() => beatmapContext.OnTracksDefinitionChanged += HandleTracksDefinitionChanged;
    private void OnDestroy() => beatmapContext.OnTracksDefinitionChanged -= HandleTracksDefinitionChanged;

    private void HandleTracksDefinitionChanged(TracksDefinitionSO tracksDefinition)
    {
        foreach (var t in Tracks.Values)
        {
            t.GridLane.Hide = true;
            t.GridLane.Controller.DeregisterChild(t.GridLane);
            reuseTracks.Push(t);
        }

        Tracks.Clear();
        groupToIdList.Clear();
        groupList.Clear();
        currentGroup = 0;

        if (tracksDefinition.Gls.Count == 0) return;

        foreach (var (id, gls) in tracksDefinition.Gls)
        {
            var track = reuseTracks.Count == 0 ? Instantiate(trackPrefab, targetGrid) : reuseTracks.Pop();
            if (!atsc.otherTracks.Contains(track.Track)) atsc.otherTracks.Add(track.Track);

            track.TrackDefinition = gls;
            track.SetText(gls);
            track.GridLane.Controller.RegisterChild(track.GridLane);
            Tracks.Add(id, track);

            groupToIdList.TryAdd(gls.Group, new());
            groupToIdList[gls.Group].Add(id);

            if (!groupList.Contains(gls.Group)) groupList.Add(gls.Group);
        }

        RefreshGroupTrack();
    }

    public void OnNextGroup(InputAction.CallbackContext context)
    {
        if (!context.performed || !editContext.EditingMode.HasFlag(EditingMode.GLS)) return;
        currentGroup++;
        currentGroup %= groupList.Count;
        RefreshGroupTrack();
    }

    private void RefreshGroupTrack()
    {
        foreach (var track in Tracks.Values) track.GridLane.Hide = true;
        var group = groupList[currentGroup];
        if (!groupToIdList.TryGetValue(group, out var idList)) return;
        var count = -idList.Count / 2;
        foreach (var i in idList)
        {
            Tracks[i].GridLane.Order = count++;
            Tracks[i].GridLane.Hide = false;
        }

        OnGroupChanged?.Invoke(group);
    }
}
