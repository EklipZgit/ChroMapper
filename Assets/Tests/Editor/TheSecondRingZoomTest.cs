using System.Collections;
using System.Linq;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // TheSecondRingZoom* runs through the normal loaded-map preview path so it covers event dispatch as well as animation.
    public class TheSecondRingZoomTest : TestBase
    {
        private const string EnvironmentSceneName = "TheSecondEnvironment";
        private const int RingZoomEventType = 9;

        private Scene environmentScene;
        private BasicEventEffectManager effectManager;

        [UnitySetUp]
        public IEnumerator LoadTheSecondEnvironmentOnly()
        {
            // Additive loading isolates The Second's production manager without replacing the shared mapper map or callbacks.
            yield return SceneManager.LoadSceneAsync(EnvironmentSceneName, LoadSceneMode.Additive);
            environmentScene = SceneManager.GetSceneByName(EnvironmentSceneName);
            var descriptor = environmentScene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EnvironmentDescriptor>(true))
                .Single();

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            // The production descriptor lifecycle owns compatibility registration before initializing its effect timelines.
            descriptor.Initialize(context);
            effectManager = descriptor.BasicEventEffectManager;
        }

        [UnityTearDown]
        public IEnumerator UnloadTheSecondEnvironmentOnly()
        {
            // Unload only the additive environment so no shared map, input callback, or mapper baseline is recreated.
            if (environmentScene.IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(environmentScene);
            }
        }

        [Test]
        public void TheSecondRingZoomIntegerValuesAnimateBetweenBasicEvents()
        {
            var observedRing = GetObservedRing();
            var before = observedRing.position;

            var current = InsertRingZoomEvent(1f, 2);
            var next = InsertRingZoomEvent(3f, 6);
            ApplyAtMidpoint(current, next);

            Assert.That(
                observedRing.position,
                Is.Not.EqualTo(before),
                "The Second's ordinary integer Event 9 values did not animate the ring hierarchy.");
        }

        [Test]
        public void TheSecondRingZoomCustomFloatStepsAnimateBetweenBasicEvents()
        {
            var observedRing = GetObservedRing();
            var before = observedRing.position;

            // Equal integer values isolate Chroma's fractional step: movement can only come from customData.step.
            var current = InsertRingZoomEvent(1f, 4, 4.25f);
            var next = InsertRingZoomEvent(3f, 4, 5.75f);
            ApplyAtMidpoint(current, next);

            Assert.That(
                observedRing.position,
                Is.Not.EqualTo(before),
                "The Second's fractional customData.step values did not animate the ring hierarchy.");
        }

        [Test]
        public void TheSecondRingZoomNegativeCustomFloatStepBypassesIntegerClamp()
        {
            var observedRing = GetObservedRing();

            // ChromaGLS permits negative custom steps even though Beat Saber's ordinary integer Event 9 values clamp to 0-9.
            var current = InsertRingZoomEvent(1f, 4, -2f);
            var next = InsertRingZoomEvent(3f, 4, -2f);
            ApplyAtMidpoint(current, next);

            Assert.That(
                observedRing.localPosition.z,
                Is.EqualTo(-1f).Within(0.0001f),
                "The Second's negative customData.step was clamped instead of spacing the second ring backwards.");
        }

        [Test]
        public void TheSecondRingZoomNegativeIntegerValueRespectsClampWithoutCustomStep()
        {
            var observedRing = GetObservedRing();

            // Negative i has no Chroma override and must therefore remain governed by The Second's serialized 0..9 OEM clamp.
            var current = InsertRingZoomEvent(1f, -2);
            var next = InsertRingZoomEvent(3f, -2);
            Assert.That(current.CustomStep, Is.Null, "Negative integer regression must not be represented by customData.step.");
            Assert.That(next.CustomStep, Is.Null, "Negative integer target must not be represented by customData.step.");
            ApplyAtMidpoint(current, next);

            Assert.That(
                observedRing.localPosition.z,
                Is.EqualTo(0f).Within(0.0001f),
                "The Second's negative integer i bypassed the OEM clamp without customData.step.");
        }

        private Transform GetObservedRing()
        {
            Assert.That(
                effectManager.EventTypeToEffects.TryGetValue(
                    RingZoomEventType,
                    out var effects),
                Is.True,
                "The Second exported SmoothStepPositionGroupEventEffect, but Event 9 has no ChroMapper movement effect.");

            var effect = effects.SingleOrDefault(candidate =>
                candidate is SmoothStepPositionEventEffect
                || candidate.GetType().Name == "SmoothStepPositionGroupEventEffect");
            Assert.That(
                effect,
                Is.Not.Null,
                "The Second's Event 9 registration does not contain its smooth-step ring zoom effect.");

            Assert.That(
                effect.transform.childCount,
                Is.GreaterThan(1),
                "The Second ring zoom effect is not attached to its ordered ring group.");
            return effect.transform.GetChild(1);
        }

        private BaseEvent InsertRingZoomEvent(float beat, int value, float? customStep = null)
        {
            // Direct manager insertion exercises production preview state without adding actions or objects to the shared map.
            var evt = new BaseEvent
            {
                JsonTime = beat,
                Type = RingZoomEventType,
                Value = value,
                FloatValue = 1f,
                CustomStep = customStep
            };
            Assert.That(effectManager.InsertData(evt), Is.True);
            return evt;
        }

        private void ApplyAtMidpoint(BaseEvent current, BaseEvent next)
        {
            // The preview effect receives the same continuous song-beat time used by LightshowController.UpdateTime.
            var midpoint = (current.SongBpmTime + next.SongBpmTime) * 0.5f;
            var effects = effectManager.EventTypeToEffects[RingZoomEventType];
            for (var i = 0; i < effects.Count; i++)
            {
                effects[i].UpdateTime(false, midpoint);
            }
        }
    }
}
