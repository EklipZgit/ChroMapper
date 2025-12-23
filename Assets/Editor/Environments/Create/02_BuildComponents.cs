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
        EnvData data,
        Dictionary<string, GameObject> chromaIdObjects,
        List<EnvDataObject> objectsToUse)
    {
        var descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        descriptor.ID = data.Data.ID;

        data.Data.FogParameters.CopyTo(descriptor.BloomFogParams);

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;
        beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);

        var lcgemData = objectsToUse.FirstOrDefault(x => x.Components.LightColorGroupEffectManager != null)
            ?.Components.LightColorGroupEffectManager;
        var lcgem = new GameObject("LightColorGroupEffectManager")
            .AddComponent<LightColorGroupEffectManager>();
        lcgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightColorGroupEffectManager = lcgem;

        var lrgemData = objectsToUse.FirstOrDefault(x => x.Components.LightRotationGroupEffectManager != null)
            ?.Components.LightRotationGroupEffectManager;
        var lrgem = new GameObject("LightRotationGroupEffectManager")
            .AddComponent<LightRotationGroupEffectManager>();
        lrgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightRotationGroupEffectManager = lrgem;

        // this shouldn't work, i just had to hack some solution to get weave working
        if (lrgemData != null)
        {
            foreach (var lrge in lrgemData.LightRotationGroupEffects)
            {
                if (chromaIdObjects.TryGetValue(lrge.Transform, out var go))
                {
                    var chromaObj = objectsToUse.First(x => x.ChromaID == lrge.Transform);

                    var instancedLight = chromaObj.Components.InstancedMaterialLightWithId;
                    if (instancedLight == null)
                    {
                        chromaObj = objectsToUse.First(x =>
                            x.ChromaID == lrge.Transform[..lrge.Transform.LastIndexOf('.')]);
                        instancedLight = chromaObj.Components.InstancedMaterialLightWithId;
                    }

                    // if (instancedLight == null) continue;
                    if (lcgemData != null)
                    {
                        var found = lcgemData.LightGroups.FirstOrDefault(x =>
                            x.StartLightId <= instancedLight.ID
                            && instancedLight.ID < x.StartLightId + x.NumberOfElements);
                        if (found != null)
                            lrgem.Register(
                                found.GroupId,
                                instancedLight.ID - found.StartLightId,
                                lrge.Axis,
                                lrge.Mirrored,
                                go.transform);
                    }
                }
            }
        }

        foreach (var envObject in objectsToUse)
        {
            var marker = chromaIdObjects[envObject.ChromaID];
            var go = marker.gameObject;

            if (envObject.Components.TubeBloomPrePassLightWithId != null)
                HandleTubeBloomPrePassLightWithId(go, envObject.Components.TubeBloomPrePassLightWithId);
            if (envObject.Components.SpriteLightWithId != null)
                HandleSpriteLightWithId(go, envObject.Components.SpriteLightWithId);
            if (envObject.Components.InstancedMaterialLightWithId != null)
                HandleInstancedMaterialLightWithId(go, envObject.Components.InstancedMaterialLightWithId);
        }

        return;

        void HandleTubeBloomPrePassLightWithId(
            GameObject go,
            List<TubeBloomPrePassLightWithIdComponent> comps)
        {
            foreach (var tubeBloomPrePass in comps)
            {
                if (tubeBloomPrePass.TubeBloomPrePassLight == null
                    || tubeBloomPrePass.ChromaLight == null
                    || string.IsNullOrEmpty(tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId)
                    || tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId == "null")
                    continue;

                var lc = go.AddComponent<LightController>();

                var boxLight = chromaIdObjects[tubeBloomPrePass.TubeBloomPrePassLight.ParametricBoxId];
                lc.LightObject = boxLight.AddComponent<LightObject>();
                lc.LightObject.Renderer = boxLight.GetComponent<Renderer>();
                lc.LightObject.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.ColorAlphaMultiplier;

                // var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                // quad.transform.SetParent(go.transform, false);
                // quad.transform.localScale = new(
                //    tubeBloomPrePass.TubeBloomPrePassLight.TubeWidth
                //    * 10f
                //    * tubeBloomPrePass.TubeBloomPrePassLight.LightWidthMultiplier,
                //    tubeBloomPrePass.TubeBloomPrePassLight.TubeLength,
                //    0f);
                // quad.transform.localPosition = new(0f, 0f, 0f);
                // var lobf = quad.AddComponent<LightObjectBloomFog>();
                // lobf.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.BloomFogIntensityMultiplier;
                // quad.layer = LayerMask.NameToLayer("Lighting Events");
                // quad.GetComponent<Renderer>().sharedMaterial = library.BloomFogMaterial;

                beec.Register<BasicLightEffect>(tubeBloomPrePass.ChromaLight.Type);
                beec.Register(tubeBloomPrePass.ChromaLight.Type, tubeBloomPrePass.ChromaLight.LightId, lc);
            }
        }

        void HandleSpriteLightWithId(GameObject go, SpriteLightWithIdComponent spriteLight)
        {
            if (spriteLight.ChromaLight == null
                || string.IsNullOrEmpty(spriteLight.SpriteName)
                || spriteLight.SpriteName == "null")
                return;

            var lc = go.AddComponent<LightController>();

            // var sprite = chromaIdObjects[spriteLight.SpriteName];
            lc.LightObject = go.AddComponent<LightObjectSprite>();
            lc.LightObject.Renderer = go.AddComponent<SpriteRenderer>();
            lc.LightObject.Multiply = spriteLight.Intensity;
        }

        void HandleInstancedMaterialLightWithId(GameObject go, InstancedMaterialLightWithIdComponent instancedLight)
        {
            var rend = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rend.transform.SetParent(go.transform.GetChild(1), false);
            rend.transform.localPosition = Vector3.up * 4.2f;
            rend.transform.localScale = (Vector3.one * 0.1f) + (Vector3.up * 8f);
            var lc = go.AddComponent<LightController>();
            lc.LightObject = go.AddComponent<LightObject>();
            lc.LightObject.Renderer = rend.GetComponent<Renderer>();
            lc.LightObject.Renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Environment/Custom/LightTransparent.mat");
            lc.LightObject.Multiply = instancedLight.Intensity;

            if (lcgemData != null)
            {
                var found = lcgemData.LightGroups.FirstOrDefault(x =>
                    x.StartLightId <= instancedLight.ID && instancedLight.ID < x.StartLightId + x.NumberOfElements);
                if (found != null) lcgem.Register(found.GroupId, instancedLight.ID - found.StartLightId, lc);
            }
        }
    }
}
