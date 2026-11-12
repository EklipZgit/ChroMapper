using System.Collections;
using System.IO;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SmokeDepthIntersectionTest : TestBase
    {
        [UnityTest]
        public IEnumerator SmokeFadesAgainstEnvironmentGeometryLikeTheDepthWritingControl()
        {
            yield return CheckIntersections(1);
        }

        [UnityTest]
        public IEnumerator SmokeDepthFadeWorksWithFourSampleAntialiasing()
        {
            yield return CheckIntersections(4);
        }

        [UnityTest]
        public IEnumerator SmokeDepthFollowsVertexDisplacement()
        {
            yield return CheckIntersections(1, -0.25f);
        }

        [UnityTest]
        public IEnumerator DissolvedGeometryDoesNotHideSmoke()
        {
            yield return CheckIntersections(1, dissolve: true);
        }

        [UnityTest]
        public IEnumerator NativeUnlitStandardDoesNotInventSmokeDepth()
        {
            yield return CheckIntersections(1, shaderName: "ChroMapper/Glowing", writesSceneDepth: false);
        }

        private IEnumerator CheckIntersections(int antiAliasing, float displacement = 0f, bool dissolve = false,
            string shaderName = "ChroMapper/Lit", bool writesSceneDepth = true)
        {
            var root = new GameObject("Smoke depth intersection repro");
            var cameraObject = new GameObject("Smoke depth camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 60f;
            camera.aspect = 1.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.cullingMask = 1 << 31;
            camera.depthTextureMode = DepthTextureMode.Depth;
            camera.allowMSAA = antiAliasing > 1;
            var smoke = GameObject.CreatePrimitive(PrimitiveType.Quad);
            smoke.name = "Smoke plane";
            smoke.transform.SetParent(root.transform);
            smoke.transform.localScale = new Vector3(14f, 8f, 1f);
            smoke.layer = 31;
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "Intersecting opaque plane";
            surface.transform.SetParent(root.transform);
            surface.transform.rotation = Quaternion.Euler(-20f, 45f, 0f);
            surface.transform.localScale = new Vector3(50f, 30f, 1f);
            surface.layer = 31;
            surface.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var source = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Environments/BigSmokeParticle (Instance).mat");
            var smokeMaterial = new Material(source);
            smokeMaterial.shaderKeywords = new[] { "SOFT_PARTICLES", "VERTEX_COLOR", "MAIN_TEXTURE", "_ALPHACHANNEL_RED" };
            smokeMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
            smokeMaterial.SetColor("_Color", Color.white);
            var controlMaterial = new Material(Shader.Find("Standard"));
            controlMaterial.color = Color.black;
            var missingDepthMaterial = new Material(Shader.Find("Unlit/Color"));
            missingDepthMaterial.color = Color.black;
            var environmentMaterial = new Material(Shader.Find(shaderName));
            if (shaderName == "ChroMapper/Glowing")
            {
                environmentMaterial.SetFloat("_FogStartOffset", float.PositiveInfinity);
            }
            environmentMaterial.SetColor("_Color", Color.black);
            if (environmentMaterial.HasProperty("_ZWrite"))
            {
                environmentMaterial.SetFloat("_ZWrite", 1f);
            }
            var mesh = Object.Instantiate(surface.GetComponent<MeshFilter>().sharedMesh);
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            surface.GetComponent<MeshFilter>().sharedMesh = mesh;
            if (displacement != 0f)
            {
                environmentMaterial.EnableKeyword("_VERTEXMODE_DISPLACEMENT");
                environmentMaterial.SetFloat("_DisplacementStrength", displacement);
            }
            if (dissolve)
            {
                environmentMaterial.EnableKeyword("DISSOLVE");
                environmentMaterial.SetVector("_DissolveAxisVector", Vector3.up);
                environmentMaterial.SetFloat("_DissolveOffset", 0f);
                environmentMaterial.SetFloat("_DissolveReverse", 0f);
            }
            var previousDepthKeyword = Shader.IsKeywordEnabled("DEPTH_TEXTURE");
            smoke.GetComponent<Renderer>().sharedMaterial = smokeMaterial;
            try
            {
                Shader.EnableKeyword("DEPTH_TEXTURE");
                var directory = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "SmokeDepthIntersection");
                Directory.CreateDirectory(directory);
                surface.GetComponent<Renderer>().sharedMaterial = controlMaterial;
                surface.transform.position = surface.transform.TransformVector(Vector3.back * displacement);
                var control = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, $"control-{antiAliasing}-{displacement}-{dissolve}.png"), 384, 256, antiAliasing);
                surface.transform.position = Vector3.zero;
                surface.GetComponent<Renderer>().sharedMaterial = environmentMaterial;
                var actual = LinearRenderCapture.Capture(camera, PathUtils.Combine(directory, $"environment-{antiAliasing}-{displacement}-{dissolve}.png"), 384, 256, antiAliasing);
                var sampleY = dissolve ? 0.5f : 0f;
                surface.GetComponent<Renderer>().sharedMaterial = missingDepthMaterial;
                var withoutDepthPass = LinearRenderCapture.Capture(camera,
                    PathUtils.Combine(directory, $"missing-pass-{antiAliasing}-{displacement}-{dissolve}.png"), 384, 256, antiAliasing);
                Assert.That(Sample(camera, withoutDepthPass, -0.5f, sampleY), Is.GreaterThan(0.9f),
                    "An opaque shader without a depth pass must reproduce the original hard intersection.");
                surface.GetComponent<Renderer>().sharedMaterial = environmentMaterial;
                var nearControl = Sample(camera, control, -0.5f, sampleY);
                var middleControl = Sample(camera, control, -2f, sampleY);
                var farControl = Sample(camera, control, -5f, sampleY);
                Assert.That(nearControl, Is.InRange(0.01f, 0.25f), "The control must show a soft edge.");
                Assert.That(middleControl, Is.InRange(0.3f, 0.7f));
                Assert.That(farControl, Is.GreaterThan(0.9f));
                if (writesSceneDepth)
                {
                    Assert.That(Sample(camera, actual, -0.5f, sampleY), Is.EqualTo(nearControl).Within(0.025f),
                        "Environment geometry must participate in the smoke's scene-depth fade.");
                    Assert.That(Sample(camera, actual, -2f, sampleY), Is.EqualTo(middleControl).Within(0.025f));
                    if (dissolve)
                    {
                        Assert.That(Sample(camera, actual, -0.5f, -1f), Is.GreaterThan(0.9f),
                            "The dissolved half must leave the smoke visible instead of writing invisible depth.");
                    }
                    else
                    {
                        Assert.That(Sample(camera, actual, -0.5f, -1f), Is.EqualTo(Sample(camera, control, -0.5f, -1f)).Within(0.025f),
                            "Depth texture orientation must match the lower part of the rendered surface.");
                    }
                    Assert.That(Sample(camera, actual, -0.5f, 1f), Is.EqualTo(Sample(camera, control, -0.5f, 1f)).Within(0.025f));
                }
                else
                {
                    Assert.That(Sample(camera, actual, -0.5f, sampleY), Is.GreaterThan(0.9f),
                        "The native Glowing route is absent from the captured game depth texture too.");
                    Assert.That(Sample(camera, actual, -2f, sampleY), Is.GreaterThan(0.9f));
                }
                Assert.That(Sample(camera, actual, 0.5f, sampleY), Is.LessThan(0.001f), "Foreground geometry must still occlude smoke.");
                if (environmentMaterial.HasProperty("_ZWrite"))
                {
                    environmentMaterial.SetFloat("_ZWrite", 0f);
                    var withoutDepthWrites = LinearRenderCapture.Capture(camera,
                        PathUtils.Combine(directory, $"no-depth-{antiAliasing}-{displacement}-{dissolve}.png"), 384, 256, antiAliasing);
                    Assert.That(Sample(camera, withoutDepthWrites, -0.5f, sampleY), Is.GreaterThan(0.9f),
                        "Materials with depth writes disabled must not introduce an invisible smoke occluder.");
                    Assert.That(Sample(camera, withoutDepthWrites, 0.5f, sampleY), Is.GreaterThan(0.9f));
                }
            }
            finally
            {
                if (!previousDepthKeyword)
                {
                    Shader.DisableKeyword("DEPTH_TEXTURE");
                }
                Object.Destroy(root);
                Object.Destroy(cameraObject);
                Object.Destroy(smokeMaterial);
                Object.Destroy(controlMaterial);
                Object.Destroy(environmentMaterial);
                Object.Destroy(missingDepthMaterial);
                Object.Destroy(mesh);
            }
            yield return null;
        }

        private static float Sample(Camera camera, Color[] pixels, float worldX, float worldY = 0f)
        {
            var viewport = camera.WorldToViewportPoint(new Vector3(worldX, worldY, 0f));
            var x = Mathf.RoundToInt(viewport.x * 384f);
            var y = Mathf.RoundToInt(viewport.y * 256f);
            var sum = 0f;
            for (var row = y - 1; row <= y + 1; row++)
            {
                for (var column = x - 1; column <= x + 1; column++)
                {
                    sum += pixels[row * 384 + column].r;
                }
            }
            return sum / 9f;
        }
    }
}
