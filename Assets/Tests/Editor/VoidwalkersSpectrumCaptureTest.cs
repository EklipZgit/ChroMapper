using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    [Explicit("Requires the local Voidwalkers map and song for a native spectrum comparison.")]
    public class VoidwalkersSpectrumCaptureTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioClip previousSong;
        private AudioClip song;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private Camera captureCamera;
        private Vector3 previousCameraPosition;
        private Quaternion previousCameraRotation;
        private float previousCameraFov;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator CaptureExpertPlusStandardAtBeat225890()
        {
            const string mapDirectory = @"C:\Users\tdrak\BSManager\SharedContent\SharedMaps\CustomLevels\50033 (Voidwalkers - Nallak, Kinz, Uzi, Sonoluminn)";
            var sourcePath = PathUtils.Combine(mapDirectory, "ExpertPlusStandard.dat");
            if (!File.Exists(sourcePath))
                Assert.Ignore("Requires the original local Voidwalkers map.");

            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(sourcePath)),
                beatsPerMinute: 142f, environmentName: "Monstercat2Environment", songLengthSeconds: 300,
                forceSceneReload: true, difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 18f, NoteStartBeatOffset = -0.5f,
                    CustomSuggestions = new List<string> { "ChromaGLS" },
                    CustomColorLeft = new Color(1f, 0.498f, 0f),
                    CustomColorRight = new Color(0f, 0.529f, 1f),
                    CustomEnvColorLeft = new Color(0.902f, 0.486f, 0.325f),
                    CustomEnvColorRight = new Color(0.4f, 0.718f, 1f),
                    CustomEnvColorBoostLeft = new Color(1f, 0.235f, 0.596f),
                    CustomEnvColorBoostRight = new Color(0.298f, 0.51f, 1f)
                });
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            previousSong = BeatSaberSongContainer.Instance.LoadedSong;
            using (var request = UnityWebRequestMultimedia.GetAudioClip(
                new System.Uri(PathUtils.Combine(mapDirectory, "song.egg")).AbsoluteUri, AudioType.OGGVORBIS))
            {
                yield return request.SendWebRequest();
                Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
                song = DownloadHandlerAudioClip.GetContent(request);
            }
            BeatSaberSongContainer.Instance.LoadedSong = song;
            clock.SongAudioSource.clip = song;
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            context.Descriptor.SpectrogramDataProvider.Initialize(clock);
            yield return context.Descriptor.SpectrogramDataProvider.WaitForPreparation();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(225.890f);
            yield return null;
            yield return null;
            Assert.That(clock.CurrentSeconds, Is.EqualTo(96.729f).Within(0.002f));
            var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "NativeSpectrumParity", "Voidwalkers");
            Directory.CreateDirectory(directory);
            var provider = context.Descriptor.SpectrogramDataProvider;
            var screens = context.Descriptor.GetComponentsInChildren<SpectrogramRowPropertyAnimator>()
                .Where(value => value.isActiveAndEnabled).ToArray();
            Assert.That(screens, Is.Not.Empty);
            Assert.That(screens.Max(value => value.MpbController.Mpb.GetFloat(value.PropertyName)), Is.GreaterThan(0.01f));
            var camera = cameras.SelectedCameraController.Camera;
            captureCamera = camera;
            previousCameraPosition = camera.transform.position;
            previousCameraRotation = camera.transform.rotation;
            previousCameraFov = camera.fieldOfView;
            camera.fieldOfView = 100f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
            var pcmStart = Mathf.FloorToInt(clock.CurrentSeconds * song.frequency) - 4096;
            var interleavedPcm = new float[8192 * song.channels];
            song.GetData(interleavedPcm, pcmStart);
            var channelZero = new float[8192];
            for (var index = 0; index < channelZero.Length; index++)
                channelZero[index] = interleavedPcm[index * song.channels];

            var output = new JSONObject
            {
                ["sourcePath"] = sourcePath,
                ["beat"] = clock.CurrentJsonTime,
                ["seconds"] = clock.CurrentSeconds,
                ["outputRate"] = TimelineSpectrum.OutputSampleRate,
                ["editorOutputRate"] = AudioSettings.outputSampleRate,
                ["clipRate"] = song.frequency,
                ["simulationRate"] = TimelineSpectrum.FramesPerSecond,
                ["audioLatencyMilliseconds"] = Settings.Instance.AudioLatencyCompensation,
                ["pcmStart"] = pcmStart,
                ["pcmChannelZero"] = Floats(channelZero),
                ["cameraPosition"] = camera.transform.position.ToString("R"),
                ["cameraRotation"] = camera.transform.eulerAngles.ToString("R"),
                ["raw"] = Floats(provider.Samples),
                ["processed"] = Floats(provider.ProcessedSamples)
            };
            var screenValues = new JSONArray();
            foreach (var screen in screens)
            {
                screenValues.Add(new JSONObject
                {
                    ["name"] = screen.name,
                    ["position"] = screen.transform.position.ToString("R"),
                    ["dataIndex"] = screen.DataIndex,
                    ["multiplier"] = screen.Multiplier,
                    ["value"] = screen.MpbController.Mpb.GetFloat(screen.PropertyName)
                });
            }
            output["screens"] = screenValues;
            File.WriteAllText(PathUtils.Combine(directory, "cm-corrected.json"), output.ToString());
            LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "cm-corrected.png"));
            CaptureRecordedNativeInputs(camera, screens, directory);
        }

        private void CaptureRecordedNativeInputs(
            Camera camera,
            SpectrogramRowPropertyAnimator[] screens,
            string directory)
        {
            var referencePath = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "DiagnosticTools",
                "NativeSpectrumSnapshot",
                "Examples",
                "Voidwalkers-225.890.json");
            var reference = JSON.Parse(File.ReadAllText(referencePath));
            var frame = reference["frames"].Children.First(value =>
                value["frame"].AsInt == reference["targetFrame"].AsInt);
            clock.MoveToTimeInSeconds(frame["songTime"].AsFloat);
            camera.fieldOfView = frame["verticalFov"].AsFloat;
            var position = frame["cameraPosition"];
            var rotation = frame["cameraRotation"];
            camera.transform.SetPositionAndRotation(
                new Vector3(position[0].AsFloat, position[1].AsFloat, position[2].AsFloat),
                new Quaternion(rotation[0].AsFloat, rotation[1].AsFloat, rotation[2].AsFloat, rotation[3].AsFloat));
            var values = new JSONArray();
            var displacementCount = 0;
            foreach (var screen in screens)
            {
                var native = frame["screens"].Children.Where(value =>
                    value["active"].AsBool &&
                    value["property"].Value == screen.PropertyName &&
                    value["dataIndex"].AsInt == screen.DataIndex)
                    .OrderBy(value => Mathf.Abs(value["multiplier"].AsFloat - screen.Multiplier))
                    .FirstOrDefault();
                Assert.That(native, Is.Not.Null, $"No recorded input for {screen.name}/{screen.PropertyName}/{screen.DataIndex}.");
                var recordedValue = native["value"].AsFloat;
                // Use the rendered native value, including its update order, rather than resmoothing its FFT.
                screen.MpbController.Mpb.SetFloat(screen.PropertyName, recordedValue);
                screen.MpbController.ApplyChanges();
                values.Add(new JSONObject
                {
                    ["name"] = screen.name,
                    ["property"] = screen.PropertyName,
                    ["dataIndex"] = screen.DataIndex,
                    ["value"] = screen.MpbController.Mpb.GetFloat(screen.PropertyName),
                    ["nativeValue"] = recordedValue
                });
                if (screen.PropertyName == "_DisplacementStrength")
                {
                    displacementCount++;
                }
            }
            Assert.That(displacementCount, Is.EqualTo(32));
            File.WriteAllText(PathUtils.Combine(directory, "cm-native-inputs.json"), new JSONObject
            {
                ["referencePath"] = referencePath,
                ["frame"] = frame["frame"].AsInt,
                ["seconds"] = clock.CurrentSeconds,
                ["screens"] = values
            }.ToString());
            LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, "cm-native-inputs.png"));
        }

        private static JSONArray Floats(float[] values)
        {
            var result = new JSONArray();
            foreach (var value in values)
                result.Add(value);

            return result;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (captureCamera != null)
            {
                captureCamera.fieldOfView = previousCameraFov;
                captureCamera.transform.SetPositionAndRotation(previousCameraPosition, previousCameraRotation);
            }
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
            {
                clock.enabled = previousClockEnabled;
                clock.SongAudioSource.clip = previousSong;
                BeatSaberSongContainer.Instance.LoadedSong = previousSong;
            }
            Settings.Instance.Animations = previousAnimations;
            if (song != null)
                Object.Destroy(song);

            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
