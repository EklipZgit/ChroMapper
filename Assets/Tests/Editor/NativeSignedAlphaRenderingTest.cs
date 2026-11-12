using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class NativeSignedAlphaRenderingTest
    {
        [TestCase(-120f, 1f, 1f, 1f)]
        [TestCase(120f, 1f, 1f, 1f)]
        [TestCase(0f, 0.069f, 0f, 0f)]
        public void MainEffectKeepsTheNativeSignedAlphaWhiteBoost(float alpha, float red, float green, float blue)
        {
            // Native 1.44.1 MainEffect averages four alpha taps and squares the result, including negative values.
            var source = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
            var material = new Material(Shader.Find("ChroMapper/Post Process/Post Bloom"));
            var target = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var pixels = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            var previousBoost = Shader.GetGlobalFloat("_BaseColorBoost");
            var previousThreshold = Shader.GetGlobalFloat("_BaseColorBoostThreshold");
            try
            {
                source.SetPixel(0, 0, new Color(0.069f, 0f, 0f, alpha));
                source.Apply();
                material.SetFloat("_Fade", 1f);
                material.SetFloat("_BloomIntensity", 0f);
                material.SetTexture("_PostBloomTexture", Texture2D.blackTexture);
                material.SetVector("_PostBloomSourceTexelSize", Vector4.one);
                Shader.SetGlobalFloat("_BaseColorBoost", 1f);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", 0f);
                Graphics.Blit(source, target, material);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
                pixels.Apply();
                var actual = pixels.GetPixel(4, 4);
                Assert.That(actual.r, Is.EqualTo(red).Within(0.003f));
                Assert.That(actual.g, Is.EqualTo(green).Within(0.003f));
                Assert.That(actual.b, Is.EqualTo(blue).Within(0.003f));
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Shader.SetGlobalFloat("_BaseColorBoost", previousBoost);
                Shader.SetGlobalFloat("_BaseColorBoostThreshold", previousThreshold);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
