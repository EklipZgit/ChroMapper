using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class LightRotationAxisParityTest
    {
        [TestCase(-0.5565387f, -0.24192187f, -0.79481965f, 63f)]
        [TestCase(0.5565387f, -0.24192187f, -0.79481965f, -3833.175f)]
        [TestCase(0f, 1f, 0f, 125f)]
        [TestCase(2f, 3f, -4f, 47f)]
        public void AccumulatedRotationMatchesNativeSelfSpaceRotate(float x, float y, float z, float angle)
        {
            var target = new GameObject("Axis rotation target");
            var reference = new GameObject("Native rotation reference");
            try
            {
                var rest = Quaternion.Euler(14f, 31f, -9f);
                target.transform.rotation = rest;
                reference.transform.rotation = rest;
                var visual = target.AddComponent<LightRotation>();
                visual.Transform = target.transform;
                visual.RotationVector = new Vector3(x, y, z);
                visual.Initialize();
                reference.transform.Rotate(visual.RotationVector, angle, Space.Self);
                visual.Apply(angle);
                Assert.That(Quaternion.Angle(target.transform.rotation, reference.transform.rotation),
                    Is.LessThan(0.05f), "The accumulated preview angle must use Transform.Rotate's axis-angle semantics.");
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(reference);
            }
        }
    }
}
