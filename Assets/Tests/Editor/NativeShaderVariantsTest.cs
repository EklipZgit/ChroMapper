using Beatmap.Appearances;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public class NativeShaderVariantsTest
    {
        private static NativeShaderVariants LoadCatalog() => new(
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Graphics/Shaders/ShaderLibrary/NativeSimpleLitVariants.json").text);

        [Test]
        public void EveryRetainedNativeCombinationResolvesWithoutLosingFeatures()
        {
            var catalog = LoadCatalog();
            foreach (var keywords in catalog.GetVariants())
                Assert.That(catalog.Resolve(keywords), Is.EquivalentTo(keywords), string.Join(" ", keywords));

        }

        [Test]
        public void UnsupportedConcreteRequestUsesNativeEmptyFallback()
        {
            Assert.That(
                LoadCatalog().Resolve(new[] { "DIFFUSE", "DIRT", "FOG", "REFLECTION_PROBE_BOX_PROJECTION" }),
                Is.Empty);
        }

        [Test]
        public void CompleteNativeDiffuseRequestKeepsFog()
        {
            var keywords = new[] { "DIFFUSE", "FOG", "MULTIPLY_REFLECTIONS", "REFLECTION_PROBE_BOX_PROJECTION" };
            Assert.That(LoadCatalog().Resolve(keywords), Is.EquivalentTo(keywords));
        }

    }
}
