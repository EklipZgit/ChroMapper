using System;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class SpriteLightColorParityTest
    {
        [TestCase(typeof(SpriteLightController), 1)]
        [TestCase(typeof(SpriteLightController), 2)]
        [TestCase(typeof(SpriteArrayLightController), 1)]
        [TestCase(typeof(SpriteArrayLightController), 2)]
        public void SpriteVertexColorsApplyNativeLinearConversionOnce(Type adapterType, int spriteCount)
        {
            var lightObject = new GameObject("Sprite light color probe");
            var cameraObject = new GameObject("Sprite color camera");
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false, true);
            var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Environments/Billie/Sun (Instance).mat"));
            Sprite sprite = null;
            try
            {
                var pixels = new Color32[256];
                Array.Fill(pixels, new Color32(255, 255, 255, 128));
                texture.SetPixels32(pixels);
                texture.Apply();
                material.SetTexture("_MainTex", texture);
                sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 16f);
                var renderer = lightObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.gameObject.layer = 31;
                lightObject.transform.position = Vector3.forward * 5f;
                var adapter = (LightController)lightObject.AddComponent(adapterType);
                var input = new Color(0.355f, 0.578f, 0.817f, 0.7490196f);
                if (adapter is SpriteLightController single)
                {
                    single.Renderer = renderer;
                    single.Intensity = 2f;
                }
                else if (adapter is SpriteArrayLightController array)
                {
                    array.SpriteRenderers = new[] { renderer };
                    array.Intensity = 2f;
                }

                adapter.Start();
                adapter.SetColor(input);
                Assert.That(
                    renderer.color.r,
                    Is.EqualTo(input.r * 2f).Within(0.0001f),
                    "The native sprite adapter stores the authored tint without a manual color-space conversion.");
                if (spriteCount == 2)
                {
                    var neighbor = new GameObject("Batching neighbor");
                    neighbor.transform.SetParent(lightObject.transform, false);
                    neighbor.transform.localPosition = Vector3.right * 0.75f;
                    neighbor.layer = 31;
                    var neighborRenderer = neighbor.AddComponent<SpriteRenderer>();
                    neighborRenderer.sprite = sprite;
                    neighborRenderer.sharedMaterial = material;
                    neighborRenderer.color = renderer.color;
                }

                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.cullingMask = 1 << 31;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "ChangeOfSceneMountains");
                System.IO.Directory.CreateDirectory(directory);
                var actual = LinearRenderCapture.Capture(
                    camera,
                    PathUtils.Combine(directory, adapterType.Name + "-" + spriteCount + ".png"),
                    32,
                    32)[16 * 32 + 16];
                // The native adapter assigns color * intensity. Unity converts and packs that tint into COLOR32 before the particle shader samples alpha.
                var nativeTint = (Color)(Color32)(input * 2f).linear.WithAlpha(input.a * 2f);
                var alpha = (128f / 255f) * nativeTint.a;
                var squaredAlpha = alpha * Mathf.Clamp01(alpha);
                // Camera callbacks publish the white boost during Render, including on this isolated camera.
                var whiteBoost = squaredAlpha * squaredAlpha * Shader.GetGlobalFloat("_BaseColorBoost")
                    - Shader.GetGlobalFloat("_BaseColorBoostThreshold");
                Assert.That(
                    actual.r,
                    Is.EqualTo(Mathf.Clamp01(nativeTint.r * squaredAlpha + whiteBoost)).Within(0.004f),
                    "Sprite tint was converted to linear twice before entering the native particle color chain.");
                Assert.That(actual.g, Is.EqualTo(Mathf.Clamp01(nativeTint.g * squaredAlpha + whiteBoost)).Within(0.004f));
                Assert.That(actual.b, Is.EqualTo(Mathf.Clamp01(nativeTint.b * squaredAlpha + whiteBoost)).Within(0.004f));
            }
            finally
            {
                Object.DestroyImmediate(lightObject);
                Object.DestroyImmediate(cameraObject);
                if (sprite != null)
                    Object.DestroyImmediate(sprite);

                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(material);
            }
        }
    }
}
