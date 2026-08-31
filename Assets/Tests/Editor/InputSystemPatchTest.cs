using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    public class InputSystemPatchTest : InputTestFixture
    {
        // CheckEqualPathsWithoutMatchingDevicesUsesSafePathFallback reproduces first-boot conflict scanning when
        // batchmode has registered binding layouts but no platform keyboard or mouse devices.
        [Test]
        public void CheckEqualPathsWithoutMatchingDevicesUsesSafePathFallback()
        {
            Assert.That(InputSystem.devices, Is.Empty, "InputTestFixture unexpectedly exposed a platform input device.");

            var equalResult = false;
            Assert.DoesNotThrow(() => equalResult = InvokeCheckEqualPaths("<Keyboard>/a", "<Keyboard>/a"));
            Assert.That(equalResult, Is.True, "Identical binding paths were not equal without a matching device.");

            var unequalResult = true;
            Assert.DoesNotThrow(() => unequalResult = InvokeCheckEqualPaths("<Keyboard>/a", "<Keyboard>/b"));
            Assert.That(unequalResult, Is.False, "Different unresolved binding paths were incorrectly equal.");
        }

        // Preserve the production exception type through reflection so the unfixed null dereference remains visible
        // instead of being obscured by TargetInvocationException in the regression failure.
        private static bool InvokeCheckEqualPaths(string pathA, string pathB)
        {
            var method = typeof(InputSystemPatch).GetMethod(
                "CheckEqualPaths",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "InputSystemPatch.CheckEqualPaths could not be found.");

            try
            {
                return (bool)method.Invoke(null, new object[] { pathA, pathB });
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
