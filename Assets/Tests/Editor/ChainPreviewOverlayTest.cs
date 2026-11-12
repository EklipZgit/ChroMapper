using System.Collections;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class ChainPreviewOverlayTest : PreviewWorkflowTestBase
    {
        private static readonly MethodInfo raycastMethod = typeof(BeatmapInputController<ChainContainer>)
            .GetMethod("RaycastFirstObject", BindingFlags.Instance | BindingFlags.NonPublic);

        private AudioTimeSyncController clock;
        private UIMode uiMode;
        private ChainGridContainer chains;
        private BaseChain chain;
        private bool previousNoteText;
        private bool previousClockEnabled;

        [SetUp]
        public void ConfigureChainOverlays()
        {
            previousNoteText = Settings.Instance.DisplayNoteText;
            Settings.Instance.DisplayNoteText = true;
            Settings.Instance.Animations = false;
            InitializeVirtualInput(includeKeyboard: false);
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            chains = Object.FindAnyObjectByType<ChainGridContainer>();
        }

        [UnityTest]
        public IEnumerator PreviewOverlaysRequirePausedHoverAndStayHiddenAfterRegeneration(
            [Values(UIModeType.Preview, UIModeType.Playing)] UIModeType mode,
            [Values(1, 5)] int sliceCount)
        {
            yield return PlaceChain(sliceCount);
            uiMode.SetUIMode(mode, false);
            var container = GetChain();
            AssertOverlays(container, false);
            container.Selected = true;
            AssertOverlays(container, false);
            container.Selected = false;

            var body = container.Nodes.Count > 0
                ? container.Nodes[0].gameObject
                : container.gameObject;
            HoverHit(body);
            AssertOverlays(container, true);
            var arrow = container.GetComponentsInChildren<ChainIndicatorContainer>()
                .First(indicator => indicator.gameObject.activeInHierarchy);
            HoverHit(arrow.gameObject);
            AssertOverlays(container, true);

            container.Highlighted = false;
            AssertOverlays(container, false);
            container.GenerateChain();
            AssertOverlays(container, false);
            container.Highlighted = true;
            AssertOverlays(container, true);

            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 0f);
            container = GetChain();
            container.Highlighted = true;
            AssertOverlays(container, false);
            container.GenerateChain();
            AssertOverlays(container, false);

            TestUtils.PauseDeterministicPlayback(clock);
            container = GetChain();
            container.Highlighted = false;
            AssertOverlays(container, false);
            container.Highlighted = true;
            AssertOverlays(container, true);
        }

        [UnityTest]
        public IEnumerator PoolRefreshClearsHoverBeforeReusingChainOverlays(
            [Values(UIModeType.Preview, UIModeType.Playing)] UIModeType mode)
        {
            yield return PlaceChain(5);
            uiMode.SetUIMode(mode, false);
            var container = GetChain();
            container.Highlighted = true;
            AssertOverlays(container, true);
            chains.RefreshPool(true);
            AssertOverlays(GetChain(), false);
        }

        [UnityTest]
        public IEnumerator PreviewHoverRespectsTextSettingAndNormalModeKeepsEditingControls(
            [Values(UIModeType.Preview, UIModeType.Playing)] UIModeType mode)
        {
            yield return PlaceChain(5);
            AssertOverlays(GetChain(), true);
            Settings.Instance.DisplayNoteText = false;
            uiMode.SetUIMode(mode, false);
            var container = GetChain();
            container.Highlighted = true;
            AssertOverlays(container, true, false);
            container.Highlighted = false;
            AssertOverlays(container, false, false);
            uiMode.SetUIMode(UIModeType.Normal, false);
            AssertOverlays(GetChain(), true, false);
        }

        private IEnumerator PlaceChain(int sliceCount)
        {
            chain = PlaceUtils.Place(
                new BaseChain
                {
                    JsonTime = 2f, TailJsonTime = 3f, TailPosX = 1, SliceCount = sliceCount, Squish = 1f
                });
            yield return null;
        }

        private ChainContainer GetChain()
        {
            Assert.That(chains.LoadedContainers.TryGetValue(chain, out var container), Is.True);
            return (ChainContainer)container;
        }

        private void HoverHit(GameObject hit)
        {
            BeatmapRaycastCache.FirstHit = hit;
            BeatmapRaycastCache.HasHit = true;
            BeatmapRaycastCache.HasRaycastThisFrame = true;
            var arguments = new object[] { null };
            var input = Object.FindAnyObjectByType<BeatmapChainInputController>();
            Assert.That((bool)raycastMethod.Invoke(input, arguments), Is.True);
            Assert.That(arguments[0], Is.SameAs(GetChain()));
            ((ChainContainer)arguments[0]).Highlighted = true;
        }

        private static void AssertOverlays(ChainContainer container, bool visible, bool textEnabled = true)
        {
            Assert.That(
                container.InfoText.gameObject.activeInHierarchy,
                Is.EqualTo(visible && textEnabled),
                "Chain text visibility does not match paused hover.");
            var indicators = container.GetComponentsInChildren<ChainIndicatorContainer>(true);
            var expectedCount = visible
                ? 2
                : 0;
            Assert.That(
                indicators.Count(indicator => indicator.gameObject.activeInHierarchy),
                Is.EqualTo(expectedCount),
                "Chain tweak arrow visibility does not match paused hover.");
        }

        [UnityTearDown]
        public IEnumerator RestoreChainOverlays()
        {
            BeatmapRaycastCache.Invalidate();
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            var cameras = Object.FindAnyObjectByType<CameraManager>();
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.DisplayNoteText = previousNoteText;
            TearDownVirtualInput();
            yield break;
        }
    }
}
