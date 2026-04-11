using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Enums;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Axis = Beatmap.Enums.Axis;

public partial class EnvironmentSceneCreator
{
    private static void BuildComponents(
        EnvironmentData environmentData,
        CreateContainer container)
    {
        var descriptor = GameObject.Find("Environment").AddComponent<EnvironmentDescriptor>();
        descriptor.ID = environmentData.Data.ID;

        descriptor.ColorSchemeProvider = descriptor.gameObject.AddComponent<ColorSchemeProvider>();
        descriptor.SpectrogramDataProvider = descriptor.gameObject.AddComponent<SpectrogramDataProvider>();

        environmentData.Data.FogParameters.CopyTo(descriptor.BloomFogParams);
        environmentData.Data.SizeData.CopyTo(descriptor.SizeData);

        var beec = new GameObject("BasicEventEffectController").AddComponent<BasicEventEffectManager>();
        beec.gameObject.transform.SetParent(GameObject.Find("Environment").transform);
        descriptor.BasicEventEffectManager = beec;
        var cbe = beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);
        cbe.ColorSchemeProvider = descriptor.ColorSchemeProvider;

        // MPB stuff
        foreach (var obj in environmentData.Objects.Where(x => x.Components.MaterialPropertyBlockController != null))
        foreach (var data in obj.Components.MaterialPropertyBlockController)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.MaterialPropertyBlockColorSetter != null))
        foreach (var data in obj.Components.MaterialPropertyBlockColorSetter)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.MaterialPropertyBlockControllerArrayRandomValueSetter != null))
        foreach (var data in obj.Components.MaterialPropertyBlockControllerArrayRandomValueSetter)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.MaterialPropertyBlockControllerRandomValueSetter != null))
        foreach (var data in obj.Components.MaterialPropertyBlockControllerRandomValueSetter)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in
            environmentData.Objects.Where(x => x.Components.MaterialPropertyBlockPositionUpdater != null))
        foreach (var data in obj.Components.MaterialPropertyBlockPositionUpdater)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.MaterialPropertyBlockRandomValueSetter != null))
        foreach (var data in obj.Components.MaterialPropertyBlockRandomValueSetter)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.MaterialPropertyValuesSetter != null))
        foreach (var data in obj.Components.MaterialPropertyValuesSetter)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        // bloom pre pass
        foreach (var obj in
            environmentData.Objects.Where(x => x.Components.BloomPrePassBackgroundColorsGradient != null))
        foreach (var data in obj.Components.BloomPrePassBackgroundColorsGradient)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.BloomPrePassBackgroundNonLightInstancedGroupRenderer != null))
        foreach (var data in obj.Components.BloomPrePassBackgroundNonLightInstancedGroupRenderer)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.BloomPrePassBackgroundNonLightRenderer != null))
        foreach (var data in obj.Components.BloomPrePassBackgroundNonLightRenderer)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        // other stuff components
        foreach (var obj in environmentData.Objects.Where(x => x.Components.TextureProcessor3D != null))
        foreach (var data in obj.Components.TextureProcessor3D)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.GridElementController != null))
        foreach (var data in obj.Components.GridElementController)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.BakedLightsNormalizer != null))
        foreach (var data in obj.Components.BakedLightsNormalizer)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.Mirror != null))
        foreach (var data in obj.Components.Mirror)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.LightManager != null))
        foreach (var data in obj.Components.LightManager)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.SDFPoint != null))
        foreach (var data in obj.Components.SDFPoint)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.SDFArrayManager != null))
        foreach (var data in obj.Components.SDFArrayManager)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.Spectrogram != null))
        foreach (var data in obj.Components.Spectrogram)
        {
            var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
            comp.SpectrogramDataProvider = descriptor.SpectrogramDataProvider;
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.SpectrogramRowPropertyAnimator != null))
        foreach (var data in obj.Components.SpectrogramRowPropertyAnimator)
        {
            var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
            comp.SpectrogramDataProvider = descriptor.SpectrogramDataProvider;
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.TransformSpectrogram != null))
        foreach (var data in obj.Components.TransformSpectrogram)
        {
            var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
            comp.SpectrogramDataProvider = descriptor.SpectrogramDataProvider;
        }

        // core lighting stuff
        var lightWithIds = new Dictionary<string, MonoBehaviour>();

        foreach (var obj in environmentData.Objects.Where(x => x.Components.DirectionalLight != null))
        foreach (var data in obj.Components.DirectionalLight)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.PointLight != null))
        foreach (var data in obj.Components.PointLight)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.BloomPrePassBackgroundColorsGradientTintColorWithLightId != null))
        {
            foreach (var data in obj.Components.BloomPrePassBackgroundColorsGradientTintColorWithLightId)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.ColorArrayLightWithIds != null))
        {
            foreach (var data in obj.Components.ColorArrayLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.DirectionalLightWithIds != null))
        {
            foreach (var data in obj.Components.DirectionalLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.DirectionalLightWithGroupIds != null))
        {
            foreach (var data in obj.Components.DirectionalLightWithGroupIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.GlobalShaderColorLightWithIds != null))
        {
            foreach (var data in obj.Components.GlobalShaderColorLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.LightmapLightWithIds != null))
        {
            foreach (var data in obj.Components.LightmapLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.LightmapLightsWithIds != null))
        {
            foreach (var data in obj.Components.LightmapLightsWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.MaterialLightWithIds != null))
        {
            foreach (var data in obj.Components.MaterialLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.MixedLightsColorSetterRuntimeLightWithIds != null))
        {
            foreach (var data in obj.Components.MixedLightsColorSetterRuntimeLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.ParticleSystemLightWithIds != null))
        {
            foreach (var data in obj.Components.ParticleSystemLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.PointLightWithIds != null))
        {
            foreach (var data in obj.Components.PointLightWithIds)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                lightWithIds.Add(obj.ChromaID, comp);
            }
        }

        // core components
        var lcgemData = environmentData
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
            {
                lightColorGroupEffect.ColorSchemeProvider = descriptor.ColorSchemeProvider;
                lightColorGroupEffect.ColorBoostEffect = cbe;
            }
        }

        var idRemapAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                Path.Combine(Constants.EditorPath, "LightIDTables", environmentData.Data.ID + ".json"));
        var typeIdRemap = new Dictionary<int, Dictionary<int, int>>();
        if (idRemapAsset != null)
            typeIdRemap = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, int>>>(idRemapAsset.text);

        var lseeData = environmentData
            .Objects
            .Where(x => x.Components.LightSwitchEventEffect != null)
            .SelectMany(x => x.Components.LightSwitchEventEffect)
            .ToArray();
        foreach (var data in lseeData)
        {
            var comp = beec.Register<BasicLightEffect>(ConvertUtils.ToEventType(data.EventType));
            data.CopyTo(comp);
            comp.ColorSchemeProvider = descriptor.ColorSchemeProvider;
            comp.ColorBoostEffect = cbe;
            foreach (var (original, remap) in typeIdRemap.GetValueOrDefault(data.LightsId, new Dictionary<int, int>()))
                comp.LightIdRemapEntries.Add(new Vector2(original, remap));
        }

        var registeredLightInstance = new HashSet<int>();
        var lightToRegister = new List<(LightController controller, int lightId, int order, bool force)>();
        var sinkObject = new GameObject("Sink Object");
        sinkObject.transform.SetParent(beec.transform.parent);

        var lightWithIdManager = environmentData
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

                    var envObject = environmentData.Objects.Find(x => x.ChromaID == light.ObjectId);
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
        foreach (var envObject in environmentData.Objects)
        {
            GetAndRegisterLight(
                envObject); // TODO: the rest of id, which is likely bad for lightId if they were inactive
        }

        var lrgemData = environmentData
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
                        var t = container.ChromaIdObjects[transformName].transform;
                        t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        lrgem.Register(data.GroupId, i, axis, mirror, t.gameObject.transform);
                    }
                }
            }
        }

        var ltgemData = environmentData
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
                    new[] { data.xTranslationLimits, data.yTranslationLimits, data.zTranslationLimits },
                    new[] { data.xDistributionLimits, data.yDistributionLimits, data.zDistributionLimits });

                RegisterTranslation(Axis.X, data.XTransforms, data.MirrorX);
                RegisterTranslation(Axis.Y, data.YTransforms, data.MirrorY);
                RegisterTranslation(Axis.Z, data.ZTransforms, data.MirrorZ);
                continue;

                void RegisterTranslation(Axis axis, string[] transforms, bool mirror)
                {
                    for (var i = 0; i < transforms.Length; i++)
                    {
                        var transformName = transforms[i];
                        var t = container.ChromaIdObjects[transformName].transform;
                        t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                        ltgem.Register(data.GroupId, i, axis, mirror, t.gameObject.transform);
                    }
                }
            }
        }

        var ffgemData = environmentData
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
        foreach (var obj in environmentData.Objects.Where(x => x.Components.TrackLaneRingsManager != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsManager)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Start();
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.TrackLaneRingsRotationEffect != null))
        foreach (var data in obj.Components.TrackLaneRingsRotationEffect)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in
            environmentData.Objects.Where(x => x.Components.TrackLaneRingsRotationEffectSpawner != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsRotationEffectSpawner)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                beec.Register(ConvertUtils.ToEventType(data.EventType), comp);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.TrackLaneRingsPositionStepEffectSpawner != null))
        {
            foreach (var data in obj.Components.TrackLaneRingsPositionStepEffectSpawner)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                var evt = beec.GetOrRegister<TrackLaneRingsPositionEffect>(
                    ConvertUtils.ToEventType(data.EventType));
                comp.EffectManager = evt;
            }
        }

        // ROTATION
        foreach (var obj in environmentData.Objects.Where(x => x.Components.LightRotationEventEffect != null))
        {
            foreach (var data in obj.Components.LightRotationEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                var lre = beec.GetOrRegister<LightRotationEffect>(ConvertUtils.ToEventType(data.EventType));
                comp.Effect = lre;
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.LightPairRotationEventEffect != null))
        {
            foreach (var data in obj.Components.LightPairRotationEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                if (ConvertUtils.ToEventType(data.EventTypeL, out var type) && type != -1)
                    comp.LeftEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.EventTypeR, out type) && type != -1)
                    comp.RightEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.SwitchOverrideRandomValuesEvent, out type) && type != -1)
                    comp.SwitchEffect = beec.GetOrRegister<GenericCallbackEventEffect>(type);
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.LightPairSinMoveEventEffect != null))
        {
            foreach (var data in obj.Components.LightPairSinMoveEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                if (ConvertUtils.ToEventType(data.EventTypeL, out var type) && type != -1)
                    comp.LeftEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.EventTypeR, out type) && type != -1)
                    comp.RightEffect = beec.GetOrRegister<LightRotationEffect>(type);
                if (ConvertUtils.ToEventType(data.SwitchOverrideRandomValuesEvent, out type) && type != -1)
                    comp.SwitchEffect = beec.GetOrRegister<GenericCallbackEventEffect>(type);
            }
        }

        // whatever this shit
        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.BackgroundTextureGradientSwitchEventEffect != null))
        foreach (var data in obj.Components.BackgroundTextureGradientSwitchEventEffect)
        {
            var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
            comp.Effect = cbe;
        }

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.BloomPrePassBackgroundColorsGradientFromColorSchemeColors != null))
        foreach (var data in obj.Components.BloomPrePassBackgroundColorsGradientFromColorSchemeColors)
        {
            var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
            comp.ColorSchemeProvider = descriptor.ColorSchemeProvider;
            comp.Effect = cbe;
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.GameObjectIntSwitchEventEffect != null))
        {
            foreach (var data in obj.Components.GameObjectIntSwitchEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Effect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.GameObjectSwitchEventEffect != null))
        {
            foreach (var data in obj.Components.GameObjectSwitchEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Effect = cbe;
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.HydraulicCarJumpEffect != null))
        {
            foreach (var data in obj.Components.HydraulicCarJumpEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Effect = beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.Event));
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.HydraulicCarSuspensionEffect != null))
        {
            foreach (var data in obj.Components.HydraulicCarSuspensionEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.ContractEffect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.ContractEvent));
                comp.ExpandEffect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.ExpandEvent));
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.MeshRendererSwitchEventEffect != null))
        {
            foreach (var data in obj.Components.MeshRendererSwitchEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Effect =
                    beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.CopyPosition != null))
        foreach (var data in obj.Components.CopyPosition)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.MovementBeatmapEventEffect != null))
        {
            foreach (var data in obj.Components.MovementBeatmapEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Effect = beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
            }
        }

        foreach (var obj in
            environmentData.Objects.Where(x => x.Components.ParticleSystemContinuousEventEffect != null))
        {
            foreach (var data in obj.Components.ParticleSystemContinuousEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Effect = beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.ParticleSystemEventEffect != null))
        {
            foreach (var data in obj.Components.ParticleSystemEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.ColorSchemeProvider = descriptor.ColorSchemeProvider;
                comp.Effect = beec.GetOrRegister<GenericCallbackEventEffect>(ConvertUtils.ToEventType(data.EventType));
            }
        }

        foreach (var obj in environmentData.Objects.Where(x => x.Components.SmoothStepPositionEventEffect != null))
        {
            foreach (var data in obj.Components.SmoothStepPositionEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                beec.Register(ConvertUtils.ToEventType(data.EventType), comp);
            }
        }

        // The freaky Fx
        foreach (var obj in environmentData.Objects.Where(x => x.Components.AlphaFloatFxGroupEffectTarget != null))
        foreach (var data in obj.Components.AlphaFloatFxGroupEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.ColliderEventEffect != null))
        {
            foreach (var data in obj.Components.ColliderEventEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Repository = ffgem.gameObject.GetOrAddComponent<ColliderRepository>();
            }
        }

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.FloatArrayMaterialPropertyEffectTarget != null))
        foreach (var data in obj.Components.FloatArrayMaterialPropertyEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.FloatFxGroupEffectCollectionTarget != null))
        foreach (var data in obj.Components.FloatFxGroupEffectCollectionTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.FloatLocalScaleEffect != null))
        foreach (var data in obj.Components.FloatLocalScaleEffect)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.FloatMaterialPropertyEffectTarget != null))
        foreach (var data in obj.Components.FloatMaterialPropertyEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.FloatSDFPointScaleEffect != null))
        foreach (var data in obj.Components.FloatSDFPointScaleEffect)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.FloatTextureProcessor3DMappingFloatEffectTarget != null))
        foreach (var data in obj.Components.FloatTextureProcessor3DMappingFloatEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.FloatTextureProcessor3DMappingVectorEffectTarget != null))
        foreach (var data in obj.Components.FloatTextureProcessor3DMappingVectorEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.FloatTextureProcessor3DMaterialSwitchEffectTarget != null))
        foreach (var data in obj.Components.FloatTextureProcessor3DMaterialSwitchEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.FloatTextureProcessor3DParameterEffectTarget != null))
        foreach (var data in obj.Components.FloatTextureProcessor3DParameterEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.FloatTextureProcessor3DPresetEffectTarget != null))
        foreach (var data in obj.Components.FloatTextureProcessor3DPresetEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.MoveInDirectionEffect != null))
        foreach (var data in obj.Components.MoveInDirectionEffect)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.Parametric3SliceSpriteWidthEndFloatFxEffectTarget != null))
        foreach (var data in obj.Components.Parametric3SliceSpriteWidthEndFloatFxEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.SpectrogramMultiplierFloatFxEffectTarget != null))
        foreach (var data in obj.Components.SpectrogramMultiplierFloatFxEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.StepFloatMaterialEffectTarget != null))
        foreach (var data in obj.Components.StepFloatMaterialEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.SwitchGameObjectArrayEffectTarget != null))
        foreach (var data in obj.Components.SwitchGameObjectArrayEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x => x.Components.SwitchGameObjectEffectTarget != null))
        foreach (var data in obj.Components.SwitchGameObjectEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.VertexDisplacementFloatFxGroupEffectTarget != null))
        foreach (var data in obj.Components.VertexDisplacementFloatFxGroupEffectTarget)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        var tffgemData = environmentData
            .Objects
            .FirstOrDefault(x => x.Components.TriggerFloatFxGroupEffectManager != null)
            ?.Components.TriggerFloatFxGroupEffectManager[0];

        if (ffgemData != null)
        {
            foreach (var data in ffgemData.FloatFxGroupEffects)
            {
                var fx = container.ChromaIdObjects[data.Target].GetComponent<FxTarget>();
                if (fx == null) continue;
                ffgem.Register(data.GroupId, data.ElementId, fx);
            }
        }

        if (tffgemData != null)
        {
            foreach (var data in tffgemData.FloatFxGroupEffects)
            {
                var fx = container.ChromaIdObjects[data.Target].GetComponent<FxTarget>();
                if (fx == null) continue;
                ffgem.Register(data.GroupId, data.ElementId, fx);
            }
        }

        // the whatever collider
        foreach (var obj in
            environmentData.Objects.Where(x => x.Components.TubeBloomPrePassLightCollisionEffect != null))
        foreach (var data in obj.Components.TubeBloomPrePassLightCollisionEffect)
            data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);

        foreach (var obj in environmentData.Objects.Where(x =>
            x.Components.TubeBloomPrePassLightReflectionEffect != null))
        {
            foreach (var data in obj.Components.TubeBloomPrePassLightReflectionEffect)
            {
                var comp = data.Apply(container.GetGameObjectOrNull(obj.ChromaID), container);
                comp.Repository = ffgem.gameObject.GetOrAddComponent<ColliderRepository>();
            }
        }

        FinalRegisterLight();

        return;

        void GetAndRegisterLight(
            EnvironmentDataObject environmentObject,
            int order = -1,
            int instanceId = -1,
            bool force = false)
        {
            if (!container.ChromaIdObjects.TryGetValue(environmentObject.ChromaID, out var marker)) return;
            var go = marker.gameObject;

            if (environmentObject.Components.BloomPrePassBackgroundColorsGradientElementWithLightId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.BloomPrePassBackgroundColorsGradientElementWithLightId.Where(x =>
                        x.InstanceId == instanceId)
                    : environmentObject.Components.BloomPrePassBackgroundColorsGradientElementWithLightId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l)
                    HandleBloomPrePassBackgroundColorsGradientElementWithLightId(comp, go, order, force);
            }

            if (environmentObject.Components.BloomPrePassBackgroundColorsGradientTintColorWithLightIds != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.BloomPrePassBackgroundColorsGradientTintColorWithLightIds.Where(x =>
                        x.InstanceId == instanceId)
                    : environmentObject.Components.BloomPrePassBackgroundColorsGradientTintColorWithLightIds.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l)
                    HandleBloomPrePassBackgroundColorsGradientTintColorWithLightIds(comp, go, order, force);
            }

            if (environmentObject.Components.DirectionalLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.DirectionalLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.DirectionalLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleDirectionalLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.EnableRendererLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.EnableRendererLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.EnableRendererLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleEnableRendererLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.InstancedMaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.InstancedMaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.InstancedMaterialLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleInstancedMaterialLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.MaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.MaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.MaterialLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleMaterialLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.ParticleSystemLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.ParticleSystemLightWithId.Where(x =>
                        x.InstanceId == instanceId)
                    : environmentObject.Components.ParticleSystemLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleParticleSystemLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.RectangleFakeGlowLightWithLightId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.RectangleFakeGlowLightWithLightId.Where(x =>
                        x.InstanceId == instanceId)
                    : environmentObject.Components.RectangleFakeGlowLightWithLightId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleRectangleFakeGlowLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.SpriteArrayLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.SpriteArrayLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.SpriteArrayLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleSpriteArrayLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.SpriteLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.SpriteLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.SpriteLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleSpriteLightWithId(comp, go, order, force);
            }

            if (environmentObject.Components.TubeBloomPrePassLightWithId != null)
            {
                var l = instanceId != -1
                    ? environmentObject.Components.TubeBloomPrePassLightWithId.Where(x => x.InstanceId == instanceId)
                    : environmentObject.Components.TubeBloomPrePassLightWithId.Where(x =>
                        !registeredLightInstance.Contains(x.InstanceId));
                foreach (var comp in l) HandleTubeBloomPrePassLightWithId(comp, go, order, force);
            }
        }

        void RegisterLight(LightController controller, int lightId, int order, bool force = false) =>
            lightToRegister.Add((controller, lightId, order, force));

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

        void HandleBloomPrePassBackgroundColorsGradientElementWithLightId(
            BloomPrePassBackgroundColorsGradientElementWithLightIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleBloomPrePassBackgroundColorsGradientTintColorWithLightIds(
            BloomPrePassBackgroundColorsGradientTintColorWithLightIdsData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleDirectionalLightWithId(
            DirectionalLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleEnableRendererLightWithId(
            EnableRendererLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleInstancedMaterialLightWithId(
            InstancedMaterialLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleMaterialLightWithId(
            MaterialLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleParticleSystemLightWithId(
            ParticleSystemLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleRectangleFakeGlowLightWithId(
            RectangleFakeGlowLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            var envObject =
                environmentData.Objects.First(y =>
                    y.ChromaID == container.ChromaIdObjects.First(x => x.Value == go).Key);
            envObject.Components.RectangleFakeGlow[0].CopyTo(comp);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleSpriteArrayLightWithId(
            SpriteArrayLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleSpriteLightWithId(
            SpriteLightWithIdData data,
            GameObject go,
            int order,
            bool force)
        {
            var comp = data.Apply(go, container);
            RegisterLight(comp, data.Id, order, force);
        }

        void HandleTubeBloomPrePassLightWithId(
            TubeBloomPrePassLightWithIdData data,
            GameObject go,
            int order,
            bool force
        )
        {
            var comp = go.AddComponent<ParametricBloomFogLightController>();
            comp.BloomFog = go.AddComponent<BloomFogObject>();
            data.CopyTo(comp);

            // Set up physical light object
            if (!string.IsNullOrEmpty(data.TubeBloomPrePassLight.ParametricBoxId)
                && data.TubeBloomPrePassLight.ParametricBoxId != "null")
            {
                var boxLight = container.ChromaIdObjects[data.TubeBloomPrePassLight.ParametricBoxId];
                var envObject = environmentData.Objects.First(x =>
                    x.ChromaID == data.TubeBloomPrePassLight.ParametricBoxId);

                comp.BoxLight = boxLight.AddComponent<ParametricBoxLight>();
                comp.BoxLight.Renderer = boxLight.GetComponent<Renderer>();
                envObject.Components.ParametricBoxController[0].CopyTo(comp.BoxLight);
            }

            // Set up sprite light object
            if (!string.IsNullOrEmpty(data.TubeBloomPrePassLight.SliceSpriteControllerId)
                && data.TubeBloomPrePassLight.SliceSpriteControllerId != "null")
            {
                var spriteLight = container.ChromaIdObjects[data.TubeBloomPrePassLight.SliceSpriteControllerId];
                var envObject = environmentData.Objects.First(x =>
                    x.ChromaID == data.TubeBloomPrePassLight.SliceSpriteControllerId);

                comp.SpriteLight = spriteLight.AddComponent<ParametricSpriteLight>();
                comp.SpriteLight.Renderer = spriteLight.GetComponent<Renderer>();

                // Good chance env data doesnt have this and it's fine
                if (comp.SpriteLight.Renderer == null || comp.SpriteLight.GetComponent<MeshFilter>() == null)
                {
                    var mesh = spriteLight.GetOrAddComponent<MeshFilter>();
                    mesh.sharedMesh = container.Library.SliceSprite;
                    var renderer = spriteLight.GetOrAddComponent<MeshRenderer>();
                    if (envObject.Components.MeshRenderer?.First().Materials.Any() ?? false)
                    {
                        if (container.Library.Materials.Lookup.TryGetValue(
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

                    comp.SpriteLight.Renderer = renderer;
                }

                envObject.Components.Parametric3SliceSpriteController[0].CopyTo(comp.SpriteLight);
            }

            RegisterLight(comp, data.Id, order, force);
        }
    }
}
