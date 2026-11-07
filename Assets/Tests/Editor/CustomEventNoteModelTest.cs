using System.Collections;
using Beatmap.Containers;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class CustomEventNoteModelTest : TestBase
    {
        private GameObject root;
        private GameObject notePrefab;
        private NoteModelSO noteModel;
        private VisualModelSO noteBody;
        private VisualRepositorySO repository;
        private string previousNoteModels;
        private bool previousApplyNoteModel;

        [UnityTest]
        public IEnumerator CustomNoteModelReplacesCustomEventBlock()
        {
            CreateCustomEvent();
            yield return null;

            AssertAppearance(noteBody, false);
        }

        [UnityTest]
        public IEnumerator ToggleAndBuiltInModelsRestoreBlockAndLogo()
        {
            var container = CreateCustomEvent();
            yield return null;

            Settings.ApplyOptionByName(nameof(Settings.ApplyNoteModelToCustomEvents), false);
            AssertAppearance(container.VisualSettings.GetBlockModel(), true);
            Settings.ApplyOptionByName(nameof(Settings.ApplyNoteModelToCustomEvents), true);
            AssertAppearance(noteBody, false);
            foreach (var modelName in new[] { "Standard", "Standard Solid", "Simple", "Simple Solid" })
            {
                Settings.ApplyOptionByName(nameof(Settings.NoteModels), modelName);
                AssertAppearance(container.VisualSettings.GetBlockModel(), true);
            }

            Settings.ApplyOptionByName(nameof(Settings.NoteModels), noteModel.name);
            AssertAppearance(noteBody, false);
        }

        [UnityTest]
        public IEnumerator RepositoryLoadingAndRemovalRefreshModelAndLogo()
        {
            var container = CreateCustomEvent();
            yield return null;

            repository.RemoveNoteModel(noteModel.name);
            AssertAppearance(container.VisualSettings.GetBlockModel(), true);
            repository.Add(noteModel);
            AssertAppearance(noteBody, false);
        }

        private CustomEventContainer CreateCustomEvent()
        {
            previousNoteModels = Settings.Instance.NoteModels;
            previousApplyNoteModel = Settings.Instance.ApplyNoteModelToCustomEvents;
            Settings.ApplyOptionByName(nameof(Settings.ApplyNoteModelToCustomEvents), true);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/MapEditor/Beatmap/Custom Event.prefab");
            root = Object.Instantiate(prefab);
            var container = root.GetComponent<CustomEventContainer>();
            repository = container.VisualSettings.Repository;
            notePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            notePrefab.SetActive(false);
            noteBody = VisualModelSO.Create(notePrefab, "CustomEventNoteModelTest");
            noteBody.Collider = notePrefab.GetComponent<MeshFilter>().sharedMesh;
            noteModel = ScriptableObject.CreateInstance<NoteModelSO>();
            noteModel.name = "CustomEventNoteModelTest";
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
                "Only the original block should display the ChroMapper logo.");
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
                Settings.ApplyOptionByName(nameof(Settings.ApplyNoteModelToCustomEvents), previousApplyNoteModel);
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
