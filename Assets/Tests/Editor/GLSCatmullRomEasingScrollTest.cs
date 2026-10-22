using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    // Ctrl+Shift+scroll on an outer GLS preview ghost must cycle the real easing list through the
    // collection-owned GLS Group prefab's icon view; a group prefab missing sprites threw
    // IndexOutOfRangeException while rebinding the preview ghost.
    public class GLSCatmullRomEasingScrollTest : InputTestBase
    {
        private const int TrackId = 2003;
        private const string TrackGroup = "Catmull-Rom easing scroll";

        // Keep the editor's cycle order explicit: the standard prefix pairs each Beat Saber InOut with
        // its true InOut, and the enum remainder follows with Catmull-Rom last.
        private static readonly EaseType[] EditorCycleOrder =
        {
            EaseType.None,
            EaseType.Linear,
            EaseType.InQuadratic,
            EaseType.OutQuadratic,
            EaseType.InOutQuadratic,
            EaseType.InCircular,
            EaseType.OutCircular,
            EaseType.InOutCircular,
            EaseType.InBack,
            EaseType.OutBack,
            EaseType.BeatSaberInOutBack,
            EaseType.InOutBack,
            EaseType.InElastic,
            EaseType.OutElastic,
            EaseType.BeatSaberInOutElastic,
            EaseType.InOutElastic,
            EaseType.InBounce,
            EaseType.OutBounce,
            EaseType.BeatSaberInOutBounce,
            EaseType.InOutBounce,
            EaseType.InSinusoidal,
            EaseType.OutSinusoidal,
            EaseType.InOutSinusoidal,
            EaseType.InCubic,
            EaseType.OutCubic,
            EaseType.InOutCubic,
            EaseType.InQuartic,
            EaseType.OutQuartic,
            EaseType.InOutQuartic,
            EaseType.InQuintic,
            EaseType.OutQuintic,
            EaseType.InOutQuintic,
            EaseType.InExponential,
            EaseType.OutExponential,
            EaseType.InOutExponential,
            EaseType.CatmullRom,
            EaseType.CatmullRomCentripetal,
            EaseType.CatmullRomChordal
        };

        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;
        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider groupProvider;
        private BeatmapActionContainer actionContainer;
        private string originalPage;
        private bool originalGhostPreview;
        private float originalGhostOpacity;
        private bool originalInvert;
        private CMInput isolatedInput;
        private GameObject controllerObject;
        private Dictionary<int, int> registeredLightCounts;
        private int registeredGroupId = -1;
        private int previousLightCount;
        private bool hadPreviousLightCount;

        protected override EditingMode InitialEditingMode => EditingMode.GLS;

        // One synthetic outer lane supplies every GLS node kind so each case spawns through the real
        // collection path against the real GLS Group prefab.
        [SetUp]
        public void ConfigureScrollTrack()
        {
            runtime = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            groupProvider = Object.FindAnyObjectByType<GLSGroupGridProvider>();
            actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            originalTracks = runtime.TrackDefinitions;
            originalPage = groupProvider.CurrentGroup;
            originalGhostPreview = Settings.Instance.EnableGLSGhostPreview;
            originalGhostOpacity = Settings.Instance.GLSOuterTrackGhostNodeOpacity;
            originalInvert = Settings.Instance.InvertScrollEventValue;
            registeredLightCounts = (Dictionary<int, int>)typeof(GLSEventCommon)
                .GetField("colorLightCounts", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            testTracks.Register(new TrackDefinitionGLS
            {
                ID = TrackId,
                Name = TrackGroup,
                Group = TrackGroup,
                ColorTrack = true,
                FloatFXTrack = true,
                RotationTracks = new[] { true },
                TranslationTracks = new[] { true }
            });
            runtime.TrackDefinitions = testTracks;
            runtime.NotifyTrackDefinitions();
            groupProvider.SetGroupPage(TrackGroup);
            Settings.Instance.EnableGLSGhostPreview = true;
            Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
            Settings.Instance.InvertScrollEventValue = false;
        }

        [TearDown]
        public void RestoreScrollTrack()
        {
            runtime.TrackDefinitions = originalTracks;
            runtime.NotifyTrackDefinitions();
            groupProvider.SetGroupPage(originalPage);
            Settings.Instance.EnableGLSGhostPreview = originalGhostPreview;
            Settings.Instance.GLSOuterTrackGhostNodeOpacity = originalGhostOpacity;
            Settings.Instance.InvertScrollEventValue = originalInvert;
            if (registeredGroupId >= 0)
            {
                if (hadPreviousLightCount)
                {
                    registeredLightCounts[registeredGroupId] = previousLightCount;
                }
                else
                {
                    registeredLightCounts.Remove(registeredGroupId);
                }
            }
            Object.DestroyImmediate(testTracks);
            if (controllerObject != null)
            {
                Object.DestroyImmediate(controllerObject);
                controllerObject = null;
            }
        }

        protected override void AfterCleanup()
        {
            BeatmapRaycastCache.Invalidate();
            if (isolatedInput != null)
            {
                isolatedInput.Disable();
                isolatedInput.Dispose();
                isolatedInput = null;
            }
            TearDownVirtualInput();
        }

        // The hovered ghost must keep its second-node binding while the action's group replacement
        // reuses the same pooled container, so each scroll edits the node the cursor was on.
        [Test]
        public void OuterPreviewGhostCtrlShiftScrollCyclesEveryEasingIntoAndOutOfCatmullRom(
            [Values] ScrollLane lane)
        {
            var group = PlaceGroup(lane);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            Assert.IsTrue(collection.LoadedContainers.TryGetValue(group, out var loaded),
                $"{lane}: the spawned group must own a loaded container.");
            var owner = loaded as GLSGroupContainer;
            Assert.NotNull(owner);
            var ghosts = GetPreviewGhosts(owner);
            Assert.AreEqual(2, ghosts.Count,
                $"{lane}: two non-held nodes after the primary must produce two preview ghosts.");
            var hovered = ghosts.Single(
                preview => Mathf.Approximately(preview.PreviewEventData.RelativeJsonTime, 1f));
            InitializeLaneController(lane, hovered);

            PressVirtualKeys(Key.LeftCtrl, Key.LeftShift);

            // The fixture starts on Linear (cycle index 1); walk the whole editor order forward so
            // every easing id reaches the real serialized icon array through the ghost's icon view.
            for (var i = 2; i < EditorCycleOrder.Length; i++)
            {
                Scroll(1f);
                AssertPreviewNode(lane, hovered, EditorCycleOrder[i], $"forward[{i}]");
            }

            AssertPreviewNode(lane, hovered, EaseType.CatmullRomChordal, "forward tail");

            // Easing scrolls merge like a drag, so one undo restores the gesture's starting Linear
            // easing and sprite; redo lands back on Chordal through the same group replacement path.
            Assert.NotNull(actionContainer.Undo(), $"{lane}: the Chordal scroll must leave an undoable action.");
            AssertPreviewNode(lane, hovered, EaseType.Linear, "undo of the merged scroll gesture");
            Assert.NotNull(actionContainer.Redo(), $"{lane}: the undone easing scroll must redo.");
            AssertPreviewNode(lane, hovered, EaseType.CatmullRomChordal, "redo to Chordal");

            Scroll(1f);
            AssertPreviewNode(lane, hovered, EaseType.None, "wrap forward past Chordal");
            Scroll(1f);
            AssertPreviewNode(lane, hovered, EaseType.Linear, "wrap forward to Linear");

            Scroll(-1f);
            AssertPreviewNode(lane, hovered, EaseType.None, "backward from Linear");
            Scroll(-1f);
            AssertPreviewNode(lane, hovered, EaseType.CatmullRomChordal, "backward wrap to Chordal");
            Scroll(-1f);
            AssertPreviewNode(lane, hovered, EaseType.CatmullRomCentripetal, "backward to Centripetal");
            Scroll(-1f);
            AssertPreviewNode(lane, hovered, EaseType.CatmullRom, "backward to Uniform");
            Scroll(-1f);
            AssertPreviewNode(lane, hovered, EaseType.InOutExponential, "backward below Catmull-Rom");
        }

        // Queue a fresh scroll pulse and reset the axis so each call performs exactly one action.
        private void Scroll(float delta)
        {
            SetVirtualMouseState(Vector2.zero, new Vector2(0f, delta));
            SetVirtualMouseState(Vector2.zero, Vector2.zero);
        }

        private void InitializeLaneController(ScrollLane lane, GLSGroupContainer hovered)
        {
            InitializeVirtualInput(includeKeyboard: true);
            isolatedInput = new CMInput();
            controllerObject = new GameObject($"Catmull-Rom {lane} easing scroll controller");
            switch (lane)
            {
                case ScrollLane.Rotation:
                    isolatedInput.GLSRotationObjects.SetCallbacks(
                        CreateController<TestGLSGroupRotationScrollController>(hovered));
                    isolatedInput.GLSRotationObjects.Enable();
                    break;
                case ScrollLane.Translation:
                    isolatedInput.GLSTranslationObjects.SetCallbacks(
                        CreateController<TestGLSGroupTranslationScrollController>(hovered));
                    isolatedInput.GLSTranslationObjects.Enable();
                    break;
                case ScrollLane.FloatFx:
                    isolatedInput.GLSFloatFXObjects.SetCallbacks(
                        CreateController<TestGLSGroupFloatFXScrollController>(hovered));
                    isolatedInput.GLSFloatFXObjects.Enable();
                    break;
                default:
                    isolatedInput.GLSColorObjects.SetCallbacks(
                        CreateController<TestGLSGroupColorScrollController>(hovered));
                    isolatedInput.GLSColorObjects.Enable();
                    break;
            }
        }

        private TController CreateController<TController>(GLSGroupContainer hovered)
            where TController : BeatmapInputController<GLSGroupContainer>, IPreviewRaycastOverride, new()
        {
            var controller = controllerObject.AddComponent<TController>();
            controller.IsHovering = true;
            controller.HoveredObject = hovered;
            controller.RaycastTarget = hovered;
            return controller;
        }

        // Re-read the rebound preview node after every replacement so assertions never inspect a stale event.
        private static void AssertPreviewNode(
            ScrollLane lane,
            GLSGroupContainer hovered,
            EaseType expected,
            string step)
        {
            var evt = hovered.PreviewEventData;
            Assert.NotNull(evt, $"{lane} {step}: the hovered ghost lost its preview node.");
            Assert.That(evt.RelativeJsonTime, Is.EqualTo(1f).Within(0.0001f),
                $"{lane} {step}: the hovered ghost must keep representing the second node.");
            if (evt is BaseLightColorBase colorEvent)
            {
                // Ctrl+Shift authors colorEasing on transitions and the plain transition type below it,
                // so the effective curve is the override falling back to the interval easing.
                var expectedInt = (int)expected;
                if (expectedInt > (int)EaseType.Linear)
                {
                    Assert.AreEqual(expectedInt, colorEvent.ChromaColorEasing,
                        $"{lane} {step}: color scrolls must author ChromaColorEasing.");
                    Assert.AreEqual((int)EaseType.Linear, colorEvent.Easing,
                        $"{lane} {step}: the interval transition must stay Linear under a curve override.");
                }
                else
                {
                    Assert.AreEqual(expectedInt, colorEvent.Easing,
                        $"{lane} {step}: wrong authoritative transition.");
                    Assert.IsNull(colorEvent.ChromaColorEasing,
                        $"{lane} {step}: no curve override should remain below a real transition.");
                }
            }
            else
            {
                Assert.AreEqual((int)expected, evt.Easing, $"{lane} {step}: wrong authoritative easing.");
            }

            var state = GLSEventIconResolver.Resolve(evt);
            var renderers = hovered.GetComponentsInChildren<SpriteRenderer>(true);
            var top = renderers.Single(renderer => renderer.name == "Primary Icon Top");
            var side = renderers.Single(renderer => renderer.name == "Primary Icon Side");
            if (state.Primary == GLSEventIconType.None)
            {
                Assert.IsFalse(top.enabled, $"{lane} {step}");
                Assert.IsFalse(side.enabled, $"{lane} {step}");
                return;
            }

            Assert.IsTrue(top.enabled, $"{lane} {step}: the primary icon face must stay enabled.");
            Assert.AreEqual(state.Primary.ToString(), top.sprite.name, $"{lane} {step}: wrong icon sprite.");
            Assert.AreSame(top.sprite, side.sprite, $"{lane} {step}: both faces must share the sprite.");
        }

        // Spawn a three-node group through the real collection so every scroll edit exercises the
        // production clone, rebind, and preview-ghost configuration path.
        private BaseEventBoxGroup PlaceGroup(ScrollLane lane)
        {
            BaseEventBoxGroup group;
            switch (lane)
            {
                case ScrollLane.Rotation:
                    group = new BaseLightRotationEventBoxGroup
                    {
                        ID = TrackId,
                        JsonTime = 20f,
                        Boxes =
                        {
                            new BaseLightRotationEventBox
                            {
                                Axis = (int)Axis.Y,
                                IndexFilter = new BaseIndexFilter
                                {
                                    Type = (int)IndexFilterType.Division,
                                    Param0 = 1,
                                    Param1 = 0
                                },
                                Events = new[]
                                {
                                    new BaseLightRotationBase
                                    {
                                        RelativeJsonTime = 0f,
                                        Rotation = 15f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseLightRotationBase
                                    {
                                        RelativeJsonTime = 1f,
                                        Rotation = 30f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseLightRotationBase
                                    {
                                        RelativeJsonTime = 2f,
                                        Rotation = 45f,
                                        Easing = (int)EaseType.Linear
                                    }
                                }
                            }
                        }
                    };
                    break;
                case ScrollLane.Translation:
                    group = new BaseLightTranslationEventBoxGroup
                    {
                        ID = TrackId,
                        JsonTime = 20f,
                        Boxes =
                        {
                            new BaseLightTranslationEventBox
                            {
                                Axis = (int)Axis.X,
                                IndexFilter = new BaseIndexFilter
                                {
                                    Type = (int)IndexFilterType.Division,
                                    Param0 = 1,
                                    Param1 = 0
                                },
                                Events = new[]
                                {
                                    new BaseLightTranslationBase
                                    {
                                        RelativeJsonTime = 0f,
                                        Translation = 0.5f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseLightTranslationBase
                                    {
                                        RelativeJsonTime = 1f,
                                        Translation = 1f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseLightTranslationBase
                                    {
                                        RelativeJsonTime = 2f,
                                        Translation = 1.5f,
                                        Easing = (int)EaseType.Linear
                                    }
                                }
                            }
                        }
                    };
                    break;
                case ScrollLane.FloatFx:
                    group = new BaseVfxEventEventBoxGroup
                    {
                        ID = TrackId,
                        JsonTime = 20f,
                        Boxes =
                        {
                            new BaseVfxEventEventBox
                            {
                                IndexFilter = new BaseIndexFilter
                                {
                                    Type = (int)IndexFilterType.Division,
                                    Param0 = 1,
                                    Param1 = 0
                                },
                                Events = new[]
                                {
                                    new BaseFxEventFloat
                                    {
                                        RelativeJsonTime = 0f,
                                        Value = 0.5f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseFxEventFloat
                                    {
                                        RelativeJsonTime = 1f,
                                        Value = 1f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseFxEventFloat
                                    {
                                        RelativeJsonTime = 2f,
                                        Value = 1.5f,
                                        Easing = (int)EaseType.Linear
                                    }
                                }
                            }
                        }
                    };
                    break;
                default:
                    group = new BaseLightColorEventBoxGroup
                    {
                        ID = TrackId,
                        JsonTime = 20f,
                        Boxes =
                        {
                            new BaseLightColorEventBox
                            {
                                IndexFilter = new BaseIndexFilter
                                {
                                    Type = (int)IndexFilterType.Division,
                                    Param0 = 1,
                                    Param1 = 0
                                },
                                Events = new[]
                                {
                                    new BaseLightColorBase
                                    {
                                        RelativeJsonTime = 0f,
                                        Color = 0,
                                        Brightness = 1f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseLightColorBase
                                    {
                                        RelativeJsonTime = 1f,
                                        Color = 1,
                                        Brightness = 1f,
                                        Easing = (int)EaseType.Linear
                                    },
                                    new BaseLightColorBase
                                    {
                                        RelativeJsonTime = 2f,
                                        Color = 0,
                                        Brightness = 1f,
                                        Easing = (int)EaseType.Linear
                                    }
                                }
                            }
                        }
                    };
                    registeredGroupId = group.ID;
                    hadPreviousLightCount =
                        registeredLightCounts.TryGetValue(group.ID, out previousLightCount);
                    GLSEventCommon.SetColorTransitionLightCount(group.ID, 1);
                    break;
            }

            // The copy constructors bind box, index, and group ownership just like loaded map data.
            group = BeatmapFactory.Clone(group);
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);
            collection.RefreshPool();
            return group;
        }

        // Read the owner's maintained slot list instead of scanning the scene for ghost objects.
        private static List<GLSGroupContainer> GetPreviewGhosts(GLSGroupContainer owner)
        {
            var field = typeof(GLSGroupContainer).GetField(
                "previewGhosts", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (List<GLSGroupContainer>)field.GetValue(owner);
        }

        public enum ScrollLane
        {
            Rotation,
            Translation,
            FloatFx,
            Color
        }

        // Each lane's controller publishes the hovered ghost as the frame's raycast target so the
        // tests need no physical mouse position; everything past raycast stays production code.
        private interface IPreviewRaycastOverride
        {
            GLSGroupContainer RaycastTarget { get; set; }
        }

        private class TestGLSGroupRotationScrollController : BeatmapGLSGroupRotationInputController,
                                                             IPreviewRaycastOverride
        {
            public GLSGroupContainer RaycastTarget { get; set; }

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                BeatmapRaycastCache.FirstHit = firstObject != null ? firstObject.gameObject : null;
                BeatmapRaycastCache.HasHit = firstObject != null;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return firstObject != null;
            }
        }

        private class TestGLSGroupTranslationScrollController : BeatmapGLSGroupTranslationInputController,
                                                                IPreviewRaycastOverride
        {
            public GLSGroupContainer RaycastTarget { get; set; }

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                BeatmapRaycastCache.FirstHit = firstObject != null ? firstObject.gameObject : null;
                BeatmapRaycastCache.HasHit = firstObject != null;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return firstObject != null;
            }
        }

        private class TestGLSGroupFloatFXScrollController : BeatmapGLSGroupFloatFXInputController,
                                                            IPreviewRaycastOverride
        {
            public GLSGroupContainer RaycastTarget { get; set; }

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                BeatmapRaycastCache.FirstHit = firstObject != null ? firstObject.gameObject : null;
                BeatmapRaycastCache.HasHit = firstObject != null;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return firstObject != null;
            }
        }

        private class TestGLSGroupColorScrollController : BeatmapGLSGroupColorInputController,
                                                          IPreviewRaycastOverride
        {
            public GLSGroupContainer RaycastTarget { get; set; }

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                BeatmapRaycastCache.FirstHit = firstObject != null ? firstObject.gameObject : null;
                BeatmapRaycastCache.HasHit = firstObject != null;
                BeatmapRaycastCache.HasRaycastThisFrame = true;
                return firstObject != null;
            }
        }
    }
}
