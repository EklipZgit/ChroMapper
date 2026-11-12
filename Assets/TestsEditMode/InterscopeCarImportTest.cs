using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Editor
{
    public class InterscopeCarImportTest
    {
        [Test]
        public void ImportedDataResolvesEveryCarBodyReference()
        {
            var data = LoadData();
            var components = CreateContainer.CollectComponentInstances(data);
            var jumps = data.Objects.SelectMany(item => item.Components.HydraulicCarJumpEffect
                ?? System.Array.Empty<HydraulicCarJumpEffectData>()).ToArray();
            Assert.That(jumps.Length, Is.EqualTo(8));
            foreach (var jump in jumps)
            {
                Assert.That(components.ContainsKey(jump.Rigidbody), Is.True, "The native car rigidbody record was omitted.");
                Assert.That(components[jump.Rigidbody], Is.TypeOf<RigidbodyData>());
            }

        }

        [Test]
        public void RegenerationTwiceRetainsAllCarBodiesAndJointBindings()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene("Assets/__Scenes/Environments/InterscopeEnvironment.unity", OpenSceneMode.Single);
            try
            {
                var library = AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(
                    "Assets/Editor/Environments/EnvironmentLibrarySO.asset");
                for (var pass = 0; pass < 2; pass++)
                {
                    EnvironmentSceneCreator.CreateEnvironment(scene, LoadData(), library, true);
                    var roots = scene.GetRootGameObjects();
                    var jumps = roots.SelectMany(root => root.GetComponentsInChildren<HydraulicCarJump>(true)).ToArray();
                    Assert.That(jumps.Length, Is.EqualTo(8));
                    foreach (var jump in jumps)
                    {
                        Assert.That(jump.Rigidbody, Is.Not.Null, "Regeneration lost a jump body.");
                        Assert.That(jump.GetComponentsInChildren<SphereCollider>(true).Length, Is.EqualTo(2));
                        Assert.That(jump.GetComponentsInChildren<BoxCollider>(true).Length, Is.EqualTo(1));
                        Assert.That(jump.GetComponentsInChildren<CapsuleCollider>(true), Is.Empty);
                    }

                    var joints = jumps.SelectMany(jump => jump.Rigidbody.GetComponents<SpringJoint>()).ToArray();
                    Assert.That(joints.Length, Is.EqualTo(16));
                    foreach (var joint in joints)
                        Assert.That(joint.connectedBody, Is.Not.Null, "Regeneration attached a car joint to the world.");

                    var timeline = roots.SelectMany(root => root.GetComponentsInChildren<HydraulicCarTimeline>(true)).Single();
                    Assert.That(timeline.Template.Bodies.Length, Is.EqualTo(24));
                    Assert.That(timeline.Template.Joints.Length, Is.EqualTo(16));
                    Assert.That(timeline.Template.gameObject.activeSelf, Is.False);

                }

            }
            finally
            {
                if (setup.Any(item => item.isLoaded && item.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

        }

        private static EnvironmentData LoadData() => CreateUtils.JsonToEnvironmentData(
            AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/__Scenes/Environments/Data/InterscopeEnvironment.json"));
    }
}
