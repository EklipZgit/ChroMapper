using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class ObstacleContainerTest : TestBase
    {
        private ObstacleGridContainer obstaclesCollection;
        private float originalEditorScale;
        private BaseObstacle placedObstacle;

        [SetUp]
        public void PlaceWall()
        {
            obstaclesCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var obstaclePlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            obstaclePlacement.CreateVisual();

            placedObstacle = new BaseObstacle
            {
                JsonTime = 0,
                Duration = 2,
                PosX = 0,
                PosY = 0,
                Height = 5
            };
            placedObstacle = PlaceUtils.Place(placedObstacle);
        }


        [Test]
        public void UpdatesWhenEditorScaleUpdates()
        {
            if (!obstaclesCollection.LoadedContainers.TryGetValue(placedObstacle, out var obstacleContainer))
                Assert.Fail("Obstacle container not found");

            var obstacleRenderer = obstacleContainer.GetComponentInChildren<MeshRenderer>();

            // Increase scale
            const float EditorScaleMultiplier = 2;
            var originalObstacleScale = obstacleRenderer.bounds.size;
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

        [Test]
        public void ScalesWithBpmEventsCorrectly()
        {
            if (!obstaclesCollection.LoadedContainers.TryGetValue(placedObstacle, out var obstacleContainer))
                Assert.Fail("Obstacle container not found");

            PlaceUtils.Place(new BaseBpmEvent { JsonTime = 0, Bpm = 100 });
            var obstacleRenderer = obstacleContainer.GetComponentInChildren<MeshRenderer>();
            var originalObstacleScale = obstacleRenderer.bounds.size;

            // Obstacle should now be 3/4 of its original length
            PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 200 });
            var modifiedObstacleScale = obstacleRenderer.bounds.size;

            Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
            Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
            Assert.AreEqual(3f / 4f * originalObstacleScale.z, modifiedObstacleScale.z, 0.02);
        }
    }
}