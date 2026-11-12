using System;
using System.Reflection;
using NUnit.Framework;

namespace TestsEditMode
{
    public class DeploymentBuildTest
    {
        [Test]
        public void DeploymentEntryPointRejectsMissingOutputBeforeBuilding()
        {
            // Assembly definitions cannot directly reference Unity's predefined editor assembly.
            var editorType = Type.GetType("SimpleEditorUtils, Assembly-CSharp-Editor");
            Assert.That(editorType, Is.Not.Null);

            var entryPoint = editorType.GetMethod("BuildWindowsForDeployment", BindingFlags.Public | BindingFlags.Static);
            Assert.That(entryPoint, Is.Not.Null, "The deploy script's Unity entry point must exist in this worktree.");

            var exception = Assert.Throws<TargetInvocationException>(() => entryPoint.Invoke(null, null));
            Assert.That(exception.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(exception.InnerException.Message, Does.Contain("-chroMapperBuildOutput"));
        }
    }
}
