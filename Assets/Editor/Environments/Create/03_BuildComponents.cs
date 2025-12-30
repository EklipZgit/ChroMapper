using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Enums;
using Editor.Environments.Structures.Components;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public partial class EnvironmentSceneCreator
{
    private static void BuildComponents(
        EnvironmentLibrarySO library,
        EnvData data,
        Dictionary<string, GameObject> chromaIdObjects)
    {
        var descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        descriptor.ID = data.Data.ID;

        data.Data.FogParameters.CopyTo(descriptor.BloomFogParams);

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;
        var boost = beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);

        var lcgemData = data
            .Objects
            .FirstOrDefault(x => x.Components.LightColorGroupEffectManager != null)
            ?.Components.LightColorGroupEffectManager[0];
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

        var lseeData = data
            .Objects
            .Where(x => x.Components.LightSwitchEventEffect != null)
            .SelectMany(x => x.Components.LightSwitchEventEffect)
            .ToArray();
        foreach (var d in lseeData)
        {
            var ble = beec.Register<BasicLightEffect>(ConvertUtils.ToEventType(d.EventType));
            ble.ColorBoostEffect = boost;
            ble.OffIntensity = d.OffColorIntensity;
            ble.LightOnStart = d.LightOnStart;
            // ble.InvertColorScheme = 
        }

        var idRemapAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(editorPath, "LightIDTables", data.Data.ID + ".json"));
        var idRemap = new Dictionary<string, Dictionary<string, int>>();
        if (idRemapAsset != null)
            idRemap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(idRemapAsset.text);

        var lightWithIdManager = data
            .Objects.FirstOrDefault(x => x.Components.LightWithIdManager != null)
            ?.Components.LightWithIdManager[0];
        if (lightWithIdManager != null)
        {
            var componentId = new Dictionary<string, int>();
            foreach (var lights in lightWithIdManager.Lights)
            {
                for (var id = 0; id < lights.Length; id++)
                {
                    var light = lights[id];
                    var envObject = data.Objects.Find(x => x.ChromaID == light.Name);
                    if (envObject is null) continue;
                    var marker = chromaIdObjects[light.Name];
                    var go = marker.gameObject;

                    componentId.TryAdd(light.Name, 0);
                    var arrayId = id;

                    if (envObject.Components.TubeBloomPrePassLightWithId != null)
                    {
                        HandleTubeBloomPrePassLightWithId(
                            componentId[light.Name],
                            arrayId,
                            light.ID,
                            go,
                            envObject.Components.TubeBloomPrePassLightWithId);
                    }

                    if (envObject.Components.SpriteLightWithId != null)
                    {
                        HandleSpriteLightWithId(
                            componentId[light.Name],
                            arrayId,
                            light.ID,
                            go,
                            envObject.Components.SpriteLightWithId);
                    }

                    if (envObject.Components.MaterialLightWithId != null)
                    {
                        HandleMaterialLightWithId(
                            componentId[light.Name],
                            arrayId,
                            light.ID,
                            go,
                            envObject.Components.MaterialLightWithId);
                    }

                    componentId[light.Name]++;
                }
            }
        }

        var lrgemData = data
            .Objects
            .FirstOrDefault(x => x.Components.LightRotationGroupEffectManager != null)
            ?.Components.LightRotationGroupEffectManager[0];
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

        var ltgemData = data
            .Objects
            .FirstOrDefault(x => x.Components.LightTranslationGroupEffectManager != null)
            ?.Components.LightTranslationGroupEffectManager[0];
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
                        FloatArrayToVector2(ltgData.xDistributionLimits),
                        FloatArrayToVector2(ltgData.yDistributionLimits),
                        FloatArrayToVector2(ltgData.zDistributionLimits)
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

        var ffgemData = data
            .Objects
            .FirstOrDefault(x => x.Components.FloatFxGroupEffectManager != null)
            ?.Components.FloatFxGroupEffectManager[0];
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

        // RINGS
        foreach (var obj in data.Objects.Where(x => x.Components.TrackLaneRingsManager != null))
        {
            foreach (var tlrmData in obj.Components.TrackLaneRingsManager)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = go.AddComponent<TrackLaneRingsManager>();
                tlrm.RingPositionStep = tlrmData.RingPositionZStep;
                tlrm.SpawnAsChildren = tlrmData.SpawnAsChildren;
                tlrm.Rings = tlrmData
                    .Rings.Select((r, i) =>
                    {
                        var tlr = chromaIdObjects[r].AddComponent<TrackLaneRing>();
                        return tlr;
                    })
                    .ToArray();
                tlrm.Start();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.TrackLaneRingsRotationEffect != null))
        {
            foreach (var tlrreData in obj.Components.TrackLaneRingsRotationEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = chromaIdObjects[tlrreData.TrackLaneRingsManager].GetComponent<TrackLaneRingsManager>();
                var tlrr = go.AddComponent<TrackLaneRingsRotation>();

                tlrr.Manager = tlrm;
                tlrr.StartupRotationAngle = tlrreData.StartupRotationAngle;
                tlrr.StartupRotationStep = tlrreData.StartupRotationStep;
                tlrr.StartupRotationPropagationSpeed = tlrreData.StartupRotationPropagationSpeed;
                tlrr.StartupRotationFlexySpeed = tlrreData.StartupRotationFlexySpeed;

                foreach (var r in tlrm.Rings)
                    r.transform.localEulerAngles = new Vector3(0, 0, tlrreData.StartupRotationAngle);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.TrackLaneRingsRotationEffectSpawner != null))
        {
            foreach (var tlrresData in obj.Components.TrackLaneRingsRotationEffectSpawner)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var tlrr = chromaIdObjects[tlrresData.TrackLaneRingsRotationEffect]
                    .GetComponent<TrackLaneRingsRotation>();
                var tlrre = go.AddComponent<TrackLaneRingsRotationEffect>();

                tlrre.Effect = tlrr;

                tlrre.Rotation = tlrresData.Rotation;
                tlrre.Step = tlrresData.RotationStep;
                tlrre.StepType = ConvertUtils.ToRotationStepType(tlrresData.RotationStepType);
                tlrre.PropagationSpeed = tlrresData.RotationPropagationSpeed;
                tlrre.FlexySpeed = tlrresData.RotationFlexySpeed;

                beec.Register(ConvertUtils.ToEventType(tlrresData.EventType), tlrre);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.TrackLaneRingsPositionStepEffectSpawner != null))
        {
            foreach (var tlrpsesData in obj.Components.TrackLaneRingsPositionStepEffectSpawner)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = chromaIdObjects[tlrpsesData.TrackLaneRingsManager].GetComponent<TrackLaneRingsManager>();
                var tlrpe = go.AddComponent<TrackLaneRingsPositionEffect>();

                tlrpe.Manager = tlrm;

                tlrpe.MinPositionStep = tlrpsesData.MinPositionStep;
                tlrpe.MaxPositionStep = tlrpsesData.MaxPositionStep;
                tlrpe.MoveSpeed = tlrpsesData.MoveSpeed;

                beec.Register(ConvertUtils.ToEventType(tlrpsesData.EventType), tlrpe);
            }
        }

        // ROTATION
        foreach (var typeObj in data
            .Objects
            .Where(x => x.Components.LightRotationEventEffect != null)
            .SelectMany(x => x.Components.LightRotationEventEffect.Select(y => (obj: x, comp: y)))
            .GroupBy(x => x.comp.EventType))
        {
            var lre = beec.Register<LightRotationEffect>(ConvertUtils.ToEventType(typeObj.Key));
            lre.TransformContainers = typeObj
                .Select(x => new LightRotationEffect.TransformContainer
                {
                    Transform = chromaIdObjects[x.obj.ChromaID].transform,
                    StartRotation = chromaIdObjects[x.obj.ChromaID].transform.rotation,
                    RotationVector = FloatArrayToVector3(x.comp.RotationVector),
                    SpeedMultiplier = x.comp.RotationSpeedMultiplier,
                })
                .ToArray();
        }

        foreach (var obj in data.Objects.Where(x => x.Components.LightPairRotationEventEffect != null))
        {
            foreach (var lpreData in obj.Components.LightPairRotationEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];

                var lT = chromaIdObjects[lpreData.TransformL].transform;
                var rT = chromaIdObjects[lpreData.TransformR].transform;

                var lpre = go.AddComponent<LightPairRotationEffect>();
                lpre.Transforms =
                    new LightPairRotationEffect.TransformContainer[]
                    {
                        new() { Transform = lT }, new() { Transform = rT }
                    };
                lpre.RotationVector = FloatArrayToVector3(lpreData.RotationVector);
                lpre.OverrideRandomValues = lpreData.OverrideRandomValues;
                lpre.UseZPositionForAngleOffset = lpreData.UseZPositionForAngleOffset;
                lpre.ZPositionAngleOffsetScale = lpreData.ZPositionAngleOffsetScale;
                lpre.StartRotation = lpreData.StartRotation;

                if (ConvertUtils.ToEventType(lpreData.EventTypeL, out var res) && res != -1) beec.Register(res, lpre);
                if (ConvertUtils.ToEventType(lpreData.EventTypeR, out res) && res != -1) beec.Register(res, lpre);
                if (ConvertUtils.ToEventType(lpreData.SwitchOverrideRandomValuesEvent, out res) && res != -1)
                    beec.Register(res, lpre);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.LightPairSinMoveEventEffect != null))
        {
            foreach (var lpsmeData in obj.Components.LightPairSinMoveEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];

                var lT = chromaIdObjects[lpsmeData.TransformL].transform;
                var rT = chromaIdObjects[lpsmeData.TransformR].transform;

                var lpsme = go.AddComponent<LightPairSinMoveEffect>();
                lpsme.Transforms =
                    new LightPairSinMoveEffect.TransformContainer[]
                    {
                        new() { Transform = lT }, new() { Transform = rT }
                    };
                lpsme.OverrideRandomValues = lpsmeData.OverrideRandomValues;
                lpsme.StartValueOffset = lpsmeData.StartValueOffset;
                lpsme.StartPositionOffset = FloatArrayToVector3(lpsmeData.StartPositionOffset);
                lpsme.EndPositionOffset = FloatArrayToVector3(lpsmeData.EndPositionOffset);

                if (ConvertUtils.ToEventType(lpsmeData.EventTypeL, out var res) && res != -1) beec.Register(res, lpsme);
                if (ConvertUtils.ToEventType(lpsmeData.EventTypeR, out res) && res != -1) beec.Register(res, lpsme);
                if (ConvertUtils.ToEventType(lpsmeData.SwitchOverrideRandomValuesEvent, out res) && res != -1)
                    beec.Register(res, lpsme);
            }
        }

        return;

        Vector2 FloatArrayToVector2(float[] array)
        {
            return new Vector2(array[0], array[1]);
        }

        Vector3 FloatArrayToVector3(float[] array)
        {
            return new Vector3(array[0], array[1], array[2]);
        }

        void RegisterLight(int orderId, int lightId, LightController controller)
        {
            var lg = lcgemData?.LightGroups.FirstOrDefault(x =>
                x.StartLightId <= lightId && lightId < x.StartLightId + x.NumberOfElements);
            if (lg != null)
            {
                lcgem.Register(lg.GroupId, lightId - lg.StartLightId, controller);
                return;
            }

            var lsee = lseeData.FirstOrDefault(x => x.LightsId == lightId);
            if (lsee != null)
            {
                var type = ConvertUtils.ToEventType(lsee.EventType);

                if (idRemap.TryGetValue(lightId.ToString(), out var remap)
                    && remap.TryGetValue(orderId.ToString(), out var newId))
                    orderId = newId;

                controller.Type = type;
                controller.ID = orderId;
                beec.Register(controller);
                return;
            }

            Debug.LogError(
                $"{controller} ID {lightId} could not be registered, missing event type or group ID register?");
        }

        void HandleTubeBloomPrePassLightWithId(
            int componentId,
            int arrayId,
            int lightId,
            GameObject go,
            TubeBloomPrePassLightWithIdComponent[] comps)
        {
            var tubeBloomPrePass = comps[componentId];
            // if (tubeBloomPrePass.TubeBloomPrePassLight == null) return;

            var lc = go.AddComponent<LightController>();

            // Set up bloom fog object
            lc.BloomFog = go.AddComponent<LightObjectBloomFog>();
            lc.BloomFog.Length = tubeBloomPrePass.TubeBloomPrePassLight.TubeLength;
            lc.BloomFog.Width = tubeBloomPrePass.TubeBloomPrePassLight.TubeWidth;
            lc.BloomFog.Center = tubeBloomPrePass.TubeBloomPrePassLight.Center;
            lc.BloomFog.Height = tubeBloomPrePass.TubeBloomPrePassLight.Height;

            lc.BloomFog.StartWidth = tubeBloomPrePass.TubeBloomPrePassLight.StartWidth;
            lc.BloomFog.EndWidth = tubeBloomPrePass.TubeBloomPrePassLight.EndWidth;

            lc.BloomFog.StartAlpha = tubeBloomPrePass.TubeBloomPrePassLight.StartAlpha;
            lc.BloomFog.EndAlpha = tubeBloomPrePass.TubeBloomPrePassLight.EndAlpha;

            lc.BloomFog.LightWidthMultiplier =
                tubeBloomPrePass.TubeBloomPrePassLight.LightWidthMultiplier;
            lc.BloomFog.Multiply = tubeBloomPrePass.TubeBloomPrePassLight.ColorAlphaMultiplier;
            lc.BloomFog.IntensityMultiplier =
                tubeBloomPrePass.TubeBloomPrePassLight.BloomFogIntensityMultiplier;

            lc.BloomFog.BoostToWhite = tubeBloomPrePass.TubeBloomPrePassLight.BoostToWhite;

            lc.BloomFog.LimitAlpha = tubeBloomPrePass.TubeBloomPrePassLight.LimitAlpha;
            lc.BloomFog.MinAlpha = tubeBloomPrePass.TubeBloomPrePassLight.MinAlpha;
            lc.BloomFog.MaxAlpha = tubeBloomPrePass.TubeBloomPrePassLight.MaxAlpha;

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
                        && envObject.Components.MeshRenderer[0].Materials.Any())
                    {
                        if (library.Materials.Lookup.TryGetValue(
                                envObject.Components.MeshRenderer[0].Materials[0],
                                out var mat)
                            && mat != null)
                            renderer.sharedMaterial = mat;
                        else
                        {
                            Debug.LogWarning(
                                $"{envObject.ChromaID} material not found for:\n{envObject.Components.MeshRenderer[0].Materials[0]}");
                        }
                    }

                    lc.SpriteLight.Renderer = renderer;
                }

                envObject
                    .Components.Parametric3SliceSpriteController[0]
                    .CopyTo(
                        (LightObjectParametric3SliceSprite)lc.SpriteLight);
            }

            RegisterLight(arrayId + componentId, lightId, lc);
        }

        void HandleSpriteLightWithId(
            int componentId,
            int arrayId,
            int lightId,
            GameObject go,
            SpriteLightWithIdComponent[] spriteLight)
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

        void HandleMaterialLightWithId(
            int componentId,
            int arrayId,
            int lightId,
            GameObject go,
            MaterialLightWithIdComponent[] materialLight)
        {
            // If you get error here, just comment or return it out
            var lc = go.AddComponent<LightController>();
            var lom = go.AddComponent<LightObjectMaterial>();
            lc.BoxLight = lom;
            lom.Renderer = go.GetComponent<Renderer>();

            lom.AlphaIntensity = materialLight[0].AlphaIntensity;
            lom.AlphaIntoColor = materialLight[0].AlphaIntoColor;
            lom.SetColorOnly = materialLight[0].SetColorOnly;
            lom.MultiplyColorWithAlpha = materialLight[0].MultiplyColorWithAlpha;
            lom.MultiplyColor = materialLight[0].MultiplyColor;
            lom.ColorMultiplier = materialLight[0].ColorMultiplier;
            lom.Alpha = materialLight[0].Alpha;

            RegisterLight(arrayId + componentId, lightId, lc);
        }
    }
}
