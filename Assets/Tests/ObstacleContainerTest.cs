using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class ObstacleContainerTest : TestBase
    {
        private ObstacleGridContainer _obstaclesCollection;
        private BaseObstacle _placedObstacle;

        [SetUp]
        public void SetUp()
        {
            _obstaclesCollection = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            _placedObstacle = new BaseObstacle
            {
                JsonTime = 0,
                Duration = 2,
                PosX = 0,
                PosY = 0,
                Height = 5
            };
            _placedObstacle = PlaceUtils.Place(_placedObstacle);
        }

        private MeshRenderer GetObstacleRenderer() =>
            _obstaclesCollection.LoadedContainers[_placedObstacle].GetComponentInChildren<MeshRenderer>();

        [Test]
        public void UpdatesWhenEditorScaleUpdates()
        {
            Assert.IsTrue(
                _obstaclesCollection.LoadedContainers.TryGetValue(_placedObstacle, out var obstacleContainer),
                "Obstacle container not found");

            var obstacleRenderer = GetObstacleRenderer();

            // Increase scale
            const float EditorScaleMultiplier = 2;
            var originalEditorScale = Settings.Instance.EditorScale;
            var originalObstacleScale = obstacleRenderer.bounds.size;
            try
            {
                Settings.Instance.EditorScale *= EditorScaleMultiplier;
                Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
                var modifiedObstacleScale = obstacleRenderer.bounds.size;

                Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
                Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
                Assert.AreEqual(
                    EditorScaleMultiplier * originalObstacleScale.z,
                    modifiedObstacleScale.z,
                    0.02); // because 0.001 was too strict
            }
            finally
            {
                Settings.Instance.EditorScale = originalEditorScale;
                Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
            }
        }

        [Test]
        public void ScalesWithBpmEventsCorrectly()
        {
            Assert.IsTrue(
                _obstaclesCollection.LoadedContainers.TryGetValue(_placedObstacle, out var obstacleContainer),
                "Obstacle container not found");

            PlaceUtils.Place(new BaseBpmEvent { JsonTime = 0, Bpm = 100 });
            var originalObstacleScale = GetObstacleRenderer().bounds.size;

            // Obstacle should now be 3/4 of its original length
            PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 200 });
            var modifiedObstacleScale = GetObstacleRenderer().bounds.size;

            Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
            Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
            Assert.AreEqual(3f / 4f * originalObstacleScale.z, modifiedObstacleScale.z, 0.02);
        }
    }
}
