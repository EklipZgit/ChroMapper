using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TestsEditMode
{
    public class OptionsSearchTest
    {
        private Scene scene;

        [SetUp]
        public void SetUp() => scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(scene);

        [Test]
        public void EverySettingsControlHasSearchKeywords()
        {
            var missing = new List<string>();
            foreach (var binder in GetComponents<SettingsBinder>())
            {
                if (!IsOffered(binder.transform))
                    continue;

                var option = binder.GetComponentInParent<SearchableOption>(true);
                if (option == null || option.Keywords == null || !option.Keywords.Any(keyword => !string.IsNullOrWhiteSpace(keyword)))
                    missing.Add(binder.BindedSetting + " (" + binder.name + ")");

            }

            Assert.That(missing, Is.Empty, "Settings controls without searchable keywords: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryStaticOptionIsReachableThroughItsTabSearch()
        {
            var missing = new List<string>();
            var options = GetComponents<SearchableOption>().Where(option => IsOffered(option.transform)).ToArray();
            foreach (var option in options)
            {
                if (option.Keywords == null || option.Keywords.Length == 0)
                    continue;

                var section = option.GetComponentInParent<SearchableSection>(true);
                var tab = option.GetComponentInParent<SearchableTab>(true);
                if (section == null || tab == null)
                {
                    missing.Add(option.name + " (missing section or tab)");
                    continue;
                }

                option.gameObject.SetActive(false);
                tab.UpdateSearch(string.Join(" ", option.Keywords));
                if (!option.gameObject.activeSelf || !section.gameObject.activeSelf)
                    missing.Add(option.name);

            }

            Assert.That(missing, Is.Empty, "Options not reached by tab search: " + string.Join(", ", missing));
        }

        [TestCase("Camera AA Dropdown", "anti aliasing")]
        [TestCase("Invert Wall Bounds Toggle", "wall bounds")]
        [TestCase("Invert Wall Duration Toggle", "wall duration")]
        [TestCase("Chromatic Abberation", "chromatic aberration")]
        public void ExistingOptionsMatchTheirPurpose(string optionName, string query)
        {
            var option = GetComponents<SearchableOption>().Single(item => item.name == optionName);
            Assert.That(option.UpdateSearch(query), Is.True, optionName + " should match " + query);
        }

        [Test]
        public void NewOptionsMatchTheirLabelsAndFeatureNames()
        {
            var queries = new Dictionary<string, string>
            {
                ["AllowSavingChromaGLSFeatures"] = "allow saving chromagls features",
                ["AllowSavingBeatToTheFutureFeatures"] = "allow saving beat to the future features",
                ["EmulateChromaAdvanced"] = "emulate chroma",
                ["VisualizeGLSLightTransitions"] = "gls light transitions",
                ["EnableGLSGhostPreview"] = "gls ghost preview",
                ["DefaultGLSAffectFirst"] = "gls affect first default",
                ["HideNoteSpawnEffect"] = "hide note spawn effect",
                ["ApplyEventModelToCustomEvents"] = "apply event models custom events",
                ["SplitCustomEventsByTrack"] = "split custom events track",
                ["EditModeWorldParticles"] = "edit mode world particles",
                ["EditModeSmoke"] = "edit mode smoke"
            };
            var missing = new List<string>();
            foreach (var binder in GetComponents<SettingsBinder>())
            {
                if (!queries.TryGetValue(binder.BindedSetting, out var query))
                    continue;

                var option = binder.GetComponentInParent<SearchableOption>(true);
                if (option == null || !option.UpdateSearch(query))
                    missing.Add(binder.BindedSetting + " (" + query + ")");

            }

            Assert.That(missing, Is.Empty, "Options missing label keywords: " + string.Join(", ", missing));
        }

        private T[] GetComponents<T>() where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static bool IsOffered(Transform control)
        {
            // Hidden legacy controls must stay hidden when search registers the offered options.
            for (var current = control; current != null; current = current.parent)
            {
                if (current.GetComponent<SearchableSection>() != null)
                    break;

                if (!current.gameObject.activeSelf)
                    return false;

            }

            return true;
        }
    }
}
