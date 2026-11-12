using System.Collections;
using System.Linq;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class BoxSelectionGroundLaneTest : TestBase
    {
        private BoxSelectionPlacement boxSelection;
        private PlacementProvider eventProvider;
        private EventGridContainer events;
        private EditModeContext editMode;
        private UIMode uiMode;
        private bool previousAnimations;
        private bool previousBoxSelect;

        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(3, null, environmentName: "KaleidoscopeEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [SetUp]
        public void InitializeSelection()
        {
            previousAnimations = Settings.Instance.Animations;
            previousBoxSelect = Settings.Instance.BoxSelect;
            Settings.Instance.BoxSelect = true;
            boxSelection = Object.FindAnyObjectByType<BoxSelectionPlacement>();
            events = Object.FindAnyObjectByType<EventGridContainer>();
            editMode = Object.FindAnyObjectByType<EditModeContext>();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            eventProvider = Object.FindObjectsByType<PlacementProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(provider => provider.Placements.Any(placement => placement is EventPlacement));
            events.PropagationEditing = EventGridContainer.PropMode.Off;
            boxSelection.Initialize(eventProvider);
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            uiMode.SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.BoxSelect = previousBoxSelect;
            boxSelection.Cancel();
            boxSelection.State = PlacementState.Idle;
            boxSelection.IsGroundHit = false;
            events.PropagationEditing = EventGridContainer.PropMode.Off;
            yield break;
        }

        [UnityTest]
        public IEnumerator PlayingRoundTripKeepsBasicEventGroundEndpointsOnActualLanes()
        {
            yield return null;
            uiMode.SetUIMode(UIModeType.Playing, false);
            yield return null;
            uiMode.SetUIMode(UIModeType.Normal, false);
            yield return null;

            AssertGroundEndpoints();
        }

        [UnityTest]
        public IEnumerator LightIdRoundTripKeepsGroundEndpointsOnActualLanes()
        {
            yield return null;
            events.EventTypeToPropagate = 0;
            events.PropagationEditing = EventGridContainer.PropMode.Light;
            yield return null;
            AssertGroundEndpoints();

            events.PropagationEditing = EventGridContainer.PropMode.Off;
            yield return null;
            AssertGroundEndpoints();
        }

        [UnityTest]
        public IEnumerator EmptyGlsViewCannotPreviewOrStartGroundSelection()
        {
            yield return null;
            events.PropagationEditing = EventGridContainer.PropMode.Light;
            HoverGround(0f);
            Assert.That(boxSelection.PlacementVisualContainer.gameObject.activeSelf, Is.True);
            editMode.EditingMode = EditingMode.GLS;
            yield return null;
            Assert.That(Object.FindAnyObjectByType<GLSGroupGridProvider>().ActiveGlsTracks, Is.Empty);

            HoverGround(0f);
            Assert.That(
                boxSelection.PlacementVisualContainer.gameObject.activeSelf,
                Is.False,
                "An empty GLS view must not retain Basic Event ground selection lanes.");
            boxSelection.Apply();
            Assert.That(boxSelection.IsPlacing, Is.False);
        }

        private void AssertGroundEndpoints()
        {
            var lane = eventProvider.Lane;
            var expectedLeft = boxSelection.PlacementTrack.InverseTransformPoint(lane.transform.position).x;
            var expectedRight = boxSelection.PlacementTrack.InverseTransformPoint(
                lane.transform.TransformPoint(Vector3.right * lane.Lane)).x - 1f;

            HoverGround(-100f);
            Assert.That(
                boxSelection.PlacementVisualContainer.transform.localPosition.x,
                Is.EqualTo(expectedLeft).Within(0.0001f),
                "The leftmost lane must be selectable without a phantom lane.");
            boxSelection.HandleApply();
            HoverGround(100f, reset: false);
            Assert.That(
                boxSelection.PlacementVisualContainer.transform.localPosition.x
                + boxSelection.PlacementVisualContainer.transform.localScale.x,
                Is.EqualTo(expectedRight + 1f).Within(0.0001f),
                "The drag must end at the actual right edge.");
            boxSelection.HandleApply();

            HoverGround(100f);
            Assert.That(
                boxSelection.PlacementVisualContainer.transform.localPosition.x,
                Is.EqualTo(expectedRight).Within(0.0001f),
                "The rightmost lane must be selectable without a phantom lane.");
            boxSelection.HandleApply();
            HoverGround(-100f, reset: false);
            Assert.That(
                boxSelection.PlacementVisualContainer.transform.localPosition.x,
                Is.EqualTo(expectedLeft).Within(0.0001f),
                "A reverse drag must reach the leftmost lane.");
            boxSelection.HandleApply();
        }

        private void HoverGround(float laneX, bool reset = true)
        {
            if (reset)
            {
                boxSelection.State = PlacementState.Active;
                boxSelection.Exit();
            }

            boxSelection.IsGroundHit = true;
            var point = boxSelection.PlacementTrack.TransformPoint(new Vector3(laneX, 0f, 4f));
            var hit = new Intersections.IntersectionHit(
                eventProvider.Lane.XZ.gameObject,
                new Bounds(Vector3.zero, Vector3.one),
                new Ray(point, Vector3.forward),
                0f);
            boxSelection.UpdateState(hit, PlacementInputState.Hover);
        }
    }
}
