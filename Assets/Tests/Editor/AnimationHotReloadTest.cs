using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class AnimationHotReloadTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousCameraOffset;
        private string difficultyPath;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousCameraOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator DirectReloadPreservesTheLivePausedCursor() => CheckDirectReload(false);

        [UnityTest]
        public IEnumerator DirectReloadPausesAndRestoresPlaybackBeforeNotifyingListeners() => CheckDirectReload(true);

        private IEnumerator CheckDirectReload(bool playing)
        {
            yield return LoadFixture();
            EditorStateService.CaptureMapData(BeatSaberSongContainer.Instance.Info);
            const float liveBeat = 9.137f;
            clock.MoveToSongBpmTime(liveBeat);
            if (playing)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, liveBeat);

            yield return null;
            yield return null;
            AssertOutputs(1);
            var mapperScene = clock.gameObject.scene;
            var loaded = false;
            var notifiedBeat = -1f;
            var notifiedPlaying = false;
            Action onLoaded = () =>
            {
                loaded = true;
                notifiedBeat = clock.CurrentSongBpmTime;
                notifiedPlaying = clock.IsPlaying;
                if (clock.IsPlaying)
                {
                    clock.SongAudioSource.Stop();
                    clock.StopScheduled = true;
                }

            };
            LoadInitialMap.OnLevelLoaded += onLoaded;
            var loader = Object.FindAnyObjectByType<LoadInitialMap>();
            Coroutine reload = null;
            try
            {
                reload = loader.StartCoroutine(loader.ReloadCurrentMapInPlace());
                Assert.That(clock.IsPlaying, Is.False, "Reload must pause before its first asynchronous loading step.");
                yield return reload;
                Assert.That(loaded, Is.True);
                Assert.That(
                    clock.CurrentSongBpmTime,
                    Is.EqualTo(liveBeat).Within(0.0001f),
                    "Reload restored saved metadata instead of the live unsnapped cursor.");
                Assert.That(clock.IsPlaying, Is.EqualTo(playing));
                Assert.That(notifiedBeat, Is.EqualTo(liveBeat).Within(0.0001f));
                Assert.That(notifiedPlaying, Is.EqualTo(playing), "OnLevelLoaded observed the intermediate playback state.");
                Assert.That(clock.SongAudioSource.isPlaying, Is.False);
                Assert.That(clock.gameObject.scene, Is.EqualTo(mapperScene));
                yield return null;
                yield return null;
                AssertOutputs(1);
            }
            finally
            {
                LoadInitialMap.OnLevelLoaded -= onLoaded;
                if (reload != null)
                    loader.StopCoroutine(reload);

            }

        }

        [UnityTest]
        public IEnumerator FullReloadDuringPlaybackKeepsTheLiveCursor()
        {
            yield return LoadFixture();
            const float liveBeat = 9.137f;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, liveBeat);
            WriteDifficulty(Fixture(2));
            yield return ReloadFromDialog(4);
            Assert.That(clock.IsPlaying, Is.True);
            Assert.That(clock.CurrentSongBpmTime, Is.EqualTo(liveBeat).Within(0.0001f));
            AssertOutputs(2);
        }

        [UnityTest]
        public IEnumerator FullReloadRebuildsAnimationsAndReleasesRemovedBindings()
        {
            yield return LoadFixture();
            AssertOutputs(1);
            foreach (var revision in new[] { 2, 1 })
            {
                WriteDifficulty(Fixture(revision));
                yield return ReloadFromDialog(4);
                AssertOutputs(revision);
            }

            var empty = JSON.Parse("{\"version\":\"3.3.0\"}");
            WriteDifficulty(empty);
            yield return ReloadFromDialog(4);
            Assert.That(Object.FindAnyObjectByType<GeometryGridContainer>().LoadedContainers, Is.Empty);
            Assert.That(Object.FindAnyObjectByType<NoteGridContainer>().MapObjects, Is.Empty);
            Assert.That(cameras.PlayerCameraController.transform.position.z, Is.EqualTo(0f).Within(0.001f));
            foreach (var name in new[] { "reloadNote", "reloadShape", "reloadParent", "reloadPlayer", "reloadMaterial" })
            {
                var track = Object.FindAnyObjectByType<TracksManager>().GetAnimationTrack(name);
                Assert.That(track.AnimatedProperties, Is.Empty, $"The removed {name} animation survived reload.");
                Assert.That(track.Children, Is.Empty, $"The removed {name} binding survived reload.");
            }
        }

        [UnityTest]
        public IEnumerator FullReloadRebuildsClonedRingMovementSnapshotsBeforeRestoringTime()
        {
            yield return LoadFixture(environmentName: "DefaultEnvironment");
            var manager = RingManager();
            var previousCount = manager.Rings.Count;
            var updated = Fixture(1);
            updated["customData"]["environment"].AsArray.Add(new JSONObject
            {
                ["id"] = @"BigTrackLaneRing\(Clone\)$",
                ["lookupMethod"] = "Regex",
                ["duplicate"] = 1
            });
            WriteDifficulty(updated);
            try
            {
                yield return ReloadFromDialog(4);
                manager = RingManager();
                Assert.That(manager.Rings.Count, Is.GreaterThan(previousCount));
                yield return Seek(9f);
                yield return Seek(8f);
                var reloadedCount = manager.Rings.Count;
                yield return ReloadFromDialog(4);
                Assert.That(RingManager().Rings.Count, Is.EqualTo(reloadedCount), "Repeated reload stacked ring clones.");
            }
            finally
            {
                // Keep a failed cursor restore from leaving invalid snapshots in the shared test scene.
                Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.Reinitialize();
            }
        }

        [UnityTest]
        public IEnumerator OtherReloadRebuildsSharedDefinitionsAndHeldTrackValues()
        {
            yield return LoadFixture();
            AssertOutputs(1);
            var originalNote = Note().NoteData;
            var updated = Fixture(2);
            updated["colorNotes"][0]["b"] = 24;
            updated["obstacles"][0]["d"] = 40;
            WriteDifficulty(updated);
            yield return ReloadFromDialog(3);
            AssertOutputs(2);
            Assert.That(Note().NoteData, Is.SameAs(originalNote), "Other reload replaced an unselected note.");
            Assert.That(Wall().ObstacleData.Duration, Is.EqualTo(20f), "Other reload replaced unselected walls.");
            yield return Seek(9f);
            yield return Seek(8f);
            AssertOutputs(2);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 8f);
            WriteDifficulty(Fixture(1));
            yield return ReloadFromDialog(3);
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, 9f);
            yield return null;
            yield return null;
            AssertOutputs(1);
            Assert.That(clock.IsPlaying, Is.True);
            Assert.That(clock.SongAudioSource.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator NoteAndWallReloadRebuildsTheirAnimationAndSpawnParameters()
        {
            yield return LoadFixture();
            var updated = Fixture(1);
            var noteData = updated["colorNotes"][0];
            noteData["b"] = 20;
            noteData["customData"]["noteJumpMovementSpeed"] = 20;
            noteData["customData"]["noteJumpStartBeatOffset"] = 8;
            noteData["customData"]["animation"] = JSON.Parse("{\"scale\":[3,3,3]}");
            var wallData = updated["obstacles"][0];
            wallData["d"] = 30;
            wallData["customData"]["animation"]["scale"] = JSON.Parse("[5,5,5]");
            WriteDifficulty(updated);
            yield return ReloadFromDialog(0);
            yield return Seek(12f);
            var note = Note();
            Assert.That(note.NoteData.JsonTime, Is.EqualTo(20f));
            Assert.That(note.NoteData.HalfJumpDuration, Is.EqualTo(9f).Within(0.001f));
            Assert.That(note.Animator.LocalTarget.localScale.x, Is.EqualTo(6f).Within(0.001f));
            Assert.That(Wall().ObstacleData.Duration, Is.EqualTo(20f), "Notes-only reload replaced the unselected walls.");
            yield return ReloadFromDialog(1);
            Assert.That(Wall().ObstacleData.Duration, Is.EqualTo(30f));
            Assert.That(Wall().Animator.LocalTarget.localScale.x, Is.EqualTo(5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator PartialReloadRetimesExistingPathAnimationsAfterBpmChanges()
        {
            var fixture = Fixture(1);
            fixture["customData"]["customEvents"].AsArray.Add(JSON.Parse(@"{
                ""b"":8,""t"":""AssignPathAnimation"",
                ""d"":{""track"":""reloadNote"",""duration"":8,""scale"":[4,4,4]}}"));
            yield return LoadFixture(fixture);
            yield return Seek(12f);
            Assert.That(Note().Animator.LocalTarget.localScale.x, Is.EqualTo(3f).Within(0.001f));
            var updated = JSON.Parse(fixture.ToString());
            updated["bpmEvents"][1]["m"] = 200;
            WriteDifficulty(updated);
            yield return ReloadFromDialog(2);
            clock.MoveToSongBpmTime(8f);
            yield return null;
            yield return null;
            Assert.That(clock.CurrentJsonTime, Is.EqualTo(12f).Within(0.001f));
            Assert.That(
                Note().Animator.LocalTarget.localScale.x,
                Is.EqualTo(3f).Within(0.001f),
                "The path transition retained its old BPM timing after a partial reload.");
        }

        private IEnumerator LoadFixture(JSONNode fixture = null, string environmentName = "BillieEnvironment")
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var source = fixture ?? Fixture(1);
            // The importer consumes some JSON sections. Keep the disk copy independent of the loaded map.
            var serialized = source.ToString();
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(serialized),
                beatsPerMinute: 100f,
                environmentName: environmentName,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 14f,
                    CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                    BeatmapFileName = "ExpertPlusLawless.dat",
                    LightshowFileName = "MissingTestLightshow.dat"
                });
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "AnimationHotReloadTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            difficultyPath = PathUtils.Combine(directory, "ExpertPlusLawless.dat");
            File.WriteAllText(difficultyPath, serialized);
            BeatSaberSongContainer.Instance.Info.Directory = directory;
            BeatSaberSongContainer.Instance.Map.DirectoryAndFile = difficultyPath;
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            yield return Seek(8f);
            yield return Seek(9f);
            yield return Seek(8f);
        }

        private void WriteDifficulty(JSONNode fixture) => File.WriteAllText(difficultyPath, fixture.ToString());

        private IEnumerator ReloadFromDialog(int option)
        {
            var mapperScene = clock.gameObject.scene;
            var previousTime = clock.CurrentSongBpmTime;
            var wasPlaying = clock.IsPlaying;
            Object.FindAnyObjectByType<RefreshMapController>().InitiateRefreshConversation();
            yield return null;
            var dialog = Object.FindAnyObjectByType<DialogBox>();
            Assert.That(dialog, Is.Not.Null);
            var buttons = dialog.GetComponentsInChildren<ButtonComponent>();
            Assert.That(buttons.Length, Is.EqualTo(6));
            var loaded = false;
            Action onLoaded = () => loaded = true;
            LoadInitialMap.OnLevelLoaded += onLoaded;
            try
            {
                Assert.DoesNotThrow(
                    () => ((Button)buttons[option].Selectable).onClick.Invoke(),
                    "Reload restored time before its animation and movement caches were ready.");
                if (option is 3 or 4)
                {
                    var deadline = Time.realtimeSinceStartup + 30f;
                    while ((!loaded || Mathf.Abs(clock.CurrentSongBpmTime - previousTime) > 0.001f
                        || clock.IsPlaying != wasPlaying) && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }

                    Assert.That(loaded, Is.True, "Hot reload did not finish loading its environment.");
                }
            }
            finally
            {
                LoadInitialMap.OnLevelLoaded -= onLoaded;
            }

            if (clock.IsPlaying)
            {
                clock.SongAudioSource.Stop();
                clock.StopScheduled = true;
            }

            yield return null;
            yield return null;
            Assert.That(clock.gameObject.scene, Is.EqualTo(mapperScene), "Hot reload replaced the mapper scene.");
            Assert.That(clock.CurrentSongBpmTime, Is.EqualTo(previousTime).Within(0.001f), "Hot reload moved the cursor.");
        }

        private IEnumerator Seek(float beat)
        {
            clock.MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        private static NoteContainer Note() => Object.FindAnyObjectByType<NoteGridContainer>()
            .LoadedContainers.Values.OfType<NoteContainer>().Single();

        private static ObstacleContainer Wall() => Object.FindAnyObjectByType<ObstacleGridContainer>()
            .LoadedContainers.Values.OfType<ObstacleContainer>().Single();

        private static TrackLaneRingsManager RingManager() => Object.FindObjectsByType<TrackLaneRingsManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None).Single(candidate => candidate.Rings.Any(ring => ring.name == "BigTrackLaneRing(Clone)"));

        private void AssertOutputs(int revision)
        {
            Assert.That(Note().Animator.LocalTarget.localScale.x, Is.EqualTo(2f * revision).Within(0.001f));
            Assert.That(Wall().Animator.LocalTarget.localScale.x, Is.EqualTo(3f * revision).Within(0.001f));
            var shape = Object.FindAnyObjectByType<GeometryGridContainer>().LoadedContainers.Values
                .Cast<GeometryContainer>().Single(item => item.EnvironmentEnhancement.Track == "reloadShape");
            Assert.That(
                shape.MpbController.Renderers[0].sharedMaterial.shader.name,
                Is.EqualTo(revision == 1 ? "ChroMapper/Lit" : "ChroMapper/Glowing"));
            Assert.That(shape.Animator.LocalTarget.localPosition.x, Is.EqualTo(revision).Within(0.001f));
            Assert.That(shape.Animator.LocalTarget.position.x, Is.EqualTo(4f * revision).Within(0.001f));
            var color = shape.MpbController.Mpb.GetColor(Shader.PropertyToID("_Color"));
            Assert.That(color.r, Is.EqualTo(revision == 1 ? 1f : 0f).Within(0.001f));
            Assert.That(color.b, Is.EqualTo(revision == 1 ? 0f : 1f).Within(0.001f));
            Assert.That(cameras.PlayerCameraController.transform.position.z, Is.EqualTo(-2f * revision).Within(0.001f));
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            Assert.That(descriptor.BloomFogParams.Attenuation, Is.EqualTo(0.001f * revision).Within(0.00001f));
            var light = shape.GetComponentInChildren<ParametricBloomFogLightController>(true);
            Assert.That(light.ColorAlphaMultiplier, Is.EqualTo(2f * revision).Within(0.001f));
            Assert.That(light.BloomFog.IntensityMultiplier, Is.EqualTo(3f * revision).Within(0.001f));
        }

        private static JSONNode Fixture(int revision)
        {
            var fixture = JSON.Parse(@"{
                ""version"":""3.3.0"",
                ""bpmEvents"":[{""b"":0,""m"":100},{""b"":4,""m"":100}],
                ""colorNotes"":[{""b"":16,""x"":0,""y"":0,""c"":0,""d"":8,
                    ""customData"":{""track"":""reloadNote"",""fake"":true,
                        ""noteJumpMovementSpeed"":14,""noteJumpStartBeatOffset"":10}}],
                ""obstacles"":[{""b"":2,""x"":0,""y"":0,""d"":20,""w"":1,""h"":1,
                    ""customData"":{""fake"":true,""noteJumpStartBeatOffset"":10,
                        ""animation"":{""scale"":""wallScale""}}}],
                ""customData"":{
                    ""pointDefinitions"":{},
                    ""materials"":{""surface"":{""shader"":""Standard"",""track"":""reloadMaterial"",""color"":[1,1,1,1]}},
                    ""environment"":[
                        {""id"":""[0]Environment"",""lookupMethod"":""EndsWith"",""track"":""reloadFog""},
                        {""geometry"":{""type"":""Cube"",""material"":""surface""},""track"":""reloadShape"",
                            ""components"":{""ILightWithId"":{""type"":1,""lightID"":9999},
                                ""TubeBloomPrePassLight"":{""colorAlphaMultiplier"":1,""bloomFogIntensityMultiplier"":1}}}
                    ],
                    ""customEvents"":[
                        {""b"":0,""t"":""AssignTrackParent"",""d"":{""parentTrack"":""reloadParent"",""childrenTracks"":[""reloadShape""]}},
                        {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""reloadParent"",""position"":""parentPosition""}},
                        {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""reloadShape"",""localPosition"":""shapePosition""}},
                        {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""reloadMaterial"",""color"":""materialColor""}},
                        {""b"":0,""t"":""AssignPathAnimation"",""d"":{""track"":""reloadNote"",""scale"":""noteScale""}},
                        {""b"":0,""t"":""AssignPlayerToTrack"",""d"":{""track"":""reloadPlayer""}},
                        {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""reloadPlayer"",""position"":""playerPosition""}},
                        {""b"":0,""t"":""AnimateComponent"",""d"":{""track"":""reloadFog"",""BloomFogEnvironment"":{""attenuation"":""fogAttenuation""}}},
                        {""b"":0,""t"":""AnimateComponent"",""d"":{""track"":""reloadShape"",""TubeBloomPrePassLight"":{
                            ""colorAlphaMultiplier"":""lightAlpha"",""bloomFogIntensityMultiplier"":""lightIntensity""}}}
                    ]
                }
            }");
            var points = fixture["customData"]["pointDefinitions"];
            points["noteScale"] = Point(2f * revision, 2f * revision, 2f * revision);
            points["wallScale"] = Point(3f * revision, 3f * revision, 3f * revision);
            points["parentPosition"] = Point(3f * revision, 0f, 0f);
            points["shapePosition"] = Point(revision, 0f, 0f);
            points["playerPosition"] = Point(0f, 0f, -2f * revision);
            points["materialColor"] = revision == 1
                ? JSON.Parse("[1,0,0,1]")
                : JSON.Parse("[0,0,1,1]");
            points["fogAttenuation"] = Point(0.001f * revision);
            points["lightAlpha"] = Point(2f * revision);
            points["lightIntensity"] = Point(3f * revision);
            if (revision == 2)
                fixture["customData"]["materials"]["surface"]["shaderKeywords"] = new JSONArray();

            return fixture;
        }

        private static JSONArray Point(params float[] values)
        {
            var result = new JSONArray();
            foreach (var value in values)
                result.Add(value);

            return result;
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            foreach (var dialog in Object.FindObjectsByType<DialogBox>(FindObjectsSortMode.None))
                dialog.Close();

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
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
