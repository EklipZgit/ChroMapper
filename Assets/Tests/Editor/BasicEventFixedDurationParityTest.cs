using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Beat Saber's 1.44.1 BillieEnvironment light switch uses highlight/normal alpha
    // 1/0.7490196. CM's steady-light brightness stays at its existing level, so these tests
    // sample that native contrast and both fixed-duration clocks at 1/64-beat positions.
    public class BasicEventFixedDurationParityTest : TestBase
    {
        private const float NormalAlpha = 1f;
        private const float HighlightAlpha = 1f / 0.7490196f;
        private const float Bpm = 150f;
        private const float Tolerance = 0.0005f;
        private GameObject previewLightObject;
        private BasicLightEffect configuredEffect;
        private float previousOffIntensity;
        private bool? previousChromaLite;

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(3, null, beatsPerMinute: Bpm, environmentName: "BillieEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // Beat Saber samples OutExpo over 1.5 song seconds, with the serialized highlight alpha at
        // the event and zero at the end. A one-beat fade would fail these 1/64-beat samples.
        [Test]
        public void RedFadeMatchesGameBrightnessAtEverySixtyFourthBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(2f, LightValue.RedFade);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var fadeBeats = Bpm * 1.5f / 60f;

            for (var step = 0; step <= Mathf.CeilToInt(fadeBeats * 64f); step++)
            {
                var beatOffset = step / 64f;
                atsc.MoveToJsonTime(2f + beatOffset);
                var progress = Mathf.Clamp01(beatOffset / fadeBeats);
                var easing = progress == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * progress);
                var expectedAlpha = HighlightAlpha * (1f - easing);
                Assert.That(light.Color.a, Is.EqualTo(expectedAlpha).Within(Tolerance),
                    $"Fade brightness at beat {2f + beatOffset} differs from Beat Saber's 1.5-second OutExpo tween.");
            }

            // Paused reverse scrubbing must sample the same brightness independent of approach direction.
            atsc.MoveToJsonTime(2.5f);
            Assert.That(light.Color.a,
                Is.EqualTo(HighlightAlpha * Mathf.Pow(2f, -10f * (0.5f / fadeBeats))).Within(Tolerance));
        }

        // Beat Saber samples OutCubic over 0.6 song seconds with a 1/0.7490196 highlight
        // contrast; CM's old hardcoded 1.2-to-1 flash had the wrong relative curve.
        [Test]
        public void RedFlashMatchesGameBrightnessAtEverySixtyFourthBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(6f, LightValue.RedFlash);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var flashBeats = Bpm * 0.6f / 60f;

            for (var step = 0; step <= Mathf.CeilToInt(flashBeats * 64f); step++)
            {
                var beatOffset = step / 64f;
                atsc.MoveToJsonTime(6f + beatOffset);
                var progress = Mathf.Clamp01(beatOffset / flashBeats);
                var easing = 1f - Mathf.Pow(1f - progress, 3f);
                var expectedAlpha = Mathf.LerpUnclamped(HighlightAlpha, NormalAlpha, easing);
                Assert.That(light.Color.a, Is.EqualTo(expectedAlpha).Within(Tolerance),
                    $"Flash brightness at beat {6f + beatOffset} differs from Beat Saber's 0.6-second OutCubic tween.");
            }

            atsc.MoveToJsonTime(6.5f);
            var reverseProgress = 0.5f / flashBeats;
            var reverseExpected = Mathf.LerpUnclamped(HighlightAlpha, NormalAlpha,
                1f - Mathf.Pow(1f - reverseProgress, 3f));
            Assert.That(light.Color.a, Is.EqualTo(reverseExpected).Within(Tolerance));
        }

        // The basic event callback restarts the native tween at a later event, so a fade must stop
        // contributing once a same-type light-on event arrives before its fixed-duration endpoint.
        [Test]
        public void NewLightEventInterruptsFadeAtItsExactBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(10f, LightValue.RedFade);
            PlaceLightEvent(10.5f, LightValue.RedOn);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.MoveToJsonTime(10.5f - (1f / 64f));
            Assert.That(light.Color.a, Is.LessThan(1f));
            atsc.MoveToJsonTime(10.5f);
            Assert.That(light.Color.a, Is.EqualTo(NormalAlpha).Within(Tolerance));
        }

        // White uses ColorManager's same alpha for normal and highlight colors in the
        // 1.44.1 light switch; a white flash changes no brightness across its 0.6-second clock.
        [Test]
        public void WhiteFlashRetainsNativeBrightnessAtSixtyFourthBeatSamples()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(12f, LightValue.WhiteFlash);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            for (var step = 0; step <= 96; step++)
            {
                atsc.MoveToJsonTime(12f + (step / 64f));
                Assert.That(light.Color.a, Is.EqualTo(1f).Within(Tolerance),
                    $"White flash changed brightness at 1/64-beat step {step}.");
            }
        }

        // A new light event replaces the active flash at its own beat, even while the
        // 0.6-second fixed-duration tween would otherwise remain in progress.
        [Test]
        public void NewLightEventInterruptsFlashAtItsExactBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(14f, LightValue.RedFlash);
            PlaceLightEvent(14.5f, LightValue.RedOn);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.MoveToJsonTime(14.5f - (1f / 64f));
            Assert.That(light.Color.a, Is.GreaterThan(NormalAlpha));
            atsc.MoveToJsonTime(14.5f);
            Assert.That(light.Color.a, Is.EqualTo(NormalAlpha).Within(Tolerance));
        }

        // Beat Saber's fade destination uses ColorWithAlpha(offIntensity * floatValue), which
        // replaces the custom color's alpha rather than multiplying it a second time.
        [Test]
        public void FadeToNonzeroOffIntensityOverridesCustomColorAlpha()
        {
            var light = CreatePreviewLight();
            configuredEffect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(light.Type);
            previousOffIntensity = configuredEffect.OffIntensity;
            configuredEffect.OffIntensity = 0.6f;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            Settings.Instance.EmulateChromaLite = true;
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 18f,
                Type = light.Type,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                CustomColor = new Color(1f, 0.2f, 0.2f, 0.25f)
            });

            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(21.75f);
            Assert.That(light.Color.a, Is.EqualTo(0.6f).Within(Tolerance));
        }

        // PyroEnvironment's fire switch uses the same FireColor asset for normal and
        // highlight channels, so a flash must not acquire BillieEnvironment's contrast.
        [UnityTest]
        public IEnumerator PyroFireFlashKeepsEqualHighlightAndNormalBrightness()
        {
            yield return TestUtils.ReloadMap(3, null, beatsPerMinute: Bpm, environmentName: "PyroEnvironment");
            var light = CreatePreviewLight((int)EventTypeValue.Event1);
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 4f,
                Type = light.Type,
                Value = (int)LightValue.RedFlash,
                FloatValue = 1f
            });
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(4f);
            Assert.That(light.Color.a, Is.EqualTo(1f).Within(Tolerance));
            atsc.MoveToJsonTime(4.5f);
            Assert.That(light.Color.a, Is.EqualTo(1f).Within(Tolerance));
            atsc.MoveToJsonTime(5.5f);
            Assert.That(light.Color.a, Is.EqualTo(1f).Within(Tolerance));
        }

        // Register a visible probe with the environment's actual BasicLightEffect and let placement
        // and ATSC callbacks drive it, rather than testing a detached interpolation helper.
        private ParityLightController CreatePreviewLight(int type = (int)EventTypeValue.Event2)
        {
            previewLightObject = new GameObject("Fixed duration parity light");
            var light = previewLightObject.AddComponent<ParityLightController>();
            light.Type = type;
            light.ID = -1;
            var effect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(light.Type);
            effect.Register(light);
            effect.Initialize();
            return light;
        }

        private static void PlaceLightEvent(float beat, LightValue value)
        {
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = beat,
                Type = (int)EventTypeValue.Event2,
                Value = (int)value,
                FloatValue = 1f
            });
        }

        protected override void AfterCleanup()
        {
            if (configuredEffect != null)
            {
                configuredEffect.OffIntensity = previousOffIntensity;
                configuredEffect = null;
            }

            if (previousChromaLite.HasValue)
            {
                Settings.Instance.EmulateChromaLite = previousChromaLite.Value;
                previousChromaLite = null;
            }

            if (previewLightObject == null)
            {
                return;
            }

            var light = previewLightObject.GetComponent<ParityLightController>();
            var effect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(light.Type);
            effect.Unregister(light);
            Object.DestroyImmediate(previewLightObject);
            previewLightObject = null;
        }

        public sealed class ParityLightController : LightController
        {
            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }
    }
}
