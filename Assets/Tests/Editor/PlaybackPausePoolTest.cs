using System.Collections;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class PlaybackPausePoolTest : TestBase
    {
        private bool previousAnimations;
        private int previousChunkDistance;
        private UIMode mode;
        private CameraManager cameras;
        private AudioTimeSyncController clock;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChunkDistance = Settings.Instance.ChunkDistance;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator PauseDoesNotPopulateInactiveBasicEventGrid(
            [Values(UIModeType.Normal, UIModeType.Preview, UIModeType.Playing)] UIModeType uiMode)
        {
            yield return LoadPauseMap();
            mode.SetUIMode(uiMode, false);
            clock.MoveToJsonTime(40f);
            yield return null;
            var events = Object.FindAnyObjectByType<EventGridContainer>(FindObjectsInactive.Include);
            Assert.That(events.isActiveAndEnabled, Is.False);
            Assert.That(events.MapObjects.Count, Is.EqualTo(3));
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 40f);
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, 149.1f);
            yield return null;
            // Remove the edit nodes while gameplay is running to reproduce the captured pause's missing nodes.
            events.RefreshPool(0f, 0f, true);
            Assert.That(events.LoadedContainers, Is.Empty);
            var spawnedOnPause = 0;
            void CountSpawn(Beatmap.Base.BaseObject _) => spawnedOnPause++;
            events.OnContainerSpawned += CountSpawn;
            try
            {
                TestUtils.PauseDeterministicPlayback(clock);
                yield return null;
                Assert.That(spawnedOnPause, Is.Zero,
                    "Pausing gameplay must not instantiate nodes and labels for an inactive Basic Events grid.");
            }
            finally
            {
                events.OnContainerSpawned -= CountSpawn;
            }
        }

        [UnityTest]
        public IEnumerator PausedEventNodesAreAvailableWhenTheirGridIsVisible(
            [Values(false, true)] bool visibleDuringPause)
        {
            yield return LoadPauseMap();
            var editing = Object.FindAnyObjectByType<EditModeContext>();
            var events = Object.FindAnyObjectByType<EventGridContainer>(FindObjectsInactive.Include);
            editing.EditingMode = EditingMode.BasicEvent;
            clock.MoveToJsonTime(149.1f);
            yield return null;
            Assert.That(events.LoadedContainers.Count, Is.EqualTo(3));
            if (!visibleDuringPause)
                editing.EditingMode = EditingMode.Gameplay;

            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 149.1f);
            events.RefreshPool(0f, 0f, true);
            TestUtils.PauseDeterministicPlayback(clock);
            editing.EditingMode = EditingMode.BasicEvent;
            yield return null;

            Assert.That(events.LoadedContainers.Count, Is.EqualTo(3),
                "Visible Basic Event nodes must be restored even when pausing and reopening the grid in the same chunk.");
            foreach (var node in events.MapObjects)
                Assert.That(events.LoadedContainers.ContainsKey(node), Is.True);
        }

        private IEnumerator LoadPauseMap()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.ChunkDistance = 2;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(@"{""version"":""3.3.0"",""basicBeatmapEvents"":[
                    {""b"":145,""et"":1,""i"":1,""f"":1},
                    {""b"":146,""et"":1,""i"":5,""f"":1},
                    {""b"":147,""et"":1,""i"":1,""f"":1}]}"),
                beatsPerMinute: 150f,
                songLengthSeconds: 240);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var editing = Object.FindAnyObjectByType<EditModeContext>();
            editing.EditingMode = EditingMode.BasicEvent;
            yield return null;
            editing.EditingMode = EditingMode.Gameplay;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.ChunkDistance = previousChunkDistance;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
