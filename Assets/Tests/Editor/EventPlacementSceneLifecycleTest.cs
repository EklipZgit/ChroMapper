using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Info;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class EventPlacementSceneLifecycleTest : TestBase
    {
        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        private readonly List<GameObject> hoverObjects = new();
        private FloatValueController floatValueControllerBeforeExit;

        [UnityTest]
        public IEnumerator PausedHoverPreviewLeavesNoOrphanEventInNextMap(
            [Values(null, "BillieEnvironment")] string environmentName)
        {
            yield return null;

            var placement = Object.FindAnyObjectByType<EventPlacement>();
            Assert.That(placement != null, Is.True);
            placement.Exit();
            var provider = FindPlacementProvider(placement);
            Assert.That(provider != null, Is.True);
            placement.Initialize(provider);

            HoverBasicEventLane(placement, 4f, (int)EventTypeValue.Event1);
            Assert.That(placement.IsActive, Is.True);
            var preview = placement.PlacementVisualContainer;
            Assert.That(preview != null && preview.IsPlacementVisual, Is.True);
            Assert.That(preview.gameObject.activeSelf, Is.True);

            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            Assert.That(pauseManager != null, Is.True);
            floatValueControllerBeforeExit = Object.FindAnyObjectByType<FloatValueController>();
            pauseManager.TogglePause();
            Assert.That(PauseManager.IsPaused, Is.True);
            yield return null;
            Assert.That(placement.IsActive, Is.True);
            Assert.That(preview.gameObject.activeSelf, Is.True);

            var captureInfo = new BaseInfo();
            EditorStateService.CaptureMapData(captureInfo);
            var editorState = captureInfo.CustomEditorsData.GetEditorData("editorState")?.AsObject;
            Assert.That(editorState, Is.Not.Null);
            var componentStates = editorState["components"]?.AsObject;
            Assert.That(componentStates != null && componentStates.HasKey("basicEventFloatInput"), Is.True);

            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("02") && !SceneTransitionManager.IsLoading);
            SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);

            yield return TestUtils.ReloadMap(
                3,
                null,
                editorState: editorState,
                environmentName: environmentName);

            Assert.That(preview == null, Is.True);
            var orphanVisuals = GetOrphanPlacementVisuals();
            Assert.That(
                orphanVisuals,
                Is.Empty,
                "Placement preview leaked into the reloaded map: "
                + string.Join("; ", orphanVisuals.Select(DescribeContainer)));

            var newPlacement = Object.FindAnyObjectByType<EventPlacement>();
            Assert.That(newPlacement != null, Is.True);
            var newProvider = FindPlacementProvider(newPlacement);
            Assert.That(newProvider != null, Is.True);
            newPlacement.Initialize(newProvider);
            HoverBasicEventLane(newPlacement, 6f, (int)EventTypeValue.Event1);
            Assert.That(newPlacement.IsActive, Is.True);
            var newPreview = newPlacement.PlacementVisualContainer;
            Assert.That(newPreview != null && newPreview.IsPlacementVisual, Is.True);
            Assert.That(newPreview.gameObject.activeSelf, Is.True);
            newPlacement.Exit();
            Assert.That(newPreview.gameObject.activeSelf, Is.False);
        }

        private void HoverBasicEventLane(EventPlacement placement, float jsonTime, int eventType)
        {
            var eventsContainer = Object.FindAnyObjectByType<EventGridContainer>(FindObjectsInactive.Include);
            Assert.That(eventsContainer != null, Is.True);
            eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;

            var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
            Assert.That(labels != null, Is.True);
            var lane = labels.EventTypeToLaneId(eventType);
            Assert.That(lane, Is.GreaterThanOrEqualTo(0));

            placement.Bounds = new Bounds(
                new Vector3(labels.LaneCount / 2f, 0.5f, 0f),
                new Vector3(labels.LaneCount, 1f, 1f));

            var songBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
            var localPoint = new Vector3(lane + 0.5f, 0f, songBpmTime * EditorScaleController.EditorScale);
            var worldPoint = placement.PlacementTrack.TransformPoint(localPoint);

            var hoverRoot = new GameObject("EventPlacementSceneLifecycleTest hover surface");
            hoverObjects.Add(hoverRoot);
            var hoverHitObject = new GameObject("EventPlacementSceneLifecycleTest hover hit");
            hoverHitObject.transform.SetParent(hoverRoot.transform);

            placement.UpdateState(
                new Intersections.IntersectionHit(
                    hoverHitObject,
                    new Bounds(Vector3.zero, Vector3.one),
                    new Ray(worldPoint, Vector3.forward),
                    0f),
                PlacementInputState.Hover);
        }

        private static PlacementProvider FindPlacementProvider(BasePlacement placement)
        {
            foreach (var provider in Object.FindObjectsByType<PlacementProvider>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (provider.Placements != null && provider.Placements.Contains(placement))
                    return provider;
            }

            return null;
        }

        private static EventContainer[] GetOrphanPlacementVisuals()
        {
            var owned = new HashSet<EventContainer>();
            foreach (var livePlacement in Object.FindObjectsByType<EventPlacement>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (livePlacement.PlacementVisualContainer != null)
                    owned.Add(livePlacement.PlacementVisualContainer);
            }

            return Object.FindObjectsByType<EventContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(container => container.IsPlacementVisual && !owned.Contains(container))
                .ToArray();
        }

        private static string DescribeContainer(EventContainer container)
        {
            var parent = container.transform.parent;
            var data = container.EventData;
            return $"'{container.name}' scene='{container.gameObject.scene.name}' "
                + $"parent='{(parent != null ? parent.name : "ROOT")}' "
                + $"active={container.gameObject.activeSelf} "
                + $"data={(data != null ? $"type={data.Type} json={data.JsonTime}" : "null")}";
        }

        [UnityTearDown]
        public IEnumerator RestoreMapperAfterSceneLifecycleTest()
        {
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);

            if (PauseManager.IsPaused)
            {
                var pauseManager = Object.FindAnyObjectByType<PauseManager>();
                if (pauseManager != null)
                    pauseManager.TogglePause();
                else
                    PauseManager.IsPaused = false;
            }

            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
                var cameras = Object.FindAnyObjectByType<CameraManager>();
                if (cameras != null)
                    cameras.SelectCamera(CameraType.Editing);
            }

            yield return TestUtils.ReloadMap(3, null);

            foreach (var container in GetOrphanPlacementVisuals())
                Object.DestroyImmediate(container.gameObject);

            EditorStateService.Unregister(floatValueControllerBeforeExit);
            floatValueControllerBeforeExit = null;

            var livePlacement = Object.FindAnyObjectByType<EventPlacement>();
            if (livePlacement != null && !livePlacement.IsIdle)
                livePlacement.Exit();

            foreach (var hoverObject in hoverObjects)
                if (hoverObject != null)
                    Object.DestroyImmediate(hoverObject);

            hoverObjects.Clear();
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
