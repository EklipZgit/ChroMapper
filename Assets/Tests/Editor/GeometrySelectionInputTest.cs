using System.Collections;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class GeometrySelectionInputTest : InputTestBase
    {
        private bool previousNodeEditorKeybind;
        private bool previousAnimations;
        private CMInput isolatedInput;
        private NodeEditorController nodeEditor;

        protected override IEnumerator OnMapLoaded()
        {
            previousNodeEditorKeybind = Settings.Instance.NodeEditor_UseKeybind;
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator NonCollidableGeometryCanBeSelectedAndNodeEdited() => CheckSelection(false);

        [UnityTest]
        public IEnumerator CollidableGeometryCanBeSelectedAndNodeEdited() => CheckSelection(true);

        private IEnumerator CheckSelection(bool collision)
        {
            yield return InitializeGeometry(collision);
            var collection = Object.FindAnyObjectByType<GeometryGridContainer>();
            var data = collection.MapObjects.Single();
            var geometry = (GeometryContainer)collection.LoadedContainers[data];
            ClickGeometry(geometry);
            Assert.That(SelectionController.IsObjectSelected(data), Is.True, "Shift-click did not select the geometry.");
            Assert.That(geometry.Selected, Is.True, "Selected geometry did not enable its outline.");

            OpenNodeEditor();
            var input = nodeEditor.GetComponentInChildren<TMP_InputField>(true);
            var json = JSON.Parse(input.text);
            Assert.That(json["geometry"]["type"].Value, Is.EqualTo("Cube"));
            var position = geometry.Animator.LocalTarget.position + Vector3.right * 2f;
            json["position"] = new JSONArray();
            json["position"].AsArray.Add(position.x);
            json["position"].AsArray.Add(position.y);
            json["position"].AsArray.Add(position.z);
            nodeEditor.NodeEditor_EndEdit(json.ToString());
            yield return null;
            var edited = collection.MapObjects.Single();
            var editedGeometry = (GeometryContainer)collection.LoadedContainers[edited];
            Assert.That(Vector3.Distance(editedGeometry.Animator.LocalTarget.position, position), Is.LessThan(0.001f));
            Assert.That(SelectionController.IsObjectSelected(edited), Is.True);
            Assert.That(
                editedGeometry.GetComponentsInChildren<Collider>().Length,
                Is.EqualTo(collision ? 1 : 0),
                "Editor picking must not change the map's physical light-collision setting.");
            nodeEditor.Close();
            SelectionController.DeselectAll();
            ClickGeometry(editedGeometry);
            Assert.That(SelectionController.IsObjectSelected(edited), Is.True, "The node-edited geometry is no longer selectable.");
        }

        [UnityTest]
        public IEnumerator NodeEditingThePrimitiveUpdatesItsSelectionMesh()
        {
            yield return InitializeGeometry(false);
            var collection = Object.FindAnyObjectByType<GeometryGridContainer>();
            var data = collection.MapObjects.Single();
            SelectionController.Select(data);
            OpenNodeEditor();
            var input = nodeEditor.GetComponentInChildren<TMP_InputField>(true);
            var json = JSON.Parse(input.text);
            json["geometry"]["type"] = "Triangle";
            nodeEditor.NodeEditor_EndEdit(json.ToString());
            yield return null;
            var geometry = (GeometryContainer)collection.LoadedContainers[collection.MapObjects.Single()];
            var mesh = geometry.MpbController.Renderers[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount, Is.EqualTo(3));
            Assert.That(
                geometry.SelectionMpbController.Renderers[0].GetComponent<MeshFilter>().sharedMesh,
                Is.SameAs(mesh),
                "The selection outline must use the node-edited primitive's mesh.");
            nodeEditor.Close();
            SelectionController.DeselectAll();
            ClickGeometry(geometry);
            Assert.That(SelectionController.IsObjectSelected(geometry.ObjectData), Is.True);
        }

        private IEnumerator InitializeGeometry(bool collision)
        {
            Settings.Instance.NodeEditor_UseKeybind = true;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            nodeEditor.Close();
            var cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Editing);
            var camera = cameras.SelectedCameraController.Camera;
            var data = new BaseEnvironmentEnhancement
            {
                Geometry = new JSONObject
                {
                    ["type"] = "Cube",
                    ["material"] = "standard",
                    ["collision"] = collision
                },
                Position = camera.transform.position + camera.transform.forward * 8f,
                Rotation = camera.transform.eulerAngles
            };
            Object.FindAnyObjectByType<GeometryGridContainer>().SpawnObject(
                data,
                removeConflicting: false,
                refreshesPool: false);
            yield return null;
            InitializeVirtualInput(includeKeyboard: true);
            isolatedInput = new CMInput();
            isolatedInput.BeatmapObjects.AddCallbacks(Object.FindAnyObjectByType<BeatmapGeometryInputController>());
            isolatedInput.NodeEditor.AddCallbacks(nodeEditor);
            isolatedInput.BeatmapObjects.Enable();
            isolatedInput.NodeEditor.Enable();
        }

        private void OpenNodeEditor()
        {
            PressVirtualKeys(Key.N);
            ReleaseVirtualKeys(Key.N);
            Assert.That(NodeEditorController.IsActive, Is.True, "The Node Editor shortcut did not open the editor.");
        }

        private void ClickGeometry(GeometryContainer geometry)
        {
            var camera = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.Camera;
            var center = geometry.MpbController.Renderers[0].bounds.center;
            var point = camera.WorldToScreenPoint(center);
            Assert.That(point.z, Is.GreaterThan(0f));
            SetVirtualControl(VirtualMouse.position, new Vector2(point.x, point.y), queueEventOnly: true);
            InputSystem.Update();
            BeatmapRaycastCache.Invalidate();
            Assert.That(
                Intersections.Raycast(camera.ScreenPointToRay(point), 9, out var hit),
                Is.True,
                "The visible geometry has no editor selection surface.");
            Assert.That(hit.GameObject.GetComponentInParent<GeometryContainer>(), Is.SameAs(geometry));
            PressVirtualKeys(Key.LeftShift);
            PressVirtualButton(VirtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            ReleaseVirtualButton(VirtualMouse.leftButton, queueEventOnly: true);
            InputSystem.Update();
            ReleaseVirtualKeys(Key.LeftShift);
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (nodeEditor != null)
                nodeEditor.Close();

            isolatedInput?.Dispose();
            isolatedInput = null;
            TearDownVirtualInput();
            BeatmapRaycastCache.Invalidate();
            Settings.Instance.NodeEditor_UseKeybind = previousNodeEditorKeybind;
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
