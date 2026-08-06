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

        var span = MapObjects.AsSpan();

        foreach (var ev in span) AddCustomEvent(ev);
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

            if (ev.Type == "AnimateTrack") tracksManager.GetAnimationTrack(track).RemoveEvent(ev);
        }
    }

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

            if (ev.Type == "AnimateTrack") tracksManager.GetAnimationTrack(track).AddEvent(ev);
        }

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
                    at.Track.transform.SetParent(
                        parent.Track.ObjectParentTransform,
                        ev.DataWorldPositionStays ?? false);
                    if (at.Animator == null)
                    {
                        at.Animator = at.gameObject.AddComponent<ObjectAnimator>();
                        at.Animator.Context = BeatmapContext;
                        at.Animator.AttachToTrack(at.Track, tr.Value);
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
                playerCamera.gameObject.SetActive(true);
                var track = tracksManager.GetAnimationTrack(ev.CustomTrack);
                playerCamera.AddPlayerTrack(ev.JsonTime, track);
                break;
        }
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
