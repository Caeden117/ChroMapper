using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace Tests.Editor
{
    public class InputSystemPatchTest : InputTestFixture
    {
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

        // Unwrap reflection failures so assertions report the production exception.
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
