using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public class GlassDesertTrackDefinitionTest
    {
        private const string EnvironmentDataPath =
            "Assets/__Scenes/Environments/Data/GlassDesertEnvironment.json";
        private const string TrackDefinitionsPath =
            "Assets/__Scripts/Environments/TrackDefinitions/GlassDesertEnvironmentTrackDefinitions.asset";

        [Test]
        public void MissingLightTracksStillImportsLaserCapabilities()
        {
            var data = LoadEnvironmentData();
            Assert.That(data.Data.LightTracks, Is.Null, "This export exercises the missing-metadata fallback.");
            var tracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            try
            {
                var definitions = data.Data.LightTracks ?? new LightTrackDefinitions();
                Assert.DoesNotThrow(
                    () => definitions.CopyTo(tracks, data.Objects, data.Data.ID),
                    "Exports without lightTracks must still generate basic lanes and discover their components.");

                AssertGlassDesertTracks(tracks);
                Assert.That(data.Data.LightTracks, Is.Null, "Import must not modify the source metadata.");
            }
            finally
            {
                Object.DestroyImmediate(tracks);
            }
        }

        [TestCase(0, "Under Track", "Top Lasers")]
        [TestCase(1, "Ceiling Lights", "Down Lasers")]
        [TestCase(4, "Lane and Wide Lights", "Player Platform Lights")]
        public void ImportCorrectsOldGlassDesertLabels(int eventType, string oldName, string expectedName)
        {
            var data = LoadEnvironmentData();
            var definitions = new LightTrackDefinitions
            {
                BasicLightTracks = new List<LightTrackDefinitions.BasicTrackDefinition>
                {
                    new() { EventType = eventType, TrackName = oldName }
                },
                GroupPages = new Dictionary<string, List<LightTrackDefinitions.PageDefinition>>()
            };
            var tracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            try
            {
                definitions.CopyTo(tracks, data.Objects, data.Data.ID);

                Assert.That(tracks.Basic[eventType].Name, Is.EqualTo(expectedName));
                Assert.That(definitions.BasicLightTracks[0].TrackName, Is.EqualTo(oldName));
            }
            finally
            {
                Object.DestroyImmediate(tracks);
            }
        }

        [Test]
        public void AssetMatchesExportedLightConsumers()
        {
            var tracks = AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(TrackDefinitionsPath);
            Assert.That(tracks, Is.Not.Null);
            tracks.Initialize();

            AssertGlassDesertTracks(tracks);
        }

        private static EnvironmentData LoadEnvironmentData()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(EnvironmentDataPath);
            Assert.That(asset, Is.Not.Null);
            return CreateUtils.JsonToEnvironmentData(asset);
        }

        private static void AssertGlassDesertTracks(TrackDefinitionsSO tracks)
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 12, 13 }, tracks.Basic.Keys);
            Assert.That(tracks.Basic[0].Name, Is.EqualTo("Top Lasers"));
            Assert.That(tracks.Basic[1].Name, Is.EqualTo("Down Lasers"));
            Assert.That(tracks.Basic[2].Name, Is.EqualTo("Left Lasers"));
            Assert.That(tracks.Basic[3].Name, Is.EqualTo("Right Lasers"));
            Assert.That(tracks.Basic[4].Name, Is.EqualTo("Player Platform Lights"));
            Assert.That(tracks.Basic[12].Name, Is.EqualTo("Left Laser Speed"));
            Assert.That(tracks.Basic[13].Name, Is.EqualTo("Right Laser Speed"));
            Assert.That(tracks.Basic[12].Kind, Is.EqualTo(BasicEventKind.IntValue));
            Assert.That(tracks.Basic[13].Kind, Is.EqualTo(BasicEventKind.IntValue));
            Assert.That(
                tracks.Basic[12].Components,
                Is.EqualTo(BasicEventComponent.LightRotation | BasicEventComponent.LightRotationLeft));
            Assert.That(
                tracks.Basic[13].Components,
                Is.EqualTo(BasicEventComponent.LightRotation | BasicEventComponent.LightRotationRight));
            Assert.That(tracks.Gls, Is.Empty);
        }
    }
}
