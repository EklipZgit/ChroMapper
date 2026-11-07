using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class MirrorEditorOverlayTest
    {
        [UnityTest]
        public IEnumerator ReflectionOmitsEditorOverlaysWithoutHidingWorldObjects()
        {
            var previousQuality = Settings.Instance.MirrorQuality;
            Settings.Instance.MirrorQuality = (int)MirrorRendererSO.MirrorQuality.Low;
            var mirror = ScriptableObject.CreateInstance<MirrorRendererSO>();
            var root = new GameObject("Mirror overlay fixture");
            var source = new GameObject("Mirror overlay camera", typeof(Camera));
            var camera = source.GetComponent<Camera>();
            var materials = new Material[3];
            var offset = new Vector3(5000f, 5000f, 5000f);
            try
            {
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = LayerMask.GetMask("Beatmap Object", "Mapping", "Editor Piece");
                camera.farClipPlane = 30f;
                camera.transform.position = offset + new Vector3(0f, 0f, -8f);
                var layers = new[] { "Mapping", "Beatmap Object", "Editor Piece" };
                var colors = new[] { Color.red, Color.green, Color.blue };
                for (var i = 0; i < layers.Length; i++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(root.transform);
                    cube.transform.position = offset + new Vector3((i - 1) * 1.5f, 0f, -3f);
                    cube.layer = LayerMask.NameToLayer(layers[i]);
                    materials[i] = new Material(Shader.Find("Unlit/Color")) { color = colors[i] };
                    cube.GetComponent<Renderer>().sharedMaterial = materials[i];
                }

                yield return null;
                var originalMask = camera.cullingMask;
                var reflected = mirror.RenderMirrorTexture(camera, offset, Vector3.back) as RenderTexture;
                Assert.That(reflected, Is.Not.Null);
                var counts = CountColors(reflected);
                Assert.That(counts.Green, Is.GreaterThan(20), "The world-object control did not reflect.");
                Assert.That(counts.Red, Is.Zero, "The mapping grid reflected.");
                Assert.That(counts.Blue, Is.Zero, "Editor pieces reflected.");
                Assert.That(camera.cullingMask, Is.EqualTo(originalMask));
                var mirrorMarker = System.Array.Find(
                    Resources.FindObjectsOfTypeAll<MirrorCamera>(),
                    candidate => candidate.GetComponent<Camera>().targetTexture == reflected);
                Assert.That(mirrorMarker, Is.Not.Null);
                var mirrorCamera = mirrorMarker.GetComponent<Camera>();
                Assert.That(mirrorCamera.cullingMask & LayerMask.GetMask("UI", "BGUI"), Is.Zero);

                var direct = new RenderTexture(256, 256, 24);
                try
                {
                    camera.targetTexture = direct;
                    camera.Render();
                    counts = CountColors(direct);
                    Assert.That(counts.Red, Is.GreaterThan(20), "The main camera lost the grid.");
                    Assert.That(counts.Blue, Is.GreaterThan(20), "The main camera lost editor pieces.");
                }
                finally
                {
                    camera.targetTexture = null;
                    direct.Release();
                    Object.DestroyImmediate(direct);
                }
            }
            finally
            {
                Object.DestroyImmediate(mirror);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(root);
                foreach (var material in materials)
                {
                    if (material != null)
                        Object.DestroyImmediate(material);
                }

                Settings.Instance.MirrorQuality = previousQuality;
            }
        }

        [Test]
        public void EditorNodeModelsAndLabelsUseTheNonReflectedLayerWithoutChangingPicking()
        {
            var names = new[]
            {
                "Event", "GLS Event", "GLS Group", "Custom Event", "BPM Change",
                "Rotation Event", "NJS Event", "Laser Speed"
            };
            foreach (var name in names)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Prefabs/MapEditor/Beatmap/{name}.prefab");
                var node = Object.Instantiate(prefab);
                try
                {
                    foreach (var model in node.GetComponentsInChildren<VisualModelController>(true))
                    {
                        model.Set(PrimitiveType.Cube);
                        model.Set(PrimitiveType.Sphere);
                        model.Set(PrimitiveType.Cube);
                    }

                    foreach (var renderer in node.GetComponentsInChildren<Renderer>(true))
                    {
                        Assert.That(
                            renderer.gameObject.layer,
                            Is.EqualTo(LayerMask.NameToLayer("Editor Piece")),
                            $"{name}/{renderer.name} can enter a mirror draw.");
                    }

                    foreach (var renderer in node.GetComponentsInChildren<CanvasRenderer>(true))
                    {
                        Assert.That(
                            renderer.gameObject.layer,
                            Is.EqualTo(LayerMask.NameToLayer("Editor Piece")),
                            $"{name}/{renderer.name} can enter a mirror canvas draw.");
                    }

                    node.SetActive(false);
                    node.SetActive(true);
                    foreach (var collider in node.GetComponentsInChildren<IntersectionCollider>(true))
                    {
                        Assert.That(
                            collider.CollisionLayer,
                            Is.EqualTo(LayerMask.NameToLayer("Beatmap Object")),
                            $"{name} lost its picking layer after pooling.");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(node);
                }
            }
        }

        private static (int Red, int Green, int Blue) CountColors(RenderTexture texture)
        {
            var previous = RenderTexture.active;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                image.Apply();
                var counts = (Red: 0, Green: 0, Blue: 0);
                foreach (var pixel in image.GetPixels32())
                {
                    if (pixel.r > 30 && pixel.r > pixel.g * 2 && pixel.r > pixel.b * 2)
                        counts.Red++;
                    if (pixel.g > 30 && pixel.g > pixel.r * 2 && pixel.g > pixel.b * 2)
                        counts.Green++;
                    if (pixel.b > 30 && pixel.b > pixel.r * 2 && pixel.b > pixel.g * 2)
                        counts.Blue++;
                }

                return counts;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
            }
        }
    }
}
