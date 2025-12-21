using System;
using System.Collections.Generic;
using System.Linq;
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

        beec.Register<ColorBoostManager>((int)EventTypeValue.ColorBoost);

        foreach (var envObject in objectsToUse)
        {
            var marker = chromaIdObjects[envObject.ChromaID];
            var go = marker.gameObject;

            if (envObject.Components.TubeBloomPrePassLightWithId != null)
                HandleTubeBloomPrePassLightWithId(go, envObject.Components.TubeBloomPrePassLightWithId);
            if (envObject.Components.SpriteLightWithId != null)
                HandleSpriteLightWithId(go, envObject.Components.SpriteLightWithId);
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

                beec.Register<BasicLightManager>(tubeBloomPrePass.ChromaLight.Type);
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
    }
}
