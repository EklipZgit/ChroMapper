using System.Diagnostics.CodeAnalysis;
using Beatmap.Base;
using UnityEngine;

internal class PluginEventHandler : MonoBehaviour
{
    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;

    private void Awake()
    {
        interfaceCallback.OnEventPassedThreshold += OnEventPassedThreshold;
        interfaceCallback.OnNotePassedThreshold += OnNotePassedThreshold;
    }

    private void OnDestroy()
    {
        interfaceCallback.OnEventPassedThreshold -= OnEventPassedThreshold;
        interfaceCallback.OnNotePassedThreshold -= OnNotePassedThreshold;
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void OnEventPassedThreshold(bool _, int __, BaseObject newlyAdded) =>
        PluginLoader.BroadcastEvent<EventPassedThresholdAttribute, BaseObject>(newlyAdded);

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void OnNotePassedThreshold(bool _, int __, BaseObject newlyAdded) =>
        PluginLoader.BroadcastEvent<NotePassedThresholdAttribute, BaseObject>(newlyAdded);
}
