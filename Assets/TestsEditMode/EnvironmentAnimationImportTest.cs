using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests.Editor
{
    public class EnvironmentAnimationImportTest
    {
        [Test]
        public void GeneratedScenesRetainNativeComponentBindings()
        {
            foreach (var asset in CreateUtils.GetEnvironmentDataRaw())
            {
                var data = CreateUtils.JsonToEnvironmentData(asset);
                var expected = data.Objects.Where(item =>
                    item.Components.RectangleFakeGlowLightWithId != null
                    || item.Components.CustomBoundingBox != null
                    || item.Components.Animation != null
                    || item.Components.ParticleSystemEventEffect != null).ToArray();
                if (expected.Length == 0)
                    continue;

                var scene = EditorSceneManager.OpenPreviewScene($"Assets/__Scenes/Environments/{asset.name}.unity");
                try
                {
                    var markers = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<ChromaIDMarker>(true))
                        .ToDictionary(marker => marker.ChromaID);
                    foreach (var item in expected)
                    {
                        var owner = markers[item.ChromaID].gameObject;
                        if (item.Components.RectangleFakeGlowLightWithId != null)
                        {
                            var glow = owner.GetComponents<RectangleFakeGlowLightController>();
                            Assert.That(glow.Length, Is.EqualTo(1), item.ChromaID);
                            var record = item.Components.RectangleFakeGlowLightWithId[0];
                            Assert.That(glow[0].MinAlpha, Is.EqualTo(record.MinAlpha), item.ChromaID);
                            Assert.That(glow[0].AlphaMultiplier, Is.EqualTo(record.AlphaMultiplier), item.ChromaID);
                        }

                        if (item.Components.CustomBoundingBox != null)
                            Assert.That(owner.GetComponent<NativeRendererBounds>(), Is.Not.Null, item.ChromaID);

                        if (item.Components.Animation != null)
                        {
                            var animation = owner.GetComponent<TimelineLegacyAnimation>();
                            Assert.That(animation, Is.Not.Null, item.ChromaID);
                            Assert.That(animation.enabled, Is.True, item.ChromaID);
                            Assert.That(animation.Clip, Is.Not.Null, item.ChromaID);
                        }

                        if (item.Components.ParticleSystemEventEffect != null)
                        {
                            var timeline = owner.GetComponent<ParticleLightTimeline>();
                            Assert.That(timeline, Is.Not.Null, item.ChromaID);
                            Assert.That(timeline.Visual, Is.SameAs(owner.GetComponent<ParticleSystemEffect>()));
                            Assert.That(timeline.Visual.ParticleSystem, Is.Not.Null, item.ChromaID);
                        }

                    }

                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }

            }

        }

        [Test]
        public void PlatformGlowLightRecordsAreImportedInEveryExport()
        {
            CheckImportedRecords("RectangleFakeGlowLightWithId", 45);
        }

        [Test]
        public void NativeRendererBoundsRecordsAreImportedInEveryExport()
        {
            CheckImportedRecords("CustomBoundingBox", 14);
        }

        [Test]
        public void LegacyAnimationRecordsAreImportedInEveryExport()
        {
            CheckImportedRecords("Animation", 20);
        }

        [Test]
        public void NativeBoundsApplyTheExportedCenterAndExtents()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/__Scenes/Environments/Data/BillieEnvironment.json");
            var source = JSON.Parse(asset.text);
            var item = source["objects"].Children.Single(value => value["name"].Value == "Rain");
            var native = item["components"]["CustomBoundingBox"][0];
            var data = CreateUtils.JsonToEnvironmentData(asset);
            var component = CreateContainer.CollectComponentInstances(data)[native["instanceId"].AsInt];
            var root = new GameObject("Rain bounds import regression");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>();
                component.SpawnComponent(root);
                component.Apply(new CreateContainer { Data = data });
                Assert.That(renderer.localBounds.center, Is.EqualTo(Vector(native["customBoundCenter"])));
                Assert.That(renderer.localBounds.extents, Is.EqualTo(Vector(native["customBoundSize"])));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

        }

        [Test]
        public void PlatformGlowRecordsShareOneControllerAcrossRebinding()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/__Scenes/Environments/Data/DefaultEnvironment.json");
            var data = CreateUtils.JsonToEnvironmentData(asset);
            var item = data.Objects.Single(value => value.Components.RectangleFakeGlow != null);
            var geometry = item.Components.RectangleFakeGlow[0];
            var light = item.Components.RectangleFakeGlowLightWithId[0];
            var root = new GameObject("Platform glow import regression");
            try
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    geometry.SpawnComponent(root, root.GetComponent<RectangleFakeGlowLightController>());
                    light.SpawnComponent(root);
                    Assert.That(light.Instance, Is.SameAs(geometry.Instance));
                    Assert.That(root.GetComponents<RectangleFakeGlowLightController>().Length, Is.EqualTo(1));
                }

            }
            finally
            {
                Object.DestroyImmediate(root);
            }

        }

        private static Vector3 Vector(JSONNode value) =>
            new(value[0].AsFloat, value[1].AsFloat, value[2].AsFloat);

        private static void CheckImportedRecords(string componentKey, int expectedCount)
        {
            var count = 0;
            var missing = new List<string>();
            foreach (var asset in CreateUtils.GetEnvironmentDataRaw())
            {
                var source = JSON.Parse(asset.text);
                var imported = CreateUtils.JsonToEnvironmentData(asset);
                var components = CreateContainer.CollectComponentInstances(imported);
                foreach (var item in source["objects"].Children)
                {
                    var records = item["components"][componentKey];
                    if (records == null)
                        continue;

                    foreach (var record in records.Children)
                    {
                        count++;
                        if (!components.ContainsKey(record["instanceId"].AsInt))
                            missing.Add(item["id"].Value);
                    }

                }

            }

            Assert.That(count, Is.EqualTo(expectedCount), "The regression must exercise the actual exported records.");
            Assert.That(missing, Is.Empty, $"{componentKey} records were skipped:\n" + string.Join("\n", missing));
        }
    }
}
