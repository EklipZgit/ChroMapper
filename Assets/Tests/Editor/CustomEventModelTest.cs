using System.Collections;
using System.IO;
using Beatmap.Containers;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class CustomEventModelTest : TestBase
    {
        private GameObject root;
        private GameObject notePrefab;
        private NoteModelSO noteModel;
        private VisualModelSO noteBody;
        private VisualRepositorySO repository;
        private string previousNoteModels;
        private string previousEventModels;
        private bool previousApplyEventModel;

        [UnityTest]
        public IEnumerator EventModelPresetsReplaceCustomEventBlock()
        {
            var container = CreateCustomEvent();
            yield return null;

            foreach (var modelName in new[] { "Pyramid", "Flat Pyramid", "Node" })
            {
                Settings.ApplyOptionByName(nameof(Settings.EventModels), modelName);
                AssertAppearance(container.VisualSettings.GetEventModel(), modelName != "Node");
            }
        }

        [UnityTest]
        public IEnumerator ToggleAndBlockModeRestoreBlockAndLogo()
        {
            var container = CreateCustomEvent();
            yield return null;

            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), false);
            AssertAppearance(container.VisualSettings.GetBlockModel(), true);
            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), true);
            AssertAppearance(container.VisualSettings.GetEventModel(), true);
            Settings.ApplyOptionByName(nameof(Settings.EventModels), "Block");
            AssertAppearance(container.VisualSettings.GetBlockModel(), true);
            Settings.ApplyOptionByName(nameof(Settings.EventModels), "Node");
            AssertAppearance(container.VisualSettings.GetEventModel(), false);
        }

        [UnityTest]
        public IEnumerator NoteModelSelectionLoadingAndRemovalDoNotChangeCustomEvents()
        {
            var container = CreateCustomEvent();
            yield return null;

            Settings.ApplyOptionByName(nameof(Settings.NoteModels), "Simple");
            AssertAppearance(container.VisualSettings.GetEventModel(), true);
            Settings.ApplyOptionByName(nameof(Settings.NoteModels), noteModel.name);
            AssertAppearance(container.VisualSettings.GetEventModel(), true);
            repository.RemoveNoteModel(noteModel.name);
            AssertAppearance(container.VisualSettings.GetEventModel(), true);
            repository.Add(noteModel);
            AssertAppearance(container.VisualSettings.GetEventModel(), true);
        }

        [UnityTest]
        public IEnumerator SelectedShapesKeepOriginalCustomEventGrayMaterial()
        {
            var container = CreateCustomEvent();
            yield return null;

            var grayMaterial = container.VisualSettings.GetBlockModel().Prefab
                .GetComponentInChildren<MeshRenderer>().sharedMaterial;
            foreach (var modelName in new[] { "Pyramid", "Flat Pyramid", "Node", "Block", "Pyramid" })
            {
                Settings.ApplyOptionByName(nameof(Settings.EventModels), modelName);
                foreach (var renderer in container.ModelController.Renderers)
                    Assert.AreSame(grayMaterial, renderer.sharedMaterial, modelName);

            }
        }

        [UnityTest]
        public IEnumerator ShapesRenderGrayWithVisiblePyramidBackLogos()
        {
            previousEventModels = Settings.Instance.EventModels;
            previousApplyEventModel = Settings.Instance.ApplyEventModelToCustomEvents;
            previousNoteModels = Settings.Instance.NoteModels;
            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), true);
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "custom-event-gray-lane-outlines",
                "renders");
            foreach (var modelName in new[] { "Pyramid", "Flat Pyramid", "Node", "Block" })
            {
                Settings.ApplyOptionByName(nameof(Settings.EventModels), modelName);
                using var scene = new CustomEventLogoRenderingTest.RenderScene(0, 0);
                scene.ShowCustomEventModel();
                scene.AimCamera(modelName == "Block" ? 45 : 0, 0);
                var pixels = scene.Capture();
                scene.SaveImage(modelName, pixels, directory);
                scene.AimCamera(20, 25);
                scene.SaveImage(modelName + "-angled", scene.Capture(), directory);
                scene.AimCamera(modelName == "Block" ? 45 : 0, 0);
                scene.Logo.gameObject.SetActive(false);
                var body = scene.Capture();
                var bodyPixels = 0;
                var logoPixels = 0;
                foreach (var pixel in body)
                {
                    Assert.LessOrEqual(Mathf.Max(pixel.r, pixel.g, pixel.b) - Mathf.Min(pixel.r, pixel.g, pixel.b), 2);
                    if (pixel.r > 10 && pixel.r < 230)
                        bodyPixels++;

                }

                foreach (var pixel in pixels)
                {
                    if (pixel.r > 240)
                        logoPixels++;

                }

                Assert.Greater(bodyPixels, 100, modelName);
                if (modelName == "Node")
                    Assert.AreEqual(0, logoPixels, modelName);
                else
                    Assert.Greater(logoPixels, 100, modelName);

            }

            yield break;
        }

        [UnityTest]
        public IEnumerator LaneLabelBlackBorderRemainsVisibleOnWhiteBackground()
        {
            using var scene = new CustomEventLogoRenderingTest.RenderScene(0, 0);
            scene.Logo.gameObject.SetActive(false);
            scene.Camera.backgroundColor = Color.white;
            var label = scene.AddLaneLabel();
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "custom-event-gray-lane-outlines",
                "renders");
            var outlined = scene.Capture();
            scene.SaveImage("LaneLabels-outline", outlined, directory);
            var withoutOutline = new Material(label.fontSharedMaterial);
            try
            {
                withoutOutline.SetFloat(ShaderUtilities.ID_OutlineWidth, 0);
                label.fontSharedMaterial = withoutOutline;
                label.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                var reference = scene.Capture();
                scene.SaveImage("LaneLabels-no-outline", reference, directory);
                var borderPixels = 0;
                for (var index = 0; index < outlined.Length; index++)
                {
                    if (outlined[index].r < 64 && reference[index].r > 240)
                        borderPixels++;

                }

                Assert.Greater(borderPixels, 100, "Black outlines must remain visible across bright grid lines.");
            }
            finally
            {
                Object.DestroyImmediate(withoutOutline);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator PyramidBackLogosUseOriginalCubeSurfaceClearance()
        {
            var container = CreateCustomEvent();
            yield return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/MapEditor/Beatmap/Custom Event.prefab");
            var originalLogo = prefab.GetComponentInChildren<SpriteRenderer>(true).transform;
            var clearance = originalLogo.localPosition.y - 0.5f;
            var artworkSize = CalculateLogoArtworkSize(originalLogo.GetComponent<SpriteRenderer>().sprite);
            foreach (var modelName in new[] { "Pyramid", "Flat Pyramid" })
            {
                Settings.ApplyOptionByName(nameof(Settings.EventModels), modelName);
                var logo = root.GetComponentInChildren<SpriteRenderer>(true).transform;
                var mesh = container.ModelController.Actives[0].OutlineMesh.sharedMesh;
                var bounds = mesh.bounds;
                var backVertices = 0;
                foreach (var vertex in mesh.vertices)
                {
                    if (Mathf.Abs(vertex.z - bounds.min.z) < 0.00001f)
                        backVertices++;

                }

                Assert.GreaterOrEqual(backVertices, 3, "The camera-facing back must be a flat face.");
                Assert.IsTrue(logo.gameObject.activeSelf, modelName);
                Assert.That(logo.localPosition.x, Is.EqualTo(bounds.center.x).Within(0.00001f));
                Assert.That(logo.localPosition.y, Is.EqualTo(bounds.center.y).Within(0.00001f));
                Assert.That(logo.localPosition.z, Is.EqualTo(bounds.min.z - clearance).Within(0.00001f));
                Assert.That(Vector3.Dot(logo.forward, Vector3.forward), Is.GreaterThan(0.9999f));
                Assert.That(
                    artworkSize.x * logo.localScale.x,
                    Is.EqualTo(bounds.size.x * 0.94f).Within(0.00001f));
                Assert.That(
                    artworkSize.y * logo.localScale.y,
                    Is.EqualTo(bounds.size.y * 0.94f).Within(0.00001f));
            }

            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), false);
            var restoredLogo = root.GetComponentInChildren<SpriteRenderer>(true).transform;
            Assert.AreEqual(originalLogo.localPosition, restoredLogo.localPosition);
            Assert.AreEqual(originalLogo.localRotation, restoredLogo.localRotation);
            Assert.AreEqual(originalLogo.localScale, restoredLogo.localScale);
        }

        [UnityTest]
        public IEnumerator PyramidLogoRendersWithThreePercentGrayBorderOnEverySide()
        {
            previousEventModels = Settings.Instance.EventModels;
            previousApplyEventModel = Settings.Instance.ApplyEventModelToCustomEvents;
            previousNoteModels = Settings.Instance.NoteModels;
            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), true);
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "custom-event-logo-fill",
                "renders");
            foreach (var modelName in new[] { "Pyramid", "Flat Pyramid" })
            {
                Settings.ApplyOptionByName(nameof(Settings.EventModels), modelName);
                using var scene = new CustomEventLogoRenderingTest.RenderScene(0, 0);
                scene.ShowCustomEventModel();
                scene.AimCamera(0, 0);
                var pixels = scene.Capture();
                scene.SaveImage(modelName + "-front", pixels, directory);
                scene.Logo.gameObject.SetActive(false);
                var body = scene.Capture();
                var width = scene.Camera.targetTexture.width;
                var faceBounds = FindPixelBounds(body, width, 5);
                var iconBounds = FindPixelBounds(pixels, width, 64);
                Assert.Greater(faceBounds.width, 100);
                Assert.Greater(faceBounds.height, 100);
                Assert.That(
                    (iconBounds.xMin - faceBounds.xMin) / faceBounds.width,
                    Is.EqualTo(0.03f).Within(0.01f),
                    modelName + " left");
                Assert.That(
                    (faceBounds.xMax - iconBounds.xMax) / faceBounds.width,
                    Is.EqualTo(0.03f).Within(0.01f),
                    modelName + " right");
                Assert.That(
                    (iconBounds.yMin - faceBounds.yMin) / faceBounds.height,
                    Is.EqualTo(0.03f).Within(0.01f),
                    modelName + " bottom");
                Assert.That(
                    (faceBounds.yMax - iconBounds.yMax) / faceBounds.height,
                    Is.EqualTo(0.03f).Within(0.01f),
                    modelName + " top");
                scene.Logo.gameObject.SetActive(true);
                scene.AimCamera(20, 25);
                scene.SaveImage(modelName + "-angled", scene.Capture(), directory);
            }

            yield break;
        }

        [UnityTest]
        public IEnumerator LogoEdgesRetainBlendedCoverageAtDifferentAngles()
        {
            previousEventModels = Settings.Instance.EventModels;
            previousApplyEventModel = Settings.Instance.ApplyEventModelToCustomEvents;
            previousNoteModels = Settings.Instance.NoteModels;
            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), true);
            Settings.ApplyOptionByName(nameof(Settings.EventModels), "Pyramid");
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "custom-event-logo-aa",
                "renders");
            foreach (var angle in new[] { new Vector2(0, 0), new Vector2(20, 25), new Vector2(55, -17) })
            {
                using var scene = new CustomEventLogoRenderingTest.RenderScene(0, 0);
                scene.ShowCustomEventModel();
                var container = scene.Node.GetComponent<CustomEventContainer>();
                foreach (var renderer in container.ModelController.Renderers)
                    renderer.enabled = false;

                scene.AimCamera(angle.x, angle.y);
                var blackBackground = scene.Capture();
                scene.Camera.backgroundColor = Color.white;
                var whiteBackground = scene.Capture();
                var fractional = 0;
                for (var index = 0; index < blackBackground.Length; index++)
                {
                    var coverage = 1f - (whiteBackground[index].r - blackBackground[index].r) / 255f;
                    if (coverage > 0.05f && coverage < 0.95f)
                        fractional++;

                }

                scene.SaveImage($"pitch{angle.x}-yaw{angle.y}-black", blackBackground, directory);
                scene.SaveImage($"pitch{angle.x}-yaw{angle.y}-white", whiteBackground, directory);
                Assert.Greater(fractional, 100, $"The logo needs blended edge coverage at {angle}.");
            }

            yield break;
        }

        private static Vector2 CalculateLogoArtworkSize(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                var logoTexturePath = PathUtils.Combine(
                    Application.dataPath,
                    "..",
                    AssetDatabase.GetAssetPath(sprite));
                texture.LoadImage(File.ReadAllBytes(logoTexturePath));
                var pixels = texture.GetPixels32();
                var minX = texture.width;
                var minY = texture.height;
                var maxX = -1;
                var maxY = -1;
                for (var index = 0; index < pixels.Length; index++)
                {
                    if (pixels[index].a < 128)
                        continue;

                    minX = Mathf.Min(minX, index % texture.width);
                    maxX = Mathf.Max(maxX, index % texture.width);
                    minY = Mathf.Min(minY, index / texture.width);
                    maxY = Mathf.Max(maxY, index / texture.width);
                }

                return new Vector2(maxX - minX + 1, maxY - minY + 1) / sprite.pixelsPerUnit;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static Rect FindPixelBounds(Color32[] pixels, int width, byte threshold)
        {
            var minX = width;
            var minY = pixels.Length / width;
            var maxX = -1;
            var maxY = -1;
            for (var index = 0; index < pixels.Length; index++)
            {
                var pixel = pixels[index];
                if (pixel.r <= threshold && pixel.g <= threshold && pixel.b <= threshold)
                    continue;

                minX = Mathf.Min(minX, index % width);
                maxX = Mathf.Max(maxX, index % width);
                minY = Mathf.Min(minY, index / width);
                maxY = Mathf.Max(maxY, index / width);
            }

            return Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
        }

        private CustomEventContainer CreateCustomEvent()
        {
            previousNoteModels = Settings.Instance.NoteModels;
            previousEventModels = Settings.Instance.EventModels;
            previousApplyEventModel = Settings.Instance.ApplyEventModelToCustomEvents;
            Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), true);
            Settings.ApplyOptionByName(nameof(Settings.EventModels), "Pyramid");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/MapEditor/Beatmap/Custom Event.prefab");
            root = Object.Instantiate(prefab);
            var container = root.GetComponent<CustomEventContainer>();
            repository = container.VisualSettings.Repository;
            notePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            notePrefab.SetActive(false);
            noteBody = VisualModelSO.Create(notePrefab, "CustomEventModelTest");
            noteBody.Collider = notePrefab.GetComponent<MeshFilter>().sharedMesh;
            noteModel = ScriptableObject.CreateInstance<NoteModelSO>();
            noteModel.name = "CustomEventModelTest";
            noteModel.NoteLeft = noteBody;
            repository.Add(noteModel);
            Settings.ApplyOptionByName(nameof(Settings.NoteModels), noteModel.name);
            container.Setup();
            return container;
        }

        private void AssertAppearance(VisualModelSO model, bool showLogo)
        {
            var controller = root.GetComponentInChildren<VisualModelController>();
            Assert.AreEqual(
                model.Name,
                controller.Actives[0].Name,
                "Custom events should use the selected model.");
            Assert.AreEqual(
                showLogo,
                root.GetComponentInChildren<SpriteRenderer>(true).gameObject.activeSelf,
                "The block and flat-backed pyramids should display the ChroMapper logo.");
            Assert.AreSame(model.Collider, root.GetComponentInChildren<IntersectionCollider>().Mesh);
            Assert.AreSame(
                controller.Actives[0].OutlineMesh.sharedMesh,
                root.GetComponentInChildren<VisualOutlineController>().MeshFilter.sharedMesh);
        }

        protected override void CleanupTestObjects()
        {
            if (root != null)
                Object.DestroyImmediate(root);

            if (repository != null && noteModel != null)
                repository.RemoveNoteModel(noteModel.name);

            if (previousNoteModels != null)
            {
                Settings.ApplyOptionByName(nameof(Settings.NoteModels), previousNoteModels);
                Settings.ApplyOptionByName(nameof(Settings.EventModels), previousEventModels);
                Settings.ApplyOptionByName(nameof(Settings.ApplyEventModelToCustomEvents), previousApplyEventModel);
            }

            if (noteModel != null)
                Object.DestroyImmediate(noteModel);

            if (noteBody != null)
                Object.DestroyImmediate(noteBody);

            if (notePrefab != null)
                Object.DestroyImmediate(notePrefab);
        }
    }
}
