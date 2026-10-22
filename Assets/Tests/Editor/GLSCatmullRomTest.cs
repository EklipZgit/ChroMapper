using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Experimental ChroMapper-only Catmull-Rom spline easings. The IDs are written as literals so the
    // fixtures run red before the enum exists; IDs 103/104/105 = uniform/centripetal/chordal.
    public static class GLSCatmullRomIds
    {
        public const int Uniform = 103;
        public const int Centripetal = 104;
        public const int Chordal = 105;
        public static readonly int[] All = { Uniform, Centripetal, Chordal };
    }

    // Scalar spline behavior is observed through the production FloatFx playback effect so filtering,
    // generated per-light states, tween pooling, and hold/step semantics all stay on the real path.
    public class GLSCatmullRomFloatFxTest : TestBase
    {
        private const int GroupId = 913;
        private GameObject effectRoot;
        private FloatFxGroupEffect effect;
        private RecordingFxTarget target;

        private static float SongTime(float jsonBeat) =>
            (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonBeat);

        private float ValueAt(float jsonBeat)
        {
            effect.UpdateTime(false, SongTime(jsonBeat));
            return target.Value;
        }

        private void LoadGroup(params BaseFxEventFloat[] events)
        {
            var box = new BaseVfxEventEventBox
            {
                IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.Division,
                    Param0 = 1,
                    Param1 = 0
                },
                Events = events
            };
            var group = new BaseVfxEventEventBoxGroup { JsonTime = 0f, ID = GroupId };
            group.Boxes.Add(box);
            group.NormalizeLoadedEventConflicts();
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            effect.InsertData(group);
            effect.Refresh();
        }

        private static BaseFxEventFloat Node(float beat, float value, int easing = 1, int usePrevious = 0) =>
            new()
            {
                RelativeJsonTime = beat,
                Value = value,
                Easing = easing,
                UsePrevious = usePrevious
            };

        [SetUp]
        public void CreateEffect()
        {
            effectRoot = new GameObject(nameof(GLSCatmullRomFloatFxTest));
            effect = effectRoot.AddComponent<FloatFxGroupEffect>();
            effect.Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(effect.Atsc, Is.Not.Null);
            effect.ID = GroupId;
            effect.Count = 1;
            target = effectRoot.AddComponent<RecordingFxTarget>();
            effect.Register(0, target);
            effect.Initialize();
        }

        [TearDown]
        public void DestroyEffect() => Object.DestroyImmediate(effectRoot);

        // The inner segment of the canonical 0,10,20,0 fixture must use all four per-light states as
        // spline controls, not a two-endpoint curve.
        [TestCase(GLSCatmullRomIds.Uniform, 16.875f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 16.25f)]
        [TestCase(GLSCatmullRomIds.Chordal, 15.625f)]
        public void CatmullRomUsesAllFourNeighborControls(int easing, float expectedMidpoint)
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, easing),
                Node(3f, 0f));

            Assert.That(ValueAt(1.5f), Is.EqualTo(expectedMidpoint).Within(1e-4f),
                $"The midpoint of segment 1-2 must evaluate the {easing} Catmull-Rom spline.");
        }

        // A non-Catmull destination on the same authored fixture must remain a plain two-point easing.
        [Test]
        public void LinearDestinationKeepsTwoPointMidpoint()
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, (int)EaseType.Linear),
                Node(3f, 0f));

            Assert.That(ValueAt(1.5f), Is.EqualTo(15f).Within(1e-4f));
        }

        // With only two nodes the missing outer controls duplicate the endpoints one duration outward.
        // Uniform still sees the symmetric chords (2.03125 at t=0.25); the nonuniform variants see a
        // zero-distance chord and stop flat at the boundary instead.
        [TestCase(GLSCatmullRomIds.Uniform, 2.03125f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 1.5625f)]
        [TestCase(GLSCatmullRomIds.Chordal, 1.5625f)]
        public void CatmullRomDuplicatesMissingOuterControls(int easing, float expected)
        {
            LoadGroup(Node(0f, 0f), Node(1f, 10f, easing));

            Assert.That(ValueAt(0.25f), Is.EqualTo(expected).Within(1e-4f));
            Assert.That(ValueAt(0.999f), Is.EqualTo(10f).Within(0.05f));
            Assert.That(ValueAt(2f), Is.EqualTo(10f).Within(1e-4f));
        }

        // An authored equal pair at the boundary is a zero chord: the nonuniform variants stop the
        // shared node tangent instead of dividing by zero. p=[5,5,10,0] gives uniform m1=2.5, m2=-2.5
        // (mid 8.125), centripetal m1=m2=0 (mid 7.5), chordal m1=0, m2=2.5 (mid 7.1875).
        [TestCase(GLSCatmullRomIds.Uniform, 8.125f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 7.5f)]
        [TestCase(GLSCatmullRomIds.Chordal, 7.1875f)]
        public void CatmullRomHandlesZeroChordAtBoundary(int easing, float expectedMidpoint)
        {
            LoadGroup(
                Node(0f, 5f),
                Node(1f, 5f),
                Node(2f, 10f, easing),
                Node(3f, 0f));

            Assert.That(float.IsFinite(ValueAt(1.5f)), Is.True);
            Assert.That(ValueAt(1.5f), Is.EqualTo(expectedMidpoint).Within(1e-3f));
        }

        // Identical inner endpoints are a zero central chord: the nonuniform variants must hold a
        // constant value across the whole segment instead of producing NaN tangents.
        [TestCase(GLSCatmullRomIds.Centripetal)]
        [TestCase(GLSCatmullRomIds.Chordal)]
        public void RepeatedCentralControlsStayConstant(int easing)
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 10f, easing),
                Node(3f, 0f));

            Assert.That(ValueAt(1.25f), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(ValueAt(1.5f), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(ValueAt(1.75f), Is.EqualTo(10f).Within(1e-4f));
        }

        // The uniform polynomial still runs on equal inner endpoints; it may overshoot but must stay finite.
        [Test]
        public void RepeatedCentralControlsStayFiniteUniform()
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 10f, GLSCatmullRomIds.Uniform),
                Node(3f, 0f));

            Assert.That(float.IsFinite(ValueAt(1.5f)), Is.True);
        }

        // Before the first node the sentinel segment eases over its whole span, so at t=.5 the value is
        // already ~the first node's value (inverse-lerp over a span starting at short.MinValue). The
        // spline must not add an excursion beyond that established behaviour.
        [Test]
        public void CatmullRomKeepsPreFirstNodeBehaviour()
        {
            LoadGroup(
                Node(1f, 10f, GLSCatmullRomIds.Uniform),
                Node(2f, 20f, GLSCatmullRomIds.Uniform),
                Node(3f, 0f));

            Assert.That(ValueAt(0.5f), Is.EqualTo(10f).Within(0.1f));
        }

        // An explicit Instant destination still steps at the boundary rather than splining.
        [Test]
        public void InstantDestinationStillSteps()
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, (int)EaseType.None),
                Node(3f, 0f, (int)EaseType.None));

            Assert.That(ValueAt(1.5f), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(ValueAt(2.1f), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(ValueAt(3.1f), Is.EqualTo(0f).Within(1e-4f));
        }

        // A UsePrevious target holds the resolved value through its span instead of drifting on a spline.
        [Test]
        public void UsePreviousTargetStillHolds()
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, GLSCatmullRomIds.Uniform, usePrevious: 1),
                Node(3f, 0f, GLSCatmullRomIds.Uniform));

            Assert.That(ValueAt(1.5f), Is.EqualTo(10f).Within(1e-4f));
        }

        // A held source's segment gets p0 from the held state's own predecessor, not from the resolved
        // start's predecessor: [0,10,Hold10,20,0] splines [10,10,20,0], not [0,10,20,0].
        [TestCase(GLSCatmullRomIds.Uniform, 16.25f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 15f)]
        [TestCase(GLSCatmullRomIds.Chordal, 14.375f)]
        public void CatmullRomHeldSourceUsesItsOwnPredecessor(int easing, float expectedMidpoint)
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 10f, usePrevious: 1),
                Node(3f, 20f, easing),
                Node(4f, 0f));

            Assert.That(ValueAt(2.5f), Is.EqualTo(expectedMidpoint).Within(1e-4f));
        }

        // Trigger effects dispatch discrete events on Refresh/UpdateTime and must never run the
        // interpolated tween path, spline or otherwise.
        [Test]
        public void TriggerEffectStaysDiscrete()
        {
            effect.Trigger = true;
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, GLSCatmullRomIds.Uniform),
                Node(3f, 0f));

            Assert.That(ValueAt(1.5f), Is.EqualTo(10f).Within(1e-4f),
                "Trigger mode must report the owning node's value, not an interpolated sample.");
        }

        // Seeking backwards must re-evaluate the same spline coefficients instead of leaking state.
        [Test]
        public void CatmullRomScrubBackwardsMatchesForward()
        {
            LoadGroup(
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, GLSCatmullRomIds.Centripetal),
                Node(3f, 0f));

            var forward = ValueAt(1.5f);
            ValueAt(2.8f);
            var backward = ValueAt(1.5f);
            Assert.That(backward, Is.EqualTo(forward).Within(1e-6f));
            Assert.That(forward, Is.EqualTo(16.25f).Within(1e-4f));
        }

        // The pooled container tween is reused across edits: swapping the destination easing back to a
        // plain curve must clear every cached spline control.
        [Test]
        public void ReusedTweenDropsSplineState()
        {
            var events = new[]
            {
                Node(0f, 0f),
                Node(1f, 10f),
                Node(2f, 20f, GLSCatmullRomIds.Uniform),
                Node(3f, 0f)
            };
            var box = new BaseVfxEventEventBox
            {
                IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.Division,
                    Param0 = 1,
                    Param1 = 0
                },
                Events = events
            };
            var group = new BaseVfxEventEventBoxGroup { JsonTime = 0f, ID = GroupId };
            group.Boxes.Add(box);
            group.NormalizeLoadedEventConflicts();
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            effect.InsertData(group);
            effect.Refresh();

            var splineValue = ValueAt(1.5f);
            Assert.That(splineValue, Is.EqualTo(16.875f).Within(1e-4f));

            events[2].Easing = (int)EaseType.Linear;
            effect.RemoveData(group, group);
            effect.InsertData(group);
            effect.Refresh();

            Assert.That(ValueAt(1.5f), Is.EqualTo(15f).Within(1e-4f),
                "Rebinding the pooled tween from Catmull-Rom to Linear must clear the spline controls.");
        }

        private sealed class RecordingFxTarget : FxTarget
        {
            public float Value;
            public override void SetValue(int group, int id, float value) => Value = value;
            public override void TriggerValue(int group, int id, float value) => Value = value;
        }
    }

    // Rotation and translation go through the live transform effects so limits, direction, loops, and
    // mirroring are exercised exactly like an environment.
    public class GLSCatmullRomTransformTest : TestBase
    {
        private const int GroupId = 914;
        private GameObject effectRoot;
        private LightRotationGroupEffect rotationEffect;
        private LightTranslationGroupEffect translationEffect;
        private Transform rotationTarget;
        private Transform translationTarget;

        private static float SongTime(float jsonBeat) =>
            (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonBeat);

        [SetUp]
        public void CreateEffects()
        {
            effectRoot = new GameObject(nameof(GLSCatmullRomTransformTest));
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.That(atsc, Is.Not.Null);

            rotationEffect = effectRoot.AddComponent<LightRotationGroupEffect>();
            rotationEffect.Atsc = atsc;
            rotationEffect.ID = GroupId;
            rotationEffect.Count = 1;
            rotationTarget = CreateChild("Rotation target");
            rotationEffect.Register(0, Axis.X, false, rotationTarget);
            rotationEffect.Initialize();

            translationEffect = effectRoot.AddComponent<LightTranslationGroupEffect>();
            translationEffect.Atsc = atsc;
            translationEffect.ID = GroupId;
            translationEffect.Count = 1;
            // Identity mapping: authored values land on the axis unchanged.
            translationEffect.TranslationLimits = new[] { new Vector2(-1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, 1f) };
            translationEffect.DistributionLimits = new[] { Vector2.zero, Vector2.zero, Vector2.zero };
            translationTarget = CreateChild("Translation target");
            translationEffect.Register(0, Axis.X, false, translationTarget);
            translationEffect.Initialize();
        }

        [TearDown]
        public void DestroyEffects() => Object.DestroyImmediate(effectRoot);

        private Transform CreateChild(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(effectRoot.transform, false);
            return child.transform;
        }

        private void LoadRotation(params BaseLightRotationBase[] events)
        {
            var box = new BaseLightRotationEventBox
            {
                Axis = (int)Axis.X,
                IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.Division,
                    Param0 = 1,
                    Param1 = 0
                },
                Events = events
            };
            var group = new BaseLightRotationEventBoxGroup { JsonTime = 0f, ID = GroupId };
            group.Boxes.Add(box);
            group.NormalizeLoadedEventConflicts();
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            rotationEffect.InsertData(group);
            rotationEffect.Refresh();
        }

        private void LoadTranslation(
            float translationDistribution = 0f,
            int translationAffectFirst = 0,
            params BaseLightTranslationBase[] events)
        {
            var box = new BaseLightTranslationEventBox
            {
                Axis = (int)Axis.X,
                IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.Division,
                    Param0 = 1,
                    Param1 = 0
                },
                TranslationDistribution = translationDistribution,
                TranslationDistributionType = (int)DistributionType.Wave,
                TranslationAffectFirst = translationAffectFirst,
                Events = events
            };
            var group = new BaseLightTranslationEventBoxGroup { JsonTime = 0f, ID = GroupId };
            group.Boxes.Add(box);
            group.NormalizeLoadedEventConflicts();
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            translationEffect.InsertData(group);
            translationEffect.Refresh();
        }

        private static BaseLightRotationBase RotationNode(
            float beat, float rotation, int easing = 1, int loop = 0,
            int direction = (int)LightRotationDirection.Automatic, int usePrevious = 0) =>
            new()
            {
                RelativeJsonTime = beat,
                Rotation = rotation,
                Easing = easing,
                Loop = loop,
                Direction = direction,
                UsePrevious = usePrevious
            };

        private static BaseLightTranslationBase TranslationNode(
            float beat, float value, int easing = 1, int usePrevious = 0) =>
            new()
            {
                RelativeJsonTime = beat,
                Translation = value,
                Easing = easing,
                UsePrevious = usePrevious
            };

        private float RotationAt(float jsonBeat)
        {
            rotationEffect.UpdateTime(false, SongTime(jsonBeat));
            // Recover the signed X-axis angle from the quaternion the effect applied.
            var q = rotationTarget.localRotation;
            var angle = 2f * Mathf.Acos(Mathf.Clamp(q.w, -1f, 1f)) * Mathf.Rad2Deg;
            if (q.x < 0f) angle = -angle;
            return angle;
        }

        private float TranslationAt(float jsonBeat)
        {
            translationEffect.UpdateTime(false, SongTime(jsonBeat));
            return translationTarget.localPosition.x;
        }

        [TestCase(GLSCatmullRomIds.Uniform, 16.875f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 16.25f)]
        [TestCase(GLSCatmullRomIds.Chordal, 15.625f)]
        public void CatmullRomTranslationUsesNeighborControls(int easing, float expectedMidpoint)
        {
            LoadTranslation(
                events: new[]
                {
                    TranslationNode(0f, 0f),
                    TranslationNode(1f, 10f),
                    TranslationNode(2f, 20f, easing),
                    TranslationNode(3f, 0f)
                });

            Assert.That(TranslationAt(1.5f), Is.EqualTo(expectedMidpoint).Within(1e-3f));
        }

        // The generated per-light states carry each light's own distribution offset, so every control of
        // light 1's spline shifts by its mapped step and its midpoint moves with them.
        [Test]
        public void CatmullRomTranslationDistributesGeneratedControls()
        {
            var light1 = CreateChild("Translation light 1");
            translationEffect.Count = 2;
            translationEffect.DistributionLimits = new[]
            {
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f)
            };
            translationEffect.Register(1, Axis.X, false, light1);
            translationEffect.Initialize();
            LoadTranslation(
                translationDistribution: 0.5f,
                translationAffectFirst: 1,
                events: new[]
                {
                    TranslationNode(0f, 0f),
                    TranslationNode(1f, 10f),
                    TranslationNode(2f, 20f, GLSCatmullRomIds.Uniform),
                    TranslationNode(3f, 0f)
                });

            translationEffect.UpdateTime(false, SongTime(1.5f));
            // Zero distribution still maps through limits (0,1) to +.5; order 1's wave step .5 gives +.75.
            Assert.That(translationTarget.localPosition.x, Is.EqualTo(17.375f).Within(1e-3f));
            Assert.That(light1.localPosition.x, Is.EqualTo(17.625f).Within(1e-3f));
        }

        // Same held-source control rule as FloatFX: the extension's predecessor is the outer control.
        [Test]
        public void CatmullRomTranslationHeldSourceUsesItsOwnPredecessor()
        {
            LoadTranslation(
                events: new[]
                {
                    TranslationNode(0f, 0f),
                    TranslationNode(1f, 10f),
                    TranslationNode(2f, 10f, usePrevious: 1),
                    TranslationNode(3f, 20f, GLSCatmullRomIds.Uniform),
                    TranslationNode(4f, 0f)
                });

            Assert.That(TranslationAt(2.5f), Is.EqualTo(16.25f).Within(1e-3f));
        }

        // The wrap case: authored 350,10,30,0 unwraps to -10,10,30,0 so the uniform midpoint is 23.125.
        [Test]
        public void CatmullRomRotationUnwrapsAcrossSeam()
        {
            LoadRotation(
                RotationNode(0f, 350f),
                RotationNode(1f, 10f),
                RotationNode(2f, 30f, GLSCatmullRomIds.Uniform),
                RotationNode(3f, 0f));

            var measured = RotationAt(1.5f);
            // The quaternion can report the same orientation through several angles.
            while (measured > 180f) measured -= 360f;
            while (measured < -180f) measured += 360f;
            Assert.That(
                Mathf.Min(
                    Mathf.Abs(measured - 23.125f),
                    Mathf.Abs(measured - (23.125f - 360f))),
                Is.LessThan(0.01f),
                $"Rotation at midpoint must equal 23.125 degrees, measured {measured}.");
        }

        // A looping destination's own loop delta extends p2 (and the next interval's delta extends p3)
        // before the spline runs.
        [Test]
        public void CatmullRomRotationHonorsDestinationLoop()
        {
            LoadRotation(
                RotationNode(0f, 350f),
                RotationNode(1f, 10f),
                RotationNode(2f, 30f, GLSCatmullRomIds.Uniform, loop: 1),
                RotationNode(3f, 0f));

            var measured = RotationAt(1.5f);
            while (measured > 180f) measured -= 360f;
            while (measured < -180f) measured += 360f;
            Assert.That(
                Mathf.Min(
                    Mathf.Abs(measured - 203.125f),
                    Mathf.Abs(measured - (203.125f - 360f))),
                Is.LessThan(0.5f),
                $"Rotation with one destination loop must equal 203.125 degrees, measured {measured}.");
        }

        // A clockwise destination forces its own direction delta before the spline runs: authored 350
        // unwraps to +350 instead of -10, so p=[-10,10,350,360] and the midpoint is 180.625.
        [Test]
        public void CatmullRomRotationHonorsDestinationDirection()
        {
            LoadRotation(
                RotationNode(0f, 350f),
                RotationNode(1f, 10f),
                RotationNode(2f, 350f, GLSCatmullRomIds.Uniform, direction: (int)LightRotationDirection.Clockwise),
                RotationNode(3f, 0f));

            var measured = RotationAt(1.5f);
            while (measured > 180f) measured -= 360f;
            while (measured < -180f) measured += 360f;
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(measured, 180.625f)),
                Is.LessThan(0.5f),
                $"Clockwise rotation midpoint must equal 180.625 degrees, measured {measured}.");
        }

        // Mirroring negates the evaluated spline at output; the mirrored container must render -23.125.
        [Test]
        public void CatmullRomRotationMirrorsOutput()
        {
            var mirrorTarget = CreateChild("Mirrored rotation target");
            rotationEffect.Count = 2;
            rotationEffect.Register(1, Axis.X, true, mirrorTarget);
            rotationEffect.Initialize();
            LoadRotation(
                RotationNode(0f, 350f),
                RotationNode(1f, 10f),
                RotationNode(2f, 30f, GLSCatmullRomIds.Uniform),
                RotationNode(3f, 0f));

            rotationEffect.UpdateTime(false, SongTime(1.5f));
            var q = mirrorTarget.localRotation;
            var measured = 2f * Mathf.Acos(Mathf.Clamp(q.w, -1f, 1f)) * Mathf.Rad2Deg;
            if (q.x < 0f)
                measured = -measured;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(measured, -23.125f)), Is.LessThan(0.01f),
                $"Mirrored rotation midpoint must equal -23.125 degrees, measured {measured}.");
        }

        [Test]
        public void CatmullRomRotationHoldsBeforeFirstNode()
        {
            LoadRotation(
                RotationNode(1f, 10f),
                RotationNode(2f, 30f, GLSCatmullRomIds.Uniform),
                RotationNode(3f, 0f));

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(RotationAt(0.5f), 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void CatmullRomRotationScrubBackwardsMatchesForward()
        {
            LoadRotation(
                RotationNode(0f, 350f),
                RotationNode(1f, 10f),
                RotationNode(2f, 30f, GLSCatmullRomIds.Centripetal),
                RotationNode(3f, 0f));

            var forward = RotationAt(1.5f);
            RotationAt(2.8f);
            Assert.That(RotationAt(1.5f), Is.EqualTo(forward).Within(1e-4f));
        }

        // The held source's predecessor is p0 and the hold's own zero-delta interval keeps p0 = p1:
        // [0,10,Hold10,30,0] splines [10,10,30,0] to midpoint 21.875, not 22.5.
        [Test]
        public void CatmullRomRotationHeldSourceUsesItsOwnPredecessor()
        {
            LoadRotation(
                RotationNode(0f, 0f),
                RotationNode(1f, 10f),
                RotationNode(2f, 10f, usePrevious: 1),
                RotationNode(3f, 30f, GLSCatmullRomIds.Uniform),
                RotationNode(4f, 0f));

            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(RotationAt(2.5f), 21.875f)),
                Is.LessThan(0.01f));
        }

        // A looping extension is an animated destination, so it can carry the spline: p0=0, p1=10,
        // p2=370 (10 + one clockwise loop), p3=390 (p2 + the 20-degree incoming delta of the next node).
        [Test]
        public void CatmullRomRotationLoopExtensionSplines()
        {
            LoadRotation(
                RotationNode(0f, 0f),
                RotationNode(1f, 10f),
                RotationNode(
                    2f,
                    10f,
                    GLSCatmullRomIds.Uniform,
                    usePrevious: 1,
                    loop: 1,
                    direction: (int)LightRotationDirection.Clockwise),
                RotationNode(3f, 30f));

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(RotationAt(1.25f), 83.359375f)), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(RotationAt(1.5f), 189.375f)), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(RotationAt(1.75f), 295.703125f)), Is.LessThan(0.05f));
        }

        // A looping extension after the destination extends p3 by its own loop delta; a plain hold
        // extension duplicates p2 instead.
        [Test]
        public void CatmullRomRotationNextLoopExtensionExtendsP3()
        {
            LoadRotation(
                RotationNode(0f, 0f),
                RotationNode(1f, 10f),
                RotationNode(2f, 20f, GLSCatmullRomIds.Uniform),
                RotationNode(
                    3f,
                    20f,
                    usePrevious: 1,
                    loop: 1,
                    direction: (int)LightRotationDirection.Clockwise),
                RotationNode(4f, 0f));

            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(RotationAt(1.5f), -6.875f)),
                Is.LessThan(0.05f));
        }

        [Test]
        public void CatmullRomRotationNextHoldExtensionDuplicatesP3()
        {
            LoadRotation(
                RotationNode(0f, 0f),
                RotationNode(1f, 10f),
                RotationNode(2f, 20f, GLSCatmullRomIds.Uniform),
                RotationNode(3f, 20f, usePrevious: 1),
                RotationNode(4f, 0f));

            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(RotationAt(1.5f), 15.625f)),
                Is.LessThan(0.05f));
        }
    }

    // Color splines run through the shared LightColorTween preparation used by both playback and the
    // ribbon timeline; rendered ribbon strips must match live preview pixels on the same knots.
    public class GLSCatmullRomColorTest : GLSColorPlaybackTestBase
    {
        // Four grayscale nodes: black, .2, .6, black; the destination of the inner segment carries the
        // Catmull-Rom id in customData.easing.
        private static string SplineMapJson(int easing, string extraCustomData = null) =>
            @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}},
                    {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}},
                    {""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.6,0.6,0.6]"
                        + (extraCustomData ?? string.Empty)
                        + @",""easing"":" + easing + @"}},
                    {""b"":3,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}}]}]}]}";

        private static string LinearMapJson() =>
            @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}},
                    {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}},
                    {""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.6,0.6,0.6]}},
                    {""b"":3,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}}]}]}]}";

        // Interval easing stays Linear while only the color channel overrides with the spline.
        private static string ChannelOverrideMapJson(int colorEasing) =>
            @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}},
                    {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}},
                    {""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.6,0.6,0.6]"
                        + @",""colorEasing"":" + colorEasing + @"}},
                    {""b"":3,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}}]}]}]}";

        // Strobing nodes (f=1 hard strobe) with grayscale strobe colors so the strobe channel's own
        // spline is directly observable mid-pulse.
        private static string StrobeSplineMapJson(int strobeColorEasing) =>
            @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0],""strobeColor"":[0,0,0]}},
                    {""b"":1,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0],""strobeColor"":[0.2,0.2,0.2]}},
                    {""b"":2,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0],""strobeColor"":[0.6,0.6,0.6]"
                        + @",""strobeColorEasing"":" + strobeColorEasing + @"}},
                    {""b"":3,""c"":0,""s"":0,""i"":1,""f"":1,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0],""strobeColor"":[0,0,0]}}]}]}]}";

        // TrueHSV hues .9,.1,.3,.7 unwrap to -.1,.1,.3,.7 around the inner endpoints so the hue spline
        // midpoint is .1875 -> RGB (0.875,1,0).
        private static string HsvSplineMapJson(int easing) =>
            @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0.6]}},
                    {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0.6,0]}},
                    {""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,1,0]"
                        + @",""easing"":" + easing + @",""easingType"":""HSV""}},
                    {""b"":3,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0,1]}}]}]}]}";

        [TestCase(GLSCatmullRomIds.Uniform, 0.45f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 0.43535534f)]
        [TestCase(GLSCatmullRomIds.Chordal, 0.425f)]
        public void CatmullRomColorMidpointUsesSharedColorVectorKnots(int easing, float expectedGray)
        {
            LoadPlayback(SplineMapJson(easing));

            var color = ColorAt(0, 1.5f);
            Assert.That(color.r, Is.EqualTo(expectedGray).Within(1e-3f),
                $"RGB midpoint of the {easing} spline must use all four distributed colors.");
            Assert.That(color.a, Is.EqualTo(1f).Within(1e-3f));
        }

        [Test]
        public void LinearColorMidpointIsUnchanged()
        {
            LoadPlayback(LinearMapJson());

            Assert.That(ColorAt(0, 1.5f).r, Is.EqualTo(0.4f).Within(1e-3f));
        }

        // The extension at beat 2 (i=2) holds .2 into the spline segment: controls [.2,.2,.6,0] give
        // midpoint .4375, while resolving p0 from the resolved start's predecessor gives [0,.2,.6,0]=.45.
        private const string HoldSourceMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}},
                {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}},
                {""b"":2,""c"":0,""s"":1,""i"":2,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}},
                {""b"":3,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.6,0.6,0.6],""easing"":103}},
                {""b"":4,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}}]}]}]}";

        [Test]
        public void CatmullRomColorHeldSourceUsesItsOwnPredecessor()
        {
            LoadPlayback(HoldSourceMapJson);

            Assert.That(ColorAt(0, 2.5f).r, Is.EqualTo(0.4375f).Within(1e-3f),
                "A held source must supply its own predecessor as the spline's first control.");
        }

        [Test]
        public void CatmullRomHeldSourceRibbonMatchesLivePreview()
        {
            LoadPlayback(HoldSourceMapJson);

            AssertRibbonPixels(0, 0, 2, 2.25f, 0);
            AssertRibbonPixels(0, 0, 2, 2.5f, 0);
        }

        // An explicit colorEasing spline must spline the color channel while the interval easing itself
        // keeps driving brightness/alpha as Linear.
        [TestCase(GLSCatmullRomIds.Uniform, 0.45f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 0.43535534f)]
        [TestCase(GLSCatmullRomIds.Chordal, 0.425f)]
        public void ExplicitColorChannelOverrideSplinesOnlyColor(int colorEasing, float expectedGray)
        {
            LoadPlayback(ChannelOverrideMapJson(colorEasing));

            var color = ColorAt(0, 1.5f);
            Assert.That(color.r, Is.EqualTo(expectedGray).Within(1e-3f));
            Assert.That(color.a, Is.EqualTo(1f).Within(1e-3f));
        }

        // The strobe color track keeps its own easing; mid-pulse the strobe color must show the spline,
        // not a linear blend. At beat 1.75 the f=1 strobe phase is .75 >= pulse width .5, so the strobe
        // color wins; its spline value at color time .75 is Hermite(0,.2,.6,0)(.75), linear gives .5.
        [TestCase(GLSCatmullRomIds.Uniform, 0.565625f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 0.550758f)]
        [TestCase(GLSCatmullRomIds.Chordal, 0.5375f)]
        public void StrobeColorChannelSplinesIndependently(int strobeColorEasing, float expectedGray)
        {
            LoadPlayback(StrobeSplineMapJson(strobeColorEasing));

            var timeline = GLSEventCommon.GetColorTimeline(Node(0, 0, 2), LightCount);
            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.TryGetOutgoing(Node(0, 0, 1), 0, out var state), Is.True);
            var tween = new LightColorTween();
            timeline.ConfigureTween(tween, state, 0, appearance, _ => false);
            tween.UpdateTime(SongTime(1.75f));
            Assert.That(tween.Color.r, Is.EqualTo(expectedGray).Within(1e-3f));
        }

        // TrueHSV hue splines through the shortest-unwrapped knots: midpoint hue .1875 -> RGB (.875,1,0).
        [TestCase(GLSCatmullRomIds.Uniform)]
        public void TrueHsvCatmullRomSplinesUnwrappedHue(int easing)
        {
            LoadPlayback(HsvSplineMapJson(easing));

            var color = ColorAt(0, 1.5f);
            Assert.That(color.r, Is.EqualTo(0.875f).Within(0.01f));
            Assert.That(color.g, Is.EqualTo(1f).Within(0.01f));
        }

        // The inner interval itself crosses the hue seam: hues .8,.9,.1,.2 unwrap to .8,.9,1.1,1.2 and
        // the spline midpoint lands exactly on hue 1.0 -> pure red. Without unwrapping the midpoint hue
        // would run backward through -.3/.7 instead.
        private static string HsvInnerSeamMapJson(int easing) =>
            @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
                {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                    {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.8,0,1]}},
                    {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0,0.6]}},
                    {""b"":2,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[1,0.6,0]"
                        + @",""easing"":" + easing + @",""easingType"":""HSV""}},
                    {""b"":3,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.8,1,0]}}]}]}]}";

        [TestCase(GLSCatmullRomIds.Uniform)]
        public void TrueHsvInnerSeamUnwrapsAroundInterval(int easing)
        {
            LoadPlayback(HsvInnerSeamMapJson(easing));

            var color = ColorAt(0, 1.5f);
            Assert.That(color.r, Is.EqualTo(1f).Within(0.02f));
            Assert.That(color.g, Is.EqualTo(0f).Within(0.02f));
            Assert.That(color.b, Is.EqualTo(0f).Within(0.02f));
        }

        // Rendered ribbon pixels must equal the live preview light for the same prepared spline at
        // non-midpoint samples too; AssertRibbonPixels renders the GPU timeline texture directly.
        [TestCase(GLSCatmullRomIds.Uniform)]
        [TestCase(GLSCatmullRomIds.Centripetal)]
        [TestCase(GLSCatmullRomIds.Chordal)]
        public void CatmullRomRibbonMatchesLivePreview(int easing)
        {
            LoadPlayback(SplineMapJson(easing));

            AssertRibbonPixels(0, 0, 1, 1.25f, 0);
            AssertRibbonPixels(0, 0, 1, 1.5f, 0);
            AssertRibbonPixels(0, 0, 1, 1.75f, 0);
        }

        [TestCase(GLSCatmullRomIds.Uniform)]
        public void TrueHsvRibbonMatchesLivePreview(int easing)
        {
            LoadPlayback(HsvSplineMapJson(easing));

            AssertRibbonPixels(0, 0, 1, 1.5f, 0);
        }
    }

    // TrackChange must invalidate the spline controls on both sides of an edit: a changed node is an
    // outer control for the segment ending at its predecessor, so the predecessor's predecessor needs
    // a ribbon refresh too.
    public class GLSCatmullRomTimelineInvalidationTest : GLSColorPlaybackTestBase
    {
        // Group 2 interrupts group 1 at beat 2, so its own box must supply the outer spline control the
        // edit rewrites; group 1 events after beat 2 are clipped by the interruption and never generate.
        private const string TwoGroupMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}},
                {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}}]}]},
            {""b"":2,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.9,0.9,0.9],""easing"":103}},
                {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.6,0.6,0.6]}}]}]}]}";

        // Mutating the fourth control of segment 1->2 through the real group replacement callbacks must
        // change that segment's prepared color and ribbon pixel even though both endpoints are untouched,
        // mark the segment before it as changed, and restore both when the edit is reverted.
        [Test]
        public void OuterControlEditMarksSecondNeighborhood()
        {
            LoadPlayback(TwoGroupMapJson);
            var timeline = GLSEventCommon.GetColorTimeline(Node(0, 0, 1), LightCount);
            Assert.That(timeline, Is.Not.Null);
            var baseline = TimelineColorAt(Node(0, 0, 1), 1.5f);
            var baselineRibbon = RibbonPixelAt(Node(0, 0, 1), 1.5f);
            // Drain the initial build's refresh batch so the post-edit collect only reports this edit.
            GLSEventCommon.TryCollectChangedColorTransitions(
                new HashSet<BaseLightColorBase>(),
                new Dictionary<BaseEventBoxGroup, HashSet<float>>());

            var owner = map.LightColorEventBoxGroups[0];
            var targetGroup = map.LightColorEventBoxGroups[1];
            var replacement = BeatmapFactory.Clone(targetGroup) as BaseLightColorEventBoxGroup;
            replacement.Boxes[0].Events[1].CustomColor = new Color(0f, 0f, 0f);
            ReplaceGroupInTimeline(targetGroup, replacement);

            var ownerNodes = owner.Boxes[0].Events;
            var changed = new HashSet<BaseLightColorBase>();
            Assert.That(
                GLSEventCommon.TryCollectChangedColorTransitions(
                    changed,
                    new Dictionary<BaseEventBoxGroup, HashSet<float>>()),
                Is.True,
                "The group edit must report an incremental ribbon refresh batch.");
            Assert.That(changed, Contains.Item(ownerNodes[0]),
                "The segment owned by node 0 must refresh: the interrupting node is its fourth spline control.");
            Assert.That(changed, Contains.Item(ownerNodes[1]));

            var edited = TimelineColorAt(ownerNodes[1], 1.5f);
            Assert.That(edited.r, Is.EqualTo(0.61875f).Within(1e-3f),
                "The interrupting group's second node must reach segment 1->2 as its fourth spline control.");
            Assert.That(edited.r, Is.Not.EqualTo(baseline.r).Within(1e-3f));
            var editedRibbon = RibbonPixelAt(ownerNodes[1], 1.5f);
            Assert.That(editedRibbon.r, Is.Not.EqualTo(baselineRibbon.r).Within(0.01f),
                "The ribbon over segment 1->2 must redraw with the rewritten fourth control.");

            ReplaceGroupInTimeline(replacement, targetGroup);

            var restored = owner.Boxes[0].Events;
            Assert.That(TimelineColorAt(restored[1], 1.5f).r, Is.EqualTo(baseline.r).Within(1e-3f));
            Assert.That(RibbonPixelAt(restored[1], 1.5f).r, Is.EqualTo(baselineRibbon.r).Within(0.01f));
        }

        // Group 1 holds .2 through an extension at beat 2; the segment H->C splines [.2,.2,.6,D].
        private const string ThreeGroupMapJson = @"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[
            {""b"":0,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}},
                {""b"":1,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}},
                {""b"":2,""c"":0,""s"":1,""i"":2,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.2,0.2,0.2]}}]}]},
            {""b"":3,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0.6,0.6,0.6],""easing"":103}}]}]},
            {""b"":4,""g"":1,""e"":[{""f"":{""f"":1,""p"":1},""w"":0,""d"":1,""r"":0,""t"":1,""b"":0,""i"":0,""e"":[
                {""b"":0,""c"":0,""s"":1,""i"":1,""f"":0,""sb"":1,""sf"":0,""customData"":{""color"":[0,0,0]}}]}]}]}";

        // Editing only D must propagate the rewritten fourth control to the held segment H->C and its
        // ribbon refresh batch. Controls [.2,.2,.6,1] move the midpoint from .4375 to .375.
        [Test]
        public void OuterControlEditMarksExtensionOwner()
        {
            LoadPlayback(ThreeGroupMapJson);
            var ownerNodes = map.LightColorEventBoxGroups[0].Boxes[0].Events;
            var heldSource = ownerNodes[2];
            Assert.That(TimelineColorAt(heldSource, 2.5f).r, Is.EqualTo(0.4375f).Within(1e-3f));
            var baselineRibbon = RibbonPixelAt(heldSource, 2.5f);
            // Drain the initial build's refresh batch so the post-edit collect only reports this edit.
            GLSEventCommon.TryCollectChangedColorTransitions(
                new HashSet<BaseLightColorBase>(),
                new Dictionary<BaseEventBoxGroup, HashSet<float>>());

            var targetGroup = map.LightColorEventBoxGroups[2];
            var replacement = BeatmapFactory.Clone(targetGroup) as BaseLightColorEventBoxGroup;
            replacement.Boxes[0].Events[0].CustomColor = new Color(1f, 1f, 1f);
            ReplaceGroupInTimeline(targetGroup, replacement);

            var changed = new HashSet<BaseLightColorBase>();
            Assert.That(
                GLSEventCommon.TryCollectChangedColorTransitions(
                    changed,
                    new Dictionary<BaseEventBoxGroup, HashSet<float>>()),
                Is.True);
            Assert.That(changed, Contains.Item(heldSource),
                "The group edit must include the held segment's owner in the ribbon refresh batch.");

            Assert.That(TimelineColorAt(heldSource, 2.5f).r, Is.EqualTo(0.375f).Within(1e-3f));
            Assert.That(
                RibbonPixelAt(heldSource, 2.5f).r,
                Is.Not.EqualTo(baselineRibbon.r).Within(0.01f),
                "The ribbon over the held segment must redraw with the rewritten fourth control.");
        }

        // Mirror the group container's spawn/delete callbacks for a modify swap; the scene collections
        // stay bound to the loaded map rather than this fixture's synthetic map.
        private void ReplaceGroupInTimeline(
            BaseLightColorEventBoxGroup removedGroup,
            BaseLightColorEventBoxGroup insertedGroup)
        {
            var index = map.LightColorEventBoxGroups.IndexOf(removedGroup);
            map.LightColorEventBoxGroups.RemoveAt(index);
            GLSEventCommon.RemoveColorTransitionGroup(removedGroup);
            insertedGroup.SetMap(map);
            insertedGroup.RecomputeSongBpmTime();
            map.LightColorEventBoxGroups.Insert(index, insertedGroup);
            GLSEventCommon.AddColorTransitionGroup(insertedGroup);
        }

        private Color TimelineColorAt(BaseLightColorBase source, float beat)
        {
            var timeline = GLSEventCommon.GetColorTimeline(source, LightCount);
            Assert.That(timeline.TryGetOutgoing(source, 0, out var state), Is.True);
            var tween = new LightColorTween();
            timeline.ConfigureTween(tween, state, 0, appearance, _ => false);
            tween.UpdateTime(SongTime(beat));
            return tween.Color;
        }

        private Color RibbonPixelAt(BaseLightColorBase source, float beat)
        {
            var ribbonObject = new GameObject("GLS mutation ribbon");
            Material material = null;
            try
            {
                var ribbon = GLSColorTransitionCacheTest.CreateRibbonController(ribbonObject, out var renderer);
                GLSEventCommon.UpdateColorTransitionRibbon(ribbon, source, appearance, _ => false, LightCount);
                Assert.IsTrue(ribbonObject.activeSelf);
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                material = GLSColorTransitionCacheTest.CreateWaveSampleMaterial(properties);
                var duration = ribbon.transform.localScale.x / (EditorScaleController.EditorScale * (4f / 3f));
                var progress = (SongTime(beat) - source.SongBpmTime) / duration;
                var lane = (LightCount - 0.5f) / LightCount;
                return GLSColorTransitionCacheTest.RenderGradientPixel(material, progress, lane);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(ribbonObject);
            }
        }
    }

    public class GLSCatmullRomSerializationTest : TestBase
    {
        // V3 color carries custom easings in customData.easing while native i stays Interpolate.
        [Test]
        public void V3ColorRoundTripsCatmullRom([Values(103, 104, 105)] int easing)
        {
            var evt = new BaseLightColorBase
            {
                Easing = easing,
                Brightness = 1f,
                CustomData = new JSONObject()
            };
            var node = V3LightColorBase.ToJson(evt);

            Assert.That(node["i"].AsInt, Is.EqualTo(1), "V3 must keep native interpolate output.");
            Assert.That(node["customData"]["easing"].AsInt, Is.EqualTo(easing));

            var loaded = V3LightColorBase.GetFromJson(node);
            Assert.That(loaded.Easing, Is.EqualTo(easing));
        }

        // The same keys must survive the documented V3 file layout: customData.easing loads as the easing.
        [Test]
        public void V3ColorLoadsAuthoredCatmullRomKey([Values(103, 104, 105)] int easing)
        {
            var loaded = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"i\":1,\"s\":1,\"customData\":{\"easing\":" + easing + "}}"));

            Assert.That(loaded.Easing, Is.EqualTo(easing));
        }

        // Per-channel keys carry their own spline ids independently of the interval easing.
        [Test]
        public void V3ColorChannelKeysRoundTripCatmullRom([Values(103, 104, 105)] int easing)
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"i\":1,\"s\":1,\"customData\":{\"colorEasing\":" + easing
                + ",\"strobeColorEasing\":" + easing + "}}"));

            Assert.That(evt.ChromaColorEasing, Is.EqualTo(easing));
            Assert.That(evt.ChromaStrobeColorEasing, Is.EqualTo(easing));
        }

        // The strobe pulse-fade easing interpolates within a single pulse, so it has no temporal
        // neighbors; authored spline ids must be ignored there.
        [Test]
        public void V3StrobePulseFadeIgnoresCatmullRom([Values(103, 104, 105)] int easing)
        {
            var evt = V3LightColorBase.GetFromJson(JSON.Parse(
                "{\"i\":1,\"s\":1,\"sf\":1,\"customData\":{\"strobeEasing\":" + easing + "}}"));

            Assert.That(evt.ChromaStrobeEasing, Is.Null,
                "Strobe pulse fade has no four-point context and must reject spline ids.");
        }

        [Test]
        public void V3RotationTranslationFloatFxRoundTripCatmullRom([Values(103, 104, 105)] int easing)
        {
            var rotation = V3LightRotationBase.GetFromJson(
                V3LightRotationBase.ToJson(new BaseLightRotationBase { Easing = easing }));
            var translation = V3LightTranslationBase.GetFromJson(
                V3LightTranslationBase.ToJson(new BaseLightTranslationBase { Easing = easing }));
            var fx = V3FloatFxEvent.GetFromJson(
                V3FloatFxEvent.ToJson(new BaseFxEventFloat { Easing = easing }));

            Assert.That(rotation.Easing, Is.EqualTo(easing));
            Assert.That(translation.Easing, Is.EqualTo(easing));
            Assert.That(fx.Easing, Is.EqualTo(easing));
        }

        [Test]
        public void V4CommonDataRoundTripsCatmullRom([Values(103, 104, 105)] int easing)
        {
            var color = V4CommonData.LightColorEvent.FromBaseLightColorEvent(
                new BaseLightColorBase { Easing = easing });
            var rotation = V4CommonData.LightRotationEvent.FromBaseLightRotationEvent(
                new BaseLightRotationBase { Easing = easing });
            var translation = V4CommonData.LightTranslationEvent.FromBaseLightTranslationEvent(
                new BaseLightTranslationBase { Easing = easing });
            var fx = V4CommonData.FloatFxEvent.FromFloatFxEventBase(
                new BaseFxEventFloat { Easing = easing });

            Assert.That(color.Easing, Is.EqualTo(easing));
            Assert.That(rotation.Easing, Is.EqualTo(easing));
            Assert.That(translation.Easing, Is.EqualTo(easing));
            Assert.That(fx.Easing, Is.EqualTo(easing));
            Assert.That(V4CommonData.LightColorEvent.GetFromJson(color.ToJson()).Easing, Is.EqualTo(easing));
            Assert.That(V4CommonData.LightRotationEvent.GetFromJson(rotation.ToJson()).Easing, Is.EqualTo(easing));
            Assert.That(V4CommonData.LightTranslationEvent.GetFromJson(translation.ToJson()).Easing, Is.EqualTo(easing));
            Assert.That(V4CommonData.FloatFxEvent.GetFromJson(fx.ToJson()).Easing, Is.EqualTo(easing));
        }

        // Existing stable ids must keep their exact V3 layout: 102 lands in customData.easing too.
        [Test]
        public void StableBeatSaberIdsKeepExistingLayout()
        {
            var evt = new BaseLightColorBase
            {
                Easing = (int)EaseType.BeatSaberInOutBounce,
                Brightness = 1f,
                CustomData = new JSONObject()
            };
            var node = V3LightColorBase.ToJson(evt);

            Assert.That(node["i"].AsInt, Is.EqualTo(1));
            Assert.That(node["customData"]["easing"].AsInt, Is.EqualTo((int)EaseType.BeatSaberInOutBounce));
        }
    }

    public class GLSCatmullRomIconTest : TestBase
    {
        private const int IconBase = (int)GLSEventIconType.NoEasingStep + 1;

        // The hover cycle includes the three spline variants for event easings but excludes them from
        // the strobe pulse-fade list, which has no temporal neighbors.
        [Test]
        public void HoverCycleOffersCatmullRomOnlyForEventEasings()
        {
            var allValues = (EaseType[])typeof(GLSEventHoverMutation)
                .GetField("AllEasingValues", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null);
            var strobeValues = (EaseType[])typeof(GLSEventHoverMutation)
                .GetField("StrobeFadeEasingValues", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null);

            foreach (var id in GLSCatmullRomIds.All)
            {
                Assert.That(allValues, Contains.Item((EaseType)id), $"easing {id} must cycle on events.");
                Assert.That(strobeValues, Has.No.Member((EaseType)id),
                    $"easing {id} has no temporal neighbors in strobe pulse fade.");
            }
        }

        [TestCase(103)]
        [TestCase(104)]
        [TestCase(105)]
        public void ResolverMapsCatmullRomToUniqueIcons(int easing)
        {
            var index = easing - GLSCatmullRomIds.Uniform;
            var expected = (GLSEventIconType)(IconBase + index);

            var translation = GLSEventIconResolver.Resolve(new BaseLightTranslationBase { Easing = easing });
            var rotation = GLSEventIconResolver.Resolve(new BaseLightRotationBase { Easing = easing });
            var fx = GLSEventIconResolver.Resolve(new BaseFxEventFloat { Easing = easing });
            var color = GLSEventIconResolver.Resolve(new BaseLightColorBase { Easing = easing });

            Assert.AreEqual(expected, translation.Primary);
            Assert.AreEqual(expected, rotation.Primary);
            Assert.AreEqual(expected, fx.Primary);
            Assert.AreEqual(expected, color.Primary);
        }

        [TestCase(103, "splineCatmullRom", "Cat")]
        [TestCase(104, "splineCatmullRomCentripetal", "Cat.5")]
        [TestCase(105, "splineCatmullRomChordal", "Cat1")]
        public void CatmullRomShortLabelsAbbreviateToCat(int easing, string internalName, string expected)
        {
            Assert.AreEqual(expected, Easing.IDToShortName[easing]);
            Assert.AreEqual(expected, Easing.InternalNameToShortName[internalName]);
        }

        // Generated sprites are the product: verify each exists and that all three share identical
        // interior marker centers and endpoint positions while drawing different curves.
        [TestCase(0, "EaseCatmullRom")]
        [TestCase(1, "EaseCatmullRomCentripetal")]
        [TestCase(2, "EaseCatmullRomChordal")]
        public void CatmullRomSpritesShareMarkerGeometry(int variant, string spriteName)
        {
            var path = $"Assets/_Graphics/Textures/GLS Event Icons/Easings/{spriteName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.IsNotNull(sprite, $"Generated sprite {path} is missing.");

            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(texture.LoadImage(bytes), $"Failed to decode {path}.");
            try
            {
                AssertMarkerDisks(texture, variant);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // Interior markers: the black/white/black disk stack is composited at 65% opacity over the
        // opaque curve on the two interior sample points (.38,.82) and (.62,.18) of the shared 4-point
        // spline. Marker radii are 6.75, 18, and 24.75 pixels. Probes step vertically outward, away
        // from the curve, so a marker over transparent background reads alpha .65 instead of opaque.
        private static void AssertMarkerDisks(Texture2D texture, int variant)
        {
            var width = texture.width;
            var height = texture.height;
            var centers = new[]
            {
                new Vector2(0.38f, 0.82f),
                new Vector2(0.62f, 0.18f)
            };
            foreach (var point in centers)
            {
                // The generator maps normalized points into the margin-inset canvas the same way for
                // every easing: x = margin + nx*(width-2*margin), y measured up from the bottom.
                var x = Mathf.RoundToInt(13f + (point.x * (width - 26f)));
                var y = Mathf.RoundToInt(13f + (point.y * (height - 26f)));
                var outward = point.y > 0.5f ? 1 : -1;

                // The curve passes under the marker center, so the 65% black center disk
                // blends to a fully opaque dark gray rather than hiding the curve.
                var center = texture.GetPixel(x, y);
                Assert.That(center.a, Is.GreaterThan(0.98f),
                    $"Variant {variant} marker center at ({x},{y}) must be opaque over the curve.");
                Assert.That(center.r, Is.EqualTo(0.35f).Within(0.12f),
                    $"Variant {variant} marker center red channel must show the curve through the disk.");
                Assert.That(center.g, Is.EqualTo(0.35f).Within(0.12f),
                    $"Variant {variant} marker center green channel must show the curve through the disk.");
                Assert.That(center.b, Is.EqualTo(0.35f).Within(0.12f),
                    $"Variant {variant} marker center blue channel must show the curve through the disk.");

                var ring = texture.GetPixel(x, y + (15 * outward));
                Assert.That(ring.a, Is.EqualTo(0.65f).Within(0.02f),
                    $"Variant {variant} marker ring at ({x},{y + 15 * outward}) must be 65% opaque.");
                Assert.That(ring.r, Is.GreaterThan(0.9f),
                    $"Variant {variant} marker ring must stay white at 65% alpha.");
                Assert.That(ring.g, Is.GreaterThan(0.9f),
                    $"Variant {variant} marker ring must stay white at 65% alpha.");
                Assert.That(ring.b, Is.GreaterThan(0.9f),
                    $"Variant {variant} marker ring must stay white at 65% alpha.");

                var outer = texture.GetPixel(x, y + (21 * outward));
                Assert.That(outer.a, Is.EqualTo(0.65f).Within(0.02f),
                    $"Variant {variant} outer ring at ({x},{y + 21 * outward}) must be 65% opaque.");
                Assert.That(outer.r, Is.LessThan(0.1f),
                    $"Variant {variant} outer ring must stay black at 65% alpha.");
                Assert.That(outer.g, Is.LessThan(0.1f),
                    $"Variant {variant} outer ring must stay black at 65% alpha.");
                Assert.That(outer.b, Is.LessThan(0.1f),
                    $"Variant {variant} outer ring must stay black at 65% alpha.");

                // Just past the 24.75px outer radius the marker must end, proving the disk diameter
                // is unchanged by the 65% compositing.
                var beyond = texture.GetPixel(x, y + (26 * outward));
                Assert.That(beyond.a, Is.LessThan(0.05f),
                    $"Variant {variant} has marker spill past the outer radius at ({x},{y + 26 * outward}).");
            }

            // The margin column and the spline endpoints stay untouched: no marker coverage off the
            // interior points, and the white curve endpoints remain fully opaque.
            Assert.That(texture.GetPixel(0, height / 2).a, Is.LessThan(0.05f),
                $"Variant {variant} must leave the left margin transparent.");
            var start = texture.GetPixel(13, 13);
            Assert.That(start.a, Is.GreaterThan(0.98f),
                $"Variant {variant} start endpoint must be opaque.");
            Assert.That(start.r + start.g + start.b, Is.GreaterThan(2.4f),
                $"Variant {variant} start endpoint must be the white curve.");
            var end = texture.GetPixel(width - 13, height - 13);
            Assert.That(end.a, Is.GreaterThan(0.98f),
                $"Variant {variant} end endpoint must be opaque.");
            Assert.That(end.r + end.g + end.b, Is.GreaterThan(2.4f),
                $"Variant {variant} end endpoint must be the white curve.");
        }
    }
}
