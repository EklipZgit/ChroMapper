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

        // GreenDayGrenadeInactiveRingRotationEffectInitializes covers serialized inactive dependencies through the
        // descriptor lifecycle and the beat-zero render that previously consumed an empty invalid snapshot.
        [Test]
        public void GreenDayGrenadeInactiveRingRotationEffectInitializes()
        {
            var ringRotationEffects = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsRotationEffect>()
                .ToArray();
            Assert.That(ringRotationEffects, Is.Not.Empty);
            Assert.That(ringRotationEffects.Any(effect => !effect.gameObject.activeInHierarchy), Is.True);

            // GreenDayGrenadeInactiveRingRotationEffectInitializes requires every position effect to retain its
            // authoritative serialized Visual binding even when the inactive spawner never receives Awake.
            var ringPositionEffects = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsPositionEffect>()
                .ToArray();
            Assert.That(ringPositionEffects, Is.Not.Empty);
            Assert.That(ringPositionEffects.All(effect => effect.Visual != null), Is.True);
            Assert.That(ringPositionEffects.Any(effect => !effect.Visual.gameObject.activeInHierarchy), Is.True);

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.DoesNotThrow(() => descriptor.Initialize(context));
            // GreenDayGrenadeInactiveRingRotationEffectInitializes verifies initialization preserves the serialized
            // spawner binding and inactive template state without relying on an Awake-time reverse assignment.
            Assert.That(ringPositionEffects.All(effect => effect.Visual != null), Is.True);
            Assert.That(ringPositionEffects.Any(effect => !effect.Visual.gameObject.activeInHierarchy), Is.True);

            // GreenDayGrenadeInactiveRingRotationEffectInitializes identifies the exact empty OEM rotation template
            // after initialization resolves its colocated Visual, matching To the City's renderer-load lifecycle.
            var dormantEffect = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsRotationEffect>()
                .Single(effect => effect.name == "LightLinesTrackLaneRings");
            Assert.That(dormantEffect.gameObject.activeInHierarchy, Is.False);
            Assert.That(dormantEffect.Visual, Is.Not.Null);
            Assert.That(dormantEffect.Visual.Manager, Is.Not.Null);
            Assert.That(dormantEffect.Visual.Manager.Rings, Is.Empty);

            // GreenDayGrenadeInactiveRingRotationEffectInitializes exercises the same beat-zero render that
            // produced null snapshot arrays during To the City's renderer-load path.
            dormantEffect.UpdateTime(false, 0f);

            LogAssert.NoUnexpectedReceived();
        }
    }
}
