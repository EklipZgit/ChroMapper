using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Visual
{
    public class PaintTest : TestBase
    {
        private ColorPicker _colorPicker;
        private PaintSelectedObjects _painter;

        [SetUp]
        public void SetUp()
        {
            Settings.Instance.MapVersion = 3;

            // The paint control is serialized to Picker 2.0; FindAnyObject can instead return the strobe picker.
            _colorPicker = ColourPicker.ActivePicker;
            Assert.NotNull(_colorPicker);
            _painter = Object.FindAnyObjectByType<PaintSelectedObjects>();
        }

        [Test]
        public void PaintGradientUndo()
        {
            Settings.Instance.MapVersion = 2;

            var selectionController = Object.FindAnyObjectByType<SelectionController>();

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

            _colorPicker.CurrentColor = Color.red;
            _painter.Paint();

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
            var eventA = new BaseEvent { JsonTime = 2, Type = 1, Value = 1 };
            eventA = PlaceUtils.Place(eventA);

            SelectionController.Select(eventA);

            _colorPicker.CurrentColor = Color.red;
            _painter.Paint();

            Object.FindAnyObjectByType<SelectionController>().ShiftSelection(1, 0);

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
            var eventA = new BaseEvent { JsonTime = 2, Type = 1, Value = 0 };
            eventA = PlaceUtils.Place(eventA);

            SelectionController.Select(eventA);

            _colorPicker.CurrentColor = Color.red;
            _painter.Paint();

            BeatmapAssertion.CollectionCount<BaseEvent>(1);
            Assert.AreEqual(2, eventA.JsonTime);
            Assert.AreEqual(1, eventA.Type);
            Assert.AreEqual(true, eventA.CustomData == null || !eventA.CustomData.HasKey(eventA.CustomKeyColor));
        }

        // Painting an inner GLS node must round-trip the rendered map data and leave no second undo action behind.
        [Test]
        public void PaintSelectedInnerGlsColorNodeRoundTripsWithoutExtraUndo()
        {
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 6, ""g"": 0, ""e"": [
                    { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] }
                ] }"));
            var groupCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            groupCollection.SpawnObject(group, false, false, true);

            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.GroupContext = group;
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);
            // Isolate the paint operation so an extra undo directly reproduces the mapper's reported follow-up undo.
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actionContainer.ClearBeatmapActions();

            _colorPicker.CurrentColor = Color.magenta;
            _painter.Paint();
            AssertSingleInnerGlsColor(provider.GroupContext, Color.magenta);

            actionContainer.Undo();
            AssertSingleInnerGlsColor(provider.GroupContext, null);
            actionContainer.Redo();
            AssertSingleInnerGlsColor(provider.GroupContext, Color.magenta);
            actionContainer.Undo();
            AssertSingleInnerGlsColor(provider.GroupContext, null);
            Assert.IsNull(actionContainer.Undo(), "Painting must not leave a second undoable GLS child action behind.");
        }

        // Assert persisted GLS node state after each operation instead of coupling the test to a particular action implementation.
        private static void AssertSingleInnerGlsColor(BaseEventBoxGroup group, Color? expectedColor)
        {
            var colorEvents = group.ReadOnlyBoxes
                .SelectMany(box => box.ReadOnlyEvents)
                .OfType<BaseLightColorBase>()
                .ToArray();
            Assert.AreEqual(1, colorEvents.Length);
            Assert.AreEqual(expectedColor, colorEvents[0].CustomColor);
        }
    }
}
