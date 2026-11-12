using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class RainHeightFadeParityTest
    {
        [TestCase(1f)]
        [TestCase(5f)]
        public void HeightFadeUsesLocalHeightPlusTranslation(float scaleY)
        {
            var surface = new GameObject("Scaled rain fade repro");
            var cameraObject = new GameObject("Rain fade camera");
            var material = new Material(Shader.Find("ChroMapper/Rain"));
            var mesh = new Mesh();
            var target = new RenderTexture(32, 32, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(32, 32, TextureFormat.RGBAFloat, false, true);
            var previousActive = RenderTexture.active;
            try
            {
                surface.layer = 30;
                surface.transform.position = new Vector3(0f, 12f, 4f);
                surface.transform.localScale = new Vector3(1f, scaleY, 1f);
                mesh.vertices = new[]
                {
                    new Vector3(-1f, -2.005f, 1f), new Vector3(1f, -2.005f, 1f),
                    new Vector3(1f, -1.995f, 1f), new Vector3(-1f, -1.995f, 1f)
                };
                mesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };
                mesh.normals = new[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
                mesh.colors = new[] { Color.black.WithAlpha(1f), Color.black.WithAlpha(1f), Color.black.WithAlpha(1f), Color.black.WithAlpha(1f) };
                mesh.uv = new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
                surface.AddComponent<MeshFilter>().sharedMesh = mesh;
                surface.AddComponent<MeshRenderer>().sharedMaterial = material;
                material.shaderKeywords = new[] { "VERTEX_COLOR", "VERTEX_SQUARE_ALPHA", "TEXTURE_COLOR", "_ALPHACHANNEL_RED", "_FOGTYPE_COLOR" };
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
                material.SetVector("_Color", Vector4.one);
                material.SetFloat("_Intensity", 1f);
                material.SetFloat("_AlphaMultiplier", 1f);
                material.SetFloat("_AlphaFromFog", 0f);
                material.SetFloat("_BottomEnd", -5f);
                material.SetFloat("_BottomFadeScale", 8.43f);
                material.SetFloat("_TopEnd", 20f);
                material.SetFloat("_TopFadeScale", 20f);
                material.SetFloat("_BlendModeSrc", 1f);
                material.SetFloat("_BlendModeDst", 0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0f, 12f - 2f * scaleY, 0f);
                camera.orthographic = true;
                camera.orthographicSize = 0.004f * scaleY;
                camera.cullingMask = 1 << 30;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 32, 32), 0, 0);
                image.Apply();
                // Native Rain vertex blob 90 fades local Y + translation Y, without scaling Y.
                // At -2 + 12 the bottom ramp is one and the top ramp is 0.5.
                Assert.That(image.GetPixel(16, 16).r, Is.EqualTo(0.7f * 0.5f * 0.5f).Within(0.002f));
            }
            finally
            {
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(surface);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(mesh);
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
