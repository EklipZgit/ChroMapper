using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class BloomFogLightMeshParityTest
    {
        [TestCase(0.001f, 0.01292f)]
        [TestCase(0.5f, 0.73535698f)]
        [TestCase(1f, 1f)]
        [TestCase(30f, 4.29730828f)]
        [TestCase(300f, 11.30520885f)]
        public void IntensityUsesNativeComputeTransfer(float intensity, float expectedAlpha)
        {
            var gameObject = new GameObject("Native bloom light intensity repro");
            try
            {
                gameObject.transform.position = new Vector3(0f, 0f, -5f);
                var light = gameObject.AddComponent<BloomFogObject>();
                light.SetColor(Color.white);
                light.IntensityMultiplier = intensity;
                var count = 0;
                var quads = new BloomfogQuad[1];
                light.ApplyToQuad(
                    ref count,
                    quads,
                    Matrix4x4.identity,
                    Matrix4x4.Perspective(130f, 1f, 0.01f, 100f),
                    0.02f);
                Assert.That(count, Is.EqualTo(1));
                // Beat Saber 1.44.1 FillMeshData uses the sRGB 1/2.4 curve above its
                // linear segment, including HDR intensities such as Billie's 30x sun tubes.
                Assert.That(quads[0].Vertex0Color.a, Is.EqualTo(expectedAlpha).Within(0.00002f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
