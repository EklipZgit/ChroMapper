using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TestsEditMode
{
    public class EnvironmentTextureDataTest
    {
        [TestCase("Britney/CirclePattern", "c8d9211d9b03e1de891282f568c31331", TextureFormat.DXT5, false)]
        [TestCase("Britney/CirclePatternInvert", "06e3f0b07e581cee6a586a71ad8d2856", TextureFormat.DXT5, false)]
        [TestCase("Pyro/BaseFireFlipbookBlended", "3a7edf8c47b94eb88f083c4cc981e16f", TextureFormat.RGBA32, false)]
        [TestCase("Pyro/BurstFlipbook7Nonblended", "1506fac5e0faa3020d4cc39f4b472fd8", TextureFormat.RGBA32, false)]
        [TestCase("Pyro/CrowdFlipbook", "2651d0d39e9ef869b638da52c3efe921", TextureFormat.BC7, true)]
        [TestCase("Pyro/FoBLogoFlipbook", "f70e10aa8ad2f3584ba9e2c983527bd5", TextureFormat.RGBA32, false)]
        [TestCase("Pyro/Smoke3Blended", "88914ae0d851bb035d6a232180e14c73", TextureFormat.BC7, true)]
        [TestCase("Metallica/CracksEmission01", "7a0cc54fbb87f1dca0bff905469dbaf5", TextureFormat.BC7, false)]
        [TestCase("Metallica/FireBurst", "0672ce72fa3ef0ed9d323347e41bd499", TextureFormat.RGBA32, false)]
        [TestCase("Metallica/FireContinous", "ac70406f82e1f8454d5476104eaab1c0", TextureFormat.RGBA32, false)]
        [TestCase("Metallica/PillarsMetal", "f5b43e7907438d69fcb18d896fd25d23", TextureFormat.BC7, false)]
        [TestCase("Prodigy/CrowdFlipbookProdigy", "0411bf1f1cfdc414585bd4bc67ade8ad", TextureFormat.DXT5, true)]
        [TestCase("Lizzo/DanceFloorTile", "444e6f06f3de421db2460aa3d18199a3", TextureFormat.DXT5, false)]
        [TestCase("DaftPunk/Helmet02_MPM", "91ae16dc475cfa5248c1db148df64eb5", TextureFormat.RGBA32, false)]
        [TestCase("RockMixtape/RockFlipbookNonblended", "7980e4b2c82c1bab644e4fcddb33f318", TextureFormat.RGBA32, false)]
        [TestCase("Gaga/SimpleGlow-Gaga", "870e70a9cc53ed5d16ecbbeee1edad82", TextureFormat.RGBA32, false)]
        public void ImportedTexturesPreserveNativeRgbData(string textureName, string textureHash,
            TextureFormat format, bool linear)
        {
            var path = $"Assets/_Graphics/Textures/Environments/{textureName}.png";
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var library = AssetDatabase.LoadAssetAtPath<EnvironmentTextureSO>(
                "Assets/Editor/Environments/EnvironmentTextureSO.asset");
            library.RebuildLookup();
            Assert.That(library.Lookup[textureHash], Is.SameAs(imported));
            Assert.That(imported.format, Is.EqualTo(format));
            var native = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
            try
            {
                // These PNGs match decoded 1.44.1 textures. Alpha-zero texels still contain masks, material data or frames.
                Assert.That(native.LoadImage(File.ReadAllBytes(path)), Is.True);
                var expected = ReadTexture(native);
                var actual = ReadTexture(imported);
                var count = 0;
                var totalError = 0f;
                for (var index = 0; index < expected.Length; index++)
                {
                    if (expected[index].a > 0.001f)
                        continue;

                    totalError += Mathf.Abs(actual[index].r - expected[index].r);
                    totalError += Mathf.Abs(actual[index].g - expected[index].g);
                    totalError += Mathf.Abs(actual[index].b - expected[index].b);
                    count++;
                }

                Assert.That(count, Is.GreaterThan(0), "Exercise transparent data regions.");
                var meanError = totalError / (count * 3);
                TestContext.WriteLine($"{textureName}: {count} transparent texels, mean RGB error {meanError}.");
                var tolerance = format == TextureFormat.RGBA32 ? 0.000001f :
                    format == TextureFormat.BC7 ? 0.001f : 0.02f;
                Assert.That(meanError, Is.LessThan(tolerance),
                    $"{textureName}: texture import changed RGB data in transparent pixels (mean error {meanError}).");
            }
            finally
            {
                Object.DestroyImmediate(native);
            }
        }

        private static Color[] ReadTexture(Texture source)
        {
            var target = RenderTexture.GetTemporary(source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            var previousSrgbWrite = GL.sRGBWrite;
            try
            {
                GL.sRGBWrite = false;
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readback.Apply();
                return readback.GetPixels();
            }
            finally
            {
                GL.sRGBWrite = previousSrgbWrite;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(readback);
            }
        }

    }
}
