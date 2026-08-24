using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class BasicEventTransitionRibbonTest : TestBase
    {
        private bool? visualizeGradientsBeforeTest;
        private int? chunkDistanceBeforeTest;

        protected override EditingMode InitialEditingMode => EditingMode.BasicEvent;

        protected override void AfterCleanup()
        {
            if (visualizeGradientsBeforeTest.HasValue)
            {
                // Restore the user's ribbon visibility after tests require transition gradients to be rendered.
                Settings.Instance.VisualizeChromaGradients = visualizeGradientsBeforeTest.Value;
                visualizeGradientsBeforeTest = null;
            }

            if (chunkDistanceBeforeTest.HasValue)
            {
                // Restore the normal chunk radius after wide ribbons deliberately unload both endpoint nodes.
                Settings.Instance.ChunkDistance = chunkDistanceBeforeTest.Value;
                chunkDistanceBeforeTest = null;
                GetEventsContainer().RefreshPool(true);
            }
        }

        [UnityTest]
        public IEnumerator WideTransitionRibbonRemainsLoadedWhenScrubbingForwardIntoMiddle()
        {
            PrepareRibbonAppearance();
            var source = PlaceLightEvent(2f, LightValue.RedOn);
            var transition = PlaceLightEvent(80f, LightValue.BlueTransition);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Load only the source visual before forward scrubbing tests spanning-ribbon retention.
            yield return SetViewAndRefreshVisualWindow(2f, 1.5f, 2.5f, 2f);
            AssertVisibleRibbon(source, transition, "before scrubbing forward");

            yield return ScrubAcrossChunkBoundary(30f, 40f);

            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(source, "the forward-scrub source");
            AssertOutsideOrdinaryChunkWindow(transition, "the forward-scrub target");
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(transition),
                Is.False,
                "The far transition target unexpectedly remained in the ordinary loaded chunk window.");
            AssertVisibleRibbon(source, transition, "after scrubbing forward into the middle");
        }

        [UnityTest]
        public IEnumerator WideTransitionRibbonRemainsLoadedWhenScrubbingBackwardIntoMiddle()
        {
            PrepareRibbonAppearance();
            var source = PlaceLightEvent(2f, LightValue.RedOn);
            var transition = PlaceLightEvent(60f, LightValue.BlueTransition);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Establish a precise unloaded starting state before backward scrubbing tests spanning-ribbon loading.
            yield return SetViewAndRefreshVisualWindow(85f, 84.5f, 85.5f);

            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(source, "the initially unloaded backward-scrub source");
            AssertOutsideOrdinaryChunkWindow(transition, "the initially unloaded backward-scrub target");
            AssertContainerUnloaded(source, "the backward-scrub source");
            AssertContainerUnloaded(transition, "the backward-scrub target");

            yield return ScrubAcrossChunkBoundary(40f, 30f);

            AssertOutsideOrdinaryChunkWindow(source, "the backward-scrub source");
            AssertOutsideOrdinaryChunkWindow(transition, "the backward-scrub target");
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(transition),
                Is.False,
                "The far transition target unexpectedly entered the ordinary loaded chunk window.");
            AssertVisibleRibbon(source, transition, "after scrubbing backward into the middle");
        }

        [UnityTest]
        public IEnumerator PlacingTransitionWithUnloadedPreviousNodeCreatesWideRibbon()
        {
            PrepareRibbonAppearance();
            var source = PlaceLightEvent(2f, LightValue.RedOn);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Unload the source deterministically before the real placement path inserts the transition at the current view.
            yield return SetViewAndRefreshVisualWindow(60f, 59.5f, 60.5f);
            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(source, "the previous node before transition placement");
            AssertContainerUnloaded(source, "the previous node before placing the distant transition");

            var transition = PlaceLightEvent(60f, LightValue.BlueTransition);
            yield return null;

            AssertOutsideOrdinaryChunkWindow(source, "the previous node at transition placement");
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(transition), Is.True);
            Assert.That(source.Next, Is.SameAs(transition));
            Assert.That(transition.Prev, Is.SameAs(source));
            AssertVisibleRibbon(source, transition, "after placing a transition ahead of an unloaded source");
        }

        [UnityTest]
        public IEnumerator PlacingOnNodeBeforeUnloadedTransitionReplacesWideRibbonSource()
        {
            PrepareRibbonAppearance();
            var oldSource = PlaceLightEvent(38f, LightValue.RedOn);
            var transition = PlaceLightEvent(80f, LightValue.BlueTransition);
            PlaceVisualRangeGuardEvents();
            UseNarrowVisualChunkWindow();

            // Load only the old source so the test begins with a visible ribbon whose target is outside loaded chunks.
            // The unrelated beat-20 guard is outside the bounded window and must now unload with correct search boundaries.
            yield return SetViewAndRefreshVisualWindow(40f, 37.5f, 40.5f, 38f);
            var eventsContainer = GetEventsContainer();
            AssertOutsideOrdinaryChunkWindow(transition, "the transition target before source replacement");
            Assert.That(eventsContainer.LoadedContainers.ContainsKey(transition), Is.False);
            AssertVisibleRibbon(oldSource, transition, "before inserting the replacement source");

            var newSource = PlaceLightEvent(40f, LightValue.BlueOn);
            yield return null;

            Assert.That(oldSource.Next, Is.SameAs(newSource));
            Assert.That(newSource.Prev, Is.SameAs(oldSource));
            Assert.That(newSource.Next, Is.SameAs(transition));
            Assert.That(transition.Prev, Is.SameAs(newSource));
            AssertHiddenRibbonIfLoaded(oldSource, "the superseded source kept its old wide ribbon");
            AssertVisibleRibbon(newSource, transition, "after inserting a new source before the unloaded transition");
        }

        private void PrepareRibbonAppearance()
        {
            // Enable ribbon rendering before placement so the source container builds the same appearance as the editor.
            visualizeGradientsBeforeTest ??= Settings.Instance.VisualizeChromaGradients;
            chunkDistanceBeforeTest ??= Settings.Instance.ChunkDistance;
            Settings.Instance.VisualizeChromaGradients = true;
            // Keep every placed event on the ordinary fixed Basic Event lane used by the ribbon scenarios.
            GetEventsContainer().PropagationEditing = EventGridContainer.PropMode.Off;
        }

        private static void UseNarrowVisualChunkWindow()
        {
            // Match existing unloaded-visual tests by narrowing chunks only after placement and immediately before scrubbing.
            Settings.Instance.ChunkDistance = 2;
        }

        private static BaseEvent PlaceLightEvent(float jsonTime, LightValue value) =>
            // Exercise EventPlacement.HandleApply through the shared placement helper instead of inserting map data directly.
            PlaceUtils.Place(CreateLightEvent(jsonTime, value, EventTypeValue.Event2));

        private static void PlaceVisualRangeGuardEvents()
        {
            // Bracket wide ribbon windows around unrelated-lane events so BinarySearchBy cannot return a tested endpoint.
            PlaceUtils.Place(CreateLightEvent(20f, LightValue.RedOn, EventTypeValue.Event3));
            PlaceUtils.Place(CreateLightEvent(95f, LightValue.RedOn, EventTypeValue.Event3));
        }

        private static BaseEvent CreateLightEvent(
            float jsonTime,
            LightValue value,
            EventTypeValue eventType) =>
            // Create full-intensity Basic Events while letting the range guard use an unrelated light lane.
            new()
            {
                JsonTime = jsonTime,
                Type = (int)eventType,
                Value = (int)value,
                FloatValue = 1f
            };

        private static EventGridContainer GetEventsContainer() =>
            BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

        private static IEnumerator SetViewAndRefreshVisualWindow(
            float viewJsonTime,
            float lowerJsonTime,
            float upperJsonTime,
            params float[] expectedLoadedJsonTimes)
        {
            // Trace the bounded refresh lifecycle so stale dictionary, ordered-list, and delayed-reload states are distinguishable.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var map = BeatSaberSongContainer.Instance.Map;
            var eventsContainer = GetEventsContainer();
            atsc.MoveToJsonTime(viewJsonTime);
            var lowerBound = (float)map.JsonTimeToSongBpmTime(lowerJsonTime);
            var upperBound = (float)map.JsonTimeToSongBpmTime(upperJsonTime);
            Debug.Log(
                $"[RibbonPoolDebug] before bounded refresh bounds={lowerBound}/{upperBound}; "
                + DescribeVisualPool(eventsContainer));
            eventsContainer.RefreshPool(lowerBound, upperBound, true);
            Debug.Log($"[RibbonPoolDebug] immediately after bounded refresh; {DescribeVisualPool(eventsContainer)}");
            var immediatelyLoadedJsonTimes = GetLoadedJsonTimes(eventsContainer);
            yield return null;
            Debug.Log($"[RibbonPoolDebug] one frame after bounded refresh; {DescribeVisualPool(eventsContainer)}");
            var loadedJsonTimesAfterFrame = GetLoadedJsonTimes(eventsContainer);
            Assert.That(
                immediatelyLoadedJsonTimes,
                Is.EquivalentTo(expectedLoadedJsonTimes),
                "The bounded setup refresh produced the wrong immediate visual pool.");
            Assert.That(
                loadedJsonTimesAfterFrame,
                Is.EquivalentTo(expectedLoadedJsonTimes),
                "The visual pool changed one frame after the bounded setup refresh.");
        }

        private static float[] GetLoadedJsonTimes(EventGridContainer eventsContainer) =>
            eventsContainer.LoadedContainers.Keys.OfType<BaseEvent>().Select(evt => evt.JsonTime).ToArray();

        private static string DescribeVisualPool(EventGridContainer eventsContainer)
        {
            // Include both authoritative visual indexes and the private chunk cursor that decides whether LateUpdate refreshes.
            var collectionType = typeof(BeatmapObjectContainerCollection);
            var previousAtscBeat = collectionType
                .GetField("previousAtscBeat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(eventsContainer);
            var previousChunk = collectionType
                .GetField("previousChunk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(eventsContainer);
            var dictionaryBeats = string.Join(
                ", ",
                eventsContainer.LoadedContainers.Keys
                    .OfType<BaseEvent>()
                    .Select(evt => $"{evt.JsonTime}/{evt.SongBpmTime}"));
            var orderedBeats = string.Join(
                ", ",
                eventsContainer.ObjectsWithContainers
                    .OfType<BaseEvent>()
                    .Select(evt => $"{evt.JsonTime}/{evt.SongBpmTime}"));
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            return $"enabled/activeSelf/activeInHierarchy/isActiveAndEnabled="
                + $"{eventsContainer.enabled}/{eventsContainer.gameObject.activeSelf}/"
                + $"{eventsContainer.gameObject.activeInHierarchy}/{eventsContainer.isActiveAndEnabled}; "
                + $"visualize={Settings.Instance.VisualizeChromaGradients}; "
                + $"intervals/query={GetPrivateCollectionCount(eventsContainer, "transitionRibbonIntervals", "intervalsBySource")}/"
                + $"{GetPrivateCollectionCount(eventsContainer, "visibleTransitionRibbonSources")}; "
                + $"playhead={atsc.CurrentJsonTime}/{atsc.CurrentSongBpmTime}; "
                + $"previous={previousAtscBeat}/{previousChunk}; "
                + $"dictionary({eventsContainer.LoadedContainers.Count})=[{dictionaryBeats}]; "
                + $"ordered({eventsContainer.ObjectsWithContainers.Count})=[{orderedBeats}]";
        }

        private static int GetPrivateCollectionCount(
            object owner,
            string fieldName,
            string nestedFieldName = null)
        {
            // Reflect diagnostic-only cache sizes so failed end-to-end tests expose whether mutation or viewport lookup broke.
            var value = owner.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(owner);
            if (value != null && nestedFieldName != null)
            {
                value = value.GetType()
                    .GetField(
                        nestedFieldName,
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(value);
            }

            var count = value?.GetType().GetProperty("Count")?.GetValue(value);
            return count is int collectionCount ? collectionCount : -1;
        }

        private static IEnumerator ScrubAcrossChunkBoundary(float stagingJsonTime, float targetJsonTime)
        {
            // Match the existing unloaded-visual tests by yielding once after each public playhead move.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(stagingJsonTime);
            yield return null;
            atsc.MoveToJsonTime(targetJsonTime);
            yield return null;
            Assert.That(
                Settings.Instance.ChunkDistance,
                Is.EqualTo(2),
                "The test's five-beat visual chunk radius was overwritten while scrubbing.");
        }

        private static void AssertOutsideOrdinaryChunkWindow(BaseEvent endpoint, string description)
        {
            // Prove endpoint retention comes from the spanning ribbon rather than the ordinary five-beat node window.
            var currentSongBpmTime = Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentSongBpmTime;
            Assert.That(
                Mathf.Abs(endpoint.SongBpmTime - currentSongBpmTime),
                Is.GreaterThan(BeatmapObjectContainerCollection.ChunkSize),
                $"Expected {description} to be outside the ordinary loaded chunk window.");
        }

        private static void AssertContainerUnloaded(BaseEvent endpoint, string description)
        {
            // Report the complete pool and linkage state when an endpoint fails to leave the visual chunk window.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var eventsContainer = GetEventsContainer();
            var loadedBeats = string.Join(
                ", ",
                eventsContainer.LoadedContainers.Keys
                    .OfType<BaseEvent>()
                    .Select(evt => $"{evt.JsonTime}/{evt.SongBpmTime}"));
            Assert.That(
                eventsContainer.LoadedContainers.ContainsKey(endpoint),
                Is.False,
                $"Expected {description} to be visually unloaded. "
                + $"Playhead json/song={atsc.CurrentJsonTime}/{atsc.CurrentSongBpmTime}; "
                + $"endpoint json/song={endpoint.JsonTime}/{endpoint.SongBpmTime}; "
                + $"chunk distance={Settings.Instance.ChunkDistance}; "
                + $"attached={endpoint.HasAttachedContainer}; collection enabled={eventsContainer.enabled}; "
                + $"previous={endpoint.Prev?.JsonTime.ToString() ?? "null"}; "
                + $"next={endpoint.Next?.JsonTime.ToString() ?? "null"}; loaded json/song beats=[{loadedBeats}].");
        }

        private static void AssertVisibleRibbon(BaseEvent source, BaseEvent transition, string operation)
        {
            var eventsContainer = GetEventsContainer();
            Assert.That(
                eventsContainer.LoadedContainers.TryGetValue(source, out var objectContainer),
                Is.True,
                $"The ribbon source at beat {source.JsonTime} was not loaded {operation}. "
                + DescribeVisualPool(eventsContainer));
            Assert.That(objectContainer, Is.TypeOf<EventContainer>());

            var ribbon = objectContainer.GetComponentInChildren<LightGradientController>(true);
            Assert.That(ribbon, Is.Not.Null, $"The loaded source had no ribbon controller {operation}.");
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null, $"The loaded source had no ribbon renderer {operation}.");
            Assert.That(ribbon.gameObject.activeInHierarchy, Is.True, $"The ribbon object was hidden {operation}.");
            Assert.That(renderer.enabled, Is.True, $"The ribbon renderer was disabled {operation}.");

            var expectedLength = (transition.SongBpmTime - source.SongBpmTime)
                * EditorScaleController.EditorScale
                * (4f / 3f);
            Assert.That(
                ribbon.transform.localScale.x,
                Is.EqualTo(expectedLength).Within(0.001f),
                $"The ribbon did not span from beat {source.JsonTime} to beat {transition.JsonTime} {operation}.");
        }

        private static void AssertHiddenRibbonIfLoaded(BaseEvent source, string message)
        {
            var eventsContainer = GetEventsContainer();
            if (!eventsContainer.LoadedContainers.TryGetValue(source, out var objectContainer))
            {
                return;
            }

            // A retained old source is valid only if its former transition ribbon has been hidden.
            var ribbon = objectContainer.GetComponentInChildren<LightGradientController>(true);
            var renderer = ribbon.GetComponentInChildren<MeshRenderer>(true);
            Assert.That(ribbon.gameObject.activeInHierarchy && renderer.enabled, Is.False, message);
        }
    }
}
