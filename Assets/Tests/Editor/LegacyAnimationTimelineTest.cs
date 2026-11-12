using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class LegacyAnimationTimelineTest
    {
        [TestCase("GagaEnvironment")]
        [TestCase("HalloweenEnvironment")]
        [TestCase("Halloween2Environment")]
        public void ImportedPlaybackMatchesNativeCapturedPosesAfterSeeking(string environment)
        {
            var fixture = AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"Assets/Tests/Fixtures/NativeLegacyAnimations/{environment}.json");
            Assert.That(fixture, Is.Not.Null);
            var samples = JSON.Parse(fixture.text)["samples"];
            var data = CreateUtils.JsonToEnvironmentData(AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"Assets/__Scenes/Environments/Data/{environment}.json"));
            foreach (var item in data.Objects.Where(item => item.Components.Animation != null))
            {
                var path = string.Join("/", Regex.Split(item.ChromaID, @"\.\[\d+\]").Skip(1));
                var root = new GameObject(path);
                try
                {
                    var first = samples[0]["animations"].Children.Single(record => record["path"].Value == path);
                    foreach (var pose in first["transforms"].Children)
                    {
                        var relativePath = pose["path"].Value.Substring(path.Length).TrimStart('/');
                        var target = FindOrCreateTransform(root.transform, relativePath);
                        target.localPosition = Vector(pose["localPosition"]);
                        target.localRotation = Rotation(pose["localRotation"]);
                        target.localScale = Vector(pose["localScale"]);
                    }

                    var animation = root.AddComponent<TimelineLegacyAnimation>();
                    item.Components.Animation[0].FillComponents(root, animation, new CreateContainer { Data = data });
                    Assert.That(animation.enabled, Is.True);
                    foreach (var index in new[] { 2, 0, 1, 0 })
                    {
                        var sample = samples[index];
                        var record = sample["animations"].Children.Single(record => record["path"].Value == path);
                        animation.SampleAtSongSeconds(sample["songSeconds"].AsFloat);
                        foreach (var pose in record["transforms"].Children)
                        {
                            var relativePath = pose["path"].Value.Substring(path.Length).TrimStart('/');
                            var target = relativePath.Length == 0 ? root.transform : root.transform.Find(relativePath);
                            var message = $"{environment} {pose["path"].Value} at {sample["songSeconds"].AsFloat}s";
                            Assert.That(Vector3.Distance(target.localPosition, Vector(pose["localPosition"])),
                                Is.LessThan(0.002f), message);
                            Assert.That(Quaternion.Angle(target.localRotation, Rotation(pose["localRotation"])),
                                Is.LessThan(0.5f), message);
                            Assert.That(Vector3.Distance(target.localScale, Vector(pose["localScale"])),
                                Is.LessThan(0.002f), message);
                        }

                    }

                }
                finally
                {
                    Object.DestroyImmediate(root);
                }

            }

        }

        private static Vector3 Vector(JSONNode node) => new(node[0].AsFloat, node[1].AsFloat, node[2].AsFloat);

        private static Quaternion Rotation(JSONNode node) =>
            new(node[0].AsFloat, node[1].AsFloat, node[2].AsFloat, node[3].AsFloat);

        private static Transform FindOrCreateTransform(Transform root, string path)
        {
            var target = root;
            foreach (var part in path.Split('/').Where(part => part.Length != 0))
            {
                var child = target.Find(part);
                if (child == null)
                {
                    child = new GameObject(part).transform;
                    child.SetParent(target, false);
                }

                target = child;
            }

            return target;
        }

        [TestCase("GagaLogoIdle")]
        [TestCase("Crow")]
        [TestCase("ZombieHand")]
        [TestCase("SkeletonHand")]
        public void NativeClipSeeksRewindAndLoopWithoutMovingItsParent(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                PathUtils.Combine(LegacyAnimationData.ClipsPath, name + ".anim"));
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.legacy, Is.True);
            Assert.That(clip.wrapMode, Is.EqualTo(WrapMode.Loop));
            var parent = new GameObject("Track parent");
            parent.transform.SetPositionAndRotation(new Vector3(10f, 3f, -2f), Quaternion.Euler(0f, 25f, 0f));
            try
            {
                var root = new GameObject("Animation root");
                root.transform.SetParent(parent.transform, false);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    FindOrCreateTransform(root.transform, binding.path);

                var animation = root.AddComponent<TimelineLegacyAnimation>();
                animation.Clip = clip;
                var transforms = root.GetComponentsInChildren<Transform>();
                animation.SampleAtSongSeconds(1.25f);
                var positions = transforms.Select(target => target.localPosition).ToArray();
                var rotations = transforms.Select(target => target.localRotation).ToArray();
                var scales = transforms.Select(target => target.localScale).ToArray();
                animation.SampleAtSongSeconds(5f);
                Assert.That(transforms.Select((target, index) => Quaternion.Angle(target.localRotation, rotations[index])).Max(),
                    Is.GreaterThan(0.001f), "The native clip must produce movement.");
                foreach (var seconds in new[] { 1.25f, clip.length + 1.25f, 1.25f })
                {
                    animation.SampleAtSongSeconds(seconds);
                    for (var index = 0; index < transforms.Length; index++)
                    {
                        Assert.That(Vector3.Distance(transforms[index].localPosition, positions[index]), Is.LessThan(0.0001f));
                        Assert.That(Quaternion.Angle(transforms[index].localRotation, rotations[index]), Is.LessThan(0.01f));
                        Assert.That(Vector3.Distance(transforms[index].localScale, scales[index]), Is.LessThan(0.0001f));
                    }

                }

                Assert.That(parent.transform.position, Is.EqualTo(new Vector3(10f, 3f, -2f)));
                Assert.That(Quaternion.Angle(parent.transform.rotation, Quaternion.Euler(0f, 25f, 0f)), Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }

        }
    }
}
