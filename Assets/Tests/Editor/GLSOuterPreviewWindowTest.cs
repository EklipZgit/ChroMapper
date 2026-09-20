using System.Collections.Generic;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;

namespace Tests.Editor
{
    // GLSOuterPreviewWindowTest prevents one retained parent group from materializing every offscreen inner node.
    public class GLSOuterPreviewWindowTest : TestBase
    {
        // RefreshPoolMaterializesOnlyPreviewNodesInsideWindow reproduces the profiler hitch with two distant offsets.
        [Test]
        public void RefreshPoolMaterializesOnlyPreviewNodesInsideWindow()
        {
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 20, ""g"": 1, ""e"": [
                    { ""f"": { ""f"": 1, ""p"": 1, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 },
                      ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0,
                      ""e"": [
                        { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                        { ""b"": 100, ""c"": 1, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 },
                        { ""b"": 200, ""c"": 0, ""s"": 1, ""i"": 1, ""f"": 1, ""sb"": 1, ""sf"": 0 }
                      ] }
                ] }"));
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.SpawnObject(group, false, false, true);

            // A narrow viewport around the parent must not retain far-future preview GameObjects.
            collection.RefreshPool(19f, 30f);

            Assert.IsTrue(collection.LoadedContainers.TryGetValue(group, out var loaded));
            var owner = loaded as GLSGroupContainer;
            Assert.IsNotNull(owner);
            Assert.AreEqual(0, GetPreviewGhosts(owner).Count,
                "The two inner nodes outside the pool window must remain data-only until their beats approach the viewport.");
        }

        // Read the maintained preview list directly so the assertion does not scan unrelated scene objects.
        private static List<GLSGroupContainer> GetPreviewGhosts(GLSGroupContainer owner)
        {
            var field = typeof(GLSGroupContainer).GetField(
                "previewGhosts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            return (List<GLSGroupContainer>)field.GetValue(owner);
        }
    }
}
