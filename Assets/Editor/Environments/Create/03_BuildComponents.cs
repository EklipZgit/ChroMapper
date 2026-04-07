using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Enums;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using Axis = Beatmap.Enums.Axis;

public partial class EnvironmentSceneCreator
{
    private static void BuildComponents(
        EnvironmentLibrarySO library,
        EnvData envData,
        Dictionary<string, GameObject> chromaIdObjects)
    {
        var descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        descriptor.ID = envData.Data.ID;

        envData.Data.FogParameters.CopyTo(descriptor.BloomFogParams);
        envData.Data.SizeData.CopyTo(descriptor.SizeData);

        foreach (var obj in envData.Objects)
        {
            if (obj.Components.MeshRenderer != null
                && obj
                    .Components.MeshRenderer[0]
                    .Materials.Any(x =>
                        envData.Data.UniqueMaterials.ToList().Exists(y => y.Hash == x && y.Shader.Contains("Mirror"))))
            {
                var go = chromaIdObjects[obj.ChromaID];
                var reflection = go.AddComponent<PlanarReflection>();
                reflection.MirrorRenderer = library.MirrorRenderer;
                reflection.Renderer = go.GetComponent<MeshRenderer>();
                reflection.PlaneTransform = chromaIdObjects.GetValueOrDefault(
                        envData.Objects.FirstOrDefault(x =>
                                !x.ChromaID.Contains("Player")
                                && x.Components.MeshRenderer != null
                                && x.Components.MeshRenderer.Any(m => m
                                    .Materials.Any(z =>
                                        envData
                                            .Data.UniqueMaterials.ToList()
                                            .Exists(y => y.Hash == z && y.Shader.Contains("Mirror")))))
                            .ChromaID,
                        go)
                    .transform;
            }
        }

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;
        var cbe = beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);

        // core lighting stuff
        foreach (var obj in envData.Objects.Where(x => x.Components.LightManager != null))
        {
            foreach (var _ in obj.Components.LightManager)
            {
                var go = chromaIdObjects[obj.ChromaID];
                go.AddComponent<LightManager>();
            }
        }

        var lightWithIds = new Dictionary<string, MonoBehaviour>();

        foreach (var obj in envData.Objects.Where(x => x.Components.DirectionalLight != null))
        {
            foreach (var data in obj.Components.DirectionalLight)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var dl = go.AddComponent<DirectionalLight>();
                data.CopyTo(dl);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.PointLight != null))
        {
            foreach (var data in obj.Components.PointLight)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var pl = go.AddComponent<PointLight>();
                data.CopyTo(pl);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.DirectionalLightWithIds != null))
        {
            foreach (var data in obj.Components.DirectionalLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var dlc = go.AddComponent<DirectionalLightsController>();
                dlc.Light = GetGameObjectOrNull(data.DirectionalLight, go)
                    .GetComponent<DirectionalLight>();
                data.CopyTo(dlc);
                lightWithIds.Add(obj.ChromaID, dlc);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.DirectionalLightWithGroupIds != null))
        {
            foreach (var data in obj.Components.DirectionalLightWithGroupIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var dlgc = go.AddComponent<DirectionalLightsGroupController>();
                dlgc.Light = GetGameObjectOrNull(data.DirectionalLight, go)
                    .GetComponent<DirectionalLight>();
                data.CopyTo(dlgc);
                lightWithIds.Add(obj.ChromaID, dlgc);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MaterialLightWithIds != null))
        {
            foreach (var data in obj.Components.MaterialLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mlc = go.AddComponent<MaterialLightsController>();
                mlc.MeshRenderer = GetGameObjectOrNull(data.MeshRenderer, go)
                    .GetComponent<MeshRenderer>();
                data.CopyTo(mlc);
                lightWithIds.Add(obj.ChromaID, mlc);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MixedLightsColorSetterRuntimeLightWithIds != null))
        {
            foreach (var data in obj.Components.MixedLightsColorSetterRuntimeLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mlc = go.AddComponent<MixedLightsController>();
                mlc.MpbColorSetter = GetGameObjectOrNull(data.MaterialPropertyBlockColorSetterId, go)
                    .GetComponent<MaterialPropertyBlockColorSetter>();
                data.CopyTo(mlc);
                lightWithIds.Add(obj.ChromaID, mlc);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.PointLightWithIds != null))
        {
            foreach (var data in obj.Components.PointLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var plc = go.AddComponent<PointLightsController>();
                plc.Light = GetGameObjectOrNull(data.PointLight, go).GetComponent<PointLight>();
                data.CopyTo(plc);
                lightWithIds.Add(obj.ChromaID, plc);
            }
        }

        // MPB stuff
        foreach (var obj in envData.Objects.Where(x => x.Components.MaterialPropertyBlockController != null))
        {
            foreach (var data in obj.Components.MaterialPropertyBlockController)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpbc = go.AddComponent<MaterialPropertyBlockController>();
                mpbc.Renderers = data
                    .Renderers.Select(y =>
                        TryGetGameObjectOrNull(y, go, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToList();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MaterialPropertyBlockColorSetter != null))
        {
            foreach (var data in obj.Components.MaterialPropertyBlockColorSetter)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpbcs = go.AddComponent<MaterialPropertyBlockColorSetter>();
                mpbcs.Controller = GetGameObjectOrNull(data.MaterialPropertyBlockControllerId, go)
                    .GetComponent<MaterialPropertyBlockController>();
                mpbcs.Property = data.Property;
                mpbcs.InverseAlpha = data.InverseAlpha;
                mpbcs.DisableOnZeroAlpha = data.DisableOnZeroAlpha;
                mpbcs.SendAlphaToProperty = data.SendAlphaToProperty;
                mpbcs.AlphaProperty = data.AlphaProperty;
                mpbcs.MultiplyWithAlpha = data.MultiplyWithAlpha;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MaterialPropertyBlockPositionUpdater != null))
        {
            foreach (var data in obj.Components.MaterialPropertyBlockPositionUpdater)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpbpa = go.AddComponent<MaterialPropertyBlockPositionAnimator>();
                mpbpa.Controller = go.GetComponent<MaterialPropertyBlockController>();
                mpbpa.Property = data.Property;
                mpbpa.TargetTransform = GetGameObjectOrNull(data.TargetTransform, go).transform;
                mpbpa.TargetTransform.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MaterialPropertyValuesSetter != null))
        {
            foreach (var data in obj.Components.MaterialPropertyValuesSetter)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpvs = go.AddComponent<MaterialPropertyValuesSetter>();
                mpvs.MpbController = GetGameObjectOrNull(data.MaterialPropertyBlockController, go)
                    .GetComponent<MaterialPropertyBlockController>();
                data.CopyTo(mpvs);
            }
        }

        // other stuff components
        foreach (var obj in envData.Objects.Where(x => x.Components.SDFPoint != null))
        {
            foreach (var data in obj.Components.SDFPoint)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<SDFPoint>();
                data.CopyTo(comp);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.SDFArrayManager != null))
        {
            foreach (var data in obj.Components.SDFArrayManager)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<SDFArrayManager>();
                comp.SDFPointArray =
                    data
                        .SDFPointArray
                        .Select(o => GetGameObjectOrNull(o, go).GetComponent<SDFPoint>())
                        .ToArray();
                data.CopyTo(comp);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.Spectrogram != null))
        {
            foreach (var data in obj.Components.Spectrogram)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<Spectrogram>();
                comp.MeshRenderers =
                    data
                        .MeshRenderers
                        .Select(o => GetGameObjectOrNull(o, go).GetComponent<MeshRenderer>())
                        .ToArray();
                comp.MpbController = GetGameObjectOrNull(data.MaterialPropertyBlockController, go)
                    .GetComponent<MaterialPropertyBlockController>();
                data.CopyTo(comp);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.SpectrogramRowPropertyAnimator != null))
        {
            foreach (var data in obj.Components.SpectrogramRowPropertyAnimator)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<SpectrogramRowPropertyAnimator>();
                comp.MpbController = GetGameObjectOrNull(data.MaterialPropertyBlockController, go)
                    .GetComponent<MaterialPropertyBlockController>();
                data.CopyTo(comp);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.TransformSpectrogram != null))
        {
            foreach (var data in obj.Components.TransformSpectrogram)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<TransformSpectrogram>();
                comp.Transforms =
                    data
                        .Transforms
                        .Select(o => GetGameObjectOrNull(o, go).transform)
                        .ToArray();
                data.CopyTo(comp);
            }
        }

        // core components
        var lcgemData = envData
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
            foreach (var lightColorGroupEffect in lcgem.IdToEffect.Values) lightColorGroupEffect.ColorBoostEffect = cbe;
        }

        var idRemapAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                Path.Combine(editorPath, "LightIDTables", envData.Data.ID + ".json"));
        var typeIdRemap = new Dictionary<int, Dictionary<int, int>>();
        if (idRemapAsset != null)
            typeIdRemap = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, int>>>(idRemapAsset.text);

        var lseeData = envData
            .Objects
            .Where(x => x.Components.LightSwitchEventEffect != null)
            .SelectMany(x => x.Components.LightSwitchEventEffect)
            .ToArray();
        foreach (var d in lseeData)
        {
            var ble = beec.Register<BasicLightEffect>(ConvertUtils.ToEventType(d.EventType));
            foreach (var (original, remap) in typeIdRemap.GetValueOrDefault(d.LightsId, new Dictionary<int, int>()))
                ble.LightIdRemapEntries.Add(new(original, remap));

            ble.ColorBoostEffect = cbe;
            ble.OffIntensity = d.OffColorIntensity;
            ble.LightOnStart = d.LightOnStart;
            // ble.InvertColorScheme = 
        }

        var registeredLightInstance = new HashSet<int>();
        var lightToRegister = new List<(LightController controller, int lightId, int order, bool force)>();
        var sinkObject = new GameObject("Sink Object");
        sinkObject.transform.SetParent(beec.transform.parent);

        var lightWithIdManager = envData
            .Objects.FirstOrDefault(x => x.Components.LightWithIdManager != null)
            ?.Components.LightWithIdManager[0];
        if (lightWithIdManager != null)
        {
            foreach (var (lightId, lights) in lightWithIdManager.Lights)
            {
                for (var order = 0; order < lights.Length; order++)
                {
                    var light = lights[order];
                    if (light is null) RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order);

                    if (light.ArrayId != null)
                    {
                        // Get runtime light
                        if (lightWithIds.TryGetValue(light.ObjectId, out var controller))
                        {
                            switch (controller)
                            {
                                case CombinedLightsController clc:
                                    RegisterLight(clc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                                case CombinedLightsGroupController clgc:
                                    RegisterLight(clgc.LightIntensityData[light.ArrayId.Value], lightId, order, true);
                                    break;
                            }
                        }
                        // Otherwise become sink
                        else
                            RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order, true);

                        continue;
                    }

                    var envObject = envData.Objects.Find(x => x.ChromaID == light.ObjectId);
                    if (envObject is null)
                    {
                        // If for whatever reason this is missing, become sink
                        RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order, true);
                        continue;
                    }

                    // Non-runtime
                    GetAndRegisterLight(envObject, order, light.InstanceId, true);
                    registeredLightInstance.Add(light.InstanceId);
                }
            }
        }

        // the rest of the light if they were not registered due to dynamic registration
        foreach (var envObject in envData.Objects)
            GetAndRegisterLight(
                envObject); // TODO: the rest of id, which is likely bad for lightId if they were inactive

        var lrgemData = envData
            .Objects
            .FirstOrDefault(x => x.Components.LightRotationGroupEffectManager != null)
            ?.Components.LightRotationGroupEffectManager[0];
        var lrgem = new GameObject("LightRotationGroupEffectManager")
            .AddComponent<LightRotationGroupEffectManager>();
        lrgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightRotationGroupEffectManager = lrgem;

        if (lrgemData != null)
        {
            foreach (var data in lrgemData.LightRotationGroups)
            {
                lrgem.Register(data.GroupId, data.Count);

                RegisterRotation(Axis.X, data.XTransforms, data.MirrorX);
                RegisterRotation(Axis.Y, data.YTransforms, data.MirrorY);
                RegisterRotation(Axis.Z, data.ZTransforms, data.MirrorZ);
                continue;

                void RegisterRotation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = chromaIdObjects[transformName].transform;
                        t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        lrgem.Register(data.GroupId, i, axis, mirror, t.gameObject.transform);
                    }
                }
            }
        }

        var ltgemData = envData
            .Objects
            .FirstOrDefault(x => x.Components.LightTranslationGroupEffectManager != null)
            ?.Components.LightTranslationGroupEffectManager[0];
        var ltgem = new GameObject("LightTranslationGroupEffectManager")
            .AddComponent<LightTranslationGroupEffectManager>();
        ltgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.LightTranslationGroupEffectManager = ltgem;

        if (ltgemData != null)
        {
            foreach (var data in ltgemData.LightTranslationGroups)
            {
                ltgem.Register(
                    data.GroupId,
                    data.Count,
                    new[]
                    {
                        ConvertUtils.ToVector2(data.xTranslationLimits),
                        ConvertUtils.ToVector2(data.yTranslationLimits),
                        ConvertUtils.ToVector2(data.zTranslationLimits)
                    },
                    new[]
                    {
                        ConvertUtils.ToVector2(data.xDistributionLimits),
                        ConvertUtils.ToVector2(data.yDistributionLimits),
                        ConvertUtils.ToVector2(data.zDistributionLimits)
                    });

                RegisterTranslation(Axis.X, data.XTransforms, data.MirrorX);
                RegisterTranslation(Axis.Y, data.YTransforms, data.MirrorY);
                RegisterTranslation(Axis.Z, data.ZTransforms, data.MirrorZ);
                continue;

                void RegisterTranslation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = chromaIdObjects[transformName].transform;
                        t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        ltgem.Register(data.GroupId, i, axis, mirror, t.gameObject.transform);
                    }
                }
            }
        }

        var ffgemData = envData
            .Objects
            .FirstOrDefault(x => x.Components.FloatFxGroupEffectManager != null)
            ?.Components.FloatFxGroupEffectManager[0];
        var ffgem = new GameObject("FloatFxGroupEffectManager")
            .AddComponent<FloatFxGroupEffectManager>();
        ffgem.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.FloatFxGroupEffectManager = ffgem;

        if (ffgemData != null)
        {
            foreach (var data in ffgemData.FloatFxGroups)
            {
                ffgem.Register(
                    data.LightGroup.GroupId,
                    data.LightGroup.NumberOfElements,
                    data.IsTriggerOnly);
            }
        }

        // RINGS
        foreach (var obj in envData.Objects.Where(x => x.Components.TrackLaneRingsManager != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsManager)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = go.AddComponent<TrackLaneRingsManager>();
                tlrm.RingPositionStep = data.RingPositionZStep;
                tlrm.SpawnAsChildren = data.SpawnAsChildren;
                if (data.Rings is null)
                    tlrm.Rings = new();
                else
                {
                    tlrm.Rings = data
                        .Rings.Select((r, i) =>
                        {
                            var tlr = chromaIdObjects[r].AddComponent<TrackLaneRing>();
                            tlr.ParentManager = tlrm;
                            return tlr;
                        })
                        .ToList();
                }

                tlrm.Start();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.TrackLaneRingsRotationEffect != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsRotationEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = GetGameObjectOrNull(data.TrackLaneRingsManager, go)
                    .GetComponent<TrackLaneRingsManager>();
                var tlrr = go.AddComponent<TrackLaneRingsRotation>();

                tlrr.Manager = tlrm;
                tlrr.StartupRotationAngle = data.StartupRotationAngle;
                tlrr.StartupRotationStep = data.StartupRotationStep;
                tlrr.StartupRotationPropagationSpeed = data.StartupRotationPropagationSpeed;
                tlrr.StartupRotationFlexySpeed = data.StartupRotationFlexySpeed;

                foreach (var r in tlrm.Rings)
                    r.transform.localEulerAngles = new Vector3(0, 0, data.StartupRotationAngle);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.TrackLaneRingsRotationEffectSpawner != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsRotationEffectSpawner)
            {
                if (!data.IsEnabled) continue;

                var go = chromaIdObjects[obj.ChromaID];
                var tlrr = GetGameObjectOrNull(data.TrackLaneRingsRotationEffect, go)
                    .GetComponent<TrackLaneRingsRotation>();
                var tlrre = go.AddComponent<TrackLaneRingsRotationEffect>();

                tlrre.Effect = tlrr;

                tlrre.Rotation = data.Rotation;
                tlrre.Step = data.RotationStep;
                tlrre.StepType = ConvertUtils.ToRotationStepType(data.RotationStepType);
                tlrre.PropagationSpeed = data.RotationPropagationSpeed;
                tlrre.FlexySpeed = data.RotationFlexySpeed;

                beec.Register(ConvertUtils.ToEventType(data.EventType), tlrre);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.TrackLaneRingsPositionStepEffectSpawner != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsPositionStepEffectSpawner)
            {
                if (!data.IsEnabled) continue;

                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = GetGameObjectOrNull(data.TrackLaneRingsManager, go)
                    .GetComponent<TrackLaneRingsManager>();
                var tlrps = go.AddComponent<TrackLaneRingsPositionSpawner>();
                var tlrpe = beec.GetOrRegister<TrackLaneRingsPositionEffect>(
                    ConvertUtils.ToEventType(data.EventType));

                tlrps.RingManager = tlrm;
                tlrps.EffectManager = tlrpe;

                tlrps.MinPositionStep = data.MinPositionStep;
                tlrps.MaxPositionStep = data.MaxPositionStep;
                tlrps.MoveSpeed = data.MoveSpeed;
            }
        }

        // ROTATION
        foreach (var obj in envData.Objects.Where(x => x.Components.LightRotationEventEffect != null))
        {
            foreach (var data in obj.Components.LightRotationEventEffect)
            {
                var lre = beec.GetOrRegister<LightRotationEffect>(ConvertUtils.ToEventType(data.EventType));
                var go = chromaIdObjects[obj.ChromaID];

                var lr = go.AddComponent<LightRotation>();
                lr.Effect = lre;
                lr.Transform = go.transform;
                lr.StartRotation = go.transform.rotation;
                lr.RotationVector = data.RotationVector;
                lr.SpeedMultiplier = data.RotationSpeedMultiplier;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.LightPairRotationEventEffect != null))
        {
            foreach (var data in obj.Components.LightPairRotationEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];

                var lT = GetGameObjectOrNull(data.TransformL, go).transform;
                lT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                var rT = GetGameObjectOrNull(data.TransformR, go).transform;
                rT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;

                var lpr = go.AddComponent<LightPairRotation>();
                lpr.Transforms =
                    new LightPairRotation.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
                lpr.RotationVector = data.RotationVector;
                lpr.OverrideRandomValues = data.OverrideRandomValues;
                lpr.UseZPositionForAngleOffset = data.UseZPositionForAngleOffset;
                lpr.ZPositionAngleOffsetScale = data.ZPositionAngleOffsetScale;
                lpr.StartRotation = data.StartRotation;

                if (ConvertUtils.ToEventType(data.EventTypeL, out var type) && type != -1)
                    lpr.LeftEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.EventTypeR, out type) && type != -1)
                    lpr.RightEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.SwitchOverrideRandomValuesEvent, out type) && type != -1)
                    lpr.SwitchEffect = beec.GetOrRegister<GenericCallbackEventEffect>(type);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.LightPairSinMoveEventEffect != null))
        {
            foreach (var data in obj.Components.LightPairSinMoveEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];

                var lT = GetGameObjectOrNull(data.TransformL, go).transform;
                lT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                var rT = GetGameObjectOrNull(data.TransformR, go).transform;
                rT.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;

                var lpsm = go.AddComponent<LightPairSinMove>();
                lpsm.Transforms =
                    new LightPairSinMove.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
                lpsm.OverrideRandomValues = data.OverrideRandomValues;
                lpsm.StartValueOffset = data.StartValueOffset;
                lpsm.StartPositionOffset = data.StartPositionOffset;
                lpsm.EndPositionOffset = data.EndPositionOffset;

                if (ConvertUtils.ToEventType(data.EventTypeL, out var type) && type != -1)
                    lpsm.LeftEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.EventTypeR, out type) && type != -1)
                    lpsm.RightEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.SwitchOverrideRandomValuesEvent, out type) && type != -1)
                    lpsm.SwitchEffect = beec.GetOrRegister<GenericCallbackEventEffect>(type);
            }
        }

        // whatever this shit
        foreach (var obj in envData.Objects.Where(x => x.Components.GameObjectIntSwitchEventEffect != null))
        {
            foreach (var data in obj.Components.GameObjectIntSwitchEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var gois = go.AddComponent<GameObjectIntSwitch>();
                gois.Effect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
                data.CopyTo(gois);
                gois.GameObjectsValueContainers =
                    data
                        .GameObjectsValueLists.Select(x => new GameObjectIntSwitch.GameObjectsValueContainer
                        {
                            Value = x.Value,
                            GameObjects =
                                x
                                    .GameObjectIds.Select(x => GetGameObjectOrNull(x, go))
                                    .Where(y => y != null)
                                    .Select(g =>
                                    {
                                        g.GetComponent<ChromaIDMarker>().MarkUse = true;
                                        g.GetComponent<ChromaIDMarker>().MarkActivator = true;
                                        return g;
                                    })
                                    .ToArray()
                        })
                        .ToArray();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.GameObjectSwitchEventEffect != null))
        {
            foreach (var data in obj.Components.GameObjectSwitchEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var gos = go.AddComponent<GameObjectSwitch>();
                gos.Effect = cbe;
                data.CopyTo(gos);
                gos.NormalGameObjects = data
                    .DeactivateOnBoostObjects.Select(x => GetGameObjectOrNull(x, go))
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.GetComponent<ChromaIDMarker>().MarkUse = true;
                        g.GetComponent<ChromaIDMarker>().MarkActivator = true;
                        return g;
                    })
                    .ToArray();
                gos.BoostGameObjects = data
                    .ActivateOnBoostObjects.Select(x => GetGameObjectOrNull(x, go))
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.GetComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MeshRendererSwitchEventEffect != null))
        {
            foreach (var data in obj.Components.MeshRendererSwitchEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mrs = go.AddComponent<MeshRendererSwitch>();
                mrs.Effect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
                data.CopyTo(mrs);
                mrs.NormalRenderers = data
                    .DeactivateOnBoostRenderers.Select(y =>
                        TryGetGameObjectOrNull(y, go, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
                mrs.BoostRenderers = data
                    .ActivateOnBoostRenderers.Select(y =>
                        TryGetGameObjectOrNull(y, go, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.CopyPosition != null))
        {
            foreach (var data in obj.Components.CopyPosition)
            {
                var go = chromaIdObjects[obj.ChromaID];
                if (!TryGetGameObjectOrNull(data.Transform, go, out var t)) continue;
                var pc = go.AddComponent<PositionConstraint>();
                t.GetComponent<ChromaIDMarker>().MarkUse = true;
                pc.AddSource(new ConstraintSource { sourceTransform = t.transform, weight = 1 });
                pc.constraintActive = true;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MovementBeatmapEventEffect != null))
        {
            foreach (var data in obj.Components.MovementBeatmapEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var m = go.AddComponent<Movement>();
                m.Effect = beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
                data.CopyTo(m);
                m.Transforms = data
                    .Transforms.Select(y =>
                        TryGetGameObjectOrNull(y, go, out var g) ? g.transform : null)
                    .Where(y => y != null)
                    .ToArray();
                foreach (var t in m.Transforms) t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.SmoothStepPositionEventEffect != null))
        {
            foreach (var data in obj.Components.SmoothStepPositionEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var sspee = go.AddComponent<SmoothStepPositionEventEffect>();
                data.CopyTo(sspee);
                beec.Register(ConvertUtils.ToEventType(data.EventType), sspee);
            }
        }

        // The freaky Fx
        foreach (var obj in envData.Objects.Where(x => x.Components.AlphaFloatFxGroupEffectTarget != null))
        {
            foreach (var data in obj.Components.AlphaFloatFxGroupEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var af = go.AddComponent<AlphaFx>();
                af.MpbControllers = data
                    .MaterialPropertyBlockControllers.Select(x => GetGameObjectOrNull(x, go))
                    .Where(x => x != null)
                    .Select(x => x.GetComponent<MaterialPropertyBlockController>())
                    .Where(x => x != null)
                    .ToArray();
                data.CopyTo(af);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.ColliderEventEffect != null))
        {
            foreach (var data in obj.Components.ColliderEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var col = TryGetGameObjectOrNull(data.EffectCollider, go, out var o)
                    ? o.GetComponent<Collider>()
                    : null;
                if (col == null) continue;

                var cf = go.AddComponent<ColliderFx>();
                cf.Repository = ffgem.gameObject.GetOrAddComponent<ColliderRepository>();
                cf.Collider = col;
                data.CopyTo(cf);
                cf.enabled = data.IsEnabled;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.FloatArrayMaterialPropertyEffectTarget != null))
        {
            foreach (var data in obj.Components.FloatArrayMaterialPropertyEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var maf = go.AddComponent<MpbArrayFx>();
                maf.MpbControllers = data
                    .MaterialPropertyBlockControllers.Select(x => GetGameObjectOrNull(x, go))
                    .Where(x => x != null)
                    .Select(x => x.GetComponent<MaterialPropertyBlockController>())
                    .Where(x => x != null)
                    .ToArray();
                data.CopyTo(maf);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.FloatFxGroupEffectCollectionTarget != null))
        {
            foreach (var data in obj.Components.FloatFxGroupEffectCollectionTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var cf = go.AddComponent<CollectionFx>();
                cf.Targets = data
                    .FloatFxGroupEffectTargets.Select(x => GetGameObjectOrNull(x, go))
                    .Where(x => x != null)
                    .Select(x => x.GetComponent<FxTarget>())
                    .Where(x => x != null)
                    .ToArray();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.FloatLocalScaleEffect != null))
        {
            foreach (var data in obj.Components.FloatLocalScaleEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var lsf = go.AddComponent<LocalScaleFx>();
                lsf.TargetTransforms = data
                    .Transforms.Select(x => GetGameObjectOrNull(x, go))
                    .Where(x => x != null)
                    .Select(x => x.transform)
                    .Select(x =>
                    {
                        x.transform.localScale = data.StartScale;
                        return x;
                    })
                    .ToArray();
                data.CopyTo(lsf);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.FloatMaterialPropertyEffectTarget != null))
        {
            foreach (var data in obj.Components.FloatMaterialPropertyEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mf = go.AddComponent<MpbFx>();
                mf.MpbController = GetGameObjectOrNull(data.MaterialPropertyBlockController, go)
                    .GetComponent<MaterialPropertyBlockController>();
                data.CopyTo(mf);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.FloatSDFPointScaleEffect != null))
        {
            foreach (var data in obj.Components.FloatSDFPointScaleEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<SDFPointScaleFx>();
                comp.ColorPoints = GetGameObjectOrNull(data.ColorPoints, go).GetComponent<SDFPoint>();
                data.CopyTo(comp);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.MoveInDirectionEffect != null))
        {
            foreach (var data in obj.Components.MoveInDirectionEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var midf = go.AddComponent<MoveInDirectionFx>();
                midf.TargetTransform = GetGameObjectOrNull(data.Transform, go).transform;
                data.CopyTo(midf);
            }
        }

        foreach (var obj in envData.Objects.Where(x =>
            x.Components.Parametric3SliceSpriteWidthEndFloatFxEffectTarget != null))
        {
            foreach (var data in obj.Components.Parametric3SliceSpriteWidthEndFloatFxEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var psewf = go.AddComponent<ParametricSliceEndWidthFx>();
                psewf.SpriteLight = GetGameObjectOrNull(data.Parametric3SliceSpriteController, go)
                    .GetComponent<ParametricSpriteLight>();
                data.CopyTo(psewf);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.SpectrogramMultiplierFloatFxEffectTarget != null))
        {
            foreach (var data in obj.Components.SpectrogramMultiplierFloatFxEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<SpectrogramMultiplierFx>();
                comp.SpectrogramRow = GetGameObjectOrNull(data.Spectrogram, go)
                    .GetComponent<SpectrogramRowPropertyAnimator>();
                data.CopyTo(comp);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.StepFloatMaterialEffectTarget != null))
        {
            foreach (var data in obj.Components.StepFloatMaterialEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var msf = go.AddComponent<MpbStepFx>();
                msf.MpbController = GetGameObjectOrNull(data.MaterialPropertyBlockController, go)
                    .GetComponent<MaterialPropertyBlockController>();
                data.CopyTo(msf);
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.SwitchGameObjectArrayEffectTarget != null))
        {
            foreach (var data in obj.Components.SwitchGameObjectArrayEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var sgoaf = go.AddComponent<SwitchGameObjectArrayFx>();
                sgoaf.GameObjects = data
                    .GameObjects.Select(x => (GetGameObjectOrNull(x.GameObject, go), x.Threshold))
                    .Where(x => x.Item1 != null)
                    .Select(x =>
                    {
                        x.Item1.GetComponent<ChromaIDMarker>().MarkUse = true;
                        x.Item1.GetComponent<ChromaIDMarker>().MarkActivator = true;
                        return new SwitchGameObjectArrayFx.GameObjectActivation
                        {
                            GameObject = x.Item1, Threshold = x.Threshold
                        };
                    })
                    .ToArray();
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.SwitchGameObjectEffectTarget != null))
        {
            foreach (var data in obj.Components.SwitchGameObjectEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var sgof = go.AddComponent<SwitchGameObjectFx>();
                sgof.GameObjectA = chromaIdObjects[data.GameObjectA];
                sgof.GameObjectB = chromaIdObjects[data.GameObjectB];

                sgof.GameObjectA.GetComponent<ChromaIDMarker>().MarkUse = true;
                sgof.GameObjectA.GetComponent<ChromaIDMarker>().MarkActivator = true;
                sgof.GameObjectB.GetComponent<ChromaIDMarker>().MarkUse = true;
                sgof.GameObjectB.GetComponent<ChromaIDMarker>().MarkActivator = true;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.VertexDisplacementFloatFxGroupEffectTarget != null))
        {
            foreach (var data in obj.Components.VertexDisplacementFloatFxGroupEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var comp = go.AddComponent<VertexDisplacementFx>();
                comp.DisplacementController = GetGameObjectOrNull(data.DisplacementController, go)
                    .GetComponent<MaterialPropertyBlockController>();
                comp.Renderer = GetGameObjectOrNull(data.Renderer, go).GetComponent<Renderer>();
                data.CopyTo(comp);
            }
        }

        var tffgemData = envData
            .Objects
            .FirstOrDefault(x => x.Components.TriggerFloatFxGroupEffectManager != null)
            ?.Components.TriggerFloatFxGroupEffectManager[0];

        if (ffgemData != null)
        {
            foreach (var data in ffgemData.FloatFxGroupEffects)
            {
                var fx = chromaIdObjects[data.Target].GetComponent<FxTarget>();
                if (fx == null) continue;
                ffgem.Register(data.GroupId, data.ElementId, fx);
            }
        }

        if (tffgemData != null)
        {
            foreach (var data in tffgemData.FloatFxGroupEffects)
            {
                var fx = chromaIdObjects[data.Target].GetComponent<FxTarget>();
                if (fx == null) continue;
                ffgem.Register(data.GroupId, data.ElementId, fx);
            }
        }

        // the whatever collider
        foreach (var obj in envData.Objects.Where(x => x.Components.TubeBloomPrePassLightCollisionEffect != null))
        {
            foreach (var data in obj.Components.TubeBloomPrePassLightCollisionEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var lc = go.AddComponent<LightCollision>();
                lc.ParametricLight = GetGameObjectOrNull(data.TubeBloomPrePassLightId, go)
                    .GetComponent<ParametricBloomFogLightController>();
                GetGameObjectOrNull(data.TubeBloomPrePassLightId, go)
                    .GetComponent<ChromaIDMarker>()
                    .MarkUse = true;
                GetGameObjectOrNull(data.TubeBloomPrePassLightId, go)
                    .GetComponent<ChromaIDMarker>()
                    .MarkActivator = true;

                lc.HitPointLightWithId = GetGameObjectOrNull(data.HitPointLightWithId, go)
                    .GetComponent<InstancedMaterialLightController>();
                GetGameObjectOrNull(data.HitPointLightWithId, go).GetComponent<ChromaIDMarker>().MarkUse =
                    true;
                GetGameObjectOrNull(data.HitPointLightWithId, go)
                    .GetComponent<ChromaIDMarker>()
                    .MarkActivator = true;

                lc.HitPointGameObject = GetGameObjectOrNull(data.HitPointGameObject, go);
                lc.HitPointTransform = GetGameObjectOrNull(data.HitPointTransform, go).transform;
                lc.UseScale = data.UseScale;
                if (TryGetGameObjectOrNull(data.ScaleTransform, go, out var o)) lc.ScaleTransform = o.transform;
                lc.EnvironmentLayerMask = library.LayerMaskLookup[data.EnvironmentLayerMask[0]];
                lc.HitPointDistanceToAlphaCurve = data.HitPointDistanceToAlphaCurve.Create();
                lc.ShowHitPoint = data.ShowHitPoint;

                lc.enabled = data.IsEnabled;
            }
        }

        foreach (var obj in envData.Objects.Where(x => x.Components.TubeBloomPrePassLightReflectionEffect != null))
        {
            foreach (var data in obj.Components.TubeBloomPrePassLightReflectionEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var lr = go.AddComponent<LightReflection>();

                lr.Repository = ffgem.gameObject.GetOrAddComponent<ColliderRepository>();
                lr.MainParametricLight = RegisterReflection(data.MainTubeBloomPrePassLight);
                lr.ParametricLightReflection =
                    data.TubeBloomPrePassLightBounces.Select(RegisterReflection).ToArray();
                lr.EnvironmentLayerMask = library.LayerMaskLookup[data.EnvironmentLayerMask[0]];

                lr.enabled = data.IsEnabled;
                continue;

                LightReflection.ParametricLightWithHitPoint RegisterReflection(
                    TubeBloomPrePassLightWithHitPoint comp)
                {
                    GetGameObjectOrNull(comp.TubeBloomPrePassLightId, go).GetComponent<ChromaIDMarker>().MarkUse =
                        true;
                    GetGameObjectOrNull(comp.TubeBloomPrePassLightId, go)
                        .GetComponent<ChromaIDMarker>()
                        .MarkActivator = true;
                    GetGameObjectOrNull(comp.HitPointLightWithId, go).GetComponent<ChromaIDMarker>().MarkUse =
                        true;
                    GetGameObjectOrNull(comp.HitPointLightWithId, go)
                        .GetComponent<ChromaIDMarker>()
                        .MarkActivator = true;
                    return new LightReflection.ParametricLightWithHitPoint
                    {
                        Light =
                            GetGameObjectOrNull(comp.TubeBloomPrePassLightId, go)
                                .GetComponent<ParametricBloomFogLightController>(),
                        HitPointLightWithId =
                            GetGameObjectOrNull(comp.HitPointLightWithId, go)
                                .GetComponent<InstancedMaterialLightController>(),
                        HitPointGameObject = chromaIdObjects[comp.HitPointGameObject],
                        HitPointTransform = chromaIdObjects[comp.HitPointTransform].transform,
                        HitPointDistanceToAlphaCurve = comp.HitPointDistanceToAlphaCurve.Create(),
                        ShowHitPoint = comp.ShowHitPoint,
                    };
                }
            }
        }

        FinalRegisterLight();

        return;

        void GetAndRegisterLight(EnvDataObject envObject, int order = -1, int instanceId = -1, bool force = false)
        {
            if (!chromaIdObjects.TryGetValue(envObject.ChromaID, out var marker)) return;
            var go = marker.gameObject;

            if (envObject.Components.DirectionalLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.DirectionalLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.DirectionalLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleDirectionalLightWithId(comp, go, order, force);
            }

            if (envObject.Components.InstancedMaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.InstancedMaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.InstancedMaterialLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleInstancedMaterialLightWithId(comp, go, order, force);
            }

            if (envObject.Components.MaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.MaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.MaterialLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleMaterialLightWithId(comp, go, order, force);
            }

            if (envObject.Components.RectangleFakeGlowLightWithLightId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.RectangleFakeGlowLightWithLightId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.RectangleFakeGlowLightWithLightId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleRectangleFakeGlowLightWithId(comp, go, order, force);
            }

            if (envObject.Components.SpriteLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.SpriteLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.SpriteLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleSpriteLightWithId(comp, go, order, force);
            }

            if (envObject.Components.TubeBloomPrePassLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.TubeBloomPrePassLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.TubeBloomPrePassLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleTubeBloomPrePassLightWithId(comp, go, order, force);
            }
        }

        void RegisterLight(LightController controller, int lightId, int order, bool force = false)
        {
            lightToRegister.Add((controller, lightId, order, force));
        }

        void FinalRegisterLight()
        {
            foreach (var (controller, lightId, order, force) in lightToRegister)
            {
                var lg = lcgemData?.LightGroups.FirstOrDefault(x =>
                    x.StartLightId <= lightId && lightId < x.StartLightId + x.NumberOfElements);
                if (lg != null)
                {
                    controller.Kind = LightController.LightKind.Group;
                    controller.Type = lg.GroupId;
                    controller.ID = lightId - lg.StartLightId;
                    if (force || !SkipThisShit(controller.transform)) descriptor.Register(controller);
                    continue;
                }

                var lsee = lseeData.FirstOrDefault(x => x.LightsId == lightId);
                if (lsee != null)
                {
                    controller.Kind = LightController.LightKind.Basic;
                    controller.Type = ConvertUtils.ToEventType(lsee.EventType);
                    controller.ID = order;
                    if (force || !SkipThisShit(controller.transform)) descriptor.Register(controller);
                    continue;
                }

                Debug.LogWarning(
                    $"{(controller.TryGetComponent<ChromaIDMarker>(out var marker) ? marker.ChromaID : "")}: {controller} ID {lightId} could not be registered, missing event type or group ID register?");
                continue;

                // Should we skip or register regardless
                bool SkipThisShit(Transform tr)
                {
                    if (tr.gameObject.activeInHierarchy) return false;
                    while (true)
                    {
                        if (!tr.gameObject.activeSelf)
                            return !tr.gameObject.GetComponent<ChromaIDMarker>().MarkActivator;
                        if (tr.parent == tr) break;
                        tr = tr.parent;
                    }

                    return true;
                }
            }
        }

        void HandleDirectionalLightWithId(
            DirectionalLightWithIdComponent comp,
            GameObject go,
            int order,
            bool force)
        {
            var dlc = go.AddComponent<DirectionalLightController>();
            dlc.Light = chromaIdObjects[comp.Light].GetComponent<DirectionalLight>();
            comp.CopyTo(dlc);
            RegisterLight(dlc, comp.Id, order, force);
        }

        void HandleInstancedMaterialLightWithId(
            InstancedMaterialLightWithIdComponent comp,
            GameObject go,
            int order,
            bool force)
        {
            var imlc = go.AddComponent<InstancedMaterialLightController>();
            // TODO: this should be string reference
            imlc.MpbColorSetter = go.GetOrAddComponent<MaterialPropertyBlockColorSetter>();
            comp.CopyTo(imlc);
            RegisterLight(imlc, comp.Id, order, force);
        }

        void HandleMaterialLightWithId(
            MaterialLightWithIdComponent comp,
            GameObject go,
            int order,
            bool force)
        {
            var mlc = go.AddComponent<MaterialLightController>();
            mlc.Renderer = go.GetComponent<Renderer>();
            comp.CopyTo(mlc);
            RegisterLight(mlc, comp.Id, order, force);
        }

        void HandleRectangleFakeGlowLightWithId(
            RectangleFakeGlowLightWithIdComponent comp,
            GameObject go,
            int order,
            bool force)
        {
            var rfglc = go.AddComponent<RectangleFakeGlowLightController>();
            rfglc.MpbController = go.GetComponent<MaterialPropertyBlockController>();
            var envObject =
                envData.Objects.First(y => y.ChromaID == chromaIdObjects.First(x => x.Value == go).Key);
            comp.CopyTo(rfglc);
            envObject.Components.RectangleFakeGlow[0].CopyTo(rfglc);
            RegisterLight(rfglc, comp.Id, order, force);
        }

        void HandleSpriteLightWithId(
            SpriteLightWithIdComponent comp,
            GameObject go,
            int order,
            bool force)
        {
            var slc = go.AddComponent<SpriteLightController>();
            var renderer = go.AddComponent<SpriteRenderer>();
            if (comp.Sprite != null)
            {
                if (library.Sprites.Lookup.TryGetValue(comp.Sprite.Name, out var val)) renderer.sprite = val;
                if (library.Materials.Lookup.TryGetValue(comp.Sprite.Materials[0], out var mat))
                    renderer.sharedMaterial = mat;
                // renderer.size = FloatArrayToVector2(comp.Sprite.Size) * 2;
            }

            slc.Renderer = renderer;
            comp.CopyTo(slc);
            RegisterLight(slc, comp.Id, order, force);
        }

        void HandleTubeBloomPrePassLightWithId(
            TubeBloomPrePassLightWithIdComponent comp,
            GameObject go,
            int order,
            bool force
        )
        {
            var pbflc = go.AddComponent<ParametricBloomFogLightController>();
            pbflc.BloomFog = go.AddComponent<BloomFogObject>();
            comp.CopyTo(pbflc);

            // Set up physical light object
            if (!string.IsNullOrEmpty(comp.TubeBloomPrePassLight.ParametricBoxId)
                && comp.TubeBloomPrePassLight.ParametricBoxId != "null")
            {
                var boxLight = chromaIdObjects[comp.TubeBloomPrePassLight.ParametricBoxId];
                var envObject = envData.Objects.First(x =>
                    x.ChromaID == comp.TubeBloomPrePassLight.ParametricBoxId);

                pbflc.BoxLight = boxLight.AddComponent<ParametricBoxLight>();
                pbflc.BoxLight.Renderer = boxLight.GetComponent<Renderer>();
                envObject.Components.ParametricBoxController[0].CopyTo(pbflc.BoxLight);
            }

            // Set up sprite light object
            if (!string.IsNullOrEmpty(comp.TubeBloomPrePassLight.SliceSpriteControllerId)
                && comp.TubeBloomPrePassLight.SliceSpriteControllerId != "null")
            {
                var spriteLight = chromaIdObjects[comp.TubeBloomPrePassLight.SliceSpriteControllerId];
                var envObject = envData.Objects.First(x =>
                    x.ChromaID == comp.TubeBloomPrePassLight.SliceSpriteControllerId);

                pbflc.SpriteLight = spriteLight.AddComponent<ParametricSpriteLight>();
                pbflc.SpriteLight.Renderer = spriteLight.GetComponent<Renderer>();

                // Good chance env data doesnt have this and it's fine
                if (pbflc.SpriteLight.Renderer == null || pbflc.SpriteLight.GetComponent<MeshFilter>() == null)
                {
                    var mesh = spriteLight.GetOrAddComponent<MeshFilter>();
                    mesh.sharedMesh = library.SliceSprite;
                    var renderer = spriteLight.GetOrAddComponent<MeshRenderer>();
                    if (envObject.Components.MeshRenderer?.First().Materials.Any() ?? false)
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

                    pbflc.SpriteLight.Renderer = renderer;
                }

                envObject.Components.Parametric3SliceSpriteController[0].CopyTo(pbflc.SpriteLight);
            }

            RegisterLight(pbflc, comp.Id, order, force);
        }

        GameObject GetGameObjectOrNull(string id, GameObject go)
        {
            if (id == "self") return go;
            return string.IsNullOrEmpty(id) ? null : chromaIdObjects.GetValueOrDefault(id);
        }

        bool TryGetGameObjectOrNull(string id, GameObject dgo, out GameObject go)
        {
            if (id == "self")
            {
                go = dgo;
                return true;
            }

            if (!string.IsNullOrEmpty(id)) return chromaIdObjects.TryGetValue(id, out go);
            go = null;
            return false;
        }
    }
}
