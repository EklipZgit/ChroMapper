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
using UnityEditor;

namespace Tests.Editor
{
    // NativeObjectSnapshot 20261109T023311038-beat241: 129bf ExpertPlus at 72.30696 song seconds.
    public class MopeMopeNative241Test : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private JSONNode expected;
        private Vector3 previousPlayerLocalPosition;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousPlayerLocalPosition = Object.FindAnyObjectByType<CameraManager>().PlayerCameraController.transform.localPosition;
            yield break;
        }

        protected override void CleanupTestObjects() { }

        [UnityTest]
        public IEnumerator JumpDepthMatchesTheCapturedNativeBomb()
        {
            yield return Load("Depth");
            var note = Find("Depth");
            // Native gravity includes the automatic player-height calibration active during this capture.
            note.JumpApexHeight = expected["notes"]["Depth"]["jumpApexHeight"].AsFloat;
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(note.Animator.LocalTarget.position, Vector(expected["notes"]["Depth"]["position"])),
                Is.LessThan(0.002f), "Native note depth includes the .65 movement center and .25 note offset.");
            Assert.That(note.MpbController.Mpb.GetFloat(Shader.PropertyToID("_Cutout")),
                Is.EqualTo(expected["notes"]["Depth"]["cutout"].AsFloat).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator InvalidLayersStackInConvertedLayerAndFileOrder()
        {
            yield return Load("Stack");
            Assert.That(Find("Stack").JumpStartHeight,
                Is.EqualTo(expected["notes"]["Stack"]["jumpStartOffset"][1].AsFloat).Within(0.0001f),
                "Noodle sorts converted base layers, preserving file order among equal layers.");
            Assert.That(Vector3.Distance(Find("Stack").Animator.LocalTarget.position,
                Vector(expected["notes"]["Stack"]["position"])), Is.LessThan(0.002f),
                "The floor phase uses the same native note depth as the jump.");
        }

        [UnityTest]
        public IEnumerator CrossedEyesStartOnTheOppositeSideWithVerticalAvoidance()
        {
            yield return Load("Crossed");
            var note = Find("Crossed");
            var position = note.Animator.LocalTarget.position;
            var native = Vector(expected["notes"]["Crossed"]["position"]);
            Assert.That(Vector3.Distance(position, native), Is.LessThan(0.002f),
                "The red eye starts at the blue eye's lane, then crosses during the first quarter of the jump.");
        }

        [UnityTest]
        public IEnumerator HorizontalDotPairUsesTheNativeNegativeNinetyDegreeTurn()
        {
            yield return Load("Dots");
            var note = Find("Dots");
            var native = expected["notes"]["Dots"]["endRotation"];
            var rotation = new Quaternion(native[0].AsFloat, native[1].AsFloat, native[2].AsFloat, native[3].AsFloat);
            Assert.That(Quaternion.Angle(Quaternion.Euler(note.DirectionTargetEuler), rotation), Is.LessThan(0.05f),
                "The two-dot SignedAngleToLine tie must use the native directed line and file order.");
        }

        [UnityTest]
        public IEnumerator HistoricalHeadIncludesThePlayerTracksOwnOffset()
        {
            yield return Load("Head");
            var position = cameras.PlayerCameraController.transform.localPosition;
            position.y = 1.7f;
            cameras.PlayerCameraController.transform.localPosition = position;
            var actual = cameras.PlayerCameraController.GetPlayerHeadPosition(240.625f);
            Assert.That(Vector3.Distance(actual, Vector(expected["headAtHit"])), Is.LessThan(0.0001f),
                "The native post-hit facing uses the player track's sampled transform, including its own offset.");
        }

        [UnityTest]
        public IEnumerator CapturedBombFacingUsesThePreviousPreviewFramePosition()
        {
            yield return Load("Facing");
            var position = cameras.PlayerCameraController.transform.localPosition;
            position.y = 1.7f;
            cameras.PlayerCameraController.transform.localPosition = position;
            var note = Find("Facing");
            note.HasHitLookRotation = false;
            note.Animator.ApplyAnimation();
            var native = expected["notes"]["Facing"]["visualRotation"];
            var rotation = new Quaternion(native[0].AsFloat, native[1].AsFloat, native[2].AsFloat, native[3].AsFloat);
            // The native capture is 120Hz. CM uses the shared deterministic 90Hz approximation.
            Assert.That(Quaternion.Angle(note.DirectionTarget.rotation, rotation), Is.LessThan(4f),
                "Facing must use the previous movement position rather than aiming ahead of the game.");
        }

        [UnityTest]
        public IEnumerator HistoricalV3HeadUsesTheRequestedPlayerPosition()
        {
            yield return Load("V3Head");
            clock.MoveToJsonTime(230.5f);
            yield return null;
            yield return null;
            var earlier = cameras.PlayerCameraController.GetPlayerHeadPosition();
            clock.MoveToJsonTime(241f);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(earlier, cameras.PlayerCameraController.GetPlayerHeadPosition()), Is.GreaterThan(0.05f));
            Assert.That(Vector3.Distance(earlier, cameras.PlayerCameraController.GetPlayerHeadPosition(230.5f)),
                Is.LessThan(0.0001f), "Facing history must sample the player's V3 position at that time, rather than the live rig.");
        }

        [UnityTest]
        public IEnumerator EyeScaleUsesThePreviousCompletedTrackFrame()
        {
            yield return Load("Crossed");
            Assert.That(Find("Crossed").Animator.LocalTarget.localScale.y, Is.EqualTo(12f).Within(0.001f),
                "Heck updates AnimateTrack at end-of-frame, after notes read their scale for rendering.");
        }

        [Test]
        public void DotGlowFadesWithoutArrowNoiseCutout()
        {
            // Native NoteCircleGlow has no cutout core, including the black dots in the capture.
            var full = RenderDot(0f, 0.6f);
            var cut = RenderDot(1f, 0.6f);
            var hidden = RenderDot(0f, 0f);
            Assert.That(full, Is.GreaterThan(1f), "The isolated dot is the render control.");
            Assert.That(cut, Is.EqualTo(full).Within(0.01f), "Native dots fade by transparency and ignore arrow noise cutout.");
            Assert.That(hidden, Is.LessThan(0.01f), "Zero arrow transparency must hide the dot.");
        }

        private static float RenderDot(float cutout, float opacity)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/MapEditor/Visuals/CM_Note_Dot.prefab"));
            var cameraObject = new GameObject("Native dot render repro");
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            var previousTarget = RenderTexture.active;
            var preview = Shader.IsKeywordEnabled("CM_PREVIEW_MODE");
            try
            {
                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.gameObject.layer = 30;
                    var properties = new MaterialPropertyBlock();
                    properties.SetColor("_Color", Color.white);
                    properties.SetColor("_GlowColor", Color.white);
                    properties.SetFloat("_ColorMultiplier", 1f);
                    properties.SetFloat("_ArrowDissolve", opacity);
                    properties.SetFloat("_Cutout", cutout);
                    properties.SetFloat("_AnimationSpawned", 1f);
                    renderer.SetPropertyBlock(properties);
                }
                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.orthographic = true;
                camera.orthographicSize = 0.35f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -1f), Quaternion.identity);
                camera.nearClipPlane = 0.01f;
                camera.targetTexture = target;
                RenderTexture.active = target;
                Shader.EnableKeyword("CM_PREVIEW_MODE");
                camera.Render();
                pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                pixels.Apply();
                var total = 0f;
                foreach (var pixel in pixels.GetPixels())
                    total += pixel.r + pixel.g + pixel.b;

                return total;
            }
            finally
            {
                if (!preview)
                    Shader.DisableKeyword("CM_PREVIEW_MODE");

                RenderTexture.active = previousTarget;
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        private IEnumerator Load(string name)
        {
            var directory = PathUtils.Combine(Application.dataPath, "Tests/Fixtures");
            expected = JSON.Parse(File.ReadAllText(PathUtils.Combine(directory, "MopeMopeNative241Expectations.json")));
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(name == "V3Head" ? 3 : 2,
                JSON.Parse(File.ReadAllText(PathUtils.Combine(directory, $"MopeMopeNative241{name}Fixture.json"))),
                beatsPerMinute: 200f, songLengthSeconds: 150, environmentName: "TriangleEnvironment",
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 16f, NoteStartBeatOffset = 2f,
                    CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Playing);
            mode.SetUIMode(UIModeType.Preview, false);
            clock.MoveToSongBpmTime(clock.GetBeatFromSeconds(expected["songSeconds"].AsFloat));
            yield return null;
            yield return null;
        }

        private NoteContainer Find(string name)
        {
            var identity = expected["notes"][name];
            return Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values.OfType<NoteContainer>()
                .Single(note => note.NoteData.JsonTime == identity["beat"].AsFloat
                    && note.NoteData.PosX == identity["x"].AsFloat && note.NoteData.PosY == identity["y"].AsFloat
                    && note.NoteData.Type == identity["type"].AsInt);
        }

        private static Vector3 Vector(JSONNode node) => new(node[0].AsFloat, node[1].AsFloat, node[2].AsFloat);

        [UnityTearDown]
        public IEnumerator Restore()
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
            {
                cameras.SelectCamera(CameraType.Editing);
                cameras.PlayerCameraController.transform.localPosition = previousPlayerLocalPosition;
            }

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
