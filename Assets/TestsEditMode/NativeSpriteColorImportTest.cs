using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TestsEditMode
{
    public class NativeSpriteColorImportTest
    {
        private static EnvironmentData LoadInterscope()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/__Scenes/Environments/Data/InterscopeEnvironment.json");
            return CreateUtils.JsonToEnvironmentData(asset);
        }

        [Test]
        public void ExportContainsNativeDecalTint()
        {
            var data = LoadInterscope();
            var logo = data.Objects.Single(item => item.ChromaID == "InterscopeEnvironment.[0]Environment.[11]Logo");
            Assert.That(logo.Components.SpriteRenderer[0].Color, Is.EqualTo(new Color(0f, 0f, 0f, 178f / 255f)));
        }

        [Test]
        public void ExplicitExportTintIsImportedVerbatim()
        {
            var asset = new TextAsset(@"{""environmentData"":{""environmentID"":""InterscopeEnvironment""},
                ""objects"":[{""id"":""InterscopeEnvironment.[0]Environment.[11]Logo"",
                ""components"":{""SpriteRenderer"":[{""color"":[1,0,0,1]}]}}]}");
            try
            {
                var data = CreateUtils.JsonToEnvironmentData(asset);
                Assert.That(data.Objects[0].Components.SpriteRenderer[0].Color, Is.EqualTo(Color.red));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
