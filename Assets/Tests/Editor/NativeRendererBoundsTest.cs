using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class NativeRendererBoundsTest
    {
        [Test]
        public void CloningRestoresNativeBoundsBeforeCulling()
        {
            var source = GameObject.CreatePrimitive(PrimitiveType.Cube);
            source.SetActive(false);
            source.transform.position = new Vector3(12f, 0f, 0f);
            var cameraObject = new GameObject("Bounds regression camera");
            GameObject clone = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.fieldOfView = 60f;
                camera.aspect = 1f;
                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                var original = source.GetComponent<MeshRenderer>();
                Assert.That(GeometryUtility.TestPlanesAABB(planes, original.bounds), Is.False);
                var expected = new Bounds(Vector3.zero, new Vector3(200f, 100f, 200f));
                source.AddComponent<NativeRendererBounds>().Configure(original, expected);
                clone = Object.Instantiate(source);
                var renderer = clone.GetComponent<MeshRenderer>();
                renderer.ResetLocalBounds();
                clone.SetActive(true);
                Assert.That(renderer.localBounds.center, Is.EqualTo(expected.center));
                Assert.That(renderer.localBounds.size, Is.EqualTo(expected.size));
                Assert.That(GeometryUtility.TestPlanesAABB(planes, renderer.bounds), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(clone);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(cameraObject);
            }

        }
    }
}
