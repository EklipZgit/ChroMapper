using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    /// <summary>
    ///     FloatFx continuity regressions for time-scaled Catmull-Rom tangents. Nodes arrive at uneven
    ///     beats so the spans meeting at each interior node differ in duration; a per-segment normalized
    ///     tangent used to produce a kink there. The frozen map has no Catmull-Rom FloatFx content, so
    ///     these use small production-loaded groups through the same shared spline helper.
    /// </summary>
    public class GLSCatmullRomContinuityFloatFxTest : TestBase
    {
        private const int GroupId = 913;
        private FloatFxGroupEffect effect;
        private RecordingFxTarget target;
        private GameObject root;

        [SetUp]
        public void Setup() => root = new GameObject(nameof(GLSCatmullRomContinuityFloatFxTest));

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(root);

        private static BaseFxEventFloat Node(float beat, float value, int easing = 1, int usePrevious = 0) =>
            new()
            {
                RelativeJsonTime = beat,
                Value = value,
                Easing = easing,
                UsePrevious = usePrevious
            };

        private void LoadFx(params BaseFxEventFloat[] nodes)
        {
            effect = root.AddComponent<FloatFxGroupEffect>();
            effect.Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            effect.ID = GroupId;
            effect.Count = 1;
            target = root.AddComponent<RecordingFxTarget>();
            effect.Register(0, target);
            effect.Initialize();
            var group = new BaseVfxEventEventBoxGroup { JsonTime = 0, ID = GroupId };
            group.Boxes.Add(new BaseVfxEventEventBox
            {
                IndexFilter = new BaseIndexFilter
                {
                    Type = (int)IndexFilterType.Division, Param0 = 1, Param1 = 0
                },
                Events = nodes
            });
            group.NormalizeLoadedEventConflicts();
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            effect.InsertData(group);
            effect.Refresh();
        }

        private float ValueAt(float beat)
        {
            effect.UpdateTime(false, beat);
            return target.Value;
        }

        // Segments [1, 2.5] and [2.5, 5] meet at the CR node with unequal durations. The shared node
        // velocity must be identical from both sides; the midpoint and shared rate are per profile.
        [TestCase(GLSCatmullRomIds.Uniform, 16.96875f, -2.5f)]
        [TestCase(GLSCatmullRomIds.Centripetal, 16.5f, 0f)]
        [TestCase(GLSCatmullRomIds.Chordal, 16.03125f, 2.5f)]
        public void FloatFxUnequalSpacingSharesVelocityAtNode(int easing, float expectedMid, float expectedRate)
        {
            LoadFx(Node(0, 0), Node(1, 10), Node(2.5f, 20, easing), Node(5, 0, easing));
            Assert.That(ValueAt(1.75f), Is.EqualTo(expectedMid).Within(0.001f));

            var (incoming, outgoing) = GLSCatmullRomContinuityTest.NodeVelocities(
                ValueAt, GLSCatmullRomContinuityTest.Subtract, 2.5f, 1f, 5f);
            Assert.That(outgoing, Is.EqualTo(incoming).Within(0.01f), "velocity continuity at beat 2.5");
            Assert.That(outgoing, Is.EqualTo(expectedRate).Within(0.01f), "shared node velocity");
        }

        // Two chained holds at beats 2 and 3 resolve their value back to the beat-1 node, but the
        // segment into the beat-6 node must use the real predecessor timestamp (2), not the resolved
        // value source's time (1). The wrong time gives 16.6875 here; geometric tangents gave 16.25.
        [Test]
        public void HeldOwnersKeepActualNeighborTimestamp()
        {
            LoadFx(
                Node(0, 0),
                Node(1, 10),
                Node(2, 10, usePrevious: 1),
                Node(3, 10, GLSCatmullRomIds.Uniform, usePrevious: 1),
                Node(6, 20, GLSCatmullRomIds.Uniform),
                Node(7, 0));
            Assert.That(ValueAt(4.5f), Is.EqualTo(16.875f).Within(0.001f));
        }

        // A 103 -> 104 -> 105 chain: the tangent at each node belongs to that node's own profile, so the
        // shared velocity stays continuous even though every segment carries a different destination.
        [Test]
        public void MixedProfilesShareVelocityAtEachNode()
        {
            LoadFx(
                Node(0, 0),
                Node(1, 10, GLSCatmullRomIds.Uniform),
                Node(2.5f, 20, GLSCatmullRomIds.Centripetal),
                Node(5, 0, GLSCatmullRomIds.Chordal));

            Assert.That(ValueAt(1.75f), Is.EqualTo(16.5f).Within(0.001f));

            var atOne = GLSCatmullRomContinuityTest.NodeVelocities(
                ValueAt, GLSCatmullRomContinuityTest.Subtract, 1f, 0f, 2.5f);
            Assert.That(atOne.Outgoing, Is.EqualTo(atOne.Incoming).Within(0.01f), "C1 at beat 1");
            Assert.That(atOne.Outgoing, Is.EqualTo(8f).Within(0.01f), "owned velocity at beat 1");

            var atTwoHalf = GLSCatmullRomContinuityTest.NodeVelocities(
                ValueAt, GLSCatmullRomContinuityTest.Subtract, 2.5f, 1f, 5f);
            Assert.That(atTwoHalf.Outgoing, Is.EqualTo(atTwoHalf.Incoming).Within(0.01f), "C1 at beat 2.5");
            Assert.That(atTwoHalf.Outgoing, Is.EqualTo(0f).Within(0.01f), "centripetal peak stops at beat 2.5");
        }

        // Repeating 10 at beats 1 and 2.5 gives the shared node a zero chord: centripetal/chordal stop
        // on both sides, so the repeat stays flat instead of borrowing a nonzero neighbor slope.
        [Test]
        public void RepeatedValueStopsVelocityOnBothSides()
        {
            LoadFx(
                Node(0, 0),
                Node(1, 10),
                Node(2.5f, 10, GLSCatmullRomIds.Centripetal),
                Node(4, 0, GLSCatmullRomIds.Centripetal));
            Assert.That(ValueAt(1.75f), Is.EqualTo(10f).Within(0.001f));
            Assert.That(ValueAt(3.25f), Is.EqualTo(5f).Within(0.001f));

            var (incoming, outgoing) = GLSCatmullRomContinuityTest.NodeVelocities(
                ValueAt, GLSCatmullRomContinuityTest.Subtract, 2.5f, 1f, 4f);
            Assert.That(incoming, Is.EqualTo(0f).Within(0.01f), "slope into repeated node");
            Assert.That(outgoing, Is.EqualTo(0f).Within(0.01f), "slope out of repeated node");
        }

        private sealed class RecordingFxTarget : FxTarget
        {
            public float Value;
            public override void SetValue(int group, int id, float value) => Value = value;
            public override void TriggerValue(int group, int id, float value) => Value = value;
        }
    }
}
