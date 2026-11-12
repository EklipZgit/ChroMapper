using System.IO;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class NulctrlRingPhysicsParityTest
    {
        [Test]
        public void CapturedWaveAdvancesToTheNextNativeRingState()
        {
            var captures = JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath,
                "Tests/Fixtures/NulctrlRingPhysicsExpectations.json")));
            var before = captures[0];
            var after = captures[1];
            var states = new RingRotationState[before.Count];
            for (var index = 0; index < states.Length; index++)
            {
                states[index] = new RingRotationState
                {
                    Rotation = before[index]["rotation"].AsFloat,
                    Destination = before[index]["destination"].AsFloat,
                    Speed = before[index]["speed"].AsFloat
                };
            }
            // The beat-43 wave reaches ring 5 between the captures. Its recorded step avoids comparing different random runs.
            var waves = new[] { new RingRotationWave
            {
                Created = true, NextFrame = 1, Progress = 5f,
                FirstRingDestination = after[0]["destination"].AsFloat,
                Step = after[1]["destination"].AsFloat - after[0]["destination"].AsFloat,
                Propagation = 1f, Speed = after[0]["speed"].AsFloat
            } };
            var count = waves.Length;
            TrackLaneRingsRotationEffect.AdvanceState(states, waves, ref count, 0, 1, states.Length);
            for (var index = 0; index < states.Length; index++)
            {
                Assert.That(states[index].Destination, Is.EqualTo(after[index]["destination"].AsFloat).Within(0.001f),
                    $"Ring at Z={after[index]["z"]} must receive the captured wave destination.");
                Assert.That(states[index].Rotation, Is.EqualTo(after[index]["rotation"].AsFloat).Within(0.001f),
                    $"Ring at Z={after[index]["z"]} must match the next native 50 Hz physics state.");
            }
        }
    }
}
