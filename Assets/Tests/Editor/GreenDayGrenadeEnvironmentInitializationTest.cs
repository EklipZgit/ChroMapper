using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // GreenDayGrenadeInactiveRingRotationEffectInitializes covers environment initialization before inactive effects get Awake.
    public class GreenDayGrenadeEnvironmentInitializationTest : TestBase
    {
        private const string EnvironmentSceneName = "GreenDayGrenadeEnvironment";

        private Scene environmentScene;
        private EnvironmentDescriptor descriptor;

        // GreenDayGrenadeInactiveRingRotationEffectInitializes loads only the production additive environment needed to
        // reproduce the installed-map crash without replacing TestBase's shared mapper scene.
        [UnitySetUp]
        public IEnumerator LoadGreenDayGrenadeEnvironmentOnly()
        {
            yield return SceneManager.LoadSceneAsync(EnvironmentSceneName, LoadSceneMode.Additive);
            environmentScene = SceneManager.GetSceneByName(EnvironmentSceneName);
            descriptor = environmentScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnvironmentDescriptor>(true))
                .Single();
        }

        // GreenDayGrenadeInactiveRingRotationEffectInitializes unloads its additive environment so no serialized effect
        // or ring manager survives into another PlayMode fixture.
        [UnityTearDown]
        public IEnumerator UnloadGreenDayGrenadeEnvironmentOnly()
        {
            if (environmentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(environmentScene);
            }
        }

        // GreenDayGrenadeInactiveRingRotationEffectInitializes proves the serialized effect is inactive before the
        // descriptor explicitly initializes it, which is the lifecycle ordering that previously left ringManager null.
        [Test]
        public void GreenDayGrenadeInactiveRingRotationEffectInitializes()
        {
            var ringRotationEffects = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsRotationEffect>()
                .ToArray();
            Assert.That(ringRotationEffects, Is.Not.Empty);
            Assert.That(ringRotationEffects.Any(effect => !effect.gameObject.activeInHierarchy), Is.True);

            // GreenDayGrenadeInactiveRingRotationEffectInitializes proves Event 9 starts with an intentionally
            // unwired evaluator because its inactive spawner never receives Awake before descriptor initialization.
            var ringPositionEffects = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsPositionEffect>()
                .ToArray();
            Assert.That(ringPositionEffects, Is.Not.Empty);
            Assert.That(ringPositionEffects.Any(effect => effect.Visual == null), Is.True);

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.DoesNotThrow(() => descriptor.Initialize(context));
            // GreenDayGrenadeInactiveRingRotationEffectInitializes verifies initialization recovered the serialized
            // spawner binding while preserving the inactive template state rather than ignoring a null dependency.
            Assert.That(ringPositionEffects.All(effect => effect.Visual != null), Is.True);
            Assert.That(ringPositionEffects.Any(effect => !effect.Visual.gameObject.activeInHierarchy), Is.True);
        }
    }
}
