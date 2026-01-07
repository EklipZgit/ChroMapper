using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Enums;
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
        var cbe = beec.Register<ColorBoostEffect>((int)EventTypeValue.ColorBoost);

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

        var lseeData = data
            .Objects
            .Where(x => x.Components.LightSwitchEventEffect != null)
            .SelectMany(x => x.Components.LightSwitchEventEffect)
            .ToArray();
        foreach (var d in lseeData)
        {
            var ble = beec.Register<BasicLightEffect>(ConvertUtils.ToEventType(d.EventType));
            ble.ColorBoostEffect = cbe;
            ble.OffIntensity = d.OffColorIntensity;
            ble.LightOnStart = d.LightOnStart;
            // ble.InvertColorScheme = 
        }

        var idRemapAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(Path.Combine(editorPath, "LightIDTables", data.Data.ID + ".json"));
        var typeIdRemap = new Dictionary<string, Dictionary<string, int>>();
        if (idRemapAsset != null)
            typeIdRemap = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(idRemapAsset.text);

        var registeredLight = new HashSet<int>();

        var lightWithIdManager = data
            .Objects.FirstOrDefault(x => x.Components.LightWithIdManager != null)
            ?.Components.LightWithIdManager[0];
        if (lightWithIdManager != null)
        {
            foreach (var lights in lightWithIdManager.Lights)
            {
                for (var id = 0; id < lights.Length; id++)
                {
                    var light = lights[id];
                    if (light is null) continue;
                    var envObject = data.Objects.Find(x => x.ChromaID == light.ObjectId);
                    if (envObject is null) continue;
                    GetAndRegisterLight(envObject, id, light.InstanceId);
                    registeredLight.Add(light.InstanceId);
                }
            }
        }

        foreach (var envObject in data.Objects) GetAndRegisterLight(envObject);

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
                var rT = chromaIdObjects[lpreData.TransformR].transform;

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
                var rT = chromaIdObjects[lpsmeData.TransformR].transform;

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
                                    .GameObjectIds.Select(y => chromaIdObjects.TryGetValue(y, out var g) ? g : null)
                                    .Where(y => y != null)
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
                    .DeactivateOnBoostObjects.Select(y => chromaIdObjects.TryGetValue(y, out var g) ? g : null)
                    .Where(y => y != null)
                    .ToArray();
                gos.BoostGameObjects = goseData
                    .ActivateOnBoostObjects.Select(y => chromaIdObjects.TryGetValue(y, out var g) ? g : null)
                    .Where(y => y != null)
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
                    .ToArray();
                mrs.BoostRenderers = mrseData
                    .ActivateOnBoostRenderers.Select(y =>
                        chromaIdObjects.TryGetValue(y, out var g) ? g.GetComponent<Renderer>() : null)
                    .Where(y => y != null)
                    .ToArray();
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

        void GetAndRegisterLight(EnvDataObject envObject, int order = -1, int instanceId = -1)
        {
            var marker = chromaIdObjects[envObject.ChromaID];
            var go = marker.gameObject;

            if (envObject.Components.MaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.MaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.MaterialLightWithId.Where(x => !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleMaterialLightWithId(comp, go, order);
            }

            if (envObject.Components.InstancedMaterialLightWithId != null)
            {
                var l = instanceId != -1
                    ? envObject.Components.InstancedMaterialLightWithId.Where(x => x.InstanceId == instanceId)
                    : envObject.Components.InstancedMaterialLightWithId.Where(x =>
                        !registeredLight.Contains(x.InstanceId));
                foreach (var comp in l) HandleInstancedMaterialLightWithId(comp, go, order);
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

        void RegisterLight(int lightId, int order, LightController controller)
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
                order += 1;
                if (typeIdRemap.TryGetValue(lightId.ToString(), out var idRemap)
                    && idRemap.TryGetValue(order.ToString(), out var newOrder))
                    order = newOrder;

                controller.Kind = LightController.LightKind.Basic;
                controller.Type = ConvertUtils.ToEventType(lsee.EventType);
                controller.ID = order;
                descriptor.Register(controller, false);
                return;
            }

            Debug.LogError(
                $"{controller} ID {lightId} could not be registered, missing event type or group ID register?");
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
            RegisterLight(comp.Id, order, mlc);
        }

        void HandleInstancedMaterialLightWithId(
            InstancedMaterialLightWithIdComponent comp,
            GameObject go,
            int order
        )
        {
            var imlc = go.AddComponent<InstancedMaterialLightController>();
            imlc.Renderer = go.GetComponent<Renderer>();
            comp.CopyTo(imlc);
            RegisterLight(comp.Id, order, imlc);
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
            RegisterLight(comp.Id, order, rfglc);
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
            RegisterLight(comp.Id, order, slc);
        }

        void HandleTubeBloomPrePassLightWithId(
            TubeBloomPrePassLightWithIdComponent comp,
            GameObject go,
            int order
        )
        {
            var pbflc = go.AddComponent<ParametricBloomFogLightController>();

            // Set up bloom fog object
            pbflc.BloomFog = go.AddComponent<BloomFogObject>();
            pbflc.BloomFog.Length = comp.TubeBloomPrePassLight.TubeLength;
            pbflc.BloomFog.Width = comp.TubeBloomPrePassLight.TubeWidth;
            pbflc.BloomFog.Center = comp.TubeBloomPrePassLight.Center;
            pbflc.BloomFog.Height = comp.TubeBloomPrePassLight.Height;

            pbflc.BloomFog.StartWidth = comp.TubeBloomPrePassLight.StartWidth;
            pbflc.BloomFog.EndWidth = comp.TubeBloomPrePassLight.EndWidth;

            pbflc.BloomFog.StartAlpha = comp.TubeBloomPrePassLight.StartAlpha;
            pbflc.BloomFog.EndAlpha = comp.TubeBloomPrePassLight.EndAlpha;

            pbflc.BloomFog.LightWidthMultiplier =
                comp.TubeBloomPrePassLight.LightWidthMultiplier;
            pbflc.BloomFog.IntensityMultiplier =
                comp.TubeBloomPrePassLight.BloomFogIntensityMultiplier;

            pbflc.BloomFog.BoostToWhite = comp.TubeBloomPrePassLight.BoostToWhite;

            pbflc.BloomFog.LimitAlpha = comp.TubeBloomPrePassLight.LimitAlpha;
            pbflc.BloomFog.MinAlpha = comp.TubeBloomPrePassLight.MinAlpha;
            pbflc.BloomFog.MaxAlpha = comp.TubeBloomPrePassLight.MaxAlpha;

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
                    var mesh = spriteLight.TryGetComponent<MeshFilter>(out var mf)
                        ? mf
                        : spriteLight.AddComponent<MeshFilter>();
                    mesh.sharedMesh = library.SliceSprite;
                    var renderer = spriteLight.TryGetComponent<MeshRenderer>(out var mr)
                        ? mr
                        : spriteLight.AddComponent<MeshRenderer>();
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

            RegisterLight(comp.Id, order, pbflc);
        }
    }
}
