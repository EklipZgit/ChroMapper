using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class GLSInitialStateTest : TestBase
    {
        private static readonly string[] Environments =
        {
            "BritneyEnvironment", "ColdplayEnvironment", "ColliderEnvironment", "DaftPunkEnvironment",
            "Dragons2Environment", "GridEnvironment", "HipHopEnvironment", "LatticeEnvironment",
            "LinkinPark2Environment", "MetallicaEnvironment", "Monstercat2Environment", "Panic2Environment",
            "ProdigyEnvironment", "QueenEnvironment", "RockMixtapeEnvironment", "TheRollingStonesEnvironment",
            "TheWeekndEnvironment"
        };

        private static readonly string[] ScaleEnvironments = { "ColdplayEnvironment", "GridEnvironment" };

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator FreshRotationMatchesRefresh([ValueSource(nameof(Environments))] string environment)
        {
            yield return LoadEnvironment(environment);
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var transforms = descriptor.GetComponentsInChildren<Transform>(true);
            var rotations = transforms.Select(transform => transform.localRotation).ToArray();
            descriptor.LightRotationGroupEffectManager.Refresh();
            var changes = new List<string>();
            for (var index = 0; index < transforms.Length; index++)
            {
                var angle = Quaternion.Angle(rotations[index], transforms[index].localRotation);
                if (angle > 0.01f)
                    changes.Add($"{GetObjectId(transforms[index])}: {angle} degrees");
            }

            Assert.That(changes.Count, Is.Zero, string.Join("\n", changes.Take(12)));
        }

        [UnityTest]
        public IEnumerator FreshLightIntensityMatchesRefresh([ValueSource(nameof(Environments))] string environment)
        {
            yield return LoadEnvironment(environment);
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var lights = descriptor.GetComponentsInChildren<LightController>(true)
                .Where(light => light.Kind == LightController.LightKind.Group)
                .ToArray();
            var colors = lights.Select(light => light.Color).ToArray();
            descriptor.LightColorGroupEffectManager.Refresh();
            var changes = new List<string>();
            for (var index = 0; index < lights.Length; index++)
            {
                var before = colors[index];
                var after = lights[index].Color;
                var colorDistance = Vector3.Distance(
                    new Vector3(before.r, before.g, before.b),
                    new Vector3(after.r, after.g, after.b));
                if (Mathf.Abs(before.a - after.a) > 0.0001f
                    || (Mathf.Max(before.a, after.a) > 0.0001f && colorDistance > 0.0001f))
                {
                    changes.Add($"{GetObjectId(lights[index].transform)}: {before} -> {after}");
                }
            }

            Assert.That(changes.Count, Is.Zero, string.Join("\n", changes.Take(12)));
        }

        [UnityTest]
        public IEnumerator FreshFloatFxMatchesRefresh([ValueSource(nameof(Environments))] string environment)
        {
            yield return LoadEnvironment(environment);
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var before = CaptureFxValues(descriptor);
            descriptor.FloatFxGroupEffectManager.Refresh();
            var after = CaptureFxValues(descriptor);
            var changes = before.Where(pair => Mathf.Abs(pair.Value - after[pair.Key]) > 0.0001f)
                .Select(pair => $"{pair.Key}: {pair.Value} -> {after[pair.Key]}")
                .ToArray();
            Assert.That(changes.Length, Is.Zero, string.Join("\n", changes.Take(12)));
        }

        [UnityTest]
        public IEnumerator ContinuousFloatFxScaleEditsAndReloadRestoreInitialValue(
            [ValueSource(nameof(ScaleEnvironments))] string environment)
        {
            yield return LoadEnvironment(environment);
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var coldplay = environment == "ColdplayEnvironment";
            var targetName = coldplay
                ? "SphereMesh"
                : "Chevron";
            var target = descriptor.GetComponentsInChildren<LocalScaleFx>(true)
                .SelectMany(effect => effect.TargetTransforms)
                .First(
                    transform => transform.name == targetName
                        && (!coldplay || GetObjectId(transform).Contains("LasersWrapperL")));
            Assert.That(target.localScale.magnitude, Is.LessThan(0.0001f));
            var group = new BaseVfxEventEventBoxGroup
            {
                ID = coldplay
                    ? 32
                    : 43,
                JsonTime = 0f,
                Boxes =
                {
                    new BaseVfxEventEventBox
                    {
                        Events = new[] { new BaseFxEventFloat { Value = 1f } }
                    }
                }
            };
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            Assert.That(descriptor.FloatFxGroupEffectManager.InsertData(group), Is.True);
            var expectedScale = coldplay
                ? Vector3.one * 0.75f
                : Vector3.one;
            Assert.That(Vector3.Distance(target.localScale, expectedScale), Is.LessThan(0.0001f));
            descriptor.FloatFxGroupEffectManager.Reinitialize();
            Assert.That(target.localScale.magnitude, Is.LessThan(0.0001f));
            yield return LoadEnvironment(environment);
            descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var targets = descriptor.GetComponentsInChildren<LocalScaleFx>(true)
                .SelectMany(effect => effect.TargetTransforms)
                .Where(transform => transform.name == targetName)
                .ToArray();
            Assert.That(targets.Length, Is.GreaterThan(0));
            foreach (var reloadedTarget in targets)
                Assert.That(reloadedTarget.localScale.magnitude, Is.LessThan(0.0001f), GetObjectId(reloadedTarget));
        }

        private static Dictionary<string, float> CaptureFxValues(EnvironmentDescriptor descriptor)
        {
            var values = new Dictionary<string, float>();
            foreach (var transform in descriptor.GetComponentsInChildren<Transform>(true))
            {
                var id = GetObjectId(transform);
                var position = transform.localPosition;
                var scale = transform.localScale;
                values[id + "/position.x"] = position.x;
                values[id + "/position.y"] = position.y;
                values[id + "/position.z"] = position.z;
                values[id + "/scale.x"] = scale.x;
                values[id + "/scale.y"] = scale.y;
                values[id + "/scale.z"] = scale.z;
                values[id + "/active"] = transform.gameObject.activeSelf
                    ? 1f
                    : 0f;
            }

            foreach (var light in descriptor.GetComponentsInChildren<ParametricSpriteLight>(true))
                values[GetObjectId(light.transform) + "/WidthEnd"] = light.WidthEnd;

            foreach (var row in descriptor.GetComponentsInChildren<SpectrogramRowPropertyAnimator>(true))
                values[GetObjectId(row.transform) + "/Multiplier"] = row.Multiplier;

            foreach (var target in descriptor.GetComponentsInChildren<ColliderFx>(true))
                values[GetObjectId(target.transform) + "/ColliderFx.Value"] = target.Value;

            foreach (var target in descriptor.GetComponentsInChildren<SDFPointScaleFx>(true))
                values[GetObjectId(target.transform) + "/SqrtRadius"] = target.ColorPoint.SqrtRadius;

            foreach (var target in descriptor.GetComponentsInChildren<MpbFx>(true))
            {
                values[GetObjectId(target.transform) + "/" + target.PropertyName] =
                    target.MpbController.Mpb.GetFloat(target.PropertyName);
            }

            foreach (var target in descriptor.GetComponentsInChildren<MpbArrayFx>(true))
            {
                for (var index = 0; index < target.MpbControllers.Length; index++)
                {
                    values[GetObjectId(target.transform) + "/" + target.PropertyName + "/" + index] =
                        target.MpbControllers[index].Mpb.GetFloat(target.PropertyName);
                }
            }

            return values;
        }

        private static string GetObjectId(Transform transform)
        {
            var marker = transform.GetComponent<ChromaIDMarker>();
            return marker != null
                ? marker.ChromaID
                : transform.name + "/" + transform.GetInstanceID();
        }

        private static IEnumerator LoadEnvironment(string environment) => TestUtils.ReloadMap(
            3,
            new JSONObject { ["version"] = "3.3.0" },
            beatsPerMinute: 120f,
            environmentName: environment);

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
