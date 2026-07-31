using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomNotes;
using UnityEngine;

public class CustomNotesLoader : MonoBehaviour
{
    public VisualRepositorySO Repository;

    private readonly Dictionary<string, string> customNotePaths = new();
    private string loadedCustomNote;
    private string loadingCustomNote;
    private int loadVersion;
    private bool destroyed;
    public static CustomNotesLoader Instance { get; private set; }

    public void Awake()
    {
        Instance = this;
        Refresh();
        Settings.NotifyBySettingName("NoteModels", HandleSelectionChanged);
    }

    public void Start() => LoadSelectedCustomNote();

    public void OnDestroy()
    {
        destroyed = true;
        if (Instance == this) Instance = null;
        Settings.StopNotifyingBySettingName("NoteModels", HandleSelectionChanged);
        loadVersion++;
        if (loadedCustomNote == null) return;

        var model = Repository.RemoveNoteModel(loadedCustomNote);
        if (model == null) return;
        UnloadImmediately(model);
    }

    public void Refresh()
    {
        customNotePaths.Clear();
        var customNotePath = Path.Combine(Settings.Instance.BeatSaberInstallation, "CustomNotes");
        if (Directory.Exists(customNotePath))
        {
            foreach (var filePath in Directory
                .EnumerateFiles(customNotePath, "*", SearchOption.TopDirectoryOnly)
                .Where(IsCustomNoteFile)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                customNotePaths[Path.GetFileName(filePath)] = filePath;
        }

        Repository.SetAvailableCustomNoteModels(customNotePaths.Keys);
        if (loadingCustomNote != null && !customNotePaths.ContainsKey(loadingCustomNote))
        {
            loadVersion++;
            loadingCustomNote = null;
        }

        if (loadedCustomNote != null && !customNotePaths.ContainsKey(loadedCustomNote))
        {
            loadVersion++;
            UnloadCurrentCustomNote();
        }

        LoadSelectedCustomNote();
    }

    public void RetrySelected()
    {
        var selected = Settings.Instance.NoteModels;
        if (!customNotePaths.ContainsKey(selected)) return;

        loadVersion++;
        UnloadCurrentCustomNote();
        loadingCustomNote = selected;
        PersistentUI.Instance.StartCoroutine(LoadAsync(selected, customNotePaths[selected], loadVersion));
    }

    private void HandleSelectionChanged(object _) => LoadSelectedCustomNote();

    private void LoadSelectedCustomNote()
    {
        var selected = Settings.Instance.NoteModels;
        if (selected == loadedCustomNote || selected == loadingCustomNote) return;

        loadVersion++;
        loadingCustomNote = null;
        UnloadCurrentCustomNote();
        if (!customNotePaths.TryGetValue(selected, out var filePath)) return;

        loadingCustomNote = selected;
        PersistentUI.Instance.StartCoroutine(LoadAsync(selected, filePath, loadVersion));
    }

    private IEnumerator LoadAsync(string selectionName, string filePath, int version)
    {
        AssetBundle assetBundle = null;
        GameObject prefab = null;
        string loadFailure = null;
        yield return AssetBundleUtils.LoadAssetFromFileAsync<GameObject>(
            filePath,
            "assets/_customnote.prefab",
            (bundle, asset) => (assetBundle, prefab) = (bundle, asset),
            reason => loadFailure = reason,
            typeof(NoteDescriptor),
            typeof(DisableNoteColorOnGameobject));

        if (loadFailure != null)
        {
            LogFailure(filePath, loadFailure);
            ClearLoading(selectionName, version);
            yield break;
        }

        if (destroyed || version != loadVersion || Settings.Instance.NoteModels != selectionName)
        {
            yield return AssetBundleUtils.UnloadAsync(assetBundle);
            yield break;
        }

        NoteModelSO model = null;
        try
        {
            if (!NoteModelSO.TryCreate(
                prefab,
                assetBundle.name,
                selectionName,
                out model,
                out var failureReason))
                LogFailure(filePath, failureReason);
        }
        catch (Exception exception)
        {
            LogFailure(filePath, $"the custom note could not be prepared ({exception.GetType().Name})");
        }

        if (model == null)
        {
            yield return AssetBundleUtils.UnloadAsync(assetBundle);
            ClearLoading(selectionName, version);
            yield break;
        }

        model.AssetBundle = assetBundle;
        loadedCustomNote = selectionName;
        loadingCustomNote = null;
        Repository.Add(model);
    }

    private void UnloadCurrentCustomNote()
    {
        if (loadedCustomNote == null) return;

        var model = Repository.RemoveNoteModel(loadedCustomNote);
        loadedCustomNote = null;
        if (model != null) PersistentUI.Instance.StartCoroutine(UnloadAsync(model));
    }

    private static IEnumerator UnloadAsync(NoteModelSO model)
    {
        yield return null;
        var assetBundle = model.AssetBundle;
        VisualModelController.PurgeCachedModel(model.name);
        model.DisposeRuntimeModel();
        if (assetBundle != null) yield return AssetBundleUtils.UnloadAsync(assetBundle);
    }

    private static void UnloadImmediately(NoteModelSO model)
    {
        var assetBundle = model.AssetBundle;
        VisualModelController.PurgeCachedModel(model.name);
        model.DisposeRuntimeModel();
        if (assetBundle != null) AssetBundleUtils.Unload(assetBundle);
    }

    private static void LogFailure(string filePath, string reason) =>
        Debug.LogWarning($"Unable to load custom note {Path.GetFileName(filePath)}: {reason}.");

    private void ClearLoading(string selectionName, int version)
    {
        if (version == loadVersion && loadingCustomNote == selectionName) loadingCustomNote = null;
    }

    private static bool IsCustomNoteFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".bloq", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".note", StringComparison.OrdinalIgnoreCase);
    }
}
