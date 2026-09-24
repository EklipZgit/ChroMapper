using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomEventGridContainer : BeatmapObjectContainerCollection<BaseCustomEvent>,
                                        CMInput.ICustomEventsContainerActions
{
    [SerializeField] private GameObject customEventPrefab;
    [SerializeField] private TextMeshProUGUI customEventLabelPrefab;
    [SerializeField] private Transform customEventLabelTransform;
    [SerializeField] private GridChild gridChild;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CameraController playerCamera;
    private List<string> customEventTypes = new();
    public override ObjectType ContainerType => ObjectType.CustomEvent;

    public ReadOnlyCollection<string> CustomEventTypes => customEventTypes.AsReadOnly();

    public Dictionary<string, List<BaseCustomEvent>> EventsByTrack;
    // The V2 binding timeline can include repeated assignments to one track; retain each
    // animator once so a map reload or edit can replace the old controller cleanly.
    private readonly List<FogAnimator> legacyFogAnimators = new();

    private void Start()
    {
        RefreshTrack();
        if (!Settings.Instance.AdvancedShit)
        {
            Debug.LogWarning("Disabling some objects since an Advanced setting is not enabled...");
            gridChild.Hide = true;
        }
    }

    public void LoadAll()
    {
        EventsByTrack = new Dictionary<string, List<BaseCustomEvent>>();
        // AssignPlayerToTrack bindings below rebuild from this map's events; the camera rig persists across
        // in-place map swaps and same-environment difficulty switches, so drop the previous map's list and
        // live binding or the first Update either rebinds nothing or indexes a stale accumulated list.
        playerCamera.ClearPlayerTracks();
        trackGetSw.Reset(); addEventSw.Reset(); parentSw.Reset(); componentSw.Reset();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var span = MapObjects.AsSpan();

        foreach (var ev in span) AddCustomEvent(ev);
        // BloomFogChromaParityAuditTest.LegacyFogBindingFollowsAssignmentCallbacksAndTrackSwitches:
        // bind V2 fog after all events are parsed, preserving callback times and file order.
        RebuildLegacyFogBinding();
        Debug.Log(
            $"[Perf] LoadAll detail: total={sw.ElapsedMilliseconds}ms trackGet={trackGetSw.ElapsedMilliseconds}ms " +
            $"addEvent={addEventSw.ElapsedMilliseconds}ms parent={parentSw.ElapsedMilliseconds}ms component={componentSw.ElapsedMilliseconds}ms events={span.Length}");
    }

    public void OnAssignObjectstoTrack(InputAction.CallbackContext context)
    {
        if (EditContext.EditingMode.HasFlag(ViewableMode)
            && Settings.Instance.AdvancedShit
            && context.performed
            && !PersistentUI.Instance.InputBoxIsEnabled)
        {
            PersistentUI.Instance.ShowInputBox(
                "Assign the selected objects to a track ID.\n\n" + "If you dont know what you're doing, turn back now.",
                HandleTrackAssign);
        }
    }

    public void OnSetTrackFilter(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
            SetTrackFilter();
    }

    public void OnCreateNewEventType(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
            CreateNewType();
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        var customEvent = obj as BaseCustomEvent;
        if (!customEventTypes.Contains(customEvent.Type))
        {
            customEventTypes.Add(customEvent.Type);
            RefreshTrack();
        }

        AddCustomEvent(customEvent);
        // Rebuild the V2 callback timeline after an editor insertion changes assignment order.
        if (Settings.Instance.MapVersion == 2) RebuildLegacyFogBinding();
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        var ev = obj as BaseCustomEvent;

        var tracks = ev.CustomTrack switch
        {
            JSONString s => new List<string> { s },
            JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
            _ => new List<string>()
        };

        foreach (var track in tracks)
        {
            EventsByTrack[track].Remove(ev);
            if (EventsByTrack[track].Count == 0)
            {
                EventsByTrack.Remove(track);
            }

            // HeliovMapParityTest.ValleyAndMountainFogFollowAuthoredTrack: a removed V2 fog
            // AnimateTrack must stop writing its old fog points as well as its object transforms.
            if (ev.Type == "AnimateTrack")
            {
                tracksManager.GetAnimationTrack(track).RemoveEvent(ev);
                if (Settings.Instance.MapVersion == 2 && HasLegacyFogProperty(ev))
                {
                    GetFogAnimator(track).RemoveEvent(ev);
                }
            }

            // BloomFogChromaParityAuditTest.LegacyFogBindingFollowsAssignmentCallbacksAndTrackSwitches:
            // deleting an assignment must remove its callback from the seek timeline.
            if (ev.Type == "AssignFogTrack" && Settings.Instance.MapVersion == 2)
                RebuildLegacyFogBinding();

            // FogAnimationTests.AnimateComponentFogEventsDriveBloomFogPreviewSeeks and
            // TubeBloomAnimationTests.AnimateComponentTubeBloomEventsDriveLightMultipliers: deleting an
            // AnimateComponent must stop its point definitions from animating the preview, symmetric with
            // the AnimateTrack removal above.
            if (ev.Type == "AnimateComponent")
            {
                if (ev.Data?.HasKey("BloomFogEnvironment") == true)
                {
                    GetFogAnimator(track).RemoveEvent(ev);
                }

                if (ev.Data?.HasKey("TubeBloomPrePassLight") == true)
                {
                    GetTubeBloomAnimator(track).RemoveEvent(ev);
                }
            }
        }
    }

    private static readonly System.Diagnostics.Stopwatch trackGetSw = new();
    private static readonly System.Diagnostics.Stopwatch addEventSw = new();
    private static readonly System.Diagnostics.Stopwatch parentSw = new();
    private static readonly System.Diagnostics.Stopwatch componentSw = new();

    private void AddCustomEvent(BaseCustomEvent ev)
    {
        var tracks = ev.CustomTrack switch
        {
            JSONString s => new List<string> { s },
            JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
            _ => new List<string>()
        };

        foreach (var track in tracks)
        {
            if (!EventsByTrack.ContainsKey(track))
            {
                EventsByTrack[track] = new List<BaseCustomEvent>();
            }

            EventsByTrack[track].Add(ev);

            if (ev.Type == "AnimateTrack")
            {
                trackGetSw.Start();
                var at = tracksManager.GetAnimationTrack(track);
                trackGetSw.Stop();
                addEventSw.Start();
                at.AddEvent(ev);
                addEventSw.Stop();

                // HeliovMapParityTest.CliffSceneAtFirstNotesUsesFogTrackAndEnvironment: the
                // map uses V2 AnimateTrack fog fields rather than V3 AnimateComponent. Parsing
                // before AssignFogTrack is safe because the fog writer stays dormant until bound.
                if (Settings.Instance.MapVersion == 2 && HasLegacyFogProperty(ev))
                {
                    GetFogAnimator(track).AddLegacyEvent(ev);
                }
            }

            // FogAnimationTests.AnimateComponentFogEventsDriveBloomFogPreviewSeeks and
            // TubeBloomAnimationTests.AnimateComponentTubeBloomEventsDriveLightMultipliers: AnimateTrack only
            // drives object transforms, while AnimateComponent drives environment components; each supported
            // component needs its own animator on the named track to reach the preview's state, mirroring
            // Heck's separate AnimateComponent handlers per component.
            if (ev.Type == "AnimateComponent")
            {
                componentSw.Start();
                if (ev.Data?.HasKey("BloomFogEnvironment") == true)
                {
                    GetFogAnimator(track).AddEvent(ev);
                }

                if (ev.Data?.HasKey("TubeBloomPrePassLight") == true)
                {
                    GetTubeBloomAnimator(track).AddEvent(ev);
                }
                componentSw.Stop();
            }
        }

        parentSw.Start();
        switch (ev.Type)
        {
            case "AssignTrackParent":
                if (ev.DataParentTrack == null) return;
                var parent = tracksManager.GetAnimationTrack(ev.DataParentTrack);
                var children = ev.DataChildrenTracks switch
                {
                    JSONArray arr => arr,
                    JSONString s => JSONObject.Parse($"[{s}]").AsArray,
                    _ => new JSONArray(),
                };
                foreach (var tr in children)
                {
                    var at = tracksManager.GetAnimationTrack(tr.Value);
                    at.ParentWorldPositionStays = ev.DataWorldPositionStays ?? false;
                    at.Track.transform.SetParent(
                        parent.Track.ObjectParentTransform,
                        ev.DataWorldPositionStays ?? false);
                    if (at.Animator == null)
                    {
                        at.Animator = at.gameObject.AddComponent<ObjectAnimator>();
                        at.Animator.Context = BeatmapContext;
                        at.Animator.AttachToTrack(at.Track, tr.Value);
                    }

                    // WorldCavesInEnvironmentTest's enhanced constructs never rode their parent tracks: only the
                    // child track moved while the matched scene objects stayed at their vanilla positions. In game
                    // Noodle's ParentObject physically parents every child-track object under the animated parent,
                    // so already-attached direct environment targets must ride the child track's object parent too.
                    // Load-time attachments are covered by ObjectAnimator.AttachToEnvironmentObject because
                    // environment enhancements spawn after custom events load.
                    foreach (var child in at.Children)
                    {
                        child.ParentDirectTargetToTrack(at.Track.ObjectParentTransform, at.ParentWorldPositionStays);
                    }

                    if (!parent.Children.Contains(at.Animator))
                    {
                        parent.Children.Add(at.Animator);
                        at.Parents.Add(parent);
                        at.OnChildrenChanged();
                    }
                }

                break;
            case "AssignPlayerToTrack":
                if (ev.CustomTrack == null) return;
                // AdditionalAnimationParityTest.AssignPlayerToTrackTargetFollowsHeckSemantics: Heck's target
                // picks the rig object the track drives (Root/Head/LeftHand/RightHand, defaulting to Root).
                // The editor preview's player is the camera, which is both root and head; the editor has no
                // VR controllers, so hand targets are logged and skipped instead of silently binding the
                // whole player.
                var playerTarget = ev.Data?.HasKey("target") == true ? (string)ev.Data["target"] : "Root";
                if (playerTarget is not ("Root" or "Head"))
                {
                    Debug.LogWarning(
                        $"AssignPlayerToTrack target [{playerTarget}] has no editor preview representation; the event was skipped.");
                    return;
                }

                playerCamera.gameObject.SetActive(true);
                var track = tracksManager.GetAnimationTrack(ev.CustomTrack);
                playerCamera.AddPlayerTrack(ev.JsonTime, track);
                break;
        }
        parentSw.Stop();
    }

    // FogAnimationTests.AnimateComponentFogEventsDriveBloomFogPreviewSeeks: the fog animator rides the same
    // named-track GameObject TracksManager hands out for AnimateTrack (so environment enhancements binding
    // objects to the track and the fog events share one track), created lazily per track and initialized
    // once with the live time controller and runtime context.
    private FogAnimator GetFogAnimator(string track)
    {
        var fog = tracksManager.GetAnimationTrack(track).gameObject.GetOrAddComponent<FogAnimator>();
        if (fog.Atsc == null)
        {
            fog.Atsc = BeatmapContext.Atsc;
            fog.Context = BeatmapContext;
            // TrackScrubParityTest: subscribe stopped-time seeks so they land on the as-if-played fog
            // state immediately instead of one frame late.
            BeatmapContext.Atsc.OnTimeChangedEarly += fog.PushOnStoppedTimeChanged;
        }

        return fog;
    }

    // BloomFogChromaParityAuditTest.LegacyFogBindingFollowsAssignmentCallbacksAndTrackSwitches:
    // one sorted assignment timeline chooses the active V2 fog track, rather than enabling
    // every track ever assigned in the file while the map is loading.
    private void RebuildLegacyFogBinding()
    {
        foreach (var animator in legacyFogAnimators) animator.SetLegacyBinding(null, false);
        legacyFogAnimators.Clear();
        if (Settings.Instance.MapVersion != 2) return;

        var binding = new LegacyFogBinding(BeatmapContext);
        FogAnimator controller = null;
        foreach (var ev in MapObjects)
        {
            if (ev.Type != "AssignFogTrack" || ev.CustomTrack is not JSONString track) continue;
            var animator = GetFogAnimator(track.Value);
            binding.Add(ev.JsonTime, animator);
            if (controller == null) controller = animator;
            if (!legacyFogAnimators.Contains(animator)) legacyFogAnimators.Add(animator);
        }

        if (controller == null) return;
        foreach (var animator in legacyFogAnimators)
            animator.SetLegacyBinding(binding, animator == controller);
    }

    // Only these four V2 AnimateTrack keys are Chroma fog parameters. Do not create a fog
    // animator for unrelated track animations among Heliov's thousands of custom events.
    private static bool HasLegacyFogProperty(BaseCustomEvent ev) =>
        ev.Data?.HasKey("_attenuation") == true
        || ev.Data?.HasKey("_offset") == true
        || ev.Data?.HasKey("_height") == true
        || ev.Data?.HasKey("_startY") == true;

    // TubeBloomAnimationTests.AnimateComponentTubeBloomEventsDriveLightMultipliers: the tube-bloom animator
    // rides the same named-track GameObject (the lights it animates are the track's bound objects), created
    // lazily per track and initialized once with the live time controller.
    private TubeBloomAnimator GetTubeBloomAnimator(string track)
    {
        var tubeBloom = tracksManager.GetAnimationTrack(track).gameObject.GetOrAddComponent<TubeBloomAnimator>();
        if (tubeBloom.Atsc == null)
        {
            tubeBloom.Atsc = BeatmapContext.Atsc;
            // TrackScrubParityTest: subscribe stopped-time seeks so they land on the as-if-played light
            // state immediately instead of one frame late.
            BeatmapContext.Atsc.OnTimeChangedEarly += tubeBloom.PushOnStoppedTimeChanged;
        }

        return tubeBloom;
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public override void RefreshPool(bool force)
    {
        if (UIMode.AnimationMode)
        {
            while (ObjectsWithContainers.Count > 0)
            {
                RecycleContainer(ObjectsWithContainers[ObjectsWithContainers.Count - 1], indexInObjectsWithContainers: ObjectsWithContainers.Count - 1); // Clearing list from index 0 made this O(N^2) including N^2/2 position shifts.... clearing from the back to front is O(N) if we dont scan with .Remove
            }
        }
        else
        {
            base.RefreshPool(force);
        }
    }

    private void RefreshTrack()
    {
        if (customEventTypes.Count == 0)
            gridChild.Hide = true;
        else
        {
            gridChild.Hide = false;
            gridChild.Lane = customEventTypes.Count;
        }

        for (var i = 0; i < customEventLabelTransform.childCount; i++)
            Destroy(customEventLabelTransform.GetChild(i).gameObject);
        foreach (var str in customEventTypes)
        {
            var newShit = Instantiate(customEventLabelPrefab.gameObject, customEventLabelTransform)
                .GetComponent<TextMeshProUGUI>();
            newShit.rectTransform.localPosition = new Vector3(customEventTypes.IndexOf(str), 0.25f, 0);
            newShit.text = str;
        }

        foreach (var obj in LoadedContainers.Values) obj.UpdateGridPosition();
    }

    internal override void SubscribeToCallbacks()
    {
        EditorScaleController.OnEditorScaleChanged += HandleEditorScaleChanged;
        LoadInitialMap.OnLevelLoaded += SetInitialTracks;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        EditorScaleController.OnEditorScaleChanged -= HandleEditorScaleChanged;
        LoadInitialMap.OnLevelLoaded -= SetInitialTracks;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    private void SetInitialTracks()
    {
        var span = MapObjects.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            var customEvent = span[i];

            if (!customEventTypes.Contains(customEvent.Type))
            {
                customEventTypes.Add(customEvent.Type);
                RefreshTrack();
            }
        }
    }

    private void HandleEditorScaleChanged(float obj) => RefreshPool(true);

    private void CreateNewType()
    {
        if (PersistentUI.Instance.InputBoxIsEnabled) return;
        PersistentUI.Instance.ShowInputBox(
            "A new custom event type, I see?\n\n"
            + "Custom event types are for the advanced of advanced users. Node Editor and JSON knowledge are required for these babies.\n\n"
            + "If you dont know what these do, or don't have the documentation for them, turn back now.\n\n"
            + "But if you do, what would you like to name this new event type?",
            HandleNewTypeCreation,
            "NewCustomEventType");
    }

    private void HandleNewTypeCreation(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;
        customEventTypes.Add(res);
        customEventTypes = customEventTypes.OrderBy(x => x).ToList();
        RefreshTrack();
    }

    private void HandleTrackAssign(string res)
    {
        if (res is null) return;
        var modified = new List<BaseObject>();
        var value = (res == "")
            ? null
            : res;
        foreach (var obj in SelectionController.SelectedObjects)
        {
            var mod = BeatmapFactory.Clone(obj);
            modified.Add(mod);

            mod.CustomTrack = value;
            mod.WriteCustom();
        }

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedCollectionAction(
                modified,
                SelectionController.SelectedObjects.ToList(),
                $"Assigned track to ({SelectionController.SelectedObjects.Count}) objects."),
            true);
    }

    public override ObjectContainer CreateContainer() =>
        CustomEventContainer.SpawnCustomEvent(null, this, ref customEventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj) =>
        con.transform.localScale = Vector3.one * 0.75f;
}
