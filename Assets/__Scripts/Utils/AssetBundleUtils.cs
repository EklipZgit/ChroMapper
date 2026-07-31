using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZLinq;

public static class AssetBundleUtils
{
    public static IEnumerator LoadAssetFromFileAsync<T>(
        string filePath,
        string assetName,
        Action<AssetBundle, T> onLoaded,
        Action<string> onFailed,
        params Type[] supportedTypes) where T : UnityEngine.Object
    {
        if (onLoaded == null) throw new ArgumentNullException(nameof(onLoaded));
        if (onFailed == null) throw new ArgumentNullException(nameof(onFailed));

        AssetBundleCreateRequest bundleRequest;
        try
        {
            bundleRequest = AssetBundle.LoadFromFileAsync(filePath);
        }
        catch (Exception exception)
        {
            onFailed($"Unity could not start loading the bundle ({exception.GetType().Name})");
            yield break;
        }

        yield return bundleRequest;
        var assetBundle = bundleRequest.assetBundle;
        if (assetBundle == null)
        {
            onFailed("Unity could not read the AssetBundle");
            yield break;
        }

        AssetBundleRequest assetRequest = null;
        Exception assetLoadException = null;
        try
        {
            assetRequest = assetBundle.LoadAssetAsync<T>(assetName);
        }
        catch (Exception exception)
        {
            assetLoadException = exception;
        }

        if (assetRequest == null)
        {
            yield return assetBundle.UnloadAsync(true);
            onFailed($"Unity could not start loading the asset ({assetLoadException?.GetType().Name ?? "Unknown"})");
            yield break;
        }

        yield return assetRequest;
        var asset = assetRequest.asset as T;
        if (asset == null)
        {
            yield return assetBundle.UnloadAsync(true);
            onFailed($"the bundle does not contain {assetName}");
            yield break;
        }

        Exception preparationException = null;
        try
        {
            if (asset is GameObject gameObject) SanitizeGameObject(gameObject, supportedTypes);
        }
        catch (Exception exception)
        {
            preparationException = exception;
        }

        if (preparationException != null)
        {
            yield return assetBundle.UnloadAsync(true);
            onFailed($"the asset could not be prepared ({preparationException.GetType().Name})");
            yield break;
        }

        onLoaded(assetBundle, asset);
    }

    public static T[] LoadAssetsWithSubAssetsFromFile<T>(
        string filePath,
        string assetName,
        params Type[] supportedTypes) where T : UnityEngine.Object
    {
        var assetBundle = AssetBundle.LoadFromFile(filePath);
        if (assetBundle == null) throw new InvalidOperationException("Unity could not read the AssetBundle");

        try
        {
            var assets = assetBundle.LoadAssetWithSubAssets<T>(assetName);
            foreach (var asset in assets)
            {
                if (asset is GameObject gameObject) SanitizeGameObject(gameObject, supportedTypes);
            }

            return assets;
        }
        finally
        {
            assetBundle.Unload(false);
        }
    }

    public static T LoadAsset<T>(
        AssetBundle assetBundle,
        string assetName,
        params Type[] supportedTypes) where T : UnityEngine.Object
    {
        if (assetBundle == null) throw new ArgumentNullException(nameof(assetBundle));

        var asset = assetBundle.LoadAsset<T>(assetName);
        if (asset is GameObject gameObject) SanitizeGameObject(gameObject, supportedTypes);
        return asset;
    }

    public static void Unload(AssetBundle assetBundle, bool unloadAllLoadedObjects = true) =>
        assetBundle.Unload(unloadAllLoadedObjects);

    public static AssetBundleUnloadOperation UnloadAsync(
        AssetBundle assetBundle,
        bool unloadAllLoadedObjects = true) =>
        assetBundle.UnloadAsync(unloadAllLoadedObjects);

    public static void SanitizeGameObject(GameObject prefab, params Type[] supportedTypes)
    {
        if (prefab == null) return;
        if (supportedTypes == null) throw new ArgumentNullException(nameof(supportedTypes));

        var supportedTypeSet = new HashSet<Type>(supportedTypes);
        var unsupportedBehaviours = prefab
            .GetComponentsInChildren<MonoBehaviour>(true)
            .AsValueEnumerable()
            .Where(behaviour => behaviour != null && !supportedTypeSet.Contains(behaviour.GetType()))
            .ToArray();

        foreach (var behaviour in unsupportedBehaviours) UnityEngine.Object.DestroyImmediate(behaviour, true);
    }
}
