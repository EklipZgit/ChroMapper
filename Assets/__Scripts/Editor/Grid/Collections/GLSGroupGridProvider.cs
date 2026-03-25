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

    [Header("Prefab")] [SerializeField] private GLSGroupTrack trackPrefab;
    [SerializeField] private Transform targetGrid;

    public readonly List<GLSGroupTrack> ActiveGlsTracks = new();
    public readonly Dictionary<int, GLSGroupTrack> IdToTracks = new();
    public readonly Dictionary<string, List<int>> GroupNameToIdList = new();
    public readonly List<string> GroupNameList = new();
    public int CurrentGroupIdx;

    private readonly Stack<GLSGroupTrack> reuseTracks = new();

    private void Start() => beatmapContext.OnTracksDefinitionChanged += HandleTracksDefinitionChanged;
    private void OnDestroy() => beatmapContext.OnTracksDefinitionChanged -= HandleTracksDefinitionChanged;

    private void HandleTracksDefinitionChanged(TracksDefinitionSO tracksDefinition)
    {
        foreach (var t in IdToTracks.Values)
        {
            t.GridLane.Hide = true;
            t.GridLane.Controller.DeregisterChild(t.GridLane);
            reuseTracks.Push(t);
        }

        IdToTracks.Clear();
        GroupNameToIdList.Clear();
        GroupNameList.Clear();
        CurrentGroupIdx = 0;

        foreach (var (id, gls) in tracksDefinition.Gls)
        {
            if (!reuseTracks.TryPop(out var glsTrack)) glsTrack = Instantiate(trackPrefab, targetGrid);
            if (!atsc.otherTracks.Contains(glsTrack.Track)) atsc.otherTracks.Add(glsTrack.Track);

            glsTrack.TrackDefinition = gls;
            glsTrack.SetText(gls);
            glsTrack.GridLane.Controller.RegisterChild(glsTrack.GridLane);
            IdToTracks.Add(id, glsTrack);

            GroupNameToIdList.TryAdd(gls.Group, new());
            GroupNameToIdList[gls.Group].Add(id);

            if (!GroupNameList.Contains(gls.Group)) GroupNameList.Add(gls.Group);
        }

        RefreshGroupTrack();
    }

    public void OnNextGroup(InputAction.CallbackContext context)
    {
        if (!context.performed || !editContext.EditingMode.HasFlag(EditingMode.GLS) || GroupNameList.Count == 0) return;
        CurrentGroupIdx++;
        CurrentGroupIdx %= GroupNameList.Count;
        RefreshGroupTrack();
    }

    private void RefreshGroupTrack()
    {
        if (GroupNameList.Count == 0) return;
        foreach (var track in IdToTracks.Values) track.GridLane.Hide = true;
        ActiveGlsTracks.Clear();

        var group = GroupNameList[CurrentGroupIdx];
        if (!GroupNameToIdList.TryGetValue(group, out var idList)) return;

        // TODO: make ordering closest to centered given the lane
        var order = -idList.Count / 2;
        foreach (var i in idList)
        {
            if (order == 0 && idList.Count % 2 == 0) order++; // even, skip center
            IdToTracks[i].GridLane.Order = order++;
            IdToTracks[i].GridLane.Hide = false;
            ActiveGlsTracks.Add(IdToTracks[i]);
        }

        OnGroupChanged?.Invoke(group);
    }
}
