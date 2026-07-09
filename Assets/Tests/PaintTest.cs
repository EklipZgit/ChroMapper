using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class PaintTest : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            Settings.Instance.MapVersion = 3;
        }

        [Test]
        public void PaintGradientUndo()
        {
            Settings.Instance.MapVersion = 2;

            var colorPicker = Object.FindAnyObjectByType<ColorPicker>();
            var painter = Object.FindAnyObjectByType<PaintSelectedObjects>();

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            Object.FindAnyObjectByType<EventPlacement>();

            var customData = new JSONObject();
            customData["_lightGradient"] = new ChromaLightGradient(Color.blue, Color.cyan).ToJson();
            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = 1,
                Value = 1,
                FloatValue = 1,
                CustomData = customData
            };
            eventA = PlaceUtils.Place(eventA);

            SelectionController.Select(eventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            var shiftedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, shiftedEvent.JsonTime);
            Assert.AreEqual(2, shiftedEvent.Type);
            Assert.AreEqual(Color.red, shiftedEvent.CustomLightGradient.StartColor);

            // Undo move
            var undoMove = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, undoMove[0].JsonTime);
            Assert.AreEqual(1, undoMove[0].Type);
            Assert.AreEqual(Color.red, undoMove[0].CustomLightGradient.StartColor);

            // Undo paint
            var undoPaint = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, undoPaint[0].JsonTime);
            Assert.AreEqual(1, undoPaint[0].Type);
            Assert.AreEqual(Color.blue, undoPaint[0].CustomLightGradient.StartColor);
        }

        [Test]
        public void PaintUndo()
        {
            var colorPicker = Object.FindAnyObjectByType<ColorPicker>();
            var painter = Object.FindAnyObjectByType<PaintSelectedObjects>();

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            Object.FindAnyObjectByType<EventPlacement>();

            var eventA = new BaseEvent { JsonTime = 2, Type = 1, Value = 1 };
            eventA = PlaceUtils.Place(eventA);

            SelectionController.Select(eventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            var shiftedEvent = SelectionController.SelectedObjects.OfType<BaseEvent>().Single();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, shiftedEvent.JsonTime);
            Assert.AreEqual(2, shiftedEvent.Type);
            Assert.AreEqual(Color.red, shiftedEvent.CustomData[shiftedEvent.CustomKeyColor].ReadColor());

            // Undo move
            var undoMove = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, undoMove[0].JsonTime);
            Assert.AreEqual(1, undoMove[0].Type);
            Assert.AreEqual(Color.red, undoMove[0].CustomData[undoMove[0].CustomKeyColor].ReadColor());

            // Undo paint
            var undoPaint = PlaceUtils.Undo<BaseEvent>().ToList();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, undoPaint[0].JsonTime);
            Assert.AreEqual(1, undoPaint[0].Type);
            Assert.AreEqual(
                true,
                undoPaint[0].CustomData == null || !undoPaint[0].CustomData.HasKey(undoPaint[0].CustomKeyColor));
        }

        [Test]
        public void IgnoresOff()
        {
            var colorPicker = Object.FindAnyObjectByType<ColorPicker>();
            var painter = Object.FindAnyObjectByType<PaintSelectedObjects>();

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            Object.FindAnyObjectByType<EventPlacement>();

            var eventA = new BaseEvent { JsonTime = 2, Type = 1, Value = 0 };
            eventA = PlaceUtils.Place(eventA);

            SelectionController.Select(eventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, eventA.JsonTime);
            Assert.AreEqual(1, eventA.Type);
            Assert.AreEqual(true, eventA.CustomData == null || !eventA.CustomData.HasKey(eventA.CustomKeyColor));
        }
    }
}