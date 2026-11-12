using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace TestsEditMode
{
    public class CustomEventOptionsTest
    {
        [Test]
        public void MappingToggleDefaultsOnAndHasLocalizedLabelAndTooltip()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");
            try
            {
                var binders = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SettingsBinder>(true)).ToArray();
                var binder = binders.Single(item => item.BindedSetting == nameof(Settings.SplitCustomEventsByTrack));
                Assert.That(new Settings().SplitCustomEventsByTrack, Is.True);
                Assert.That(
                    binder.transform.parent,
                    Is.EqualTo(binders.Single(item => item.BindedSetting == nameof(Settings.Animations)).transform.parent));
                var label = binder.GetComponentInChildren<LocalizeStringEvent>(true).StringReference;
                var tooltip = binder.GetComponent<Tooltip>().LocalizedTooltip;
                var localeCount = 0;
                foreach (var guid in AssetDatabase.FindAssets("t:StringTable", new[] { "Assets/Locales" }))
                {
                    var table = AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guid));
                    if (table.SharedData.TableCollectionNameGuid != label.TableReference.TableCollectionNameGuid)
                        continue;

                    localeCount++;
                    Assert.That(
                        table.GetEntry(label.TableEntryReference.KeyId)?.Value,
                        Is.Not.Null.And.Not.Empty,
                        table.LocaleIdentifier.Code);
                    Assert.That(
                        table.GetEntry(tooltip.TableEntryReference.KeyId)?.Value,
                        Is.Not.Null.And.Not.Empty,
                        table.LocaleIdentifier.Code);
                    if (table.LocaleIdentifier.Code == "en")
                    {
                        Assert.That(
                            table.GetEntry(label.TableEntryReference.KeyId).Value,
                            Is.EqualTo("Split custom events out to lanes per track"));
                    }
                }

                Assert.That(localeCount, Is.EqualTo(15));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
