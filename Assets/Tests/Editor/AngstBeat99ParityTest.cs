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

namespace Tests.Editor
{
    public class AngstBeat99ParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChromaLite;
        private bool previousChromaAdvanced;
        private bool previousHideSpawnEffect;
        private float previousFov;
        private float previousOffset;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            previousChromaAdvanced = Settings.Instance.EmulateChromaAdvanced;
            previousHideSpawnEffect = Settings.Instance.HideNoteSpawnEffect;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator AuthoredLocalRotationReachesTheSpawnEffect([Values(false, true)] bool playback)
        {
            var map = JSON.Parse(File.ReadAllText(FixturePath("AngstSpawnAngleFixture.json")));
            Settings.Instance.HideNoteSpawnEffect = false;
            yield return LoadMap(map);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var expected = Quaternion.Euler(map["_notes"][0]["_customData"]["_localRotation"].ReadVector3());
            if (playback)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 358.765f);
            foreach (var beat in new[] { 358.765f, 359f, 364f, 358.765f })
            {
                if (playback)
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                else
                    clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single();
                Assert.That(note.SpawnEffect.SpriteRenderer.enabled, Is.EqualTo(beat < 360f));
                Assert.That(Quaternion.Angle(note.Animator.LocalTarget.rotation, expected), Is.LessThan(0.01f),
                    "The reduced note retains its original fixed local rotation.");
                if (beat >= 360f)
                    continue;

                Assert.That(Quaternion.Angle(note.SpawnEffect.transform.rotation, expected), Is.LessThan(0.01f),
                    "Noodle's BeatEffect uses the note controller's rotation at jump start, including its fixed local rotation.");
            }
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "AngstSpawnAngles");
            Directory.CreateDirectory(directory);
            LinearRenderCapture.Capture(cameras.SelectedCameraController.Camera,
                PathUtils.Combine(directory, $"minimal-{playback}.png"));
        }

        [UnityTest]
        public IEnumerator NotesBornAtFrozenHitTimeKeepTheirFixedFacing([Values(false, true)] bool playback)
        {
            var map = JSON.Parse(File.ReadAllText(FixturePath("AngstSpawnAngleFixture.json")));
            yield return LoadMap(map);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (playback)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 358.765f);
            foreach (var beat in new[] { 358.765f, 364f, 358.765f, 364f })
            {
                if (playback)
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                else
                    clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single();
                var expected = Quaternion.Euler(map["_notes"][0]["_customData"]["_localRotation"].ReadVector3());
                Assert.That(note.Animator.DisableNoteLook, Is.False);
                Assert.That(Quaternion.Angle(note.Animator.LocalTarget.rotation, expected), Is.LessThan(0.01f));
                Assert.That(Quaternion.Angle(note.DirectionTarget.localRotation, Quaternion.identity), Is.LessThan(0.01f),
                    $"At {beat}, a note born at frozen time 0.5 must keep SetToStart's identity because NoteJump never enters its pre-hit rotation window.");
            }
        }

        [UnityTest]
        public IEnumerator TrackParentProxySurvivesRewindAndMapReload()
        {
            var map = JSON.Parse(@"{
                'version':'3.3.0','colorNotes':[],'obstacles':[],
                'customData':{
                    'materials':{'cube':{'shader':'Standard','color':[1,1,1,1],'shaderKeywords':[]}},
                    'environment':[{'geometry':{'type':'Cube','material':'cube'},'track':'child'}],
                    'customEvents':[
                        {'b':0,'t':'AnimateTrack','d':{'track':'child','offsetWorldRotation':[[0,0,30,0]]}},
                        {'b':1,'t':'AssignTrackParent','d':{'childrenTracks':['child'],'parentTrack':'parent'}},
                        {'b':1,'t':'AnimateTrack','d':{'track':'parent','localRotation':[[0,0,20,0]]}}
                    ]}}".Replace('\'', '"'));
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
            // Clearing the previous map mutates its collections, so each reload needs its own JSON nodes.
            var mapJson = map.ToString();
            for (var load = 0; load < 2; load++)
            {
                Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
                yield return TestUtils.ReloadMap(3, JSON.Parse(mapJson), environmentName: "BTSEnvironment");
                TestUtils.CaptureCurrentMapAsSharedBaseline();
                mode = Object.FindAnyObjectByType<UIMode>();
                cameras = Object.FindAnyObjectByType<CameraManager>();
                mode.SetUIMode(UIModeType.Playing, false);
                cameras.SelectCamera(CameraType.Playing);
                var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
                foreach (var beat in new[] { 0f, 2f, 0f, 2f })
                {
                    clock.MoveToJsonTime(beat);
                    yield return null;
                    var cubes = Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None);
                    Assert.That(cubes.Length, Is.EqualTo(1), $"Load {load}, beat {beat}: the fixture's cube must be present.");
                    var cube = cubes.Single();
                    var renderObject = new GameObject("Parent reload render check");
                    var rotation = Quaternion.identity;
                    var captured = false;
                    try
                    {
                        var renderer = renderObject.AddComponent<CameraRenderAfterLateUpdate>();
                        renderer.TargetCamera = cameras.SelectedCameraController.Camera;
                        renderer.BeforeRender = () =>
                        {
                            rotation = cube.transform.rotation;
                            captured = true;
                        };
                        yield return null;
                        Assert.That(captured, Is.True);
                        // Heck stops writing the completed 30-degree rotation, so the later parent adds its 20 degrees.
                        var expected = Quaternion.Euler(0f, 0f, beat >= 1f ? 50f : 30f);
                        Assert.That(Quaternion.Angle(rotation, expected), Is.LessThan(0.05f),
                            $"Load {load}, beat {beat}: the held rotation must follow the active parent after reload and rewind.");
                    }
                    finally
                    {
                        Object.DestroyImmediate(renderObject);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator BlueFrozenNoteMatchesNativeAtRenderTime([Values(UIModeType.Preview, UIModeType.Playing)] UIModeType previewMode)
        {
            var map = JSON.Parse(File.ReadAllText(FixturePath("AngstFrozenNoteAnglesFixture.json")));
            var reference = JSON.Parse(File.ReadAllText(FixturePath("AngstBeat364NativeNoteAngles.json")));
            map["_notes"] = new JSONArray { [0] = map["_notes"][182] };
            reference["notes"] = new JSONArray { [0] = reference["notes"][182] };
            Assert.That(map["_notes"][0]["_time"].AsFloat, Is.EqualTo(360.182f));
            yield return CheckFrozenNoteAngles(map, false, reference, previewMode);
        }

        [UnityTest]
        public IEnumerator FrozenNoteAnglesMatchNativeCapture([Values(false, true)] bool playback)
        {
            var map = JSON.Parse(File.ReadAllText(FixturePath("AngstFrozenNoteAnglesFixture.json")));
            yield return CheckFrozenNoteAngles(map, playback);
        }

        [UnityTest, Explicit("Requires the original Angst difficulty under workspace Scratch/AngstOpeningColor.")]
        public IEnumerator FullMapFrozenNoteAnglesMatchNativeCapture([Values(false, true)] bool playback)
        {
            var path = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "AngstOpeningColor",
                "ExpertPlusStandard.dat");
            yield return CheckFrozenNoteAngles(JSON.Parse(File.ReadAllText(path)), playback);
        }

        private IEnumerator CheckFrozenNoteAngles(JSONNode map, bool playback, JSONNode reference = null,
            UIModeType previewMode = UIModeType.Playing)
        {
            reference ??= JSON.Parse(File.ReadAllText(FixturePath("AngstBeat364NativeNoteAngles.json")));
            yield return LoadMap(map);
            mode.SetUIMode(previewMode, false);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var captureBeat = reference["songBeat"].AsFloat;
            if (playback)
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 357f);
                var beginSeconds = clock.GetSecondsFromBeat(357f);
                var endSeconds = reference["songSeconds"].AsFloat;
                for (var seconds = beginSeconds; seconds < endSeconds; seconds += 1f / 90f)
                {
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, clock.GetBeatFromSeconds(seconds));
                    yield return null;
                }
            }
            var failures = new List<string>();
            foreach (var beat in new[] { captureBeat, 358.765f, captureBeat })
            {
                if (playback)
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                else
                    clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                if (beat != captureBeat)
                    continue;

                JSONNode renderedNotes = null;
                var renderObject = new GameObject("Angst note pose after animations");
                try
                {
                    var renderer = renderObject.AddComponent<CameraRenderAfterLateUpdate>();
                    renderer.TargetCamera = cameras.SelectedCameraController.Camera;
                    renderer.BeforeRender = () =>
                    {
                        renderedNotes = new JSONArray();
                        foreach (var note in Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                            .OfType<NoteContainer>().Where(note => note.NoteData.CustomData?["_track"].Value == "anxiety")
                            .OrderBy(note => note.NoteData.JsonTime))
                        {
                            renderedNotes.Add(new JSONObject
                            {
                                ["beat"] = note.NoteData.JsonTime,
                                ["root"] = WriteQuaternion(note.Animator.LocalTarget.rotation),
                                ["visual"] = WriteQuaternion(note.DirectionTarget.rotation),
                                ["local"] = WriteQuaternion(note.DirectionTarget.localRotation),
                                ["model"] = WriteQuaternion(note.ModelController.Actives[0].GameObject.transform.rotation)
                            });
                        }
                        var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "AngstSpawnAngles");
                        Directory.CreateDirectory(directory);
                        LinearRenderCapture.Capture(renderer.TargetCamera,
                            PathUtils.Combine(directory, $"rendered-notes-{map["_notes"].Count}-{playback}.png"));
                    };
                    yield return null;
                    Assert.That(renderedNotes, Is.Not.Null, "The native comparison must sample after the animation LateUpdate.");
                }
                finally
                {
                    Object.DestroyImmediate(renderObject);
                }
                var camera = cameras.SelectedCameraController.Camera;
                Assert.That(Vector3.Distance(camera.transform.position,
                    reference["camera"]["transform"]["position"].ReadVector3()), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(camera.transform.rotation,
                    ReadQuaternion(reference["camera"]["transform"]["rotation"])), Is.LessThan(0.05f));
                Assert.That(camera.fieldOfView, Is.EqualTo(reference["camera"]["fieldOfView"].AsFloat));
                Assert.That(renderedNotes.Count, Is.EqualTo(reference["notes"].Count));
                for (var i = 0; i < renderedNotes.Count; i++)
                {
                    var expected = reference["notes"][i];
                    var note = renderedNotes[i];
                    Assert.That(note["beat"].AsFloat, Is.EqualTo(expected["beat"].AsFloat));
                    var rootAngle = Quaternion.Angle(ReadQuaternion(note["root"]), ReadQuaternion(expected["rootRotation"]));
                    var visualAngle = Quaternion.Angle(ReadQuaternion(note["visual"]), ReadQuaternion(expected["visualRotation"]));
                    var localAngle = Quaternion.Angle(ReadQuaternion(note["local"]),
                        ReadQuaternion(expected["rotatedObjectLocalRotation"]));
                    // The FBX child includes its axis conversion. The model root shares native NoteCube's coordinate basis.
                    var modelAngle = Quaternion.Angle(ReadQuaternion(note["model"]),
                        ReadQuaternion(expected["visualRotation"]));
                    if (rootAngle > 0.05f || visualAngle > 0.05f || localAngle > 0.05f || modelAngle > 0.05f)
                    {
                        failures.Add($"beat {beat}, source note {expected["sourceNoteIndex"]}: "
                            + $"root {rootAngle:F4}°, visual {visualAngle:F4}°, rotated-object local {localAngle:F4}°, model {modelAngle:F4}°");
                    }
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(12)));
        }

        private static JSONNode WriteQuaternion(Quaternion value) =>
            new JSONArray { [0] = value.x, [1] = value.y, [2] = value.z, [3] = value.w };

        private static Quaternion ReadQuaternion(JSONNode value) =>
            new(value[0].AsFloat, value[1].AsFloat, value[2].AsFloat, value[3].AsFloat);

        [UnityTest]
        public IEnumerator FrozenTimePreservesNativeNoteFacing([Values(0.499f, 0.5f)] float frozenTime)
        {
            var map = JSON.Parse(File.ReadAllText(FixturePath("AngstNoteDissolveFixture.json")));
            map["_customData"]["_customEvents"][0]["_data"]["_time"][0][0] = frozenTime;
            map["_notes"][0]["_customData"]["_animation"]["_time"][0][0] = frozenTime;
            yield return LoadMap(map);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var failures = new List<string>();
            foreach (var beat in new[] { 99.171f, 100f, 99.171f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single();
                // Heck interpolates quaternion points after applying the event's easeOutQuart.
                var progress = Mathf.Clamp01((beat - 99f) / 12f);
                var eased = 1f - Mathf.Pow(1f - progress, 4f);
                var parent = Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    Quaternion.Euler(358.479f, 266.028f, 339.43f),
                    eased);
                // SetToStart initializes identity. Noodle's rotation callback remains inside num2 < 0.5.
                var expected = frozenTime < 0.5f
                    ? parent * Quaternion.Euler(0f, 0f, -180f)
                    : parent;
                var angle = Quaternion.Angle(note.DirectionTarget.rotation, expected);
                if (angle > 0.05f)
                {
                    failures.Add(
                        $"beat {beat}: rotation differs by {angle:F3} degrees; "
                        + $"cut={note.DirectionTargetEuler:R}, direction={note.DirectionTarget.rotation:R}, "
                        + $"parent={note.DirectionTarget.parent.rotation:R}, expected={expected:R}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest, Explicit("Requires the original Angst difficulty under workspace Scratch/AngstOpeningColor.")]
        public IEnumerator FullMapFrozenNotesPreserveNativeFacing()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "AngstOpeningColor",
                "ExpertPlusStandard.dat");
            var map = JSON.Parse(File.ReadAllText(path));
            var rotations = new Dictionary<string, Quaternion>();
            foreach (var entry in map["_customData"]["_customEvents"].AsArray)
            {
                var ev = entry.Value;
                var track = ev["_data"]["_track"].Value;
                if (ev["_time"].AsFloat != 99f || !track.StartsWith("NoteExplode"))
                    continue;

                var end = ev["_data"]["_localRotation"][1];
                rotations.Add(track, Quaternion.Euler(end[0].AsFloat, end[1].AsFloat, end[2].AsFloat));
            }

            yield return LoadMap(map);
            var failures = new List<string>();
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "AngstOrientation");
            Directory.CreateDirectory(directory);
            var sample = 0;
            foreach (var beat in new[] { 99.171f, 100f, 99.171f })
            {
                Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var notes = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Where(note => note.NoteData.JsonTime == 98.5f).ToArray();
                Assert.That(notes.Length, Is.EqualTo(100));
                foreach (var note in notes)
                {
                    var track = note.NoteData.CustomTrack.Value;
                    var progress = Mathf.Clamp01((beat - 99f) / 12f);
                    var eased = 1f - Mathf.Pow(1f - progress, 4f);
                    var parent = Quaternion.SlerpUnclamped(Quaternion.identity, rotations[track], eased);
                    var expected = parent;
                    var angle = Quaternion.Angle(note.DirectionTarget.rotation, expected);
                    if (angle > 0.05f)
                    {
                        failures.Add(
                            $"beat {beat}, {track}: error={angle:F3}, cut={note.DirectionTargetEuler:R}, "
                            + $"direction={note.DirectionTarget.rotation:R}, expected={expected:R}");
                    }
                }

                LinearRenderCapture.Capture(
                    cameras.SelectedCameraController.Camera,
                    PathUtils.Combine(directory, $"{sample++}-{beat}.png"));
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator ReducedCloudsPillarsAndOverheadFacesReceiveDirectionalLighting()
        {
            yield return LoadMap(JSON.Parse(File.ReadAllText(FixturePath("AngstSurfaceLightingFixture.json"))));
            var camera = cameras.SelectedCameraController.Camera;
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "AngstBeat99");
            Directory.CreateDirectory(directory);
            var actual = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "surfaces-actual.png"));
            var lightshow = Object.FindAnyObjectByType<LightshowController>();
            var wasEnabled = lightshow.enabled;
            lightshow.enabled = false;
            var mixers = Object.FindObjectsByType<DirectionalLightsController>(FindObjectsSortMode.None);
            var originalColors = mixers.Select(mixer => mixer.Light.Color).ToArray();
            try
            {
                foreach (var mixer in mixers)
                    mixer.Refresh();

                yield return null;
                yield return null;
                var reference = LinearRenderCapture.Capture(
                    camera,
                    PathUtils.Combine(directory, "surfaces-refreshed.png"));
                foreach (var mixer in mixers)
                    mixer.Light.Color = Color.clear;

                yield return null;
                yield return null;
                var unlit = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "surfaces-unlit.png"));
                var regions = new Dictionary<string, RectInt>
                {
                    ["lower cloud tops"] = new RectInt(150, 100, 225, 175),
                    ["upper cloud bottoms"] = new RectInt(15, 395, 250, 175),
                    ["runway-facing pillar"] = new RectInt(350, 255, 75, 110),
                    ["overhead inward face"] = new RectInt(610, 570, 15, 85)
                };
                var failures = new List<string>();
                foreach (var region in regions)
                {
                    var expectedEnergy = 0f;
                    var actualEnergy = 0f;
                    var pixels = 0;
                    for (var y = region.Value.yMin; y < region.Value.yMax; y++)
                    {
                        for (var x = region.Value.xMin; x < region.Value.xMax; x++)
                        {
                            var index = y * 1280 + x;
                            var expected = reference[index] - unlit[index];
                            if (expected.maxColorComponent < 0.02f)
                                continue;

                            pixels++;
                            expectedEnergy += expected.grayscale;
                            actualEnergy += (actual[index] - unlit[index]).grayscale;
                        }
                    }

                    if (pixels < 30 || actualEnergy < expectedEnergy * 0.95f)
                    {
                        failures.Add(
                            $"{region.Key}: {pixels} lit control pixels, "
                            + $"received {actualEnergy:R} of {expectedEnergy:R} expected light");
                    }
                }

                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
            finally
            {
                for (var i = 0; i < mixers.Length; i++)
                    mixers[i].Light.Color = originalColors[i];

                lightshow.enabled = wasEnabled;
            }
        }

        [UnityTest]
        public IEnumerator HalfDissolvedNoteKeepsNativeMaterialAlpha()
        {
            yield return LoadMap(JSON.Parse(File.ReadAllText(FixturePath("AngstNoteDissolveFixture.json"))));
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single();
            Assert.That(note.NoteData.CustomColor.Value.a, Is.EqualTo(2f), "Keep the original map color intact.");
            Assert.That(note.MpbController.Mpb.GetFloat("_Cutout"), Is.EqualTo(0.5f).Within(0.0001f));
            var source = note.ModelController.MpbController.Renderers[0];
            var sample = new GameObject("Angst isolated note body");
            var cameraObject = new GameObject("Angst isolated note camera");
            var material = new Material(source.sharedMaterial);
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "AngstBeat99");
            Directory.CreateDirectory(directory);
            try
            {
                sample.layer = 30;
                sample.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var renderer = sample.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                material.DisableKeyword("_FOGTYPE_LERP");
                var properties = new MaterialPropertyBlock();
                source.GetPropertyBlock(properties);
                renderer.SetPropertyBlock(properties);
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << 30;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = 0.4f;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -3f), Quaternion.identity);
                var actual = LinearRenderCapture.Capture(
                    camera,
                    PathUtils.Combine(directory, "note-actual.png"),
                    256,
                    256);
                var color = properties.GetColor("_Color");
                color.a = 1f;
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);
                var reference = LinearRenderCapture.Capture(
                    camera,
                    PathUtils.Combine(directory, "note-native-alpha.png"),
                    256,
                    256);
                var bodyPixels = 0;
                var actualBody = 0f;
                var referenceBody = 0f;
                for (var i = 0; i < actual.Length; i++)
                {
                    if (reference[i].a > 0.1f || reference[i].maxColorComponent < 0.001f)
                        continue;

                    bodyPixels++;
                    actualBody += actual[i].grayscale;
                    referenceBody += reference[i].grayscale;
                }

                Assert.That(bodyPixels, Is.GreaterThan(100), "The control must contain surviving reflected body surfaces.");
                Assert.That(
                    actualBody / referenceBody,
                    Is.EqualTo(1f).Within(0.005f),
                    "Chroma recolors note RGB while retaining ColorNoteVisuals' material alpha of one.");
                Assert.That(note.MpbController.Mpb.GetColor("_Color").a, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(sample);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(material);
            }
        }

        [UnityTest]
        public IEnumerator TrackedCoreLightingKeepsRefreshingDirectionalColors([Values(false, true)] bool duplicate)
        {
            var map = JSON.Parse(@"{
                '_version':'2.6.0', '_notes':[], '_obstacles':[],
                '_events':[{'_time':99,'_type':4,'_value':1,
                    '_customData':{'_color':[0.737,0.027,0.918,1]}}],
                '_customData':{
                    '_environment':[{'_id':'\\]CoreLighting$','_lookupMethod':'Regex','_track':'Env'}],
                    '_customEvents':[{'_time':335,'_type':'AssignTrackParent','_data':{
                        '_childrenTracks':['Env'],'_parentTrack':'Env1','_worldPositionStays':true}}]}
            }".Replace('\'', '"'));
            if (duplicate)
                map["_customData"]["_environment"][0]["_duplicate"] = 1;

            yield return LoadMap(map);
            for (var pass = 0; pass < 2; pass++)
            {
                var lights = Object.FindObjectsByType<DirectionalLightsController>(FindObjectsSortMode.None)
                    .Where(controller => Mathf.Abs(controller.Light.transform.position.y - 2.07f) < 0.01f)
                    .ToArray();
                Assert.That(lights.Length, Is.EqualTo(duplicate ? 2 : 1));
                foreach (var light in lights)
                {
                    var input = light.LightIntensityData[4].Color;
                    Assert.That(input.b, Is.GreaterThan(0.9f), "The event must reach the mixer input.");
                    Assert.That(
                        light.Light.Color.b,
                        Is.EqualTo(input.b * Mathf.Sqrt(input.a)).Within(0.0001f),
                        "Tracking CoreLighting must retain its directional mixer in the environment refresh list.");
                }

                if (pass == 0)
                {
                    Object.FindAnyObjectByType<GeometryGridContainer>().RefreshPool(true);
                    yield return null;
                    yield return null;
                }
            }
        }

        [UnityTest, Explicit("Requires the complete original Angst difficulty under workspace Scratch.")]
        public IEnumerator FullMapAtReportedBeat()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "AngstOpeningColor",
                "ExpertPlusStandard.dat");
            Assert.That(File.Exists(path), Is.True);
            yield return LoadMap(JSON.Parse(File.ReadAllText(path)));
            var camera = cameras.SelectedCameraController.Camera;
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "AngstBeat99");
            LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "full-map.png"), 2560, 1440);

            var notes = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Where(note => note.NoteData.JsonTime == 98.5f).ToArray();
            Assert.That(notes.Length, Is.EqualTo(100));
            foreach (var note in notes)
            {
                Assert.That(note.MpbController.Mpb.GetFloat("_Cutout"), Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(note.MpbController.Mpb.GetColor("_Color").a, Is.EqualTo(1f));
            }

            foreach (var height in new[] { 0.5f, 2.07f })
            {
                var light = DirectionalLight.Lights.Single(
                    candidate => Mathf.Abs(candidate.transform.position.y - height) < 0.01f);
                Assert.That(light.Color.maxColorComponent, Is.GreaterThan(0.5f));
            }
        }

        [UnityTest, Explicit("Requires the original Angst difficulty under workspace Scratch/AngstOpeningColor.")]
        public IEnumerator FullMapPauseDoesNotPopulateInactiveEventGrid()
        {
            var path = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "AngstOpeningColor",
                "ExpertPlusStandard.dat");
            Assert.That(File.Exists(path), Is.True);
            yield return LoadMap(JSON.Parse(File.ReadAllText(path)));
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var events = Object.FindAnyObjectByType<EventGridContainer>(FindObjectsInactive.Include);
            clock.MoveToJsonTime(140f);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 140f);
            for (var step = 1; step <= 91; step++)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, 140f + step / 10f);
                yield return null;
            }
            Assert.That(events.isActiveAndEnabled, Is.False);
            var countBeforePause = events.LoadedContainers.Count;
            TestUtils.PauseDeterministicPlayback(clock);
            Assert.That(events.LoadedContainers.Count, Is.EqualTo(countBeforePause),
                "Pausing the animated note exit must not populate the inactive Basic Events timeline.");
        }

        private static string FixturePath(string name) =>
            PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", name);

        [UnityTest]
        public IEnumerator FutureParentAssignmentPreservesPillarLaserRotation([Values(false, true)] bool playback)
        {
            var map = JSON.Parse(@"{
                '_version':'2.2.0','_notes':[],'_obstacles':[],'_events':[],
                '_customData':{
                    '_environment':[
                        {'_id':'\\]SmallPillarPair\\.\\[\\d*\\]PillarL$','_lookupMethod':'Regex',
                         '_track':'pillar','_localRotation':[0,0,180]},
                        {'_id':'\\]SmallPillarPair\\.\\[\\d*\\]PillarL\\.\\[\\d*\\]LaserL$',
                         '_lookupMethod':'Regex','_track':'environment'}],
                    '_customEvents':[
                        {'_time':0,'_type':'AnimateTrack','_data':{
                            '_track':'pillar','_localRotation':[[0,0,160,0]]}},
                        {'_time':335,'_type':'AssignTrackParent','_data':{
                            '_childrenTracks':['environment'],'_parentTrack':'parent',
                            '_worldPositionStays':true}}]}}".Replace('\'', '"'));
            yield return LoadMap(map);
            var markers = Object.FindObjectsByType<ChromaIDMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var pillar = markers.Single(marker => marker.ChromaID.EndsWith("SmallPillarPair.[0]PillarL")).transform;
            var laser = markers.Single(marker => marker.ChromaID.EndsWith("SmallPillarPair.[0]PillarL.[2]LaserL")).transform;
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (playback)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 243f);
            foreach (var beat in new[] { 243f, 336f, 243f })
            {
                if (playback)
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                else
                    clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                Assert.That(laser.parent == pillar, Is.EqualTo(beat < 335f),
                    $"The beat-335 parenting event must take effect at its callback time, including after rewind at {beat}.");
                Assert.That(Quaternion.Angle(laser.rotation, Quaternion.Euler(0f, 0f, 160f)), Is.LessThan(0.05f),
                    "The laser follows its pillar before reassignment, and worldPositionStays preserves that angle afterward.");
            }
        }

        private IEnumerator LoadMap(JSONNode map)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.EmulateChromaAdvanced = true;
            Settings.Instance.PlayerCameraFOV = 100f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return TestUtils.ReloadMap(
                2,
                map,
                beatsPerMinute: 150f,
                environmentName: "BTSEnvironment",
                songLengthSeconds: 240,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 19f,
                    NoteStartBeatOffset = -0.5f,
                    CustomSuggestions = new List<string> { "Chroma" },
                    CustomRequirements = new List<string> { "Noodle Extensions" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(99.187f);
            yield return null;
            yield return null;
            cameras.SelectedCameraController.Camera.transform.SetPositionAndRotation(
                new Vector3(0f, 1.7f, 0f),
                Quaternion.identity);
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChromaLite;
            Settings.Instance.EmulateChromaAdvanced = previousChromaAdvanced;
            Settings.Instance.HideNoteSpawnEffect = previousHideSpawnEffect;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
