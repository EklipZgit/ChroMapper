using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class GLSEventGridContainer : BeatmapObjectContainerCollection<BaseEventBoxGroup>,
                                     CMInput.IGLSGroupPlacementActions
{
    public event Action<string> OnGroupChanged;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GLSEventTrack trackPrefab;
    [SerializeField] private Transform targetGrid;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSEventAppearanceSO glsEventAppearance;

    private readonly Stack<GLSEventTrack> reuseTracks = new();
    private readonly Dictionary<int, GLSEventTrack> glsEventTracks = new();
    private readonly Dictionary<string, List<int>> groupToIdList = new();
    private readonly List<string> groupList = new();
    private int currentGroup;

    public override ObjectType ContainerType => ObjectType.GLSGroup;

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.OnTracksDefinitionChanged += HandleTracksDefinitionChanged;
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.OnTracksDefinitionChanged -= HandleTracksDefinitionChanged;
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
    }

    private void HandleTracksDefinitionChanged(TracksDefinitionSO tracksDefinition)
    {
        foreach (var t in glsEventTracks.Values)
        {
            t.GridLane.Hide = true;
            t.GridLane.Controller.DeregisterChild(t.GridLane);
            reuseTracks.Push(t);
        }

        glsEventTracks.Clear();
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
            glsEventTracks.Add(id, track);

            groupToIdList.TryAdd(gls.Group, new());
            groupToIdList[gls.Group].Add(id);

            if (!groupList.Contains(gls.Group)) groupList.Add(gls.Group);
        }

        RefreshGroupTrack();
    }

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    public override ObjectContainer CreateContainer() =>
        GLSEventContainer.SpawnGLSGroup(
            null,
            BeatmapContext.TracksDefinition,
            ref eventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var e = obj as BaseEventBoxGroup;
        con.transform.localScale = Vector3.one * 0.75f;
        con.MpbController.Mpb.SetColor("_Color", Color.gray);
        con.MpbController.ApplyChanges();
        con.transform.SetParent(
            glsEventTracks.TryGetValue(e.ID, out var track) ? track.Track.ObjectParentTransform : TargetTransform,
            false);
        con.UpdateGridPosition();
        glsEventAppearance.SetAppearance(
            con as GLSEventContainer,
            true,
            eventGridContainer.AllBoostEvents.FindLast(x => x.JsonTime <= obj.JsonTime)?.Value == 1);
    }

    public void OnNextGroup(InputAction.CallbackContext context)
    {
        if (!context.performed || !EditContext.EditingMode.HasFlag(EditingMode.GLS)) return;
        currentGroup++;
        currentGroup %= groupList.Count;
        RefreshGroupTrack();
    }

    private void RefreshGroupTrack()
    {
        foreach (var track in glsEventTracks.Values) track.GridLane.Hide = true;
        var group = groupList[currentGroup];
        if (!groupToIdList.TryGetValue(group, out var idList)) return;
        var count = -idList.Count / 2;
        foreach (var i in idList)
        {
            glsEventTracks[i].GridLane.Order = count++;
            glsEventTracks[i].GridLane.Hide = false;
        }

        OnGroupChanged?.Invoke(group);
    }
}
