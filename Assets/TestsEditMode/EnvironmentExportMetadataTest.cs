using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace TestsEditMode
{
    public class EnvironmentExportMetadataTest
    {
        [Test]
        public void SpriteTintIsStoredOnTheExportedRenderer()
        {
            var source = ReadExport("InterscopeEnvironment");
            var logo = source["objects"].Children.Single(item => item["id"].Value ==
                "InterscopeEnvironment.[0]Environment.[11]Logo");
            var color = logo["components"]["SpriteRenderer"][0]["color"];
            Assert.That(color.IsArray, Is.True, "Sprite tint must be exported with its component.");
            Assert.That(color[0].AsFloat, Is.Zero);
            Assert.That(color[3].AsFloat, Is.EqualTo(178f / 255f).Within(0.00001f));
        }

        [TestCase("GagaEnvironment", 1)]
        [TestCase("HalloweenEnvironment", 11)]
        [TestCase("Halloween2Environment", 8)]
        public void AnimationPlaybackIsStoredOnEachExportedComponent(string environment, int count)
        {
            var records = ReadExport(environment)["objects"].Children
                .SelectMany(item => item["components"]["Animation"].Children).ToArray();
            Assert.That(records.Length, Is.EqualTo(count));
            foreach (var record in records)
            {
                Assert.That(record["clipName"].Value, Is.Not.Empty);
                Assert.That(record.HasKey("phaseSeconds"), Is.True);
                Assert.That(record["playbackSpeed"].AsFloat, Is.EqualTo(1f));
                Assert.That(record["wrapMode"].Value, Is.EqualTo("Loop"));
            }
        }

        [Test]
        public void CarPhysicsStartupIsStoredInTheEnvironmentExport()
        {
            var source = ReadExport("InterscopeEnvironment");
            Assert.That(source["environmentData"]["physics"]["fixedDeltaTime"].AsFloat, Is.GreaterThan(0f));
            var bodies = source["objects"].Children
                .SelectMany(item => item["components"]["RigidBodyWrapper"].Children).ToArray();
            Assert.That(bodies.Length, Is.EqualTo(24));
            foreach (var body in bodies)
            {
                var state = body["startupState"];
                Assert.That(state["position"].Count, Is.EqualTo(3));
                Assert.That(state["rotation"].Count, Is.EqualTo(4));
                Assert.That(state["linearVelocity"].Count, Is.EqualTo(3));
                Assert.That(state["angularVelocity"].Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void IndependentExportDoesNotAcquireUnrelatedSavedTint()
        {
            var asset = new TextAsset(@"{""environmentData"":{""environmentID"":""InterscopeEnvironment""},
                ""objects"":[{""id"":""InterscopeEnvironment.[0]Environment.[11]Logo"",
                ""components"":{""SpriteRenderer"":[{""instanceId"":1}]}}]}");
            try
            {
                var data = CreateUtils.JsonToEnvironmentData(asset);
                Assert.That(data.Objects[0].Components.SpriteRenderer[0].Color, Is.EqualTo(Color.white),
                    "Missing component data must not be silently replaced from another export.");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        private static JSONNode ReadExport(string environment) => JSON.Parse(
            AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/__Scenes/Environments/Data/{environment}.json").text);
    }
}
