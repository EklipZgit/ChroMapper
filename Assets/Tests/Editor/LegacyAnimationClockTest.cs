using System.Collections;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class LegacyAnimationClockTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator ImportedLogoFollowsTheClockWhilePausedPlayingAndAfterRebinding()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" },
                beatsPerMinute: 120f, environmentName: "GagaEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            var animation = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor
                .GetComponentInChildren<TimelineLegacyAnimation>(true);
            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.enabled, Is.True);
            clock.MoveToJsonTime(2f);
            yield return null;
            var expected = animation.transform.localRotation;
            for (var frame = 0; frame < 8; frame++)
                yield return null;

            Assert.That(Quaternion.Angle(animation.transform.localRotation, expected), Is.LessThan(0.01f));
            clock.MoveToJsonTime(10f);
            yield return null;
            Assert.That(Quaternion.Angle(animation.transform.localRotation, expected), Is.GreaterThan(0.01f));
            clock.MoveToJsonTime(2f);
            animation.Initialize(clock);
            yield return null;
            Assert.That(Quaternion.Angle(animation.transform.localRotation, expected), Is.LessThan(0.01f));
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 2f);
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, 3f);
            yield return null;
            var playing = animation.transform.localRotation;
            Assert.That(Quaternion.Angle(playing, expected), Is.GreaterThan(0.01f));
            TestUtils.PauseDeterministicPlayback(clock);
            clock.MoveToJsonTime(3f);
            yield return null;
            Assert.That(Quaternion.Angle(animation.transform.localRotation, playing), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator StartingPhasesAreIndependentAndSurviveSeekingAndRebinding()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                beatsPerMinute: 120f,
                environmentName: "HalloweenEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            var template = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor
                .GetComponentInChildren<TimelineLegacyAnimation>(true);
            var first = Object.Instantiate(template);
            var second = Object.Instantiate(template);
            var randomState = Random.state;
            try
            {
                Random.InitState(812);
                first.Initialize(clock);
                second.Initialize(clock);
                var phase = first.PhaseSeconds;
                Assert.That(phase, Is.InRange(0f, first.Clip.length));
                Assert.That(second.PhaseSeconds, Is.InRange(0f, second.Clip.length));
                Assert.That(
                    Mathf.Abs(second.PhaseSeconds - phase),
                    Is.GreaterThan(0.0001f),
                    "New animation instances must choose independent starting phases.");
                clock.MoveToJsonTime(2f);
                var rotation = first.transform.localRotation;
                clock.MoveToJsonTime(20f);
                clock.MoveToJsonTime(2f);
                first.Initialize(clock);
                Assert.That(
                    first.PhaseSeconds,
                    Is.EqualTo(phase),
                    "Seeking and rebinding must retain this instance's starting phase.");
                Assert.That(Quaternion.Angle(first.transform.localRotation, rotation), Is.LessThan(0.01f));
            }
            finally
            {
                Random.state = randomState;
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
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
