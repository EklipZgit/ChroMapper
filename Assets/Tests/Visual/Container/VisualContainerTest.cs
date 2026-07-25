using System.Collections;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    public class VisualContainerTest : TestBase
    {
        [UnityTest]
        public IEnumerator UpdatesPositionWhenPreviewChangesEditorScale()
        {
            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var rotationEventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
                    ObjectType.RotationEvent);
            var bpmEventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);

            var uiMode = Object.FindAnyObjectByType<UIMode>();

            var beatmapEvent = PlaceUtils.Place(
                new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.Event0, Value = (int)LightValue.RedFade });

            var rotationEvent = PlaceUtils.Place(
                new BaseRotationEvent { JsonTime = 4, Type = (int)EventTypeValue.LateRotationEventType, Rotation = 90 });

            var bpmEvent = PlaceUtils.Place(new BaseBpmEvent { JsonTime = 5, Bpm = 120f });

            uiMode.SetUIMode(UIModeType.Preview, false);
            yield return null;

            try
            {
                var eventContainer = eventsContainer.LoadedContainers[beatmapEvent] as EventContainer;
                Assert.AreEqual(
                    beatmapEvent.SongBpmTime * EditorScaleController.EditorScale,
                    eventContainer.transform.localPosition.z,
                    0.001f);

                var rotationContainer =
                    rotationEventsContainer.LoadedContainers[rotationEvent] as RotationEventContainer;
                Assert.AreEqual(
                    rotationEvent.SongBpmTime * EditorScaleController.EditorScale,
                    rotationContainer.transform.localPosition.z,
                    0.001f);

                var bpmContainer = bpmEventsContainer.LoadedContainers[bpmEvent] as BpmEventContainer;
                Assert.AreEqual(
                    bpmEvent.SongBpmTime * EditorScaleController.EditorScale,
                    bpmContainer.transform.localPosition.z,
                    0.001f);
            }
            finally
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            yield return null;
        }
    }
}
