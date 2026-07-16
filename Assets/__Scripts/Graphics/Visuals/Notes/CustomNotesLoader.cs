using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CustomNotesLoader : MonoBehaviour
{
    public VisualRepositorySO Repository;

    private readonly Dictionary<string, string> customNotePaths = new();
    private string loadedCustomNote;
    private string loadingCustomNote;
    private int loadVersion;
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
        if (Instance == this) Instance = null;
        Settings.StopNotifyingBySettingName("NoteModels", HandleSelectionChanged);
        loadVersion++;
        if (loadedCustomNote == null) return;

        var model = Repository.RemoveNoteModel(loadedCustomNote);
        if (model == null) return;
        var assetBundle = model.AssetBundle;
        VisualModelController.PurgeCachedModel(model.name);
        model.DisposeRuntimeModel();
        assetBundle?.Unload(true);
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
        StartCoroutine(LoadAsync(selected, customNotePaths[selected], loadVersion));
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
        StartCoroutine(LoadAsync(selected, filePath, loadVersion));
    }

    private IEnumerator LoadAsync(string selectionName, string filePath, int version)
    {
        AssetBundleCreateRequest bundleRequest;
        try
        {
            bundleRequest = AssetBundle.LoadFromFileAsync(filePath);
        }
        catch (Exception exception)
        {
            LogFailure(filePath, $"Unity could not start loading the bundle ({exception.GetType().Name})");
            ClearLoading(selectionName, version);
            yield break;
        }

        yield return bundleRequest;
        var assetBundle = bundleRequest.assetBundle;
        if (assetBundle == null)
        {
            LogFailure(filePath, "Unity could not read the AssetBundle");
            ClearLoading(selectionName, version);
            yield break;
        }

        AssetBundleRequest assetRequest = null;
        try
        {
            assetRequest = assetBundle.LoadAssetAsync<GameObject>("assets/_customnote.prefab");
        }
        catch (Exception exception)
        {
            LogFailure(filePath, $"Unity could not start loading the custom note prefab ({exception.GetType().Name})");
        }

        if (assetRequest == null)
        {
            yield return assetBundle.UnloadAsync(true);
            ClearLoading(selectionName, version);
            yield break;
        }

        yield return assetRequest;
        NoteModelSO model = null;
        try
        {
            if (!NoteModelSO.TryCreate(
                assetRequest.asset as GameObject,
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
            yield return assetBundle.UnloadAsync(true);
            ClearLoading(selectionName, version);
            yield break;
        }

        if (version != loadVersion || Settings.Instance.NoteModels != selectionName)
        {
            model.DisposeRuntimeModel();
            yield return assetBundle.UnloadAsync(true);
            yield break;
        }

        model.AssetBundle = assetBundle;
        model.FileName = filePath;
        loadedCustomNote = selectionName;
        loadingCustomNote = null;
        Repository.Add(model);
    }

    private void UnloadCurrentCustomNote()
    {
        if (loadedCustomNote == null) return;

        var model = Repository.RemoveNoteModel(loadedCustomNote);
        loadedCustomNote = null;
        if (model != null) StartCoroutine(UnloadAsync(model));
    }

    private static IEnumerator UnloadAsync(NoteModelSO model)
    {
        yield return null;
        var assetBundle = model.AssetBundle;
        VisualModelController.PurgeCachedModel(model.name);
        model.DisposeRuntimeModel();
        if (assetBundle != null) yield return assetBundle.UnloadAsync(true);
    }

    private static void LogFailure(string filePath, string reason) =>
        Debug.LogWarning($"Unable to load custom note {Path.GetFileName(filePath)}: {reason}.");

    private void ClearLoading(string selectionName, int version)
    {
        if (version == loadVersion && loadingCustomNote == selectionName) loadingCustomNote = null;
    }

    internal static bool IsCustomNoteFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".bloq", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".note", StringComparison.OrdinalIgnoreCase);
    }
}
