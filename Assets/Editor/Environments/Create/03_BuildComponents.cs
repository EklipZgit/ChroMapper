using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Editor.Environments.Structures.Components;
using UnityEditor;
using UnityEngine;

public partial class EnvironmentSceneCreator
{
    private static void BuildComponents(
        EnvironmentLibrarySO library,
        EnvData data,
        Dictionary<string, GameObject> chromaIdObjects,
        List<EnvDataObject> objectsToUse)
    {
        var descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        descriptor.ID = data.Data.ID;
        descriptor.ChromaIDMarkers = descriptor.GetComponentsInChildren<ChromaIDMarker>(true).ToList();

        data.Data.FogParameters.CopyTo(descriptor.BloomFogParams);

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;
        var boost = beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);

        var lcgemData = objectsToUse.FirstOrDefault(x => x.Components.LightColorGroupEffectManager != null)
            ?.Components.LightColorGroupEffectManager;
        var lcgem = new GameObject("LightColorGroupEffectManager")
            .AddComponent<LightColorGroupEffectManager>();
        lcgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightColorGroupEffectManager = lcgem;

        if (lcgemData != null)
        {
            foreach (var lg in lcgemData.LightGroups) lcgem.Register(lg.GroupId, lg.NumberOfElements);
            foreach (var lightColorGroupEffect in lcgem.IdToEffect.Values)
                lightColorGroupEffect.ColorBoostEffect = boost;
        }

        var lightWithIdManager = data.Objects.FirstOrDefault(x => x.Components.LightWithIdManager != null)
            ?.Components.LightWithIdManager;
        if (lightWithIdManager != null)
        {
            foreach (var lights in lightWithIdManager.Lights)
            {
                foreach (var l in lights)
                {
                    var envObject = objectsToUse.Find(x => x.ChromaID == l.Name);
                    if (envObject is null) continue;
                    var marker = chromaIdObjects[l.Name];
                    var go = marker.gameObject;

                    if (envObject.Components.TubeBloomPrePassLightWithId != null)
                        HandleTubeBloomPrePassLightWithId(l.ID, go, envObject.Components.TubeBloomPrePassLightWithId);
                    if (envObject.Components.SpriteLightWithId != null)
                        HandleSpriteLightWithId(l.ID, go, envObject.Components.SpriteLightWithId);
                    if (envObject.Components.InstancedMaterialLightWithId != null)
                        HandleInstancedMaterialLightWithId(l.ID, go, envObject.Components.InstancedMaterialLightWithId);
                }
            }
        }

        var lrgemData = objectsToUse.FirstOrDefault(x => x.Components.LightRotationGroupEffectManager != null)
            ?.Components.LightRotationGroupEffectManager;
        var lrgem = new GameObject("LightRotationGroupEffectManager")
            .AddComponent<LightRotationGroupEffectManager>();
        lrgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightRotationGroupEffectManager = lrgem;

        if (lrgemData != null)
        {
            foreach (var lrgData in lrgemData.LightRotationGroups)
            {
                lrgem.Register(lrgData.GroupId, lrgData.Count);

                RegisterRotation(Axis.X, lrgData.XTransforms, lrgData.MirrorX);
                RegisterRotation(Axis.Y, lrgData.YTransforms, lrgData.MirrorY);
                RegisterRotation(Axis.Z, lrgData.ZTransforms, lrgData.MirrorZ);
                continue;

                void RegisterRotation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = chromaIdObjects[transformName].transform;
                        lrgem.Register(lrgData.GroupId, i, axis, mirror, t.gameObject.transform);
                    }
                }
            }
        }

        var ltgemData = objectsToUse.FirstOrDefault(x => x.Components.LightTranslationGroupEffectManager != null)
            ?.Components.LightTranslationGroupEffectManager;
        var ltgem = new GameObject("LightTranslationGroupEffectManager")
            .AddComponent<LightTranslationGroupEffectManager>();
        ltgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightTranslationGroupEffectManager = ltgem;

        if (ltgemData != null)
        {
            foreach (var ltgData in ltgemData.LightTranslationGroups)
            {
                ltgem.Register(
                    ltgData.GroupId,
                    ltgData.Count,
                    new[]
                    {
                        FloatArrayToVector2(ltgData.xTranslationLimits),
                        FloatArrayToVector2(ltgData.yTranslationLimits),
                        FloatArrayToVector2(ltgData.zTranslationLimits)
                    },
                    new[]
                    {
                        Vector2.zero, Vector2.zero, Vector2.zero
                        // FloatArrayToVector2(ltgData.xDistributionLimits),
                        // FloatArrayToVector2(ltgData.yDistributionLimits),
                        // FloatArrayToVector2(ltgData.zDistributionLimits)
                    });

                RegisterTranslation(Axis.X, ltgData.XTransforms, ltgData.MirrorX);
                RegisterTranslation(Axis.Y, ltgData.YTransforms, ltgData.MirrorY);
                RegisterTranslation(Axis.Z, ltgData.ZTransforms, ltgData.MirrorZ);
                continue;

                void RegisterTranslation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = chromaIdObjects[transformName].transform;
                        ltgem.Register(ltgData.GroupId, i, axis, mirror, t.gameObject.transform);
                    }
                }
            }
        }

        var ffgemData = objectsToUse.FirstOrDefault(x => x.Components.FloatFxGroupEffectManager != null)
            ?.Components.FloatFxGroupEffectManager;
        var ffgem = new GameObject("FloatFxGroupEffectManager")
            .AddComponent<FloatFxGroupEffectManager>();
        ffgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.FloatFxGroupEffectManager = ffgem;

        if (ffgemData != null)
        {
            foreach (var ffgData in ffgemData.FloatFxGroups)
            {
                ffgem.Register(
                    ffgData.LightGroup.GroupId,
                    ffgData.LightGroup.NumberOfElements);
            }
        }

        return;

        Vector2 FloatArrayToVector2(float[] array)
        {
            return new Vector2(array[0], array[1]);
        }

        void RegisterLight(int id, LightController controller)
        {
            var lg = lcgemData.LightGroups.FirstOrDefault(x =>
                x.StartLightId <= id && id < x.StartLightId + x.NumberOfElements);
            if (lg is not null)
            {
                lcgem.Register(lg.GroupId, id - lg.StartLightId, controller);
                return;
            }

            Debug.LogError($"Somehow ID {id} is not registered for {controller}");
        }

        void HandleTubeBloomPrePassLightWithId(
            int id,
            GameObject go,
            List<TubeBloomPrePassLightWithIdComponent> comps)
        {
            foreach (var tubeBloomPrePass in comps)
            {
                if (tubeBloomPrePass.TubeBloomPrePassLight == null) continue;

                var lc = go.AddComponent<LightController>();

                // Set up bloom fog object
                lc.BloomFog = go.AddComponent<LightObjectBloomFog>();
                lc.BloomFog.Length = tubeBloomPrePass.TubeBloomPrePassLight.TubeLength;
                lc.BloomFog.Width = tubeBloomPrePass.TubeBloomPrePassLight.TubeWidth;
                lc.BloomFog.Center = tubeBloomPrePass.TubeBloomPrePassLight.Center;
                lc.BloomFog.Height = tubeBloomPrePass.TubeBloomPrePassLight.Height;

                lc.BloomFog.StartAlpha = tubeBloomPrePass.TubeBloomPrePassLight.StartAlpha;
                lc.BloomFog.EndAlpha = tubeBloomPrePass.TubeBloomPrePassLight.EndAlpha;

                lc.BloomFog.LightWidthMultiplier =
                    tubeBloomPrePass.TubeBloomPrePassLight.LightWidthMultiplier;
                lc.BloomFog.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.ColorAlphaMultiplier;
                lc.BloomFog.IntensityMultiplier =
                    tubeBloomPrePass.TubeBloomPrePassLight.BloomFogIntensityMultiplier;

                // Set up physical light object
                if (!string.IsNullOrEmpty(tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId)
                    && tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId != "null")
                {
                    var boxLight = chromaIdObjects[tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId];
                    lc.BoxLight = boxLight.AddComponent<LightObject>();
                    lc.BoxLight.Renderer = boxLight.GetComponent<Renderer>();
                    lc.BoxLight.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.ColorAlphaMultiplier;
                }

                // Set up sprite light object
                if (!string.IsNullOrEmpty(tubeBloomPrePass.TubeBloomPrePassLight.SliceSpriteControllerId)
                    && tubeBloomPrePass.TubeBloomPrePassLight.SliceSpriteControllerId != "null")
                {
                    var spriteLight = chromaIdObjects[tubeBloomPrePass.TubeBloomPrePassLight.SliceSpriteControllerId];
                    var envObject = data.Objects.First(x =>
                        x.ChromaID == tubeBloomPrePass.TubeBloomPrePassLight.SliceSpriteControllerId);

                    lc.SpriteLight = spriteLight.AddComponent<LightObjectParametric3SliceSprite>();
                    lc.SpriteLight.Renderer = spriteLight.GetComponent<Renderer>();

                    // Good chance env data doesnt have this and it's fine
                    if (lc.SpriteLight.Renderer == null)
                    {
                        spriteLight.AddComponent<MeshFilter>();
                        var renderer = spriteLight.AddComponent<MeshRenderer>();
                        if (envObject.Components.MeshRenderer != null
                            && envObject.Components.MeshRenderer.Materials.Any())
                        {
                            if (library.Materials.Lookup.TryGetValue(
                                    envObject.Components.MeshRenderer.Materials[0],
                                    out var mat)
                                && mat != null)
                                renderer.sharedMaterial = mat;
                            else
                            {
                                Debug.LogWarning(
                                    $"{envObject.ChromaID} material not found for:\n{envObject.Components.MeshRenderer.Materials[0]}");
                            }
                        }

                        lc.SpriteLight.Renderer = renderer;
                    }

                    envObject.Components.Parametric3SliceSpriteController.CopyTo(
                        (LightObjectParametric3SliceSprite)lc.SpriteLight);
                }

                RegisterLight(id, lc);
            }
        }

        void HandleSpriteLightWithId(int id, GameObject go, SpriteLightWithIdComponent spriteLight)
        {
            // if (string.IsNullOrEmpty(spriteLight.SpriteName)
            //     || spriteLight.SpriteName == "null")
            //     return;
            //
            // var lc = go.AddComponent<LightController>();
            //
            // // var sprite = chromaIdObjects[spriteLight.SpriteName];
            // lc.LightObject = go.AddComponent<LightObjectSprite>();
            // lc.LightObject.Renderer = go.AddComponent<SpriteRenderer>();
            // lc.LightObject.Multiply = spriteLight.Intensity;
        }

        void HandleInstancedMaterialLightWithId(
            int id,
            GameObject go,
            InstancedMaterialLightWithIdComponent instancedLight)
        {
            // If you get error here, just comment or return it out
            return;
            var lc = go.AddComponent<LightController>();
            lc.BoxLight = go.AddComponent<LightObject>();
            lc.BoxLight.Renderer = go.transform.GetChild(1).Find("LightGlow")?.GetComponent<Renderer>();
            lc.BoxLight.Multiply = instancedLight.Intensity;

            RegisterLight(id, lc);
        }
    }
}
