using System.Collections;
using System.Linq;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class ParticleLightTimelineTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private ParticleSystemEffect particles;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator ParticleFlashHoldsItsColorWhilePaused()
        {
            yield return LoadDragons();
            var effect = FindLeftParticles();
            clock.MoveToJsonTime(2.25f);
            yield return null;
            yield return null;
            var before = effect.ParticleSystem.main.startColor.color;
            for (var frame = 0; frame < 8; frame++)
                yield return null;

            AssertColor(effect.ParticleSystem.main.startColor.color, before);
        }

        [UnityTest]
        public IEnumerator ParticleFlashUsesTheNativeUpdateDecay()
        {
            yield return LoadDragons();
            var effect = FindLeftParticles();
            var scheme = effect.ColorSchemeProvider.ColorScheme;
            clock.MoveToJsonTime(2f + (2f / 90f));
            yield return null;
            AssertColor(effect.ParticleSystem.main.startColor.color, scheme.EnvironmentLeftBoostColor);
            clock.MoveToJsonTime(3f);
            yield return null;
            var highlight = 1f;
            for (var frame = 0; frame < 44; frame++)
                highlight = Mathf.Lerp(highlight, 0f, 2f / 90f);

            AssertColor(
                effect.ParticleSystem.main.startColor.color,
                Color.Lerp(scheme.EnvironmentLeftColor, scheme.EnvironmentLeftBoostColor, highlight));
        }

        [UnityTest]
        public IEnumerator ChangingColorsRebuildsAFlashAtBeatZero()
        {
            yield return LoadDragons();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var original = ScriptableObject.CreateInstance<ColorSchemeSO>().Copy(context.ColorScheme);
            try
            {
                var timeline = particles.GetComponent<ParticleLightTimeline>();
                timeline.InsertData(new BaseEvent { Type = 2, Value = 2, songBpmTime = 0f });
                clock.MoveToJsonTime(0.25f);
                context.ColorScheme.EnvironmentLeftColor = new Color(0.7f, 0.2f, 0.4f, 1f);
                context.ColorScheme.EnvironmentLeftBoostColor = new Color(0.2f, 0.6f, 0.3f, 1f);
                context.NotifyColorScheme();
                var highlight = 1f;
                for (var frame = 0; frame < 11; frame++)
                    highlight = Mathf.Lerp(highlight, 0f, 2f / 90f);

                AssertColor(
                    particles.ParticleSystem.main.startColor.color,
                    Color.Lerp(context.ColorScheme.EnvironmentLeftColor, context.ColorScheme.EnvironmentLeftBoostColor, highlight));
            }
            finally
            {
                context.SetColorScheme(original);
                Object.DestroyImmediate(original);
            }

        }

        [UnityTest]
        public IEnumerator ParticleFlashRewindsToTheSameColor()
        {
            yield return LoadDragons();
            var effect = FindLeftParticles();
            clock.MoveToJsonTime(2.25f);
            yield return null;
            var expected = effect.ParticleSystem.main.startColor.color;
            clock.MoveToJsonTime(5f);
            yield return null;
            clock.MoveToJsonTime(2.25f);
            yield return null;
            AssertColor(effect.ParticleSystem.main.startColor.color, expected);
        }

        private IEnumerator LoadDragons()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(@"{""version"":""3.3.0"",""basicBeatmapEvents"":[
                    {""b"":2,""et"":2,""i"":2,""f"":1},
                    {""b"":2,""et"":3,""i"":6,""f"":1},
                    {""b"":6,""et"":2,""i"":0,""f"":1}
                ]}"),
                beatsPerMinute: 120f,
                environmentName: "DefaultEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var data = CreateUtils.JsonToEnvironmentData(AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/__Scenes/Environments/Data/DragonsEnvironment.json"));
            var record = data.Objects.Single(item => item.GameObjectName == "DragonsSidePSL");
            var owner = new GameObject("Dragons particle light regression");
            owner.SetActive(false);
            owner.transform.SetParent(context.Descriptor.transform, false);
            var system = owner.AddComponent<ParticleSystem>();
            var main = system.main;
            main.playOnAwake = false;
            main.maxParticles = 2;
            particles = owner.AddComponent<ParticleSystemEffect>();
            var container = new CreateContainer
            {
                Data = data,
                Descriptor = context.Descriptor,
                ComponentInstances = CreateContainer.CollectComponentInstances(data)
            };
            container.ComponentInstances[record.Components.ParticleSystem[0].InstanceId].Instance = system;
            record.Components.ParticleSystemEventEffect[0].FillComponents(owner, particles, container);
            owner.GetComponent<ParticleLightTimeline>().Atsc = clock;
            owner.SetActive(true);
            Object.FindAnyObjectByType<LightshowController>().RefreshEnvironmentEnhancements();
            Assert.That(owner.GetComponent<ParticleLightTimeline>(), Is.Not.Null);
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(0f);
            yield return null;
        }

        private ParticleSystemEffect FindLeftParticles() => particles;

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001f));
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
