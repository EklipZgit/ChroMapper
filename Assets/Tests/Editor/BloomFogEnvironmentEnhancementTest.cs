using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // BloomFogEnvironmentEnhancementUpdatesRenderingState reproduces DEVOLUTION's late-applied startY override and
    // verifies every supported component value reaches the live renderer after the environment-loaded notification.
    public class BloomFogEnvironmentEnhancementTest : TestBase
    {
        private const float ExpectedOffset = 12.5f;
        private const float ExpectedHeight = 84f;
        private const float ExpectedStartY = -120f;
        private const float ExpectedAttenuation = 0.0075f;
        private const float ExpectedAutoExposureLimit = 42f;

        // The production map loader applies environment enhancements after the renderer consumes the scene defaults,
        // so this test requires both descriptor storage and the shader-facing renderer state to contain authored data.
        [UnityTest]
        public IEnumerator BloomFogEnvironmentEnhancementUpdatesRenderingState()
        {
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            yield return null;

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var controller = Object.FindAnyObjectByType<BloomfogRenderingController>();
            Assert.That(context, Is.Not.Null, "The mapper did not expose its beatmap runtime context.");
            Assert.That(context.Descriptor, Is.Not.Null, "The mapper did not load an environment descriptor.");
            Assert.That(controller, Is.Not.Null, "The mapper did not create its bloom-fog renderer.");

            var parameters = context.Descriptor.BloomFogParams;
            Assert.That(parameters.Offset, Is.EqualTo(ExpectedOffset).Within(0.0001f));
            Assert.That(parameters.Height, Is.EqualTo(ExpectedHeight).Within(0.0001f));
            Assert.That(parameters.StartY, Is.EqualTo(ExpectedStartY).Within(0.0001f));
            Assert.That(parameters.Attenuation, Is.EqualTo(ExpectedAttenuation).Within(0.0001f));
            Assert.That(parameters.AutoExposureLimit, Is.EqualTo(ExpectedAutoExposureLimit).Within(0.0001f));
            Assert.That(parameters.LegacyAutoExposure, Is.True);

            Assert.That(
                Shader.GetGlobalFloat("_CustomFogOffset"),
                Is.EqualTo(ExpectedOffset).Within(0.0001f));
            Assert.That(
                Shader.GetGlobalFloat("_CustomFogHeightFogHeight"),
                Is.EqualTo(ExpectedHeight).Within(0.0001f));
            Assert.That(
                Shader.GetGlobalFloat("_CustomFogHeightFogStartY"),
                Is.EqualTo(ExpectedStartY).Within(0.0001f));
            Assert.That(
                Shader.GetGlobalFloat("_CustomFogAttenuation"),
                Is.EqualTo(ExpectedAttenuation).Within(0.0001f));
            Assert.That(
                ReadPrivateField<float>(controller, "bloomFogAutoExposureLimit"),
                Is.EqualTo(ExpectedAutoExposureLimit).Within(0.0001f));
            Assert.That(ReadPrivateField<bool>(controller, "bloomFogLegacyAutoExposure"), Is.True);
        }

        // Restore both the shared map and shader globals so this renderer-state fixture cannot affect later tests.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The fixture mirrors the reported V3 component while assigning distinct values to every supported parameter.
        private static JSONNode CreateDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = "KaleidoscopeEnvironment.[0]Environment",
                ["lookupMethod"] = "Exact",
                ["components"] = new JSONObject
                {
                    ["BloomFogEnvironment"] = new JSONObject
                    {
                        ["offset"] = ExpectedOffset,
                        ["height"] = ExpectedHeight,
                        ["startY"] = ExpectedStartY,
                        ["attenuation"] = ExpectedAttenuation,
                        ["autoExposureLimit"] = ExpectedAutoExposureLimit,
                        ["legacyAutoExposure"] = true
                    }
                }
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment
                }
            };
        }

        // Renderer exposure values are intentionally private implementation state but are the shader inputs on render.
        private static T ReadPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Bloom-fog renderer field '{fieldName}' was not found.");
            return (T)field.GetValue(instance);
        }
    }
}
