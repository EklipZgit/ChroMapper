using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class BurstSliderLocalRotationTest : TestBase
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
        public IEnumerator CoincidentSlicesUseNativeRotationInPreview() => CheckRotation(false);

        [UnityTest]
        public IEnumerator CoincidentSlicesUseNativeRotationDuringPlayback() => CheckRotation(true);

        [UnityTest]
        public IEnumerator CurvedSlicesRetainTheirNativeTangentRotation() => CheckRotation(false, true);

        private IEnumerator CheckRotation(bool playback, bool curved = false)
        {
            Settings.Instance.Animations = true;
            var fixture = JSON.Parse(@"{
                ""version"":""3.3.0"",
                ""burstSliders"":[{
                    ""b"":0,""c"":1,""x"":3,""y"":1,""d"":0,
                    ""tb"":0,""tx"":3,""ty"":1,""sc"":2,""s"":1,
                    ""customData"":{
                        ""coordinates"":[1,1],""tailCoordinates"":[1,1],
                        ""localRotation"":[0,0,-51,0]
                    }
                }]
            }");
            if (curved)
            {
                fixture["burstSliders"][0]["tx"] = 4;
                fixture["burstSliders"][0]["sc"] = 3;
                fixture["burstSliders"][0]["customData"]["tailCoordinates"][0] = 2;
            }

            if (playback)
            {
                // Sample before the hit so playback has not already despawned the beat-zero slice.
                fixture["burstSliders"][0]["b"] = 8;
                fixture["burstSliders"][0]["tb"] = 8;
            }

            yield return TestUtils.ReloadMap(
                3,
                fixture,
                beatsPerMinute: 120f,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 14f,
                    CustomRequirements = new List<string> { "Noodle Extensions" },
                    LightshowFileName = "MissingTestLightshow.dat"
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(playback ? UIModeType.Playing : UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            if (playback)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 7.9f);
            else
                clock.MoveToJsonTime(0f);

            yield return null;
            yield return null;
            var chain = Object.FindAnyObjectByType<ChainGridContainer>().LoadedContainers.Values
                .OfType<ChainContainer>().Single();
            // Native SignedAngle gives zero for coincident endpoints. The curved control has rightward
            // midpoint tangent and a final tangent of (2, -1), giving 90 and 63.43495 degrees.
            var expectedAngles = curved
                ? new[] { 90f, 63.43495f }
                : new[] { 0f };
            Assert.That(chain.Nodes.Count, Is.EqualTo(expectedAngles.Length));
            for (var i = 0; i < expectedAngles.Length; i++)
            {
                var slice = chain.Nodes[i].transform;
                Assert.That(
                    Quaternion.Angle(slice.localRotation, Quaternion.Euler(0f, 0f, expectedAngles[i])),
                    Is.LessThan(0.01f),
                    "The slice tangent rotation differs from BurstSliderSpawner before Noodle localRotation.");
                Assert.That(
                    Quaternion.Angle(slice.rotation, Quaternion.Euler(0f, 0f, expectedAngles[i] - 51f)),
                    Is.LessThan(0.01f),
                    "The supplied localRotation must compose with the native slice angle.");
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
