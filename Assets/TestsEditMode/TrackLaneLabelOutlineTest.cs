using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TestsEditMode
{
    public class TrackLaneLabelOutlineTest
    {
        [TestCase("Assets/_Prefabs/MapEditor/Events Label.prefab")]
        [TestCase("Assets/_Prefabs/MapEditor/Custom Events Label.prefab")]
        public void LaneLabelPrefabsUseThinBlackOutline(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
                AssertThinBlackOutline(text);

        }

        [Test]
        public void MapperLaneLabelTemplatesUseThinBlackOutline()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/03_Mapper.unity");
            try
            {
                var templateNames = new[]
                {
                    "GLS Event Label", "GLS Group Label", "Rotation Event Label", "BPM Event Label", "NJS Event Label"
                };
                var labels = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                    .Where(text => templateNames.Contains(text.transform.parent.name)).ToArray();
                Assert.AreEqual(6, labels.Length);
                foreach (var label in labels)
                    AssertThinBlackOutline(label);

            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void AssertThinBlackOutline(TMP_Text text)
        {
            var bpmMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Graphics/Materials/Font/Teko 3D Outline BPM.mat");
            var material = text.fontSharedMaterial;
            var width = material.GetFloat(ShaderUtilities.ID_OutlineWidth);
            Assert.Greater(width, 0, text.name);
            Assert.LessOrEqual(width, bpmMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth), text.name);
            Assert.AreEqual(Color.black, material.GetColor(ShaderUtilities.ID_OutlineColor), text.name);
            Assert.AreSame(bpmMaterial.mainTexture, text.font.atlasTexture);
        }
    }
}
