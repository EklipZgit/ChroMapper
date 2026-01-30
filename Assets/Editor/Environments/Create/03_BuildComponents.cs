using System;
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
        EnvData data,
        Dictionary<string, GameObject> chromaIdObjects)
    {
        var descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        descriptor.ID = data.Data.ID;

        data.Data.FogParameters.CopyTo(descriptor.BloomFogParams);

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;
        var cbe = beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);

        // core lighting stuff
        foreach (var obj in data.Objects.Where(x => x.Components.LightManager != null))
        {
            foreach (var _ in obj.Components.LightManager)
            {
                var go = chromaIdObjects[obj.ChromaID];
                go.AddComponent<LightManager>();
            }
        }

        var lightWithIds = new Dictionary<int, MonoBehaviour>();

        foreach (var obj in data.Objects.Where(x => x.Components.DirectionalLight != null))
        {
            foreach (var dlData in obj.Components.DirectionalLight)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var dl = go.AddComponent<DirectionalLight>();
                dlData.CopyTo(dl);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.PointLight != null))
        {
            foreach (var plData in obj.Components.PointLight)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var pl = go.AddComponent<PointLight>();
                plData.CopyTo(pl);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.DirectionalLightWithIds != null))
        {
            foreach (var dlwiData in obj.Components.DirectionalLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var dlc = go.AddComponent<DirectionalLightsController>();
                dlc.Light = chromaIdObjects[dlwiData.DirectionalLight].GetComponent<DirectionalLight>();
                dlwiData.CopyTo(dlc);
                lightWithIds.Add(dlwiData.InstanceId, dlc);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.DirectionalLightWithGroupIds != null))
        {
            foreach (var dligiData in obj.Components.DirectionalLightWithGroupIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var dlgc = go.AddComponent<DirectionalLightsGroupController>();
                dlgc.Light = chromaIdObjects[dligiData.DirectionalLight].GetComponent<DirectionalLight>();
                dligiData.CopyTo(dlgc);
                lightWithIds.Add(dligiData.InstanceId, dlgc);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MaterialLightWithIds != null))
        {
            foreach (var mlwiData in obj.Components.MaterialLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mlc = go.AddComponent<MaterialLightsController>();
                mlc.MeshRenderer = chromaIdObjects[mlwiData.MeshRenderer].GetComponent<MeshRenderer>();
                mlwiData.CopyTo(mlc);
                lightWithIds.Add(mlwiData.InstanceId, mlc);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MixedLightsColorSetterRuntimeLightWithIds != null))
        {
            foreach (var mlcsrlwiData in obj.Components.MixedLightsColorSetterRuntimeLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mlc = go.AddComponent<MixedLightsController>();
                mlc.MpbColorSetter = chromaIdObjects[mlcsrlwiData.MaterialPropertyBlockColorSetterId]
                    .GetComponent<MaterialPropertyBlockColorSetter>();
                mlcsrlwiData.CopyTo(mlc);
                lightWithIds.Add(mlcsrlwiData.InstanceId, mlc);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.PointLightWithIds != null))
        {
            foreach (var plwiData in obj.Components.PointLightWithIds)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var plc = go.AddComponent<PointLightsController>();
                plc.Light = chromaIdObjects[plwiData.PointLight].GetComponent<PointLight>();
                plwiData.CopyTo(plc);
                lightWithIds.Add(plwiData.InstanceId, plc);
            }
        }

        // MPB stuff
        foreach (var obj in data.Objects.Where(x => x.Components.MaterialPropertyBlockController != null))
        {
            foreach (var mpbcData in obj.Components.MaterialPropertyBlockController)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpbc = go.AddComponent<MaterialPropertyBlockController>();
                mpbc.Renderers = mpbcData
                    .Renderers.Select(y =>
                        chromaIdObjects.TryGetValue(y, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MaterialPropertyBlockColorSetter != null))
        {
            foreach (var mpbcsData in obj.Components.MaterialPropertyBlockColorSetter)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpbcs = go.AddComponent<MaterialPropertyBlockColorSetter>();
                mpbcs.Controller = chromaIdObjects[mpbcsData.MaterialPropertyBlockControllerId]
                    .GetComponent<MaterialPropertyBlockController>();
                mpbcs.Property = mpbcsData.Property;
                mpbcs.InverseAlpha = mpbcsData.InverseAlpha;
                mpbcs.DisableOnZeroAlpha = mpbcsData.DisableOnZeroAlpha;
                mpbcs.SendAlphaToProperty = mpbcsData.SendAlphaToProperty;
                mpbcs.AlphaProperty = mpbcsData.AlphaProperty;
                mpbcs.MultiplyWithAlpha = mpbcsData.MultiplyWithAlpha;
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MaterialPropertyBlockPositionUpdater != null))
        {
            foreach (var mpbpuData in obj.Components.MaterialPropertyBlockPositionUpdater)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mpbpa = go.AddComponent<MaterialPropertyBlockPositionAnimator>();
                mpbpa.Controller = go.GetComponent<MaterialPropertyBlockController>();
                mpbpa.Property = mpbpuData.Property;
                mpbpa.TargetTransform = chromaIdObjects[mpbpuData.TargetTransform].transform;
                mpbpa.TargetTransform.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
            }
        }

        // core components
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
            foreach (var lightColorGroupEffect in lcgem.IdToEffect.Values) lightColorGroupEffect.ColorBoostEffect = cbe;
        }

        var idRemapAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(editorPath, "LightIDTables", data.Data.ID + ".json"));
        var typeIdRemap = new Dictionary<int, Dictionary<int, int>>();
        if (idRemapAsset != null)
            typeIdRemap = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, int>>>(idRemapAsset.text);

        var lseeData = data
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

        var registeredLight = new HashSet<int>();
        var sinkObject = new GameObject("Sink Object");
        sinkObject.transform.SetParent(beec.transform.parent);

        var lightWithIdManager = data
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
                        if (lightWithIds.TryGetValue(light.InstanceId, out var controller))
                        {
                            switch (controller)
                            {
                                case CombinedLightsController clc:
                                    RegisterLight(clc.LightIntensityData[light.ArrayId.Value], lightId, order);
                                    break;
                                case CombinedLightsGroupController clgc:
                                    RegisterLight(clgc.LightIntensityData[light.ArrayId.Value], lightId, order);
                                    break;
                            }
                        }
                        // Otherwise become sink
                        else
                            RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order);

                        continue;
                    }

                    var envObject = data.Objects.Find(x => x.ChromaID == light.ObjectId);
                    if (envObject is null)
                    {
                        // If for whatever reason this is missing, become sink
                        RegisterLight(sinkObject.AddComponent<LightSink>(), lightId, order);
                        continue;
                    }

                    // Non-runtime
                    GetAndRegisterLight(envObject, order, light.InstanceId);
                    registeredLight.Add(light.InstanceId);
                }
            }
        }

        foreach (var envObject in data.Objects)
            GetAndRegisterLight(
                envObject); // TODO: the rest of id, which is likely bad for lightId if they were inactive

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
                        t.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
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
                        ConvertUtils.ToVector2(ltgData.xTranslationLimits),
                        ConvertUtils.ToVector2(ltgData.yTranslationLimits),
                        ConvertUtils.ToVector2(ltgData.zTranslationLimits)
                    },
                    new[]
                    {
                        ConvertUtils.ToVector2(ltgData.xDistributionLimits),
                        ConvertUtils.ToVector2(ltgData.yDistributionLimits),
                        ConvertUtils.ToVector2(ltgData.zDistributionLimits)
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
                        t.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
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
                    ffgData.LightGroup.NumberOfElements,
                    ffgData.IsTriggerOnly);
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
                if (tlrmData.Rings is null)
                    tlrm.Rings = new();
                else
                {
                    tlrm.Rings = tlrmData
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
                if (!tlrresData.IsEnabled) continue;

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
                if (!tlrpsesData.IsEnabled) continue;

                var go = chromaIdObjects[obj.ChromaID];
                var tlrm = chromaIdObjects[tlrpsesData.TrackLaneRingsManager].GetComponent<TrackLaneRingsManager>();
                var tlrps = go.AddComponent<TrackLaneRingsPositionSpawner>();
                var tlrpe = beec.GetOrRegister<TrackLaneRingsPositionEffect>(
                    ConvertUtils.ToEventType(tlrpsesData.EventType));

                tlrps.RingManager = tlrm;
                tlrps.EffectManager = tlrpe;

                tlrps.MinPositionStep = tlrpsesData.MinPositionStep;
                tlrps.MaxPositionStep = tlrpsesData.MaxPositionStep;
                tlrps.MoveSpeed = tlrpsesData.MoveSpeed;
            }
        }

        // ROTATION
        foreach (var obj in data.Objects.Where(x => x.Components.LightRotationEventEffect != null))
        {
            foreach (var lreData in obj.Components.LightRotationEventEffect)
            {
                var lre = beec.GetOrRegister<LightRotationEffect>(ConvertUtils.ToEventType(lreData.EventType));
                var go = chromaIdObjects[obj.ChromaID];

                var lr = go.AddComponent<LightRotation>();
                lr.Effect = lre;
                lr.Transform = go.transform;
                lr.StartRotation = go.transform.rotation;
                lr.RotationVector = FloatArrayToVector3(lreData.RotationVector);
                lr.SpeedMultiplier = lreData.RotationSpeedMultiplier;
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.LightPairRotationEventEffect != null))
        {
            foreach (var lpreData in obj.Components.LightPairRotationEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];

                var lT = chromaIdObjects[lpreData.TransformL].transform;
                lT.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                var rT = chromaIdObjects[lpreData.TransformR].transform;
                rT.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;

                var lpr = go.AddComponent<LightPairRotation>();
                lpr.Transforms =
                    new LightPairRotation.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
                lpr.RotationVector = FloatArrayToVector3(lpreData.RotationVector);
                lpr.OverrideRandomValues = lpreData.OverrideRandomValues;
                lpr.UseZPositionForAngleOffset = lpreData.UseZPositionForAngleOffset;
                lpr.ZPositionAngleOffsetScale = lpreData.ZPositionAngleOffsetScale;
                lpr.StartRotation = lpreData.StartRotation;

                if (ConvertUtils.ToEventType(lpreData.EventTypeL, out var type) && type != -1)
                    lpr.LeftEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(lpreData.EventTypeR, out type) && type != -1)
                    lpr.RightEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(lpreData.SwitchOverrideRandomValuesEvent, out type) && type != -1)
                    lpr.SwitchEffect = beec.GetOrRegister<GenericCallbackEventEffect>(type);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.LightPairSinMoveEventEffect != null))
        {
            foreach (var lpsmeData in obj.Components.LightPairSinMoveEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];

                var lT = chromaIdObjects[lpsmeData.TransformL].transform;
                lT.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                var rT = chromaIdObjects[lpsmeData.TransformR].transform;
                rT.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;

                var lpsm = go.AddComponent<LightPairSinMove>();
                lpsm.Transforms =
                    new LightPairSinMove.TransformContainer[] { new() { Transform = lT }, new() { Transform = rT } };
                lpsm.OverrideRandomValues = lpsmeData.OverrideRandomValues;
                lpsm.StartValueOffset = lpsmeData.StartValueOffset;
                lpsm.StartPositionOffset = FloatArrayToVector3(lpsmeData.StartPositionOffset);
                lpsm.EndPositionOffset = FloatArrayToVector3(lpsmeData.EndPositionOffset);

                if (ConvertUtils.ToEventType(lpsmeData.EventTypeL, out var type) && type != -1)
                    lpsm.LeftEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(lpsmeData.EventTypeR, out type) && type != -1)
                    lpsm.RightEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(lpsmeData.SwitchOverrideRandomValuesEvent, out type) && type != -1)
                    lpsm.SwitchEffect = beec.GetOrRegister<GenericCallbackEventEffect>(type);
            }
        }

        // whatever this shit
        foreach (var obj in data.Objects.Where(x => x.Components.GameObjectIntSwitchEventEffect != null))
        {
            foreach (var goiseData in obj.Components.GameObjectIntSwitchEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var gois = go.AddComponent<GameObjectIntSwitch>();
                gois.Effect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(goiseData.EventType));
                goiseData.CopyTo(gois);
                gois.GameObjectsValueContainers =
                    goiseData
                        .GameObjectsValueLists.Select(x => new GameObjectIntSwitch.GameObjectsValueContainer()
                        {
                            Value = x.Value,
                            GameObjects =
                                x
                                    .GameObjectIds.Select(chromaIdObjects.GetValueOrDefault)
                                    .Where(y => y != null)
                                    .Select(g =>
                                    {
                                        g.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                                        return g;
                                    })
                                    .ToArray()
                        })
                        .ToArray();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.GameObjectSwitchEventEffect != null))
        {
            foreach (var goseData in obj.Components.GameObjectSwitchEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var gos = go.AddComponent<GameObjectSwitch>();
                gos.Effect = cbe;
                goseData.CopyTo(gos);
                gos.NormalGameObjects = goseData
                    .DeactivateOnBoostObjects.Select(chromaIdObjects.GetValueOrDefault)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
                gos.BoostGameObjects = goseData
                    .ActivateOnBoostObjects.Select(chromaIdObjects.GetValueOrDefault)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MeshRendererSwitchEventEffect != null))
        {
            foreach (var mrseData in obj.Components.MeshRendererSwitchEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mrs = go.AddComponent<MeshRendererSwitch>();
                mrs.Effect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(mrseData.EventType));
                mrseData.CopyTo(mrs);
                mrs.NormalRenderers = mrseData
                    .DeactivateOnBoostRenderers.Select(y =>
                        chromaIdObjects.TryGetValue(y, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
                mrs.BoostRenderers = mrseData
                    .ActivateOnBoostRenderers.Select(y =>
                        chromaIdObjects.TryGetValue(y, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .Select(g =>
                    {
                        g.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
                        return g;
                    })
                    .ToArray();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.CopyPosition != null))
        {
            foreach (var cpData in obj.Components.CopyPosition)
            {
                var go = chromaIdObjects[obj.ChromaID];
                if (!chromaIdObjects.TryGetValue(cpData.Transform, out var t)) continue;
                var pc = go.AddComponent<PositionConstraint>();
                t.GetComponent<ChromaIDMarker>().MarkUse = true;
                pc.AddSource(new ConstraintSource { sourceTransform = t.transform, weight = 1 });
                pc.constraintActive = true;
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MovementBeatmapEventEffect != null))
        {
            foreach (var mbeData in obj.Components.MovementBeatmapEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var m = go.AddComponent<Movement>();
                m.Effect = beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(mbeData.EventType));
                mbeData.CopyTo(m);
                m.Transforms = mbeData
                    .Transforms.Select(y =>
                        chromaIdObjects.TryGetValue(y, out var g) ? g.transform : null)
                    .Where(y => y != null)
                    .ToArray();
                foreach (var t in m.Transforms) t.gameObject.GetOrAddComponent<ChromaIDMarker>().MarkUse = true;
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.SmoothStepPositionEventEffect != null))
        {
            foreach (var mbeData in obj.Components.SmoothStepPositionEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var sspee = go.AddComponent<SmoothStepPositionEventEffect>();
                mbeData.CopyTo(sspee);
                beec.Register(ConvertUtils.ToEventType(mbeData.EventType), sspee);
            }
        }

        // The freaky Fx
        foreach (var obj in data.Objects.Where(x => x.Components.AlphaFloatFxGroupEffectTarget != null))
        {
            foreach (var affgetData in obj.Components.AlphaFloatFxGroupEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var af = go.AddComponent<AlphaFx>();
                af.MpbControllers = affgetData
                    .MaterialPropertyBlockControllerId.Select(x => chromaIdObjects.GetValueOrDefault(x, null))
                    .Where(x => x != null)
                    .Select(x => x.GetComponent<MaterialPropertyBlockController>())
                    .ToArray();
                affgetData.CopyTo(af);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.ColliderEventEffect != null))
        {
            foreach (var ceeData in obj.Components.ColliderEventEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var col = chromaIdObjects.TryGetValue(ceeData.EffectCollider, out var o)
                    ? o.GetComponent<Collider>()
                    : null;
                if (col == null) continue;

                var cf = go.AddComponent<ColliderFx>();
                cf.Repository = ffgem.gameObject.GetOrAddComponent<ColliderRepository>();
                cf.Collider = col;
                ceeData.CopyTo(cf);
                cf.enabled = ceeData.IsEnabled;
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.FloatArrayMaterialPropertyEffectTarget != null))
        {
            foreach (var fampetData in obj.Components.FloatArrayMaterialPropertyEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var maf = go.AddComponent<MpbArrayFx>();
                maf.MpbControllers = fampetData
                    .MaterialPropertyBlockControllers.Select(x => chromaIdObjects.GetValueOrDefault(x, null))
                    .Where(x => x != null)
                    .Select(x => x.GetComponent<MaterialPropertyBlockController>())
                    .ToArray();
                fampetData.CopyTo(maf);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.FloatFxGroupEffectCollectionTarget != null))
        {
            foreach (var ffgectData in obj.Components.FloatFxGroupEffectCollectionTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var cf = go.AddComponent<CollectionFx>();
                cf.Targets = ffgectData
                    .FloatFxGroupEffectTargets.Select(x => chromaIdObjects.GetValueOrDefault(x, null))
                    .Where(x => x != null)
                    .Select(x => x.GetComponent<FxTarget>())
                    .ToArray();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.FloatLocalScaleEffect != null))
        {
            foreach (var flseData in obj.Components.FloatLocalScaleEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var lsf = go.AddComponent<LocalScaleFx>();
                lsf.TargetTransforms = flseData
                    .Transforms.Select(x => chromaIdObjects.GetValueOrDefault(x, null))
                    .Where(x => x != null)
                    .Select(x => x.transform)
                    .ToArray();
                flseData.CopyTo(lsf);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.FloatMaterialPropertyEffectTarget != null))
        {
            foreach (var fmpetData in obj.Components.FloatMaterialPropertyEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var mf = go.AddComponent<MpbFx>();
                mf.MpbController = chromaIdObjects[fmpetData.MaterialPropertyBlockController]
                    .GetComponent<MaterialPropertyBlockController>();
                fmpetData.CopyTo(mf);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.MoveInDirectionEffect != null))
        {
            foreach (var mideData in obj.Components.MoveInDirectionEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var midf = go.AddComponent<MoveInDirectionFx>();
                midf.TargetTransform = chromaIdObjects[mideData.TargetTransform].transform;
                mideData.CopyTo(midf);
            }
        }

        foreach (var obj in data.Objects.Where(x =>
            x.Components.Parametric3SliceSpriteWidthEndFloatFxEffectTarget != null))
        {
            foreach (var p3ssweffetData in obj.Components.Parametric3SliceSpriteWidthEndFloatFxEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var psewf = go.AddComponent<ParametricSliceEndWidthFx>();
                psewf.SpriteLight = chromaIdObjects[p3ssweffetData.Parametric3SliceSpriteController]
                    .GetComponent<ParametricSpriteLight>();
                p3ssweffetData.CopyTo(psewf);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.StepFloatMaterialEffectTarget != null))
        {
            foreach (var sfmetData in obj.Components.StepFloatMaterialEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var msf = go.AddComponent<MpbStepFx>();
                msf.MpbController = chromaIdObjects[sfmetData.MaterialPropertyBlockController]
                    .GetComponent<MaterialPropertyBlockController>();
                sfmetData.CopyTo(msf);
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.SwitchGameObjectArrayEffectTarget != null))
        {
            foreach (var sgoaetData in obj.Components.SwitchGameObjectArrayEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var sgoaf = go.AddComponent<SwitchGameObjectArrayFx>();
                sgoaf.GameObjects = sgoaetData
                    .GameObjects.Select(x => (chromaIdObjects.GetValueOrDefault(x.GameObject, null), x.Threshold))
                    .Where(x => x.Item1 != null)
                    .Select(x => new SwitchGameObjectArrayFx.GameObjectActivation()
                    {
                        GameObject = x.Item1, Threshold = x.Threshold
                    })
                    .ToArray();
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.SwitchGameObjectEffectTarget != null))
        {
            foreach (var sgoetData in obj.Components.SwitchGameObjectEffectTarget)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var sgof = go.AddComponent<SwitchGameObjectFx>();
                sgof.GameObjectA = chromaIdObjects[sgoetData.GameObjectA];
                sgof.GameObjectB = chromaIdObjects[sgoetData.GameObjectB];
            }
        }

        var tffgemData = data
            .Objects
            .FirstOrDefault(x => x.Components.TriggerFloatFxGroupEffectManager != null)
            ?.Components.TriggerFloatFxGroupEffectManager[0];

        if (ffgemData != null)
        {
            foreach (var ffgData in ffgemData.FloatFxGroupEffects)
            {
                var fx = chromaIdObjects[ffgData.Target].GetComponent<FxTarget>();
                if (fx == null) continue;
                ffgem.Register(ffgData.GroupId, ffgData.ElementId, fx);
            }
        }

        if (tffgemData != null)
        {
            foreach (var ffgData in tffgemData.FloatFxGroupEffects)
            {
                var fx = chromaIdObjects[ffgData.Target].GetComponent<FxTarget>();
                if (fx == null) continue;
                ffgem.Register(ffgData.GroupId, ffgData.ElementId, fx);
            }
        }

        // the whatever collider
        foreach (var obj in data.Objects.Where(x => x.Components.TubeBloomPrePassLightCollisionEffect != null))
        {
            foreach (var tbpplcData in obj.Components.TubeBloomPrePassLightCollisionEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var lc = go.AddComponent<LightCollision>();
                lc.ParametricLight = chromaIdObjects[tbpplcData.TubeBloomPrePassLightId]
                    .GetComponent<ParametricBloomFogLightController>();
                lc.HitPointLightWithId = chromaIdObjects[tbpplcData.HitPointLightWithId]
                    .GetComponent<InstancedMaterialLightController>();
                lc.HitPointGameObject = chromaIdObjects[tbpplcData.HitPointGameObject];
                lc.HitPointTransform = chromaIdObjects[tbpplcData.HitPointTransform].transform;
                lc.UseScale = tbpplcData.UseScale;
                if (chromaIdObjects.TryGetValue(tbpplcData.ScaleTransform, out var o)) lc.ScaleTransform = o.transform;
                lc.EnvironmentLayerMask = library.LayerMaskLookup[tbpplcData.EnvironmentLayerMask[0]];
                lc.HitPointDistanceToAlphaCurve = tbpplcData.HitPointDistanceToAlphaCurve.Create();
                lc.ShowHitPoint = tbpplcData.ShowHitPoint;

                lc.enabled = tbpplcData.IsEnabled;
            }
        }

        foreach (var obj in data.Objects.Where(x => x.Components.TubeBloomPrePassLightReflectionEffect != null))
        {
            foreach (var tbpplrData in obj.Components.TubeBloomPrePassLightReflectionEffect)
            {
                var go = chromaIdObjects[obj.ChromaID];
                var lr = go.AddComponent<LightReflection>();

                LightReflection.ParametricLightWithHitPoint RegisterReflection(
                    TubeBloomPrePassLightWithHitPoint comp)
                {
                    return new LightReflection.ParametricLightWithHitPoint
                    {
                        Light =
                            chromaIdObjects[comp.TubeBloomPrePassLightId]
                                .GetComponent<ParametricBloomFogLightController>(),
                        HitPointLightWithId =
                            chromaIdObjects[comp.HitPointLightWithId].GetComponent<InstancedMaterialLightController>(),
                        HitPointGameObject = chromaIdObjects[comp.HitPointGameObject],
                        HitPointTransform = chromaIdObjects[comp.HitPointTransform].transform,
                        HitPointDistanceToAlphaCurve = comp.HitPointDistanceToAlphaCurve.Create(),
                        ShowHitPoint = comp.ShowHitPoint,
                    };
                }

                lr.Repository = ffgem.gameObject.GetOrAddComponent<ColliderRepository>();
                lr.MainParametricLight = RegisterReflection(tbpplrData.MainTubeBloomPrePassLight);
                lr.ParametricLightReflection =
                    tbpplrData.TubeBloomPrePassLightBounces.Select(RegisterReflection).ToArray();
                lr.EnvironmentLayerMask = library.LayerMaskLookup[tbpplrData.EnvironmentLayerMask[0]];

                lr.enabled = tbpplrData.IsEnabled;
            }
        }

        return;

        Vector3 FloatArrayToVector3(float[] array)
        {
            return new Vector3(array[0], array[1], array[2]);
        }

        void GetAndRegisterLight(EnvDataObject envObject, int order = -1, int instanceId = -1)
        {
            if (!chromaIdObjects.TryGetValue(envObject.ChromaID, out var marker)) return;
            var go = marker.gameObject;

            if (envObject.Components.DirectionalLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.DirectionalLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.DirectionalLightWithId.Where(x => !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleDirectionalLightWithId(comp, go, order);
            }

            if (envObject.Components.InstancedMaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.InstancedMaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.InstancedMaterialLightWithId.Where(x =>
                        !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleInstancedMaterialLightWithId(comp, go, order);
            }

            if (envObject.Components.MaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.MaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.MaterialLightWithId.Where(x => !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleMaterialLightWithId(comp, go, order);
            }

            if (envObject.Components.RectangleFakeGlowLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.RectangleFakeGlowLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.RectangleFakeGlowLightWithId.Where(x =>
                        !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleRectangleFakeGlowLightWithId(comp, go, order);
            }

            if (envObject.Components.SpriteLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.SpriteLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.SpriteLightWithId.Where(x => !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleSpriteLightWithId(comp, go, order);
            }

            if (envObject.Components.TubeBloomPrePassLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.TubeBloomPrePassLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.TubeBloomPrePassLightWithId.Where(x =>
                        !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleTubeBloomPrePassLightWithId(comp, go, order);
            }
        }

        void RegisterLight(LightController controller, int lightId, int order)
        {
            var lg = lcgemData?.LightGroups.FirstOrDefault(x =>
                x.StartLightId <= lightId && lightId < x.StartLightId + x.NumberOfElements);
            if (lg != null)
            {
                controller.Kind = LightController.LightKind.Group;
                controller.Type = lg.GroupId;
                controller.ID = lightId - lg.StartLightId;
                descriptor.Register(controller);
                return;
            }

            var lsee = lseeData.FirstOrDefault(x => x.LightsId == lightId);
            if (lsee != null)
            {
                controller.Kind = LightController.LightKind.Basic;
                controller.Type = ConvertUtils.ToEventType(lsee.EventType);
                controller.ID = order;
                descriptor.Register(controller);
                return;
            }

            Debug.LogError(
                $"{controller} ID {lightId} could not be registered, missing event type or group ID register?");
        }

        void HandleDirectionalLightWithId(
            DirectionalLightWithIdComponent comp,
            GameObject go,
            int order
        )
        {
            var dlc = go.AddComponent<DirectionalLightController>();
            dlc.Light = chromaIdObjects[comp.Light].GetComponent<DirectionalLight>();
            comp.CopyTo(dlc);
            RegisterLight(dlc, comp.Id, order);
        }

        void HandleInstancedMaterialLightWithId(
            InstancedMaterialLightWithIdComponent comp,
            GameObject go,
            int order
        )
        {
            var imlc = go.AddComponent<InstancedMaterialLightController>();
            // TODO: this should be string reference
            imlc.MpbColorSetter = go.GetOrAddComponent<MaterialPropertyBlockColorSetter>();
            comp.CopyTo(imlc);
            RegisterLight(imlc, comp.Id, order);
        }

        void HandleMaterialLightWithId(
            MaterialLightWithIdComponent comp,
            GameObject go,
            int order
        )
        {
            var mlc = go.AddComponent<MaterialLightController>();
            mlc.Renderer = go.GetComponent<Renderer>();
            comp.CopyTo(mlc);
            RegisterLight(mlc, comp.Id, order);
        }

        void HandleRectangleFakeGlowLightWithId(
            RectangleFakeGlowLightWithIdComponent comp,
            GameObject go,
            int order)
        {
            var rfglc = go.AddComponent<RectangleFakeGlowLightController>();
            rfglc.Renderer = go.GetComponent<Renderer>();
            var envObject =
                data.Objects.First(y => y.ChromaID == chromaIdObjects.First(x => x.Value == go).Key);
            comp.CopyTo(rfglc);
            envObject.Components.RectangleFakeGlow[0].CopyTo(rfglc);
            RegisterLight(rfglc, comp.Id, order);
        }

        void HandleSpriteLightWithId(
            SpriteLightWithIdComponent comp,
            GameObject go,
            int order)
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
            RegisterLight(slc, comp.Id, order);
        }

        void HandleTubeBloomPrePassLightWithId(
            TubeBloomPrePassLightWithIdComponent comp,
            GameObject go,
            int order
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
                var envObject = data.Objects.First(x =>
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
                var envObject = data.Objects.First(x =>
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

            RegisterLight(pbflc, comp.Id, order);
        }
    }
}
