using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;

namespace Tests.Editor
{
    // Lock down ribbon-source retention and cache rewiring before replacing the viewport scans with indexes.
    public class GLSColorTransitionCacheTest : TestBase
    {
        private BaseDifficulty originalMap;

        [OneTimeSetUp]
        public void CaptureOriginalMap()
        {
            // Restore the scene-owned map after this isolated cache fixture so subsequent editor tests keep their shared state.
            originalMap = BeatSaberSongContainer.Instance.Map;
        }

        [OneTimeTearDown]
        public void RestoreOriginalMap()
        {
            // Avoid leaking the synthetic cache fixture map into test fixtures that run after this one.
            BeatSaberSongContainer.Instance.Map = originalMap;
        }

        [Test]
        public void BoundaryQueryReturnsOnlySourcesWhoseTransitionsCrossTheBoundary()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(3f, 1, 0)));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        [Test]
        public void InnerBoundaryQueryReturnsOnlyTheActiveGroupSource()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(2f, 1, 0),
                CreateGroup(6f, 1, 1)));
            var retainedSources = new List<BaseLightColorBase>();

            GLSEventCommon.GetColorTransitionSourcesAt(
                4f,
                map.LightColorEventBoxGroups[0],
                null,
                retainedSources);

            Assert.That(
                retainedSources,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0].Boxes[0].Events[0] }));
        }

        [Test]
        public void BoundaryQueryIncludesTransitionsEndingAtTheBoundary()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1)));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(5f, null, retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        [Test]
        public void RemovingTransitionTargetRewiresThePreviousSourceToTheNextTarget()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(5f, 0, 1),
                CreateGroup(8f, 0, 1)));
            var firstSource = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var removedTarget = map.LightColorEventBoxGroups[1];

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(firstSource, out var initialEnd), Is.True);
            Assert.That(initialEnd, Is.EqualTo(5f));

            GLSEventCommon.RemoveColorTransitionGroup(removedTarget);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(firstSource, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(8f));

            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(6f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.EquivalentTo(new[] { firstSource }));
        }

        [Test]
        public void AddingTransitionTargetRewiresThePreviousSourceToTheCloserTarget()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0),
                CreateGroup(8f, 0, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var insertedTarget = new BaseLightColorEventBoxGroup(CreateGroup(5f, 0, 1));
            insertedTarget.SetMap(map);
            insertedTarget.RecomputeSongBpmTime();

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var initialEnd), Is.True);
            Assert.That(initialEnd, Is.EqualTo(8f));

            GLSEventCommon.AddColorTransitionGroup(insertedTarget);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out var rewiredEnd), Is.True);
            Assert.That(rewiredEnd, Is.EqualTo(5f));

            var retainedSources = new List<BaseLightColorBase>();
            GLSEventCommon.GetColorTransitionSourcesAt(4f, map.LightColorEventBoxGroups[0], null, retainedSources);
            Assert.That(retainedSources, Is.EquivalentTo(new[] { source }));
        }

        [Test]
        public void BoundaryQueryKeepsEverySameTimestampSourceFromIndependentFilters()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, 1),
                CreateGroup(1f, 0, 0, 2),
                CreateGroup(5f, 0, 1, 1),
                CreateGroup(5f, 0, 1, 2)));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(
                retainedGroups,
                Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0], map.LightColorEventBoxGroups[1] }));
        }

        [Test]
        public void BoundaryQueryDoesNotCrossGroupOrFilterTimelines()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, 1),
                CreateGroup(5f, 0, 1, 2),
                CreateGroup(5f, 1, 1, 1)));
            var source = map.LightColorEventBoxGroups[0].Boxes[0].Events[0];
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, null, retainedGroups);

            Assert.That(GLSEventCommon.TryGetColorTransitionEndTime(source, out _), Is.False);
            Assert.That(retainedGroups, Is.Empty);
        }

        [Test]
        public void BoundaryQueryReturnsOnlySourcesMatchingTheActiveTrack()
        {
            var map = LoadMap(CreateDifficultyJson(
                CreateGroup(1f, 0, 0, 1, "selected"),
                CreateGroup(5f, 0, 1, 1, "selected"),
                CreateGroup(2f, 0, 0, 2, "other"),
                CreateGroup(6f, 0, 1, 2, "other")));
            var retainedGroups = new HashSet<BaseLightColorEventBoxGroup>();

            GLSEventCommon.GetColorTransitionSourceGroupsAt(4f, "selected", retainedGroups);

            Assert.That(retainedGroups, Is.EquivalentTo(new[] { map.LightColorEventBoxGroups[0] }));
        }

        // Create a map-scoped cache input without involving editor prefabs or viewport state.
        private static BaseDifficulty LoadMap(JSONNode json)
        {
            var map = BeatmapFactory.GetDifficultyFromJson(
                json,
                "testmap",
                BeatSaberSongContainer.Instance.Info,
                BeatSaberSongContainer.Instance.MapDifficultyInfo);
            BeatSaberSongContainer.Instance.Map = map;
            return map;
        }

        private static JSONNode CreateDifficultyJson(params JSONNode[] groups)
        {
            var groupArray = new JSONArray();
            foreach (var group in groups)
            {
                groupArray.Add(group);
            }

            return new JSONObject
            {
                ["version"] = "3.2.0",
                ["lightColorEventBoxGroups"] = groupArray,
            };
        }

        private static JSONNode CreateGroup(
            float time,
            int id,
            int transitionType,
            int filterParam = 1,
            string track = null)
        {
            var eventArray = new JSONArray();
            eventArray.Add(new JSONObject
            {
                ["b"] = 0,
                ["i"] = transitionType,
                ["c"] = 1,
                ["s"] = 1,
                ["f"] = 0,
                ["sb"] = 0,
                ["sf"] = 0,
            });

            var boxArray = new JSONArray();
            boxArray.Add(new JSONObject
            {
                ["f"] = new JSONObject
                {
                    ["c"] = 1,
                    ["f"] = 1,
                    ["p"] = filterParam,
                    ["t"] = 0,
                    ["r"] = 0,
                    ["n"] = 0,
                    ["s"] = 0,
                    ["l"] = 0,
                    ["d"] = 0,
                },
                ["w"] = 1,
                ["d"] = 1,
                ["r"] = 1,
                ["t"] = 1,
                ["b"] = 1,
                ["i"] = 0,
                ["e"] = eventArray,
            });

            var group = new JSONObject
            {
                ["b"] = time,
                ["g"] = id,
                ["e"] = boxArray,
            };

            if (track != null)
            {
                // Exercise the same group-level track predicate used by viewport retention.
                group["customData"] = new JSONObject { ["unusedKeyTrack"] = track };
            }

            return group;
        }
    }
}
