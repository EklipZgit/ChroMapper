using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor
{
    // Box edits replace the parent group and refresh its distribution arrays. Keep unchanged rows alive so
    // typing into a text box does not lose focus.
    public class GLSColorDistributionRowRefreshTest
    {
        private const string RowPrefabPath = "Assets/_Prefabs/UI/GLS Color Distribution Row.prefab";

        private static readonly FieldInfo RowTemplateField =
            typeof(GLSColorDistributionArrayViewController)
                .GetField("rowTemplate", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ListRootField =
            typeof(GLSColorDistributionArrayViewController)
                .GetField("listRoot", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo RowsField =
            typeof(GLSColorDistributionArrayViewController)
                .GetField("rows", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo InputFieldField =
            typeof(TextBoxNumberComponent<float>)
                .GetField("InputField", BindingFlags.NonPublic | BindingFlags.Instance);

        // Build the panel's prefab wiring with an inactive row template under the list root.
        private static GLSColorDistributionArrayViewController CreateController(GameObject root)
        {
            var listRoot = new GameObject("ListRoot", typeof(RectTransform));
            listRoot.transform.SetParent(root.transform);
            var template = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath),
                listRoot.transform);
            template.SetActive(false);

            var controller = root.AddComponent<GLSColorDistributionArrayViewController>();
            RowTemplateField.SetValue(controller, template.GetComponent<GLSColorDistributionRowView>());
            ListRootField.SetValue(controller, listRoot.transform);
            return controller;
        }

        private static List<GLSColorDistributionRowView> Rows(GLSColorDistributionArrayViewController controller) =>
            (List<GLSColorDistributionRowView>)RowsField.GetValue(controller);

        private static TMPro.TMP_InputField InputField(GLSColorDistributionRowView row) =>
            (TMPro.TMP_InputField)InputFieldField.GetValue(row.ValueInput);

        [Test]
        public void SetColorDistributionsPreservesUnchangedRowsAndFocusedInput()
        {
            var root = new GameObject("DistributionArrayView");
            try
            {
                var controller = CreateController(root);
                var notifications = new List<string[]>();
                controller.Initialize(notifications.Add);

                controller.SetColorDistributions(new[] { "h,0.5,L", "s,0.2,IO^2,l" });
                var first = Rows(controller)[0];
                var second = Rows(controller)[1];
                // TMP focus activation is deferred; checking the surviving input component also works when
                // EditMode cannot activate focus.
                var firstInput = InputField(first);
                firstInput.ActivateInputField();
                var engagedFocus = firstInput.isFocused;
                // Leave unfinished text in the input. An unchanged row must keep it instead of replacing it
                // with the saved value, 180.
                firstInput.SetTextWithoutNotify("180.");
                Assert.That(firstInput.text, Is.EqualTo("180."));

                controller.SetColorDistributions(new[] { "h,0.5,L", "v,0.9,I^3" });

                Assert.That(Rows(controller)[0], Is.SameAs(first));
                Assert.That(Rows(controller)[1], Is.SameAs(second));
                Assert.That(InputField(Rows(controller)[0]).text, Is.EqualTo("180."));
                Assert.That(first.GetDistribution(), Is.EqualTo("h,0.5,L"));
                Assert.That(second.GetDistribution(), Is.EqualTo("v,0.9,I^3"));
                Assert.That(InputField(first), Is.SameAs(firstInput));
                if (engagedFocus)
                {
                    Assert.That(firstInput.isFocused, Is.True);
                }

                CollectionAssert.AreEqual(
                    new[] { "h,0.5,L", "v,0.9,I^3" },
                    controller.GetColorDistributions());
                Assert.That(notifications, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetColorDistributionsPreservesCommonPrefixWhenLengthChanges()
        {
            var root = new GameObject("DistributionArrayView");
            try
            {
                var controller = CreateController(root);
                var notifications = new List<string[]>();
                controller.Initialize(notifications.Add);

                controller.SetColorDistributions(new[] { "h,0.5,L", "s,0.2,L" });
                var first = Rows(controller)[0];
                var second = Rows(controller)[1];

                controller.SetColorDistributions(new[] { "h,0.5,L", "s,0.2,L", "f,0.7,N" });
                Assert.That(Rows(controller), Has.Count.EqualTo(3));
                Assert.That(Rows(controller)[0], Is.SameAs(first));
                Assert.That(Rows(controller)[1], Is.SameAs(second));
                Assert.That(Rows(controller)[2].GetDistribution(), Is.EqualTo("f,0.7,N"));

                controller.SetColorDistributions(new[] { "h,0.5,L" });
                Assert.That(Rows(controller), Has.Count.EqualTo(1));
                Assert.That(Rows(controller)[0], Is.SameAs(first));

                controller.SetColorDistributions(null);
                Assert.That(Rows(controller), Is.Empty);
                CollectionAssert.AreEqual(new string[0], controller.GetColorDistributions());
                Assert.That(notifications, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
