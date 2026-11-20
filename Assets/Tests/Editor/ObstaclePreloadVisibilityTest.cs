using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Regression: during playback the obstacle spawn lead creates walls up to JUMP_TIME before
    // their visible window and UpdateTime parks them at JUMP_FAR, so a ring of distant walls
    // rendered during playback but vanished on pause/scrub (Vanishing Point b300+ helix walls).
    // Native does not create the object until the floor-slide start, so preloaded containers
    // keep their visual target hidden until then. An authored _time animation owns the object's
    // lifetime and keeps its pose.
    public class ObstaclePreloadVisibilityTest : InputTestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private EditModeContext editMode;
        private EditingMode previousEditingMode;

        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(2, Difficulty(), beatsPerMinute: 128,
                environmentName: "BillieEnvironment", songLengthSeconds: 420);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator PreloadedWallsStayHiddenUntilTheirVisibleWindow()
        {
            InitializeVirtualInput(includeKeyboard: true);
            editMode = Object.FindAnyObjectByType<EditModeContext>();
            previousEditingMode = editMode.EditingMode;
            editMode.EditingMode = EditingMode.BasicEvent;
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var obstacleGrid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(obstacleGrid, Is.Not.Null, "ObstacleGridContainer missing.");

            // Paused seek: pre-window walls are not spawned at all.
            atsc.MoveToJsonTime(299f);
            yield return null;
            yield return null;
            var lateWall = FindWall(obstacleGrid, 305f);
            Assert.That(lateWall, Is.Null, "b305 wall must not exist on a paused seek at b299.");
            var inWindowWall = FindWall(obstacleGrid, 302f);
            Assert.That(inWindowWall, Is.Not.Null, "b302 wall should exist paused at b299.");
            Assert.That(inWindowWall.Animator.LocalTarget.gameObject.activeSelf, Is.True,
                "b302 wall is inside its floor-slide window at b299 and must render.");

            atsc.TogglePlaying();
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            yield return null;
            yield return null;

            // Playback preload: b305 spawns ahead of its window but stays hidden while parked.
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(301f));
            yield return null;
            yield return null;
            lateWall = FindWall(obstacleGrid, 305f);
            Assert.That(lateWall, Is.Not.Null, "b305 wall should be preloaded by the playback lead.");
            Assert.That(lateWall.Animator.LocalTarget.gameObject.activeSelf, Is.False,
                $"b305 wall is parked beyond its visible window at {atsc.CurrentJsonTime:F2} " +
                $"(z={lateWall.transform.position.z:F1}) and must not render.");

            // An authored _time override resolves inside the object's lifetime, so a
            // preloaded _time wall keeps its pose even while the clock sits before the
            // wall's natural visible window.
            var timeWall = FindWall(obstacleGrid, 306f);
            Assert.That(timeWall, Is.Not.Null, "b306 _time wall should be preloaded by the playback lead.");
            Assert.That(timeWall.Animator.LocalTarget.gameObject.activeSelf, Is.True,
                "authored _time wall keeps its pose while preloaded.");

            // Once the clock reaches the visible window the parked wall renders normally.
            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(304f));
            yield return null;
            yield return null;
            lateWall = FindWall(obstacleGrid, 305f);
            Assert.That(lateWall, Is.Not.Null, "b305 wall should still exist inside its window.");
            Assert.That(lateWall.Animator.LocalTarget.gameObject.activeSelf, Is.True,
                $"b305 wall is inside its visible window at {atsc.CurrentJsonTime:F2} and must render.");

            atsc.TogglePlaying();
            yield return null;
            yield return null;
        }

        private static ObstacleContainer FindWall(ObstacleGridContainer grid, float jsonTime) =>
            grid.LoadedContainers
                .Where(pair => pair.Key is BaseObstacle wall && Mathf.Approximately(wall.JsonTime, jsonTime))
                .Select(pair => (ObstacleContainer)pair.Value)
                .FirstOrDefault();

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            TearDownVirtualInput();
            if (uiMode != null) uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null) cameraManager.SelectCamera(CameraType.Editing);
            if (editMode != null) editMode.EditingMode = previousEditingMode;
            Settings.Instance.Animations = animationsBeforeTest;
            yield break;
        }

        private static JSONNode Difficulty() => JSON.Parse(@"{
            ""_version"": ""2.2.0"",
            ""_events"": [],
            ""_notes"": [],
            ""_waypoints"": [],
            ""_obstacles"": [
                { ""_time"": 300, ""_lineIndex"": 0, ""_type"": 3, ""_duration"": 0, ""_width"": 0,
                  ""_customData"": { ""_interactable"": false, ""_disableNoteGravity"": true, ""_track"": ""helix"",
                    ""_fake"": true, ""_rotation"": [0,0,-2400], ""_position"": [0,11,-300], ""_scale"": [1,1,5],
                    ""_color"": [1,0,0,1], ""_disableNoteLook"": true, ""_noteJumpMovementSpeed"": 16,
                    ""_noteJumpStartBeatOffset"": -0.25 } },
                { ""_time"": 300.5, ""_lineIndex"": 0, ""_type"": 3, ""_duration"": 0, ""_width"": 0,
                  ""_customData"": { ""_interactable"": false, ""_disableNoteGravity"": true, ""_track"": ""helix"",
                    ""_fake"": true, ""_rotation"": [0,0,-2430], ""_position"": [0,11,-300], ""_scale"": [1,1,5],
                    ""_color"": [1,0,0,1], ""_disableNoteLook"": true, ""_noteJumpMovementSpeed"": 16,
                    ""_noteJumpStartBeatOffset"": -0.25 } },
                { ""_time"": 301, ""_lineIndex"": 0, ""_type"": 3, ""_duration"": 0, ""_width"": 0,
                  ""_customData"": { ""_interactable"": false, ""_disableNoteGravity"": true, ""_track"": ""helix"",
                    ""_fake"": true, ""_rotation"": [0,0,-2460], ""_position"": [0,11,-300], ""_scale"": [1,1,5],
                    ""_color"": [1,0,0,1], ""_disableNoteLook"": true, ""_noteJumpMovementSpeed"": 16,
                    ""_noteJumpStartBeatOffset"": -0.25 } },
                { ""_time"": 302, ""_lineIndex"": 1, ""_type"": 0, ""_duration"": 0.5, ""_width"": 4 },
                { ""_time"": 305, ""_lineIndex"": 1, ""_type"": 0, ""_duration"": 0.5, ""_width"": 4 },
                { ""_time"": 306, ""_lineIndex"": 1, ""_type"": 0, ""_duration"": 0.5, ""_width"": 4,
                  ""_customData"": { ""_track"": ""timewall"" } }
            ],
            ""_customData"": {
                ""_customEvents"": [
                    { ""_time"": 0, ""_type"": ""AnimateTrack"",
                      ""_data"": { ""_dissolve"": [[0,0],[0,1]], ""_track"": ""helix"", ""_duration"": 400 } },
                    { ""_time"": 0, ""_type"": ""AnimateTrack"",
                      ""_data"": { ""_time"": [[0.1,0],[0.1,1]], ""_track"": ""timewall"", ""_duration"": 400 } }
                ],
                ""_pointDefinitions"": []
            }
        }");
    }
}
