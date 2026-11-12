using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TestsEditMode
{
    public class NativeMaterialColorPropertyTest
    {
        [Test]
        public void ExportedColorInputsRetainNativeColorSpaceConversion()
        {
            var library = AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(
                "Assets/Editor/Environments/EnvironmentLibrarySO.asset");
            var path = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "NativeMaterialColorProperties.json");
            var bindings = JSON.Parse(File.ReadAllText(path));
            var mismatches = new List<string>();
            foreach (var binding in bindings.Children)
            {
                var shader = library.ShaderLookup[binding["shader"].Value];
                var property = binding["property"].Value;
                var index = shader.FindPropertyIndex(property);
                Assert.That(index, Is.GreaterThanOrEqualTo(0), shader.name + " " + property);
                if (shader.GetPropertyType(index) != ShaderPropertyType.Color)
                    mismatches.Add(shader.name + " " + property);
            }

            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }
    }
}
