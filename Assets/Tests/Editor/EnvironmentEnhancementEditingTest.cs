using System.Collections.Generic;
using System.Text.RegularExpressions;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class EnvironmentEnhancementEditingTest : TestBase
    {
        private static readonly Vector3 OriginalMiddlePosition = new(5f, 6f, 7f);
        private static readonly Vector3 EditedMiddlePosition = new(40f, 41f, 42f);

        private static GeometryGridContainer Collection => Object.FindAnyObjectByType<GeometryGridContainer>();

        private static BeatmapActionContainer Actions => Object.FindAnyObjectByType<BeatmapActionContainer>();

        private static BaseEnvironmentEnhancement FullEnhancement() => new()
        {
            ID = "EnhancementEditingTarget",
            LookupMethod = EnvironmentLookupMethod.Exact,
            Geometry = new JSONObject
            {
                ["type"] = "Cube",
                ["material"] = new JSONObject { ["shader"] = "Standard", ["color"] = JSON.Parse("[1,0.5,0.25,1]") }
            },
            TrackData = JSON.Parse("[\"editingTrackA\",\"editingTrackB\"]"),
            Duplicate = 2,
            Active = new JSONBool(false),
            Scale = new Vector3(1f, 2f, 3f),
            Position = new Vector3(4f, 5f, 6f),
            Rotation = new Vector3(7f, 8f, 9f),
            LocalPosition = new Vector3(10f, 11f, 12f),
            LocalRotation = new Vector3(13f, 14f, 15f),
            Components = new JSONObject
            {
                ["ILightWithId"] = new JSONObject { ["lightID"] = 23, ["type"] = 4 }
            }
        };

        private static BaseEnvironmentEnhancement SpawnPrimitiveEnhancement(string type, Vector3 position)
        {
            var enhancement = new BaseEnvironmentEnhancement
            {
                Geometry = new JSONObject { ["type"] = type, ["material"] = "standard" },
                Position = position
            };
            Collection.SpawnObject(enhancement, removeConflicting: false, refreshesPool: false);
            return enhancement;
        }

        private static BaseEnvironmentEnhancement SpawnUnmatchedEnhancement(string id)
        {
            var enhancement = new BaseEnvironmentEnhancement
            {
                ID = id,
                LookupMethod = EnvironmentLookupMethod.Exact,
                Position = new Vector3(1f, 2f, 3f)
            };
            LogAssert.Expect(LogType.Warning, new Regex("found nothing"));
            Collection.SpawnObject(enhancement, removeConflicting: false, refreshesPool: false);
            return enhancement;
        }

        private static List<BaseEnvironmentEnhancement> MapObjects => Collection.MapObjects;

        private static void AssertOrder(params BaseEnvironmentEnhancement[] expected)
        {
            Assert.That(MapObjects, Has.Count.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(MapObjects[i], Is.SameAs(expected[i]), $"environment[{i}] lost its authored instruction.");
            }
        }

        private static void AssertPayloadEqual(
            BaseEnvironmentEnhancement expected,
            BaseEnvironmentEnhancement actual,
            string stage)
        {
            Assert.That(actual.ID, Is.EqualTo(expected.ID), $"{stage}: ID");
            Assert.That(actual.LookupMethod, Is.EqualTo(expected.LookupMethod), $"{stage}: LookupMethod");
            Assert.That(actual.Geometry?.ToString(), Is.EqualTo(expected.Geometry?.ToString()), $"{stage}: Geometry");
            Assert.That(actual.TrackData?.ToString(), Is.EqualTo(expected.TrackData?.ToString()), $"{stage}: TrackData");
            Assert.That(actual.TrackNames, Is.EqualTo(expected.TrackNames), $"{stage}: TrackNames");
            Assert.That(actual.Duplicate, Is.EqualTo(expected.Duplicate), $"{stage}: Duplicate");
            Assert.That(actual.Active?.ToString(), Is.EqualTo(expected.Active?.ToString()), $"{stage}: Active");
            Assert.That(actual.Scale, Is.EqualTo(expected.Scale), $"{stage}: Scale");
            Assert.That(actual.Position, Is.EqualTo(expected.Position), $"{stage}: Position");
            Assert.That(actual.Rotation, Is.EqualTo(expected.Rotation), $"{stage}: Rotation");
            Assert.That(actual.LocalPosition, Is.EqualTo(expected.LocalPosition), $"{stage}: LocalPosition");
            Assert.That(actual.LocalRotation, Is.EqualTo(expected.LocalRotation), $"{stage}: LocalRotation");
            Assert.That(actual.Components?.ToString(), Is.EqualTo(expected.Components?.ToString()), $"{stage}: Components");
            Assert.That(actual.LightID, Is.EqualTo(expected.LightID), $"{stage}: LightID");
            Assert.That(actual.LightType, Is.EqualTo(expected.LightType), $"{stage}: LightType");
        }

        [Test]
        public void CloneCopiesCompleteEnhancementPayloadWithOwnedJson()
        {
            var source = FullEnhancement();
            source.FileOrder = 3;

            var clone = BeatmapFactory.Clone(source);

            AssertPayloadEqual(source, clone, "clone");
            Assert.AreNotSame(source.Geometry, clone.Geometry);
            Assert.AreNotSame(source.TrackData, clone.TrackData);
            Assert.AreNotSame(source.Active, clone.Active);
            Assert.AreNotSame(source.Components, clone.Components);
            Assert.That(clone.FileOrder, Is.EqualTo(3), "A replacement clone must retain the authored instruction ordinal.");

            clone.Geometry["type"] = "Sphere";
            clone.Components["ILightWithId"]["lightID"] = 99;
            Assert.That(source.Geometry["type"].Value, Is.EqualTo("Cube"), "Clone geometry shares the source JSON tree.");
            Assert.That(source.LightID, Is.EqualTo(23), "Clone components share the source JSON tree.");
        }

        [Test]
        public void ApplyCopiesCompleteEnhancementPayloadWithOwnedJson()
        {
            var source = FullEnhancement();
            source.JsonTime = 5f;
            var target = new BaseEnvironmentEnhancement { ID = "Other", FileOrder = 9 };

            target.Apply(source);

            AssertPayloadEqual(source, target, "apply");
            Assert.AreNotSame(source.Geometry, target.Geometry);
            Assert.AreNotSame(source.TrackData, target.TrackData);
            Assert.AreNotSame(source.Active, target.Active);
            Assert.AreNotSame(source.Components, target.Components);
            Assert.That(target.JsonTime, Is.EqualTo(5f));
            Assert.That(target.FileOrder, Is.EqualTo(9), "Apply must not overwrite the receiving object's identity ordinal.");
        }

        [Test]
        public void ApplyClearsRemovedEnhancementPayload()
        {
            var target = FullEnhancement();
            var sparse = new BaseEnvironmentEnhancement
            {
                ID = "cleared",
                LookupMethod = EnvironmentLookupMethod.Contains
            };

            target.Apply(sparse);

            Assert.That(target.ID, Is.EqualTo("cleared"));
            Assert.That(target.LookupMethod, Is.EqualTo(EnvironmentLookupMethod.Contains));
            Assert.That(target.Geometry, Is.Null);
            Assert.That(target.TrackData, Is.Null);
            Assert.That(target.TrackNames, Is.Empty);
            Assert.That(target.Duplicate, Is.Null);
            Assert.That(target.Active, Is.Null);
            Assert.That(target.Scale, Is.Null);
            Assert.That(target.Position, Is.Null);
            Assert.That(target.Rotation, Is.Null);
            Assert.That(target.LocalPosition, Is.Null);
            Assert.That(target.LocalRotation, Is.Null);
            Assert.That(target.Components, Is.Null);
            Assert.That(target.LightID, Is.Null);
            Assert.That(target.LightType, Is.Null);
        }

        [Test]
        public void HasSameContentDetectsGeometryOnlyChange()
        {
            var source = FullEnhancement();
            var geometryEdited = BeatmapFactory.Clone(source);
            geometryEdited.Geometry["type"] = "Sphere";

            Assert.That(source.HasSameContent(geometryEdited), Is.False, "A geometry-only edit is still a different instruction.");
            Assert.That(geometryEdited.Equals(source), Is.False, "Value equality must include geometry.");
        }

        [Test]
        public void HasSameContentDetectsComponentsOnlyChange()
        {
            var source = FullEnhancement();
            var componentsEdited = BeatmapFactory.Clone(source);
            componentsEdited.Components["ILightWithId"]["lightID"] = 99;

            Assert.That(source.HasSameContent(componentsEdited), Is.False, "A components-only edit is still a different instruction.");
            Assert.That(componentsEdited.Equals(source), Is.False, "Value equality must include components.");
        }

        [Test]
        public void HasSameContentDetectsActiveAndTrackChanges()
        {
            var source = FullEnhancement();
            var activeEdited = BeatmapFactory.Clone(source);
            activeEdited.Active = new JSONBool(true);
            var trackEdited = BeatmapFactory.Clone(source);
            trackEdited.TrackData = new JSONString("anotherTrack");

            Assert.That(source.HasSameContent(activeEdited), Is.False);
            Assert.That(source.HasSameContent(trackEdited), Is.False);
        }

        [Test]
        public void CompleteCloneHasSameContentAndSharesHashCode()
        {
            var source = FullEnhancement();
            var clone = BeatmapFactory.Clone(source);

            Assert.That(source.HasSameContent(clone), Is.True);
            Assert.That(clone.Equals(source), Is.True);
            Assert.That(
                clone.GetHashCode(),
                Is.EqualTo(source.GetHashCode()),
                "Equal enhancements must agree on a hash code for collection dictionary lookups.");
            var lookup = new HashSet<BaseEnvironmentEnhancement> { source };
            Assert.That(lookup.Contains(clone), Is.True);
        }

        [Test]
        public void SnapshotContentLookupDeletesTheSecondOfThreeInstructions()
        {
            var first = SpawnUnmatchedEnhancement("edit-missing-a");
            var second = SpawnUnmatchedEnhancement("edit-missing-b");
            var third = SpawnUnmatchedEnhancement("edit-missing-c");

            var snapshot = BeatmapFactory.Clone(second);
            Collection.DeleteObject(snapshot, false, false);

            AssertOrder(first, third);
        }

        [Test]
        public void LiveReferenceLookupDeletesTheSecondOfThreeInstructions()
        {
            var first = SpawnUnmatchedEnhancement("edit-missing-a");
            var second = SpawnUnmatchedEnhancement("edit-missing-b");
            var third = SpawnUnmatchedEnhancement("edit-missing-c");

            Collection.DeleteObject(second, false, false);

            AssertOrder(first, third);
        }

        [Test]
        public void ModifiedCollectionActionRestoresMiddleInstructionSlotAcrossUndoRedo()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            var edited = BeatmapFactory.Clone(middle);
            edited.Position = EditedMiddlePosition;
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedCollectionAction(
                    new List<BaseObject> { edited },
                    new List<BaseObject> { middle },
                    "Edited an environment enhancement."),
                true);

            AssertOrder(first, edited, last);
            Assert.That(MapObjects[1].Position, Is.EqualTo(EditedMiddlePosition));

            Actions.Undo();
            AssertOrder(first, middle, last);
            Assert.That(MapObjects[1].Position, Is.EqualTo(OriginalMiddlePosition));

            Actions.Redo();
            AssertOrder(first, edited, last);
            Assert.That(MapObjects[1].Position, Is.EqualTo(EditedMiddlePosition));
        }

        [Test]
        public void ModifiedCollectionActionMultiEditRetainsMiddleInstruction()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            var editedFirst = BeatmapFactory.Clone(first);
            editedFirst.Position = new Vector3(10f, 0f, 0f);
            var editedLast = BeatmapFactory.Clone(last);
            editedLast.Position = new Vector3(0f, 0f, 10f);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedCollectionAction(
                    new List<BaseObject> { editedFirst, editedLast },
                    new List<BaseObject> { first, last },
                    "Edited environment enhancements."),
                true);

            AssertOrder(editedFirst, middle, editedLast);

            Actions.Undo();
            AssertOrder(first, middle, last);

            Actions.Redo();
            AssertOrder(editedFirst, middle, editedLast);
        }

        [Test]
        public void UpdatedActionRestoresEditedInstructionSlotAcrossUndoRedo()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            var edited = BeatmapFactory.Clone(middle);
            edited.Position = EditedMiddlePosition;
            BeatmapActionContainer.AddAction(
                new BeatmapObjectUpdatedAction(edited, middle, "Updated an environment enhancement."),
                true);

            AssertOrder(first, edited, last);
            Assert.That(MapObjects[1].Position, Is.EqualTo(EditedMiddlePosition));

            Actions.Undo();
            AssertOrder(first, middle, last);
            Assert.That(MapObjects[1].Position, Is.EqualTo(OriginalMiddlePosition));

            Actions.Redo();
            AssertOrder(first, edited, last);
            Assert.That(MapObjects[1].Position, Is.EqualTo(EditedMiddlePosition));
        }

        [Test]
        public void LegacyModifiedActionRestoresEditedPayloadAcrossUndoRedo()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            var originalData = BeatmapFactory.Clone(middle);
            middle.Position = EditedMiddlePosition;
#pragma warning disable CS0618
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedAction(middle, middle, originalData, "Edited an environment enhancement."),
                true);
#pragma warning restore CS0618

            AssertOrder(first, middle, last);
            Assert.That(middle.Position, Is.EqualTo(EditedMiddlePosition));

            Actions.Undo();
            AssertOrder(first, middle, last);
            Assert.That(middle.Position, Is.EqualTo(OriginalMiddlePosition), "Undo must restore the pre-edit enhancement payload through Apply.");

            Actions.Redo();
            AssertOrder(first, middle, last);
            Assert.That(middle.Position, Is.EqualTo(EditedMiddlePosition), "Redo must reapply the edited enhancement payload through Apply.");
        }

        [Test]
        public void DeletionUndoRestoresInstructionSlot()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            Collection.DeleteObject(middle, true, false);
            AssertOrder(first, last);

            Actions.Undo();
            AssertOrder(first, middle, last);

            Actions.Redo();
            AssertOrder(first, last);
        }

        [Test]
        public void IdenticalPayloadInstructionsOwnDistinctContainers()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 2f, 3f));
            var second = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 2f, 3f));

            AssertOrder(first, second);
            Assert.That(Collection.LoadedContainers, Has.Count.EqualTo(2));
            Assert.AreNotSame(
                Collection.LoadedContainers[first],
                Collection.LoadedContainers[second]);

            Collection.RefreshPool(true);
            Assert.That(Collection.LoadedContainers, Has.Count.EqualTo(2));
            Assert.That(
                Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(2));

            Collection.DeleteObject(second, false, false);
            Assert.That(Collection.LoadedContainers, Has.Count.EqualTo(1));
            Assert.That(Collection.LoadedContainers.TryGetValue(first, out _), Is.True);
            Assert.That(
                Collection.ObjectsWithContainers,
                Has.Count.EqualTo(1),
                "container ownership must follow object identity, not payload equality");
            Assert.That(Collection.ObjectsWithContainers[0], Is.SameAs(first));
        }

        [Test]
        public void InPlaceEditKeepsContainerOwnershipThroughPayloadHashChange()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 2f, 3f));
            var second = SpawnPrimitiveEnhancement("Sphere", new Vector3(4f, 5f, 6f));

            first.Position = new Vector3(9f, 9f, 9f);

            Assert.That(Collection.LoadedContainers.TryGetValue(first, out var owned), Is.True);
            Assert.That(owned.ObjectData, Is.SameAs(first));

            Collection.ClearSpawnedGeometry();
            Assert.That(Collection.LoadedContainers, Is.Empty);
            Assert.That(
                Object.FindObjectsByType<GeometryContainer>(FindObjectsSortMode.None),
                Is.Empty);
        }

        [Test]
        public void SpawnedInstructionAppendsAndUndoRedoRetainsOrder()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            var fourth = new BaseEnvironmentEnhancement
            {
                Geometry = new JSONObject { ["type"] = "Quad", ["material"] = "standard" },
                Position = new Vector3(0f, 4f, 0f)
            };
            Collection.SpawnObject(fourth, removeConflicting: false, refreshesPool: false);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(fourth, new List<BaseObject>(), "Placed an environment enhancement."));
            AssertOrder(first, middle, last, fourth);

            Actions.Undo();
            AssertOrder(first, middle, last);

            Actions.Redo();
            AssertOrder(first, middle, last, fourth);
        }
    }
}
