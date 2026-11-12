using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    public class NulctrlDotGlowRenderingTest
    {
        [Test]
        public void StretchedDotUsesTheCapturedNativeGlowProfile()
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Prefabs/MapEditor/Visuals/CM_Note_Dot.prefab"));
            var cameraObject = new GameObject("NULCTRL stretched dot repro");
            var camera = cameraObject.AddComponent<Camera>();
            var referenceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var referenceMaterial = new Material(model.GetComponent<Renderer>().sharedMaterial);
            var target = new RenderTexture(512, 128, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(512, 128, TextureFormat.RGBAFloat, false, true);
            var previousActive = RenderTexture.active;
            var preview = Shader.IsKeywordEnabled("CM_PREVIEW_MODE");
            var fog = Shader.IsKeywordEnabled("BLOOM_FOG");
            try
            {
                // Native NoteCircleGlow at beat 43.329 uses this texture, a half-unit quad, and a 20:3.2 track scale.
                referenceTexture.LoadImage(File.ReadAllBytes(PathUtils.Combine(Application.dataPath,
                    "Tests/Fixtures/NulctrlNativeDotGlow.png")));
                referenceTexture.wrapMode = TextureWrapMode.Clamp;
                referenceMaterial.mainTexture = referenceTexture;
                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = new Vector3(10f, 1.6f, 0.5f);
                model.layer = 30;
                var renderer = model.GetComponent<Renderer>();
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_GlowColor", Color.white);
                properties.SetFloat("_ArrowDissolve", 1f);
                properties.SetFloat("_AnimationSpawned", 1f);
                renderer.SetPropertyBlock(properties);
                camera.enabled = false;
                camera.cullingMask = 1 << 30;
                camera.orthographic = true;
                camera.orthographicSize = 2f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -2f), Quaternion.identity);
                camera.nearClipPlane = 0.01f;
                camera.targetTexture = target;
                Shader.EnableKeyword("CM_PREVIEW_MODE");
                Shader.DisableKeyword("BLOOM_FOG");
                var actual = Render(camera, target, image);
                renderer.sharedMaterial = referenceMaterial;
                var expected = Render(camera, target, image);
                var error = 0f;
                var energy = 0f;
                for (var index = 0; index < actual.Length; index++)
                {
                    error += Mathf.Abs(actual[index].r - expected[index].r);
                    energy += expected[index].r;
                }
                Assert.That(energy, Is.GreaterThan(1f), "The native glow profile must render visibly.");
                Assert.That(error / energy, Is.LessThan(0.03f),
                    "The entire stretched dot must retain the native core and surrounding glow profile.");
            }
            finally
            {
                if (!preview)
                    Shader.DisableKeyword("CM_PREVIEW_MODE");
                if (fog)
                    Shader.EnableKeyword("BLOOM_FOG");
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(referenceMaterial);
                Object.DestroyImmediate(referenceTexture);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
        }

        private static Color[] Render(Camera camera, RenderTexture target, Texture2D image)
        {
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            return image.GetPixels();
        }
    }
}
