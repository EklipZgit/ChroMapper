using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GLSGroupGridProvider : MonoBehaviour, CMInput.IGLSGroupTabsActions, IEditorStateProvider
{
    public event Action<string> OnGroupPageChanged;

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
    public string CurrentGroup;
    public int CurrentGroupIdx;

    // Keep the selected GLS group page with the provider that owns its track visibility.
    public string StateKey => "glsGroupPage";

    private readonly Stack<GLSGroupTrack> reuseTracks = new();

    private void Start()
    {
        beatmapContext.OnTracksDefinitionChanged += HandleTracksDefinitionChanged;
        EditorStateService.Register(this);
    }

    private void OnDestroy()
    {
        EditorStateService.Unregister(this);
        beatmapContext.OnTracksDefinitionChanged -= HandleTracksDefinitionChanged;
    }

    // Save the group currently shown in the GLS lane grid.
    public void CaptureEditorState(SimpleJSON.JSONObject data) => data["group"] = CurrentGroup;

    // Restore only after tracks have populated; an empty pre-refresh provider is retried by map-load dispatch.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (data.HasKey("group"))
        {
            SetGroupPage(data["group"].Value);
        }
    }

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
        CurrentGroup = "";

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

        RefreshGroupPageTrack();
    }

    public void OnNextGroup(InputAction.CallbackContext context)
    {
        if (!context.performed || !editContext.EditingMode.HasFlag(EditingMode.GLS) || GroupNameList.Count == 0) return;
        CurrentGroupIdx++;
        CurrentGroupIdx %= GroupNameList.Count;
        RefreshGroupPageTrack();
    }

    public void OnPreviousGroup(InputAction.CallbackContext context)
    {
        if (!context.performed || !editContext.EditingMode.HasFlag(EditingMode.GLS) || GroupNameList.Count == 0) return;
        CurrentGroupIdx--;
        if (CurrentGroupIdx < 0) CurrentGroupIdx = GroupNameList.Count - 1;
        RefreshGroupPageTrack();
    }

    public void SetGroupPage(string groupPage)
    {
        var i = GroupNameList.FindIndex(g => g == groupPage);
        if (i == -1) return;
        CurrentGroupIdx = i;
        RefreshGroupPageTrack();
    }

    private void RefreshGroupPageTrack()
    {
        if (GroupNameList.Count == 0) return;
        foreach (var track in IdToTracks.Values) track.GridLane.Hide = true;
        ActiveGlsTracks.Clear();

        CurrentGroup = GroupNameList[CurrentGroupIdx];
        if (!GroupNameToIdList.TryGetValue(CurrentGroup, out var idList)) return;

        // TODO: make ordering closest to centered given the lane
        var order = -idList.Count / 2;
        foreach (var i in idList)
        {
            if (order == 0 && idList.Count % 2 == 0) order++; // even, skip center
            IdToTracks[i].GridLane.Order = order++;
            IdToTracks[i].GridLane.Hide = false;
            ActiveGlsTracks.Add(IdToTracks[i]);
        }

        OnGroupPageChanged?.Invoke(CurrentGroup);
    }
}
