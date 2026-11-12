using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class EnvironmentEnhancementRefreshTest : TestBase
    {
        private const string BoxId = "BTSEnvironment.[0]Environment.[22]PillarPair (3).[1]PillarR.[2]LaserR.[1]BoxLight";
        private const string EnvironmentName = "BTSEnvironment";
        private const string PointName = "refreshPoint";
        private const string PointTrackName = "refreshPointTrack";
        private const string MaterialName = "refreshMaterial";
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Vector3 OriginalMiddlePosition = new(5f, 6f, 7f);
        private static readonly Vector3 EditedMiddlePosition = new(40f, 41f, 42f);
        private bool previousAnimations;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator PoolRefreshRestoresNativeTargetsWithoutReplacingTheMap()
        {
            yield return Load(3, Difficulty(3, null));
            var collection = Object.FindAnyObjectByType<GeometryGridContainer>();
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var originalBox = BoxTarget();
            var originalPosition = originalBox.position;
            var originalActive = originalBox.gameObject.activeSelf;
            var retargetId = UnrelatedMarkerId(originalBox);
            var other = MarkerTarget(retargetId);
            var otherPosition = other.position;
            var note = PlaceUtils.Place(new BaseNote { JsonTime = 8 });
            var wall = PlaceUtils.Place(new BaseObstacle { JsonTime = 2, Duration = 8, Width = 1, Height = 3 });
            var notes = Object.FindAnyObjectByType<NoteGridContainer>();
            var walls = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var noteContainer = notes.LoadedContainers[note];
            var wallContainer = walls.LoadedContainers[wall];
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(4f);
            var enhancement = BeatmapFactory.EnvironmentEnhancement(MovedEnhancement(3));
            collection.MapObjects.Add(enhancement);
            collection.RefreshPool(true);
            Assert.That(Vector3.Distance(originalBox.position, new Vector3(7f, 8f, 9f)), Is.LessThan(0.001f));
            Assert.That(originalBox.gameObject.activeSelf, Is.False);

            enhancement.ID = retargetId;
            enhancement.Position = new Vector3(9f, 8f, 7f);
            enhancement.Active = null;
            collection.RefreshPool(true);
            Assert.That(
                Vector3.Distance(originalBox.position, originalPosition),
                Is.LessThan(0.001f),
                "Retargeting must undo the instruction's writes to its former target.");
            Assert.That(originalBox.gameObject.activeSelf, Is.EqualTo(originalActive));
            Assert.That(Vector3.Distance(other.position, enhancement.Position.Value), Is.LessThan(0.001f));

            collection.MapObjects.Remove(enhancement);
            collection.RefreshPool(true);
            Assert.That(Vector3.Distance(other.position, otherPosition), Is.LessThan(0.001f));
            Assert.That(Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor, Is.SameAs(descriptor));
            Assert.That(BoxTarget(), Is.SameAs(originalBox));
            Assert.That(notes.LoadedContainers[note], Is.SameAs(noteContainer));
            Assert.That(walls.LoadedContainers[wall], Is.SameAs(wallContainer));
            Assert.That(clock.CurrentJsonTime, Is.EqualTo(4f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator PoolRefreshRemovesClonesWithoutReplacingTheEnvironment()
        {
            yield return Load(3, Difficulty(3, null), "DefaultEnvironment");
            var collection = Object.FindAnyObjectByType<GeometryGridContainer>();
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var source = descriptor.ChromaIDMarkers.First(marker => marker.ChromaID.EndsWith("BigTrackLaneRing(Clone)"));
            var markerCount = Markers();
            var ringCount = EnvironmentComponentCount<TrackLaneRing>();
            var markersPerClone = source.GetComponentsInChildren<ChromaIDMarker>(true).Length;
            var ringsPerClone = source.GetComponentsInChildren<TrackLaneRing>(true).Length;
            var enhancement = BeatmapFactory.EnvironmentEnhancement(DuplicateEnhancement(3, source.ChromaID));
            collection.MapObjects.Add(enhancement);
            try
            {
                collection.RefreshPool(true);
                Assert.That(Markers(), Is.EqualTo(markerCount + 2 * markersPerClone));
                var cloneIds = descriptor.ChromaIDMarkers.Select(marker => marker.ChromaID).ToArray();
                collection.RefreshPool(true);
                Assert.That(Markers(), Is.EqualTo(markerCount + 2 * markersPerClone), "Refreshing accumulated native clones.");
                Assert.That(EnvironmentComponentCount<TrackLaneRing>(), Is.EqualTo(ringCount + 2 * ringsPerClone));
                Assert.That(descriptor.ChromaIDMarkers.Select(marker => marker.ChromaID), Is.EqualTo(cloneIds));
                var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
                clock.MoveToJsonTime(8f);
                clock.MoveToJsonTime(2f);
                collection.MapObjects.Remove(enhancement);
                collection.RefreshPool(true);
                Assert.That(Markers(), Is.EqualTo(markerCount));
                Assert.That(EnvironmentComponentCount<TrackLaneRing>(), Is.EqualTo(ringCount));
                Assert.That(Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor, Is.SameAs(descriptor));
                Assert.That(descriptor.ChromaIDMarkers, Does.Contain(source));
                clock.MoveToJsonTime(8f);
                clock.MoveToJsonTime(2f);
            }
            finally
            {
                descriptor.Reinitialize();
            }

        }

        [UnityTest]
        public IEnumerator PoolRefreshRebindsAnimatedNativeTargetsAtTheCurrentBeat()
        {
            yield return Load(3, PointDifficulty());
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(4f);
            yield return null;
            yield return null;
            var original = BoxTarget();
            var otherId = UnrelatedMarkerId(original);
            var other = MarkerTarget(otherId);
            var enhancement = MapObjects.Single();
            enhancement.ID = otherId;
            Collection.RefreshPool(true);
            Assert.That(Vector3.Distance(other.position, new Vector3(9f, 0f, 0f)), Is.LessThan(0.001f));
            var restoredPosition = original.position;
            clock.MoveToJsonTime(1f);
            clock.MoveToJsonTime(4f);
            yield return null;
            Assert.That(Vector3.Distance(original.position, restoredPosition), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(other.position, new Vector3(9f, 0f, 0f)), Is.LessThan(0.001f));

            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 4f);
            enhancement.ID = BoxId;
            Collection.RefreshPool(true);
            Assert.That(clock.IsPlaying, Is.True);
            Assert.That(clock.CurrentJsonTime, Is.EqualTo(4f).Within(0.001f));
            Assert.That(Vector3.Distance(original.position, new Vector3(9f, 0f, 0f)), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator PoolRefreshRestoresLightIdsAndTubeBloomOverrides()
        {
            yield return Load(3, Difficulty(3, null));
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var marker = descriptor.ChromaIDMarkers.First(target =>
                target.GetComponent<ParametricBloomFogLightController>() is { Kind: LightController.LightKind.Basic });
            var controller = marker.GetComponent<ParametricBloomFogLightController>();
            var originalType = controller.Type;
            var originalId = controller.ID;
            var originalAlpha = controller.ColorAlphaMultiplier;
            var originalBloom = controller.BloomFogIntensityMultiplier;
            var data = new JSONObject
            {
                ["id"] = marker.ChromaID,
                ["lookupMethod"] = "Exact",
                ["components"] = new JSONObject
                {
                    ["ILightWithId"] = new JSONObject { ["type"] = originalType, ["lightID"] = 10000 },
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = 0.37f,
                        ["bloomFogIntensityMultiplier"] = 0.21f
                    }
                }
            };
            var enhancement = BeatmapFactory.EnvironmentEnhancement(data);
            MapObjects.Add(enhancement);
            Collection.RefreshPool(true);
            var enhancedId = controller.ID;
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(0.37f).Within(0.001f));
            Assert.That(controller.BloomFogIntensityMultiplier, Is.EqualTo(0.21f).Within(0.001f));
            Collection.RefreshPool(true);
            Assert.That(controller.ID, Is.EqualTo(enhancedId), "Refresh must retain deterministic light registration indices.");
            MapObjects.Remove(enhancement);
            Collection.RefreshPool(true);
            Assert.That(controller.Type, Is.EqualTo(originalType));
            Assert.That(controller.ID, Is.EqualTo(originalId));
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(originalAlpha));
            Assert.That(controller.BloomFogIntensityMultiplier, Is.EqualTo(originalBloom));
            var effect = descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(originalType);
            Assert.That(effect.SetColorForId(originalId, Color.magenta), Is.True);
            Assert.That(controller.Color, Is.EqualTo(Color.magenta));
            Assert.That(effect.SetColorForId(enhancedId, Color.cyan), Is.False);
        }

        [UnityTest]
        public IEnumerator PoolRefreshRemovesFogComponentBindingsWithoutRemovingEvents()
        {
            yield return Load(3, Difficulty(3, null));
            var defaultOffset = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.BloomFogParams.Offset;
            var data = Difficulty(3, new JSONObject
            {
                ["id"] = "BTSEnvironment.[0]Environment",
                ["lookupMethod"] = "Exact",
                ["track"] = PointTrackName
            });
            data["customData"]["customEvents"] = JSON.Parse(
                "[{\"b\":2,\"t\":\"AnimateComponent\",\"d\":{\"track\":\"refreshPointTrack\",\"BloomFogEnvironment\":{\"offset\":[7]}}}]");
            yield return Load(3, data);
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            context.Atsc.MoveToJsonTime(4f);
            Assert.That(context.Descriptor.BloomFogParams.Offset, Is.EqualTo(7f));
            MapObjects.Clear();
            Collection.RefreshPool(true);
            Assert.That(context.Descriptor.BloomFogParams.Offset, Is.EqualTo(defaultOffset));
            context.Atsc.MoveToJsonTime(1f);
            context.Atsc.MoveToJsonTime(4f);
            yield return null;
            Assert.That(context.Descriptor.BloomFogParams.Offset, Is.EqualTo(defaultOffset));
            Assert.That(BeatSaberSongContainer.Instance.Map.CustomEvents, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PoolRefreshDiscardsAnimatedTubeBloomTargets()
        {
            yield return Load(3, Difficulty(3, null));
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var markerId = descriptor.ChromaIDMarkers.First(target =>
                target.GetComponent<ParametricBloomFogLightController>() != null).ChromaID;
            var defaultAlpha = MarkerTarget(markerId).GetComponent<ParametricBloomFogLightController>().ColorAlphaMultiplier;
            var data = Difficulty(3, new JSONObject
            {
                ["id"] = markerId,
                ["lookupMethod"] = "Exact",
                ["track"] = PointTrackName
            });
            data["customData"]["customEvents"] = JSON.Parse(
                "[{\"b\":2,\"t\":\"AnimateComponent\",\"d\":{\"track\":\"refreshPointTrack\",\"TubeBloomPrePassLight\":{\"colorAlphaMultiplier\":[0.37]}}}]");
            yield return Load(3, data);
            var controller = MarkerTarget(markerId).GetComponent<ParametricBloomFogLightController>();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(4f);
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(0.37f).Within(0.001f));
            Collection.RefreshPool(true);
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(0.37f).Within(0.001f));
            MapObjects.Clear();
            Collection.RefreshPool(true);
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(defaultAlpha));
            clock.MoveToJsonTime(1f);
            clock.MoveToJsonTime(4f);
            yield return null;
            Assert.That(controller.ColorAlphaMultiplier, Is.EqualTo(defaultAlpha));
        }

        [UnityTest]
        public IEnumerator InPlaceReloadRestoresOemTargetAfterInstructionRemovalAndRetarget()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                yield return Load(version, Difficulty(version, null));
                var authoredPosition = BoxTarget().position;
                var authoredActive = BoxTarget().gameObject.activeSelf;

                var added = BeatmapFactory.EnvironmentEnhancement(MovedEnhancement(version));
                BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Add(added);
                var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
                clock.MoveToJsonTime(4f);
                yield return ReloadInPlace();

                RecordTarget(
                    failures,
                    BoxTarget(),
                    new Vector3(7f, 8f, 9f) * PositionUnits(version),
                    false,
                    $"V{version} applying the in-memory instruction");
                if (Mathf.Abs(clock.CurrentJsonTime - 4f) > 0.001f)
                    failures.Add($"V{version}: editor cursor did not survive the in-place reload (got {clock.CurrentJsonTime}).");

                var retargetId = UnrelatedMarkerId(BoxTarget());
                var retargetAuthoredPosition = MarkerTarget(retargetId).position;
                added.Position = new Vector3(9f, 8f, 7f);
                added.Active = null;
                added.ID = retargetId;
                yield return ReloadInPlace();

                RecordTarget(
                    failures,
                    BoxTarget(),
                    authoredPosition,
                    authoredActive,
                    $"V{version} retargeting the instruction away from the box");
                RecordPosition(
                    failures,
                    MarkerTarget(retargetId).position,
                    new Vector3(9f, 8f, 7f) * PositionUnits(version),
                    $"V{version} retargeted instruction position");

                BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Remove(added);
                yield return ReloadInPlace();

                RecordTarget(
                    failures,
                    BoxTarget(),
                    authoredPosition,
                    authoredActive,
                    $"V{version} removing the instruction");
                RecordPosition(
                    failures,
                    MarkerTarget(retargetId).position,
                    retargetAuthoredPosition,
                    $"V{version} removed instruction leftover");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator InPlaceReloadDoesNotStackDuplicateInstructions()
        {
            var failures = new List<string>();
            foreach (var version in new[] { 2, 3 })
            {
                yield return Load(version, Difficulty(version, null), "DefaultEnvironment");
                var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
                var ring = descriptor.ChromaIDMarkers
                    .First(marker => marker.ChromaID.EndsWith("BigTrackLaneRing(Clone)"));
                var ringId = ring.ChromaID;
                var perCloneMarkers = ring.GetComponentsInChildren<ChromaIDMarker>(true).Length;
                var perCloneRings = ring.GetComponentsInChildren<TrackLaneRing>(true).Length;
                var markerCount = descriptor.ChromaIDMarkers.Count;
                var ringCount = EnvironmentComponentCount<TrackLaneRing>();

                var added = BeatmapFactory.EnvironmentEnhancement(DuplicateEnhancement(version, ringId));
                BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Add(added);
                yield return ReloadInPlace();

                RecordCount(
                    failures,
                    Markers(),
                    markerCount + (2 * perCloneMarkers),
                    $"V{version} first reload markers");
                RecordCount(
                    failures,
                    EnvironmentComponentCount<TrackLaneRing>(),
                    ringCount + (2 * perCloneRings),
                    $"V{version} first reload rings");

                yield return ReloadInPlace();

                RecordCount(
                    failures,
                    Markers(),
                    markerCount + (2 * perCloneMarkers),
                    $"V{version} repeated reload markers");
                RecordCount(
                    failures,
                    EnvironmentComponentCount<TrackLaneRing>(),
                    ringCount + (2 * perCloneRings),
                    $"V{version} repeated reload rings");

                BeatSaberSongContainer.Instance.Map.EnvironmentEnhancements.Remove(added);
                yield return ReloadInPlace();

                RecordCount(failures, Markers(), markerCount, $"V{version} removing the duplicate instruction");
                RecordCount(
                    failures,
                    EnvironmentComponentCount<TrackLaneRing>(),
                    ringCount,
                    $"V{version} removing the duplicate rings");
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator InPlaceReloadReflectsInMemoryMaterialEdits()
        {
            yield return Load(3, MaterialDifficulty());
            var color = Geometry().MpbController.Mpb.GetColor(ColorId);
            Assert.That(Vector4.Distance(color, Color.red), Is.LessThan(0.01f), $"authored material color is {color}");

            BeatSaberSongContainer.Instance.Map.Materials[MaterialName].Color = Color.green;
            yield return ReloadInPlace();
            yield return null;
            yield return null;

            var reloaded = Geometry().MpbController.Mpb.GetColor(ColorId);
            Assert.That(
                Vector4.Distance(reloaded, Color.green),
                Is.LessThan(0.01f),
                $"in-memory material edit must reach the reloaded geometry; got {reloaded}");
        }

        [UnityTest]
        public IEnumerator InPlaceReloadReflectsInMemoryPointDefinitionEdits()
        {
            yield return Load(3, PointDifficulty());
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(4f);
            yield return null;
            yield return null;
            Assert.That(
                Vector3.Distance(BoxTarget().position, new Vector3(9f, 0f, 0f)),
                Is.LessThan(0.001f),
                $"authored point definition drives the track; got {BoxTarget().position}");

            BeatSaberSongContainer.Instance.Map.PointDefinitions[PointName] = JSON.Parse("[[4,0,0,0]]").AsArray;
            yield return ReloadInPlace();
            yield return null;
            yield return null;

            Assert.That(
                Vector3.Distance(BoxTarget().position, new Vector3(4f, 0f, 0f)),
                Is.LessThan(0.001f),
                $"edited point definition must reach the reloaded animation; got {BoxTarget().position}");
            Assert.That(Mathf.Abs(clock.CurrentJsonTime - 4f), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator InPlaceReloadPreservesInstructionOrdinalsForDeletionUndo()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            Collection.DeleteObject(middle, true, false);
            AssertOrder(first, last);

            yield return ReloadInPlace();
            AssertOrder(first, last);

            Actions.Undo();
            AssertOrder(first, middle, last);

            Actions.Redo();
            AssertOrder(first, last);
        }

        [UnityTest]
        public IEnumerator InPlaceReloadPreservesInstructionOrdinalsForReplacementUndoRedo()
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

            yield return ReloadInPlace();
            AssertOrder(first, edited, last);

            Actions.Undo();
            AssertOrder(first, middle, last);

            yield return ReloadInPlace();
            AssertOrder(first, middle, last);

            Actions.Redo();
            AssertOrder(first, edited, last);
        }

        [UnityTest]
        public IEnumerator InPlaceReloadDoesNotRecycleDeletedTailOrdinal()
        {
            var first = SpawnPrimitiveEnhancement("Cube", new Vector3(1f, 0f, 0f));
            var middle = SpawnPrimitiveEnhancement("Sphere", OriginalMiddlePosition);
            var last = SpawnPrimitiveEnhancement("Cylinder", new Vector3(0f, 0f, 1f));

            var deleteAction = new BeatmapObjectDeletionAction(last, "Deleted tail enhancement.");
            BeatmapActionContainer.AddAction(deleteAction, true);
            AssertOrder(first, middle);

            yield return ReloadInPlace();
            AssertOrder(first, middle);

            var fourth = SpawnPrimitiveEnhancement("Quad", new Vector3(0f, 4f, 0f));
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    fourth,
                    new List<BaseObject>(),
                    "Placed an environment enhancement."));
            Assert.That(
                fourth.FileOrder,
                Is.GreaterThan(last.FileOrder),
                "A newly authored instruction must not reuse an ordinal still owned by a pending deletion undo.");

            deleteAction.Undo(new BeatmapActionContainer.BeatmapActionParams(Actions));
            AssertOrder(first, middle, last, fourth);
            Assert.That(
                MapObjects[2],
                Is.SameAs(last),
                "The restored tail instruction must precede the newly authored instruction.");
        }

        private static GeometryGridContainer Collection => Object.FindAnyObjectByType<GeometryGridContainer>();

        private static BeatmapActionContainer Actions => Object.FindAnyObjectByType<BeatmapActionContainer>();

        private static List<BaseEnvironmentEnhancement> MapObjects => Collection.MapObjects;

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

        private static void AssertOrder(params BaseEnvironmentEnhancement[] expected)
        {
            Assert.That(MapObjects, Has.Count.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(
                    MapObjects[i],
                    Is.SameAs(expected[i]),
                    $"environment[{i}] lost its authored instruction.");
            }
        }

        private IEnumerator Load(int version, JSONNode data, string environmentName = EnvironmentName)
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(version, data, environmentName: environmentName);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private static IEnumerator ReloadInPlace()
        {
            yield return Object.FindAnyObjectByType<LoadInitialMap>().ReloadCurrentMapInPlace();
            yield return null;
        }

        private static string Key(int version, string name) => version == 2
            ? "_" + name
            : name;

        private static float PositionUnits(int version) => version == 2
            ? 0.6f
            : 1f;

        private static JSONNode Difficulty(int version, JSONObject enhancement)
        {
            var environment = new JSONArray();
            if (enhancement != null)
                environment.Add(enhancement);
            return new JSONObject
            {
                [Key(version, "version")] = version == 2 ? "2.6.0" : "3.2.0",
                [Key(version, "customData")] = new JSONObject { [Key(version, "environment")] = environment }
            };
        }

        private static JSONObject MovedEnhancement(int version) => new()
        {
            [Key(version, "id")] = BoxId,
            [Key(version, "lookupMethod")] = "Exact",
            [Key(version, "position")] = JSON.Parse("[7,8,9]"),
            [Key(version, "active")] = false
        };

        private static JSONObject DuplicateEnhancement(int version, string ringId) => new()
        {
            [Key(version, "id")] = ringId,
            [Key(version, "lookupMethod")] = "Exact",
            [Key(version, "duplicate")] = 2
        };

        private static JSONNode MaterialDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["geometry"] = new JSONObject
                {
                    ["type"] = "Cube",
                    ["material"] = MaterialName
                }
            });
            return new JSONObject
            {
                ["version"] = "3.2.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["materials"] = new JSONObject
                    {
                        [MaterialName] = new JSONObject
                        {
                            ["shader"] = "Standard",
                            ["color"] = JSON.Parse("[1,0,0,1]")
                        }
                    }
                }
            };
        }

        private static JSONNode PointDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = BoxId,
                ["lookupMethod"] = "Exact",
                ["track"] = PointTrackName
            });
            var customEvents = new JSONArray();
            customEvents.Add(new JSONObject
            {
                ["b"] = 2f,
                ["t"] = "AnimateTrack",
                ["d"] = new JSONObject
                {
                    ["track"] = PointTrackName,
                    ["position"] = PointName
                }
            });
            return new JSONObject
            {
                ["version"] = "3.2.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = customEvents,
                    ["pointDefinitions"] = new JSONObject
                    {
                        [PointName] = JSON.Parse("[[9,0,0,0]]")
                    }
                }
            };
        }

        private static Transform BoxTarget() => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.Single(marker => marker.ChromaID == BoxId).transform;

        private static Transform MarkerTarget(string chromaId) => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.First(marker => marker.ChromaID == chromaId).transform;

        private static string UnrelatedMarkerId(Transform box)
        {
            var markers = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.ChromaIDMarkers;
            return markers
                .First(marker =>
                    marker.gameObject != box.gameObject
                    && !marker.transform.IsChildOf(box)
                    && !box.IsChildOf(marker.transform))
                .ChromaID;
        }

        private static int Markers() => Object.FindAnyObjectByType<BeatmapRuntimeContext>()
            .Descriptor.ChromaIDMarkers.Count;

        private static int EnvironmentComponentCount<T>() where T : Component
        {
            var scene = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor.gameObject.scene;
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Count();
        }

        private static GeometryContainer Geometry() => Object.FindAnyObjectByType<GeometryGridContainer>()
            .LoadedContainers.Values.OfType<GeometryContainer>().Single();

        private static void RecordTarget(
            List<string> failures,
            Transform target,
            Vector3 expectedPosition,
            bool expectedActive,
            string stage)
        {
            if (Vector3.Distance(target.position, expectedPosition) > 0.001f)
                failures.Add($"{stage}: position={target.position}, expected {expectedPosition}.");
            if (target.gameObject.activeSelf != expectedActive)
                failures.Add($"{stage}: active={target.gameObject.activeSelf}, expected {expectedActive}.");
        }

        private static void RecordPosition(
            List<string> failures,
            Vector3 actual,
            Vector3 expected,
            string stage)
        {
            if (Vector3.Distance(actual, expected) > 0.001f)
                failures.Add($"{stage}: actual={actual}, expected {expected}.");
        }

        private static void RecordCount(List<string> failures, int actual, int expected, string stage)
        {
            if (actual != expected)
                failures.Add($"{stage}: count={actual}, expected {expected}.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedFixture()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
