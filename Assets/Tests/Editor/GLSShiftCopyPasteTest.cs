// Exercise the real copy, paste, and child-mutation paths so shift payloads cannot regress through another parent rebuild.
using System;
using System.Globalization;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Box and node shift payloads live in customData while every GLS inner edit replaces the whole parent
    // group, so each scenario must keep authored arrays, parsed caches, unknown fields, and serialized JSON together.
    public class GLSShiftCopyPasteTest : SongBoundaryTestBase
    {
        private static readonly string[] BoxShifts = { "h,-0.2,ioqn", "f,0.1,iq" };
        private static readonly string[] BoxStrobeShifts = { "s,-0.2,lin,l" };
        private static readonly string[] SecondBoxStrobeShifts = { "hs,0.4,lin" };
        private static readonly string[] NodeShifts = { "v,0.4,lin" };
        private static readonly string[] NodeStrobeShifts = { "r,-0.1,lin" };

        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider groupProvider;
        private GLSEventGridProvider innerProvider;
        private GLSEventGridContainer innerCollection;
        private BeatmapActionContainer actions;
        private TracksDefinitionSO originalTracks;
        private TracksDefinitionSO testTracks;
        private string originalPage;
        private int originalMapVersion;

        // Real GetNewObjects rejects unavailable GLS tracks, so install a valid color lane definition.
        [SetUp]
        public void ConfigureGlsTracks()
        {
            runtime = GetField<BeatmapRuntimeContext>(Selection, "beatmapRuntimeContext");
            groupProvider = GetField<GLSGroupGridProvider>(Selection, "glsGroupGridProvider");
            innerProvider = GetField<GLSEventGridProvider>(Selection, "glsEventGridProvider");
            innerCollection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            originalTracks = runtime.TracksDefinition;
            originalPage = groupProvider.CurrentGroup;
            testTracks = ScriptableObject.CreateInstance<TracksDefinitionSO>();
            testTracks.Copy(originalTracks);
            SetField(testTracks, "glsEntries", Enumerable.Range(1, 3).Select(id => new TrackDefinitionGLS
            {
                ID = id,
                Name = "Shift lane " + id,
                Group = "GLS shift tests",
                ColorTrack = true,
                RotationTracks = new[] { false, false, false },
                TranslationTracks = new[] { false, false, false },
                FloatFXTrack = false
            }).ToList());
            testTracks.Initialize();
            runtime.TracksDefinition = testTracks;
            runtime.NotifyTracksDefinition();
            groupProvider.SetGroupPage("GLS shift tests");
            // Group and box ToJson select a V3/V4 writer; pin the version these fixtures serialize.
            originalMapVersion = Settings.Instance.MapVersion;
            Settings.Instance.MapVersion = 3;
        }

        // Tracks are scene state, so failed assertions must not leave subsequent fixtures on the synthetic GLS page.
        [TearDown]
        public void RestoreGlsTracks()
        {
            if (runtime != null && originalTracks != null)
            {
                runtime.TracksDefinition = originalTracks;
                runtime.NotifyTracksDefinition();
                groupProvider.SetGroupPage(originalPage);
            }
            if (testTracks != null)
            {
                Object.DestroyImmediate(testTracks);
            }
            Settings.Instance.MapVersion = originalMapVersion;
        }

        // Copying a whole outer group must keep every filter lane's box and node payload on the pasted group.
        [Test]
        public void PasteOuterColorGroupPreservesEveryBoxShiftPayload()
        {
            SetMode(EditingMode.GLS);
            var source = PlaceShiftedColorGroup(8f, 1);
            SelectOnly(source);
            CopyWithKeyboard();
            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(1));
            // The clipboard entry is already a clone; its lanes must carry the shift payload too.
            AssertShiftedGroupPayload((BaseLightColorEventBoxGroup)SelectionController.CopiedObjects.First());
            Atsc.MoveToJsonTime(24f);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            var pasted = ColorGroups().Single(g => g.ID == 1 && !ReferenceEquals(g, source));
            AssertRoundTrip(
                () =>
                {
                    Assert.That(ColorGroups().Any(g => ReferenceEquals(g, source)), Is.True);
                    AssertShiftedGroupPayload(source);
                    AssertShiftedGroupPayload(pasted);
                    // Independent ownership: the pasted boxes and node must not share the source payloads.
                    Assert.AreNotSame(source.Boxes[0].CustomData, pasted.Boxes[0].CustomData);
                    Assert.AreNotSame(source.Boxes[0].Shifts, pasted.Boxes[0].Shifts);
                    Assert.AreNotSame(source.Boxes[0].StrobeShifts, pasted.Boxes[0].StrobeShifts);
                    Assert.AreNotSame(source.Boxes[1].CustomData, pasted.Boxes[1].CustomData);
                    Assert.AreNotSame(
                        source.Boxes[0].Events[0].CustomData,
                        pasted.Boxes[0].Events[0].CustomData);
                },
                () =>
                {
                    Assert.That(ColorGroups().Any(g => ReferenceEquals(g, pasted)), Is.False);
                    AssertShiftedGroupPayload(source);
                });
        }

        // Pasting a node into a shifted box rebuilds the destination parent; both the parent lane payload and
        // the copied node's own event-scope payload must survive the replacement.
        [Test]
        public void PasteInnerNodePreservesBoxShiftsAndPastedNodeShiftPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceShiftedColorGroup(8f, 1);
            OpenGroup(group);
            var sourceNode = group.Boxes[0].Events[0];
            SelectOnly(sourceNode);
            CopyWithKeyboard();
            var clipboardNode = (BaseLightColorBase)SelectionController.CopiedObjects.First();
            CollectionAssert.AreEqual(NodeShifts, clipboardNode.Shifts);
            CollectionAssert.AreEqual(NodeStrobeShifts, clipboardNode.StrobeShifts);
            SelectionController.DeselectAll();
            ConfigureInnerColorPaste(group, 0, 3f);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            AssertRoundTrip(
                () =>
                {
                    var pasted = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(pasted, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(pasted.Boxes[0].Events.Length, Is.EqualTo(3));
                    AssertShiftedGroupPayload(pasted);
                    var pastedNode = pasted.Boxes[0].Events
                        .Single(e => Mathf.Abs(e.RelativeJsonTime - 3f) < 0.0001f);
                    CollectionAssert.AreEqual(NodeShifts, pastedNode.Shifts);
                    CollectionAssert.AreEqual(NodeStrobeShifts, pastedNode.StrobeShifts);
                    Assert.That(pastedNode.ParsedShifts.Count, Is.EqualTo(1));
                    Assert.That(pastedNode.ParsedStrobeShifts.Count, Is.EqualTo(1));
                    Assert.AreNotSame(sourceNode.CustomData, pastedNode.CustomData);
                    Assert.AreNotSame(sourceNode.Shifts, pastedNode.Shifts);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertShiftedGroupPayload(group);
                });
        }

        // Editing an inner node's own shifts uses the same group clone as every other node command; the parent
        // lanes and untouched sibling payloads must not be rewritten by the replacement.
        [Test]
        public void EditInnerNodePreservesContainingBoxShiftPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceShiftedColorGroup(8f, 1);
            OpenGroup(group);
            var editedNode = group.Boxes[0].Events[1];
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            GLSEventColorCommand.SetBrightness(editedNode, 0.42f);

            AssertRoundTrip(
                () =>
                {
                    var edited = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(edited, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(edited.Boxes[0].Events[1].Brightness, Is.EqualTo(0.42f).Within(0.0001f));
                    AssertShiftedGroupPayload(edited);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertShiftedGroupPayload(group);
                });
        }

        // Deleting an inner node rebuilds its parent from the open child collection; box payloads on every lane
        // and the surviving node's own shifts must be retained.
        [Test]
        public void DeleteInnerNodePreservesContainingBoxShiftPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceShiftedColorGroup(8f, 1);
            OpenGroup(group);
            SelectOnly(group.Boxes[0].Events[1]);

            Selection.Delete();

            AssertRoundTrip(
                () =>
                {
                    var edited = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(edited, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(edited.Boxes[0].Events.Length, Is.EqualTo(1));
                    AssertShiftedGroupPayload(edited);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertShiftedGroupPayload(group);
                });
        }

        // Placing a new node into the open group rebuilds the parent through the same path as an edit or delete.
        [Test]
        public void AddInnerNodePreservesContainingBoxShiftPayload()
        {
            SetMode(EditingMode.EventBox);
            var group = PlaceShiftedColorGroup(8f, 1);
            OpenGroup(group);
            var node = new BaseLightColorBase
            {
                RelativeJsonTime = 2.5f,
                Brightness = 1f,
                EventBoxGroupData = group,
                EventBoxData = group.Boxes[0],
                BoxIndex = 0
            };
            node.SetMap(BeatSaberSongContainer.Instance.Map);
            node.RecomputeSongBpmTime();
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            innerCollection.SpawnObject(node, false, true);

            AssertRoundTrip(
                () =>
                {
                    var edited = innerProvider.GroupContext as BaseLightColorEventBoxGroup;
                    Assert.That(edited, Is.Not.Null.And.Not.SameAs(group));
                    Assert.That(edited.Boxes[0].Events.Length, Is.EqualTo(3));
                    AssertShiftedGroupPayload(edited);
                },
                () =>
                {
                    Assert.That(innerProvider.GroupContext, Is.SameAs(group));
                    AssertShiftedGroupPayload(group);
                });
        }

        // A two-lane group whose first lane owns box shifts, strobe shifts, an unknown field, and a shifted node.
        private static BaseLightColorEventBoxGroup CreateShiftedColorGroup(float beat, int id) =>
            BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                "{\"b\":" + beat.ToString(CultureInfo.InvariantCulture) +
                ",\"g\":" + id.ToString(CultureInfo.InvariantCulture) +
                ",\"e\":[" +
                "{\"f\":{\"c\":1,\"f\":0,\"p\":0,\"t\":0,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":1,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":0," +
                "\"customData\":{\"shifts\":[\"h,-0.2,ioqn\",\"f,0.1,iq\"]," +
                "\"strobeShifts\":[\"s,-0.2,lin,l\"],\"future\":42}," +
                "\"e\":[" +
                "{\"b\":0.5,\"c\":0,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0," +
                "\"customData\":{\"shifts\":[\"v,0.4,lin\"],\"strobeShifts\":[\"r,-0.1,lin\"]," +
                "\"eventFuture\":\"kept\"}}," +
                "{\"b\":1.5,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}," +
                "{\"f\":{\"c\":1,\"f\":1,\"p\":2,\"t\":4,\"r\":0,\"n\":0,\"s\":0,\"l\":0,\"d\":0}," +
                "\"w\":1,\"d\":0,\"r\":0,\"t\":0,\"b\":0,\"i\":0," +
                "\"customData\":{\"strobeShifts\":[\"hs,0.4,lin\"]}," +
                "\"e\":[{\"b\":0.75,\"c\":1,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0}]}]}"));

        private static BaseLightColorEventBoxGroup PlaceShiftedColorGroup(float beat, int id) =>
            Spawn(CreateShiftedColorGroup(beat, id));

        // Check the live arrays, the parsed playback caches, and the serialized JSON boundary so a fix cannot
        // keep the strings while still losing playback state or stripping keys on save.
        private static void AssertShiftedGroupPayload(BaseLightColorEventBoxGroup group)
        {
            var first = group.Boxes[0];
            CollectionAssert.AreEqual(BoxShifts, first.Shifts);
            CollectionAssert.AreEqual(BoxStrobeShifts, first.StrobeShifts);
            Assert.That(first.ParsedShifts.Count, Is.EqualTo(2));
            Assert.That(first.ParsedStrobeShifts.Count, Is.EqualTo(1));
            Assert.That(first.ParsedStrobeShifts[0].UsesAffectedLightProgress, Is.True);
            Assert.That(first.CustomData["future"].AsInt, Is.EqualTo(42));

            var shiftedNode = first.Events[0];
            CollectionAssert.AreEqual(NodeShifts, shiftedNode.Shifts);
            CollectionAssert.AreEqual(NodeStrobeShifts, shiftedNode.StrobeShifts);
            Assert.That(shiftedNode.ParsedShifts.Count, Is.EqualTo(1));
            Assert.That(shiftedNode.ParsedStrobeShifts.Count, Is.EqualTo(1));
            Assert.That(shiftedNode.CustomData["eventFuture"].Value, Is.EqualTo("kept"));

            var second = group.Boxes[1];
            CollectionAssert.AreEqual(SecondBoxStrobeShifts, second.StrobeShifts);
            Assert.That(second.ParsedStrobeShifts.Count, Is.EqualTo(1));

            // SaveCustom rewrites both owned arrays during serialization; assert the keys remain authored.
            var output = group.ToJson();
            Assert.That(output["e"][0]["customData"]["shifts"][0].Value, Is.EqualTo("h,-0.2,ioqn"));
            Assert.That(output["e"][0]["customData"]["shifts"][1].Value, Is.EqualTo("f,0.1,iq"));
            Assert.That(output["e"][0]["customData"]["strobeShifts"][0].Value, Is.EqualTo("s,-0.2,lin,l"));
            Assert.That(output["e"][0]["customData"]["future"].AsInt, Is.EqualTo(42));
            Assert.That(output["e"][0]["e"][0]["customData"]["shifts"][0].Value, Is.EqualTo("v,0.4,lin"));
            Assert.That(
                output["e"][0]["e"][0]["customData"]["strobeShifts"][0].Value,
                Is.EqualTo("r,-0.1,lin"));
            Assert.That(output["e"][1]["customData"]["strobeShifts"][0].Value, Is.EqualTo("hs,0.4,lin"));
        }

        // Keep selection gestures out of the action stack so one undo reverses exactly the tested operation.
        private static void SelectOnly(params BaseObject[] objects)
        {
            SelectionController.DeselectAll();
            foreach (var obj in objects)
            {
                SelectionController.Select(obj, true, false, false);
            }
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        // Clearing retirement metadata mirrors existing GLS fixtures and prevents deferred replacement races.
        private void OpenGroup(BaseEventBoxGroup group)
        {
            innerProvider.LastContext = null;
            innerProvider.GroupContext = group;
        }

        // Inner paste uses a relative hover offset; the queued color placement supplies the destination lane.
        private void ConfigureInnerColorPaste(BaseEventBoxGroup group, int boxIndex, float relativeBeat)
        {
            var placement = GetField<BasePlacement>(Selection, "glsEventColorPlacement");
            var queued = GetField<BaseGLSEvent>(placement, "QueuedData");
            queued.EventBoxGroupData = group;
            queued.EventBoxData = group.ReadOnlyBoxes[boxIndex];
            queued.BoxIndex = boxIndex;
            queued.RelativeJsonTime = relativeBeat;
            queued.SetMap(BeatSaberSongContainer.Instance.Map);
            queued.RecomputeSongBpmTime();
            placement.State = PlacementState.Active;
        }

        private static BaseLightColorEventBoxGroup[] ColorGroups() =>
            BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.GLSColor)
                .LoadedObjects.Cast<BaseLightColorEventBoxGroup>().ToArray();

        // Exactly one action must restore and replay the complete edit, not require one undo per node or lane.
        private void AssertRoundTrip(Action assertApplied, Action assertOriginal)
        {
            assertApplied();
            var action = actions.Undo();
            Assert.That(action, Is.Not.Null);
            assertOriginal();
            Assert.That(actions.Undo(), Is.Null, "The edit must be one atomic action.");
            Assert.That(actions.Redo(), Is.SameAs(action));
            assertApplied();
            Assert.That(actions.Redo(), Is.Null, "No extra edits may remain on the redo stack.");
        }
    }
}
