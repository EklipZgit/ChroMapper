using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class EnvironmentStaticBatchRemovalTest : TestBase
    {
        private const string ContainerName = "Static Batch Component Container";
        private const string RockMixtapePlatformId =
            "RockMixtapeEnvironment.[0]Environment.[19]PlayersPlace.[7]PlayerPlatformDesign.[0]PlayerPlatformDesign";

        private static readonly string[] Environments =
        {
            "WeaveEnvironment", "LizzoEnvironment", "Panic2Environment", "RockMixtapeEnvironment"
        };

        [UnityTest]
        public IEnumerator HidingStaticBatchContainerHidesTheCombinedGeometry(
            [ValueSource(nameof(Environments))] string environment)
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                environmentName: environment);
            var batch = StaticBatch(Descriptor());
            Assert.That(batch, Is.Not.Empty);
            Assert.That(batch.All(IsVisible), Is.True, "The unmodified environment must display its combined geometry.");

            var enhancements = new JSONArray();
            enhancements.Add(new JSONObject
            {
                ["id"] = ContainerName,
                ["lookupMethod"] = "EndsWith",
                ["active"] = false
            });
            yield return TestUtils.ReloadMap(3, new JSONObject
            {
                ["version"] = "3.2.0",
                ["customData"] = new JSONObject { ["environment"] = enhancements }
            }, environmentName: environment);

            var descriptor = Descriptor();
            batch = StaticBatch(descriptor);
            Assert.That(batch, Is.Not.Empty);
            Assert.That(batch.Any(IsVisible), Is.False, "The enhancement must hide the combined geometry.");

            var batchMaterials = batch.SelectMany(renderer => renderer.sharedMaterials).ToHashSet();
            var renderersOutsideBatch = descriptor.ChromaIDMarkers
                .Select(marker => (marker.ChromaID, Renderer: marker.GetComponent<MeshRenderer>()))
                .Where(entry => entry.Renderer != null && !batch.Contains(entry.Renderer) &&
                    entry.Renderer.sharedMaterials.Any(batchMaterials.Contains)).ToArray();
            var sources = renderersOutsideBatch.Where(entry => entry.ChromaID != RockMixtapePlatformId).ToArray();
            Assert.That(sources, Is.Not.Empty);
            Assert.That(sources.Count(entry => IsVisible(entry.Renderer)), Is.Zero,
                "The source renderers must stay hidden after the batch is removed.");

            if (environment == "RockMixtapeEnvironment")
            {
                var platform = renderersOutsideBatch.Single(entry => entry.ChromaID == RockMixtapePlatformId);
                Assert.That(IsVisible(platform.Renderer), Is.True, "The player platform shares a material and renders separately.");
            }
        }

        private static EnvironmentDescriptor Descriptor() =>
            Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;

        private static MeshRenderer[] StaticBatch(EnvironmentDescriptor descriptor)
        {
            var container = descriptor.ChromaIDMarkers.Single(marker => marker.name == ContainerName);
            return container.GetComponentsInChildren<MeshRenderer>(true);
        }

        private static bool IsVisible(MeshRenderer renderer) => renderer.enabled && renderer.gameObject.activeInHierarchy;

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
