using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Graphics/VisualSettings")]
// TODO: expand this to be proper model selection
public class VisualSettingsSO : ScriptableObject
{
    public VisualRepositorySO Repository;

    [Header("Default Models")] public VisualModelSO DefaultBlock;
    public VisualModelSO DefaultEvent;
    public VisualModelSO DefaultNote;
    public VisualModelSO DefaultBomb;
    public VisualModelSO DefaultChainHead;
    public VisualModelSO DefaultChainLink;

    public event Action OnBlockModelChanged;
    public event Action OnEventModelChanged;
    public event Action OnNoteModelChanged;
    public event Action OnBombModelChanged;
    public event Action OnChainHeadModelChanged;
    public event Action OnChainLinkModelChanged;

    public void OnEnable()
    {
    #if UNITY_EDITOR
        if (!Application.isPlaying) return;
    #endif
        Settings.NotifyBySettingName("EventModels", HandleBlockModelChanged);
        Settings.NotifyBySettingName("EventModels", HandleEventModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleNoteModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleBombModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleChainHeadModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleChainLinkModelChanged);
        Settings.NotifyBySettingName("NoteModels", HandleChainLinkModelChanged);
        CacheEventModelName();
    }

    public void OnDisable()
    {
    #if UNITY_EDITOR
        if (!Application.isPlaying) return;
    #endif
        Settings.ClearSettingNotifications("NoteModels");
        Settings.ClearSettingNotifications("EventModels");
    }

    private string cachedEventModelName;

    private void CacheEventModelName() =>
        cachedEventModelName = "CM_Event_" + Settings.Instance.EventModels.Replace(' ', '_');

    private void HandleBlockModelChanged(object _) => OnBlockModelChanged?.Invoke();

    private void HandleEventModelChanged(object _)
    {
        CacheEventModelName();
        OnEventModelChanged?.Invoke();
    }

    private void HandleNoteModelChanged(object _) => OnNoteModelChanged?.Invoke();
    private void HandleBombModelChanged(object _) => OnBombModelChanged?.Invoke();
    private void HandleChainHeadModelChanged(object _) => OnChainHeadModelChanged?.Invoke();
    private void HandleChainLinkModelChanged(object _) => OnChainLinkModelChanged?.Invoke();

    public VisualModelSO GetBlockModel() => Repository.ModelsByName.GetValueOrDefault("CM_Block", DefaultBlock);

    public VisualModelSO GetEventBlockModel() =>
        Repository.ModelsByName.GetValueOrDefault("CM_Event_Block", DefaultBlock);

    public VisualModelSO GetEventModel() =>
        Repository.ModelsByName.GetValueOrDefault(
            cachedEventModelName,
            DefaultEvent);

    public VisualModelSO GetNoteModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteLeft
            : DefaultNote;

    public VisualModelSO GetNoteLeftModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteLeft
            : DefaultNote;

    public VisualModelSO GetNoteRightModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteRight
            : DefaultNote;

    public VisualModelSO GetNoteDotLeftModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteDotLeft
            : DefaultNote;

    public VisualModelSO GetNoteDotRightModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteDotRight
            : DefaultNote;

    public VisualModelSO GetBombModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.NoteBomb
            : DefaultBomb;

    public VisualModelSO GetBurstSliderLeftModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderLeft
            : DefaultChainLink;

    public VisualModelSO GetBurstSliderRightModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderRight
            : DefaultChainLink;

    public VisualModelSO GetBurstSliderHeadLeftModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadLeft
            : DefaultChainHead;

    public VisualModelSO GetBurstSliderHeadRightModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadRight
            : DefaultChainHead;

    public VisualModelSO GetBurstSliderHeadDotLeftModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadDotLeft
            : DefaultChainHead;

    public VisualModelSO GetBurstSliderHeadDotRightModel() =>
        Repository.NoteModelsByName.TryGetValue(
            Settings.Instance.NoteModels,
            out var val)
            ? val.BurstSliderHeadDotRight
            : DefaultChainHead;
}
