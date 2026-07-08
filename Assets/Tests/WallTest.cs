using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;

namespace Tests
{
    public class WallTest : TestBase
    {
        [Test]
        public void EnsureWallIntegrity()
        {
            var obstaclesContainer =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var wallPlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            wallPlacement.CreateVisual();

            var wallA = new BaseObstacle
            {
                JsonTime = 0f,
                PosX = 1,
                Type = 0,
                Duration = 1f,
                Width = 1
            };
            wallA = PlaceUtils.Place(wallA);

            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 0f,
                    PosX = 1,
                    Type = 0,
                    PosY = 0,
                    Duration = 1f,
                    Width = 1,
                    Height = 5
                },
                obstaclesContainer.MapObjects[0],
                "Check v2 wall attributes");

            wallA.Type = 0;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 0f,
                    PosX = 1,
                    Type = 0,
                    PosY = 0,
                    Duration = 1f,
                    Width = 1,
                    Height = 5
                },
                obstaclesContainer.MapObjects[0],
                "Check type 0 v2 wall attributes");

            wallA.Type = 1;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 0f,
                    PosX = 1,
                    Type = 1,
                    PosY = 2,
                    Duration = 1f,
                    Width = 1,
                    Height = 3
                },
                obstaclesContainer.MapObjects[0],
                "Check type 1 v2 wall attributes");

            // wallA.Type = 2;
            // BeatmapAssertion.Assert(new BaseObstacle { JsonTime = 0f, PosX = 1, Type = 2, PosY = 0, Duration = 1f, Width = 1, Height = 5 }, obstaclesContainer.MapObjects[0], "Check type 2 v2 wall attributes");

            var expectedWallA = BeatmapFactory.Clone(wallA);
            expectedWallA.Type = 5436;
            wallA.Type = 5436;
            BeatmapAssertion.IsEqual(
                expectedWallA,
                obstaclesContainer.MapObjects[0],
                "Check arbitrary type v2 wall attributes");

            // test v3 wall
            var wallB = new BaseObstacle
            {
                JsonTime = 1f,
                PosX = 1,
                PosY = 0,
                Duration = 1f,
                Width = 1,
                Height = 5
            };
            wallB = PlaceUtils.Place(wallB);

            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 0,
                    PosY = 0,
                    Duration = 1f,
                    Width = 1,
                    Height = 5
                },
                obstaclesContainer.MapObjects[1],
                "Check v3 wall attributes");

            wallB.Type = 0;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 0,
                    PosY = 0,
                    Duration = 1f,
                    Width = 1,
                    Height = 5
                },
                obstaclesContainer.MapObjects[1],
                "Check type 0 v3 wall attributes");

            wallB.Type = 1;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 1,
                    PosY = 2,
                    Duration = 1f,
                    Width = 1,
                    Height = 3
                },
                obstaclesContainer.MapObjects[1],
                "Check type 1 v3 wall attributes");

            // wallB.Type = 2;
            // BeatmapAssertion.Assert(new BaseObstacle { JsonTime = 1f, PosX = 1, Type = 0, PosY = 0, Duration = 1f, Width = 1, Height = 5 }, obstaclesContainer.MapObjects[1], "Check type 2 v3 wall attributes");

            wallB.Height = 3;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 1,
                    PosY = 2,
                    Duration = 1f,
                    Width = 1,
                    Height = 3
                },
                obstaclesContainer.MapObjects[1],
                "Height 3 should change nothing else for v3 wall");

            wallB.Height = 5;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 1,
                    PosY = 2,
                    Duration = 1f,
                    Width = 1,
                    Height = 5
                },
                obstaclesContainer.MapObjects[1],
                "Height 5 should change nothing else for v3 wall");

            wallB.Height = 4;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 1,
                    PosY = 2,
                    Duration = 1f,
                    Width = 1,
                    Height = 4
                },
                obstaclesContainer.MapObjects[1],
                "Height 4 should change nothing else for v3 wall");

            wallB.PosY = 2;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 1,
                    PosY = 2,
                    Duration = 1f,
                    Width = 1,
                    Height = 4
                },
                obstaclesContainer.MapObjects[1],
                "Pos Y 2 should change Type to crouch for v3 wall");

            wallB.PosY = 0;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 0,
                    PosY = 0,
                    Duration = 1f,
                    Width = 1,
                    Height = 4
                },
                obstaclesContainer.MapObjects[1],
                "Pos Y 0 should change Type to full for v3 wall");

            wallB.PosY = 1;
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 1f,
                    PosX = 1,
                    Type = 0,
                    PosY = 1,
                    Duration = 1f,
                    Width = 1,
                    Height = 4
                },
                obstaclesContainer.MapObjects[1],
                "Pos Y 1 should change nothing else for v3 wall");
        }

        [Test]
        public void HyperWall()
        {
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            var obstaclesCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var wallPlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            var inputController = Object.FindAnyObjectByType<BeatmapObstacleInputController>();
            wallPlacement.CreateVisual();

            var wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Duration = 2,
                Width = 1
            };
            wallA = PlaceUtils.Place(wallA);

            if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                inputController.ToggleHyperWall(container);

            var toDelete = obstaclesCollection.MapObjects.First();
            obstaclesCollection.DeleteObject(toDelete);

            Assert.AreEqual(0, obstaclesCollection.MapObjects.Count);

            actionContainer.Undo();

            Assert.AreEqual(1, obstaclesCollection.MapObjects.Count);
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 4,
                    PosX = (int)GridX.Left,
                    Type = (int)ObstacleType.Full,
                    PosY = 0,
                    Duration = -2.0f,
                    Width = 1,
                    Height = 5
                },
                obstaclesCollection.MapObjects[0],
                "Perform hyper wall");

            actionContainer.Undo();
            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    Type = (int)ObstacleType.Full,
                    PosY = 0,
                    Duration = 2.0f,
                    Width = 1,
                    Height = 5
                },
                obstaclesCollection.MapObjects[0],
                "Undo hyper wall");
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            Settings.Instance.MapVersion = 2;

            var obstaclesCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var wallPlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            wallPlacement.CreateVisual();

            var customCoord = new JSONArray { [0] = 0, [1] = 1 };
            var customSize = new JSONArray { [0] = 0, [1] = null, [2] = 420 };

            var wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Duration = 2,
                Width = 1
            };
            wallA.CustomCoordinate = customCoord;
            wallA.CustomSize = customSize;
            wallA = PlaceUtils.Place(wallA);

            BeatmapAssertion.IsEqual(
                new BaseObstacle
                {
                    JsonTime = 2,
                    PosX = (int)GridX.Left,
                    Type = (int)ObstacleType.Full,
                    PosY = 0,
                    Duration = 2.0f,
                    Width = 1,
                    Height = 5,
                    CustomData = new JSONObject { ["_position"] = customCoord, ["_scale"] = customSize }
                },
                obstaclesCollection.MapObjects[0],
                "Applies CustomProperties to CustomData");
        }
    }
}