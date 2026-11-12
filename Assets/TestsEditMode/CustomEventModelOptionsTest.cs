using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace TestsEditMode
{
    public class CustomEventModelOptionsTest
    {
        [Test]
        public void LocalizedToggleFollowsEventModelsAndDefaultsToEnabled()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");
            try
            {
                var binders = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SettingsBinder>(true)).ToArray();
                var toggle = binders.SingleOrDefault(binder => binder.BindedSetting == nameof(Settings.ApplyEventModelToCustomEvents));
                Assert.IsNotNull(toggle, "The graphics panel must expose Apply event models to Custom Events.");
                var eventModels = binders.Single(binder => binder.BindedSetting == nameof(Settings.EventModels));
                Assert.AreSame(eventModels.transform.parent.parent, toggle.transform.parent);
                Assert.AreEqual(eventModels.transform.parent.GetSiblingIndex() + 1, toggle.transform.GetSiblingIndex());
                Assert.IsNotNull(toggle.GetComponent<BetterToggle>());
                Assert.IsTrue(new Settings().ApplyEventModelToCustomEvents);

                var label = toggle.GetComponentInChildren<LocalizeStringEvent>(true).StringReference;
                foreach (var guid in AssetDatabase.FindAssets("t:StringTable", new[] { "Assets/Locales" }))
                {
                    var table = AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guid));
                    if (table.SharedData.TableCollectionNameGuid != label.TableReference.TableCollectionNameGuid)
                        continue;

                    var entry = table.GetEntry(label.TableEntryReference.KeyId);
                    Assert.IsNotNull(entry, $"Missing option label in {table.LocaleIdentifier.Code}.");
                    Assert.IsNotEmpty(entry.Value);
                    if (table.LocaleIdentifier.Code == "en")
                        Assert.AreEqual("Apply event models to Custom Events", entry.Value);

                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
