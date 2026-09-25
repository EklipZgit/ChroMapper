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

        [UnityTearDown]
        public IEnumerator UnloadGreenDayGrenadeEnvironmentOnly()
        {
            if (environmentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(environmentScene);
            }
        }

        [Test]
        public void GreenDayGrenadeInactiveRingRotationEffectInitializes()
        {
            var ringRotationEffects = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsRotationEffect>()
                .ToArray();
            Assert.That(ringRotationEffects, Is.Not.Empty);
            Assert.That(ringRotationEffects.Any(effect => !effect.gameObject.activeInHierarchy), Is.True);

            var ringPositionEffects = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsPositionEffect>()
                .ToArray();
            Assert.That(ringPositionEffects, Is.Not.Empty);
            Assert.That(ringPositionEffects.All(effect => effect.Visual != null), Is.True);
            Assert.That(ringPositionEffects.Any(effect => !effect.Visual.gameObject.activeInHierarchy), Is.True);

            var dormantEffect = descriptor.BasicEventEffectManager.Effects
                .OfType<TrackLaneRingsRotationEffect>()
                .Single(effect => effect.name == "LightLinesTrackLaneRings");
            Assert.That(dormantEffect.gameObject.activeInHierarchy, Is.False);
            Assert.That(dormantEffect.Visual, Is.Not.Null);
            Assert.That(dormantEffect.Visual.Manager, Is.Not.Null);
            var authoredRings = dormantEffect.Visual.Manager.Rings.ToArray();

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null);
            Assert.DoesNotThrow(() => descriptor.Initialize(context));
            Assert.That(ringPositionEffects.All(effect => effect.Visual != null), Is.True);
            Assert.That(ringPositionEffects.Any(effect => !effect.Visual.gameObject.activeInHierarchy), Is.True);
            // The integrated environment pass serializes this inactive template's rings while GlsQol's older asset
            // left it empty; initialization must preserve either authored representation without activating the effect.
            Assert.That(dormantEffect.Visual.Manager.Rings, Is.EqualTo(authoredRings));

            dormantEffect.UpdateTime(false, 0f);

            LogAssert.NoUnexpectedReceived();
        }
    }
}
