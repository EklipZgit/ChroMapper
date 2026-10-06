using System.Collections;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Regression: the bloomfog skybox maps the fog texture's low-v rows to
    // below-horizon rays, so the gradient's low-t (down-facing) band must live at
    // low v. A positive-Y unprojection moved the band into the rows the skybox
    // samples at the top of the screen, painting BTS's red glow where the game
    // shows none.
    public class SkyGradientOrientationTest
    {
        [UnityTest]
        public IEnumerator DownRayBandRendersToBottomOfFogTexture()
        {
            var gradientGO = new GameObject("TestGradient");
            var gradient = gradientGO.AddComponent<BloomPrePassBackgroundColorsGradient>();
            gradient.Elements = new[]
            {
                new BloomPrePassBackgroundColorsGradient.Element
                    { StartT = 0f, Exp = 1f, Color = Color.red },
                new BloomPrePassBackgroundColorsGradient.Element
                    { StartT = 0.3f, Exp = 1f, Color = Color.red },
                new BloomPrePassBackgroundColorsGradient.Element
                    { StartT = 0.35f, Exp = 1f, Color = Color.black },
                new BloomPrePassBackgroundColorsGradient.Element
                    { StartT = 1f, Exp = 1f, Color = Color.black },
            };
            yield return null; // Start() bakes the texture

            var rt = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGB32);
            try
            {
                // Match the production call: camera-style view/projection with the
                // fog frustum crop already applied (ratio ~1 for a dedicated RT).
                var view = Matrix4x4.identity;
                var proj = Matrix4x4.Perspective(60f, 1f, 0.01f, 100f);
                gradient.Render(rt, view, proj);

                var previousActive = RenderTexture.active;
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
                    tex.Apply();
                }
                finally
                {
                    RenderTexture.active = previousActive;
                }

                // Fragment at high texel v unprojects to an up-facing ray, which
                // samples t > 0.5 = black; the low-t red band must live at low v.
                var topOfTexture = tex.GetPixel(32, 60);   // v ~= 0.95
                var bottomOfTexture = tex.GetPixel(32, 3); // v ~= 0.05
                Assert.Greater(bottomOfTexture.r, 0.1f,
                    $"low-v texels should sample the low-t red band, got {bottomOfTexture}");
                Assert.Less(topOfTexture.r, 0.1f,
                    $"high-v texels should sample t>0.5 (black), got {topOfTexture}");
            }
            finally
            {
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(gradientGO);
            }
        }
    }
}
