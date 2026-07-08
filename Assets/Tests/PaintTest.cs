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

            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var colorPicker = Object.FindAnyObjectByType<ColorPicker>();
            var painter = Object.FindAnyObjectByType<PaintSelectedObjects>();

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var customData = new JSONObject();
            customData["_lightGradient"] = new ChromaLightGradient(Color.blue, Color.cyan).ToJson();
            var baseEventA = new BaseEvent
            {
                JsonTime = 2,
                Type = 1,
                Value = 1,
                FloatValue = 1,
                CustomData = customData
            };
            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(Color.red, eventsContainer.MapObjects[0].CustomLightGradient.StartColor);

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(1, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(Color.red, eventsContainer.MapObjects[0].CustomLightGradient.StartColor);

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(1, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(Color.blue, eventsContainer.MapObjects[0].CustomLightGradient.StartColor);
        }

        [Test]
        public void PaintUndo()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var colorPicker = Object.FindAnyObjectByType<ColorPicker>();
            var painter = Object.FindAnyObjectByType<PaintSelectedObjects>();

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent { JsonTime = 2, Type = 1, Value = 1 };
            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            selectionController.ShiftSelection(1, 0);

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(
                Color.red,
                eventsContainer.MapObjects[0].CustomData[baseEventA.CustomKeyColor].ReadColor());

            // Undo move
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(1, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(
                Color.red,
                eventsContainer.MapObjects[0].CustomData[baseEventA.CustomKeyColor].ReadColor());

            // Undo paint
            actionContainer.Undo();

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(1, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(
                true,
                eventsContainer.MapObjects[0].CustomData == null
                || !eventsContainer
                    .MapObjects[0]
                    .CustomData
                    .HasKey(baseEventA.CustomKeyColor));
        }

        [Test]
        public void IgnoresOff()
        {
            var colorPicker = Object.FindAnyObjectByType<ColorPicker>();
            var painter = Object.FindAnyObjectByType<PaintSelectedObjects>();

            var eventsContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
            var eventPlacement = Object.FindAnyObjectByType<EventPlacement>();

            var baseEventA = new BaseEvent { JsonTime = 2, Type = 1, Value = 0 };
            baseEventA = PlaceUtils.Place(baseEventA);

            SelectionController.Select(baseEventA);

            colorPicker.CurrentColor = Color.red;
            painter.Paint();

            Assert.AreEqual(1, eventsContainer.MapObjects.Count);
            Assert.AreEqual(2, eventsContainer.MapObjects[0].JsonTime);
            Assert.AreEqual(1, eventsContainer.MapObjects[0].Type);
            Assert.AreEqual(
                true,
                eventsContainer.MapObjects[0].CustomData == null
                || !eventsContainer
                    .MapObjects[0]
                    .CustomData
                    .HasKey(baseEventA.CustomKeyColor));
        }
    }
}