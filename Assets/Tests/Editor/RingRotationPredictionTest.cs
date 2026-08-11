using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class RingRotationPredictionTest
    {
        private const string FixtureRoot = "Assets/Tests/Fixtures/";
        private const string Fixture170Fps = "RingRotationTest_170fps";
        private const string Fixture120Fps = "RingRotationTest_120fps";
        private const string Fixture90Fps = "RingRotationTest_90fps";
        private const string SystemName = "SmallTrackLaneRings";
        private const int RingCount = 20;

        private const float ContinuousStateTolerance = 0.2f;
        private const float MomentumContinuousStateTolerance = 0.05f;
        private const float SpeedContinuousStateTolerance = 0.0001f;

        [Test]
        public void RingRotationTest_170fps_SchedulerMatchesCapturedAssignmentFrames() =>
            AssertSchedulerMatchesCapturedFrames(Fixture170Fps, "170fps");

        [Test]
        public void RingRotationTest_120fps_SchedulerMatchesCapturedAssignmentFrames() =>
            AssertSchedulerMatchesCapturedFrames(Fixture120Fps, "120fps");

        [Test]
        public void RingRotationTest_90fps_SchedulerMatchesCapturedAssignmentFrames() =>
            AssertSchedulerMatchesCapturedFrames(Fixture90Fps, "90fps");

        private static void AssertSchedulerMatchesCapturedFrames(string fixtureFolder, string fixtureName)
        {
            var fixture = LoadCapturedFixture(fixtureFolder);
            var bpmInfo = JSON.Parse(LoadFixtureText(fixtureFolder, "BPMInfo.dat"));
            var authoredEvents = LoadRingEvents(fixtureFolder);
            var authoredWaves = fixture.Waves.Skip(1).ToArray();
            Assert.That(authoredEvents.Length, Is.EqualTo(authoredWaves.Length));

            var firstPredictedFrame = GetPredictedFirstAssignmentFrame(
                authoredEvents[0]["b"].AsFloat,
                bpmInfo,
                fixture.FixedDeltaTime);
            var capturedSequenceOffset = authoredWaves[0].FixedSequence + 1 - firstPredictedFrame;
            var mismatches = new List<string>();
            for (var i = 0; i < authoredWaves.Length; i++)
            {
                var beat = authoredEvents[i]["b"].AsFloat;
                var predictedFrame = GetPredictedFirstAssignmentFrame(beat, bpmInfo, fixture.FixedDeltaTime);
                var capturedFrame = authoredWaves[i].FixedSequence + 1 - capturedSequenceOffset;
                // Beat Saber dispatches on a render callback, so it can start on the
                // following fixed tick. ChroMapper may be one fixed frame early, but it
                // must never start after Beat Saber or more than one frame before it.
                var delta = predictedFrame - capturedFrame;
                if (delta is 0 or -1)
                    continue;

                var callbackFrame = Mathf.FloorToInt(
                    authoredWaves[i].CallbackSeconds / fixture.FixedDeltaTime) + 1;
                mismatches.Add(
                    $"Wave {authoredWaves[i].WaveId} beat={beat:R}: CM={predictedFrame}, "
                    + $"BeatSaber={capturedFrame}, callback={authoredWaves[i].CallbackSeconds:R}s "
                    + $"(frame={callbackFrame}), delta={delta:+#;-#;0}");
            }

            if (mismatches.Count > 0)
                Assert.Fail($"{fixtureName}: {mismatches.Count} ring waves start on the wrong fixed frame:\n" + string.Join("\n", mismatches));
        }

        // TEST CONTRACT: map files are the only ChroMapper simulation input. Captured
        // CSV values are output expectations only; never inject their wave frames, callback
        // times, destinations, or state into the simulator under test.
        [Test]
        public void RingRotationTest_170fps_MapPlaybackMatchesCapturedHalfBeatStates() =>
            AssertMapPlaybackMatchesCapturedStates(Fixture170Fps, "170fps", 170f);

        [Test]
        public void RingRotationTest_120fps_MapPlaybackMatchesCapturedHalfBeatStates() =>
            AssertMapPlaybackMatchesCapturedStates(Fixture120Fps, "120fps", 120f);

        [Test]
        public void RingRotationTest_90fps_MapPlaybackMatchesCapturedHalfBeatStates() =>
            AssertMapPlaybackMatchesCapturedStates(Fixture90Fps, "90fps", 90f);

        [Test]
        public void RingRotationTest_170fps_FixtureMatchesMapAndCaptureIsComplete() =>
            AssertFixtureMatchesMapAndIsComplete(Fixture170Fps);

        [Test]
        public void RingRotationTest_120fps_FixtureMatchesMapAndCaptureIsComplete() =>
            AssertFixtureMatchesMapAndIsComplete(Fixture120Fps);

        [Test]
        public void RingRotationTest_90fps_FixtureMatchesMapAndCaptureIsComplete() =>
            AssertFixtureMatchesMapAndIsComplete(Fixture90Fps);

        private static void AssertMapPlaybackMatchesCapturedStates(
            string fixtureFolder,
            string fixtureName,
            float refreshRate)
        {
            var fixture = LoadCapturedFixture(fixtureFolder);
            AssertMapMatchesCapturedWaves(fixtureFolder, fixture.Waves);
            var bpmInfo = JSON.Parse(LoadFixtureText(fixtureFolder, "BPMInfo.dat"));
            var authoredEvents = LoadRingEvents(fixtureFolder);
            var authoredWaves = fixture.Waves.Skip(1).ToArray();
            var firstPredictedFrame = GetRenderAliasedAssignmentFrame(
                authoredEvents[0]["b"].AsFloat,
                bpmInfo,
                fixture.FixedDeltaTime,
                refreshRate,
                fixture.FirstFixedTime);
            var capturedSequenceOffset = authoredWaves[0].FixedSequence + 1 - firstPredictedFrame;
            AssertSimulationMatchesCapture(
                fixture,
                wave => wave.WaveId == fixture.Waves[0].WaveId
                    ? 1
                    : GetRenderAliasedAssignmentFrame(
                        authoredEvents[fixture.Waves.IndexOf(wave) - 1]["b"].AsFloat,
                        bpmInfo,
                        fixture.FixedDeltaTime,
                        refreshRate,
                        fixture.FirstFixedTime) + capturedSequenceOffset,
                fixtureName + " map playback",
                allowBeatSaberOneFrameLate: true);
        }

        private static void AssertFixtureMatchesMapAndIsComplete(string fixtureFolder)
        {
            var fixture = LoadCapturedFixture(fixtureFolder);
            AssertMapMatchesCapturedWaves(fixtureFolder, fixture.Waves);
            foreach (var sampleGroup in fixture.Samples.GroupBy(sample => sample.Beat))
            {
                Assert.That(
                    sampleGroup.Select(sample => sample.Ring),
                    Is.EquivalentTo(Enumerable.Range(0, RingCount)),
                    $"Fixture {fixtureFolder} is incomplete at beat {sampleGroup.Key:R}.");
            }
        }

        private static void AssertSimulationMatchesCapture(
            CapturedFixture fixture,
            Func<CapturedWave, int> getStartFrame,
            string simulationName,
            bool allowBeatSaberOneFrameLate = false)
        {
            var originalFixedDeltaTime = Time.fixedDeltaTime;
            Time.fixedDeltaTime = fixture.FixedDeltaTime;
            try
            {
                var mismatches = SimulateAndCollectMismatches(
                    fixture,
                    getStartFrame,
                    allowBeatSaberOneFrameLate);
                if (mismatches.Count > 0)
                {
                    Assert.Fail(
                        $"{mismatches.Count} {simulationName} field mismatches (first 100 shown):\n"
                        + string.Join("\n", mismatches.Take(100)));
                }
            }
            finally
            {
                Time.fixedDeltaTime = originalFixedDeltaTime;
            }
        }

        private static List<string> SimulateAndCollectMismatches(
            CapturedFixture fixture,
            Func<CapturedWave, int> getStartFrame,
            bool allowBeatSaberOneFrameLate)
        {
            var mismatches = new List<string>();
            var ringStates = new RingRotationState[RingCount];
            var previousRingStates = new RingRotationState[RingCount];
            var previousMomentum = new float[RingCount];
            var activeWaves = new RingRotationWave[fixture.Waves.Count];
            var activeWaveCount = 0;
            var wavesByStartFrame = fixture.Waves
                .GroupBy(getStartFrame)
                .ToDictionary(group => group.Key, group => group.ToArray());
            var destinationsByFrame = fixture.Destinations
                .GroupBy(state => state.FixedSequence)
                .ToDictionary(group => group.Key, group => group.ToArray());
            var samplesByFrame = fixture.Samples
                .GroupBy(sample => sample.FixedSequence)
                .ToDictionary(group => group.Key, group => group.ToArray());
            var maxFrame = Math.Max(
                fixture.Destinations.Max(state => state.FixedSequence),
                fixture.Samples.Max(sample => sample.FixedSequence));

            for (var frame = 1; frame <= maxFrame; frame++)
            {
                if (wavesByStartFrame.TryGetValue(frame, out var startingWaves))
                {
                    foreach (var wave in startingWaves)
                    {
                        activeWaves[activeWaveCount++] = new RingRotationWave
                        {
                            NextFrame = frame,
                            Progress = 0f,
                            FirstRingDestination = wave.Target,
                            Step = wave.Step,
                            Propagation = wave.Propagation,
                            Speed = wave.Speed,
                            TraceId = wave.WaveId
                        };
                    }
                }

                var currentMomentum = new float[RingCount];
                for (var ring = 0; ring < RingCount; ring++)
                    previousRingStates[ring] = ringStates[ring];

                TrackLaneRingsRotationEffect.AdvanceState(
                    ringStates,
                    activeWaves,
                    ref activeWaveCount,
                    frame - 1,
                    frame,
                    RingCount);

                for (var ring = 0; ring < RingCount; ring++)
                    currentMomentum[ring] = ringStates[ring].Rotation - previousRingStates[ring].Rotation;

                if (destinationsByFrame.TryGetValue(frame, out var destinationStates))
                {
                    foreach (var expected in destinationStates)
                    {
                        var actual = ringStates[expected.Ring];
                        var previous = previousRingStates[expected.Ring];
                        var destinationMatches = Mathf.Approximately(actual.Destination, expected.Destination)
                            || (allowBeatSaberOneFrameLate
                                && IsBetween(expected.Destination, previous.Destination, actual.Destination, 0.0001f));
                        var speedMatches = Mathf.Approximately(actual.Speed, expected.Speed)
                            || (allowBeatSaberOneFrameLate
                                && IsBetween(expected.Speed, previous.Speed, actual.Speed, SpeedContinuousStateTolerance));
                        if (!destinationMatches)
                        {
                            mismatches.Add(
                                $"DST frame={frame} ring={expected.Ring} "
                                + FormatDestinationFields(actual, expected, previous));
                        }

                        if (!speedMatches)
                        {
                            mismatches.Add(
                                $"SPD frame={frame} ring={expected.Ring} "
                                + FormatDestinationFields(actual, expected, previous));
                        }
                    }
                }

                if (!samplesByFrame.TryGetValue(frame, out var samples))
                {
                    Array.Copy(currentMomentum, previousMomentum, RingCount);
                    continue;
                }

                foreach (var expected in samples)
                {
                    var actual = ringStates[expected.Ring];
                    var previous = previousRingStates[expected.Ring];
                    // Beat Saber can sample between adjacent fixed states, but values outside
                    // that one-frame interval still identify real simulation drift.
                    var rotationMatches = Mathf.Abs(actual.Rotation - expected.Current) <= ContinuousStateTolerance
                        || (allowBeatSaberOneFrameLate
                            && IsBetween(expected.Current, previous.Rotation, actual.Rotation, ContinuousStateTolerance));
                    var destinationMatches = Mathf.Approximately(actual.Destination, expected.Destination)
                        || (allowBeatSaberOneFrameLate
                            && IsBetween(expected.Destination, previous.Destination, actual.Destination, 0.0001f));
                    var speedMatches = Mathf.Approximately(actual.Speed, expected.Speed)
                        || (allowBeatSaberOneFrameLate
                            && IsBetween(expected.Speed, previous.Speed, actual.Speed, SpeedContinuousStateTolerance));
                    var momentumMatches = Mathf.Abs(currentMomentum[expected.Ring] - expected.Momentum) <= MomentumContinuousStateTolerance
                        || (allowBeatSaberOneFrameLate
                            && IsBetween(expected.Momentum, previousMomentum[expected.Ring], currentMomentum[expected.Ring], MomentumContinuousStateTolerance));
                    var fields = FormatSampleFields(
                        actual,
                        currentMomentum[expected.Ring],
                        expected,
                        previous,
                        previousMomentum[expected.Ring]);
                    if (!rotationMatches)
                    {
                        mismatches.Add($"ROT beat={expected.Beat:R} frame={frame} ring={expected.Ring} {fields}");
                    }

                    if (!destinationMatches)
                    {
                        mismatches.Add($"DST beat={expected.Beat:R} frame={frame} ring={expected.Ring} {fields}");
                    }

                    if (!speedMatches)
                    {
                        mismatches.Add($"SPD beat={expected.Beat:R} frame={frame} ring={expected.Ring} {fields}");
                    }

                    if (!momentumMatches)
                    {
                        mismatches.Add($"MOM beat={expected.Beat:R} frame={frame} ring={expected.Ring} {fields}");
                    }
                }

                Array.Copy(currentMomentum, previousMomentum, RingCount);
            }

            return mismatches;
        }

        private static bool IsBetween(float value, float first, float second, float tolerance)
        {
            var minimum = Mathf.Min(first, second) - tolerance;
            var maximum = Mathf.Max(first, second) + tolerance;
            return value >= minimum && value <= maximum;
        }


        private static string FormatDestinationFields(
            RingRotationState actual,
            CapturedDestination expected,
            RingRotationState previous) =>
            $"destination={FormatTriplet(actual.Destination, expected.Destination, previous.Destination)} "
            + $"speed={FormatTriplet(actual.Speed, expected.Speed, previous.Speed)}";

        private static string FormatSampleFields(
            RingRotationState actual,
            float actualMomentum,
            CapturedSample expected,
            RingRotationState previous,
            float previousMomentum) =>
            $"rotation={FormatTriplet(actual.Rotation, expected.Current, previous.Rotation)} "
            + $"destination={FormatTriplet(actual.Destination, expected.Destination, previous.Destination)} "
            + $"speed={FormatTriplet(actual.Speed, expected.Speed, previous.Speed)} "
            + $"momentum={FormatTriplet(actualMomentum, expected.Momentum, previousMomentum)}";

        private static string FormatTriplet(float current, float expected, float previous) =>
            "{" + current.ToString("R", CultureInfo.InvariantCulture)
            + "}/{" + expected.ToString("R", CultureInfo.InvariantCulture)
            + "}/{" + previous.ToString("R", CultureInfo.InvariantCulture) + "}";

        private static CapturedFixture LoadCapturedFixture(string fixtureFolder)
        {
            var starts = ParseCsv(fixtureFolder, "ChromaGLS-RingWaveStarts.csv");
            var states = ParseCsv(fixtureFolder, "ChromaGLS-RingHalfBeatStates.csv");
            var fixedDeltaTime = ParseFloat(starts[0]["fixedDeltaTime"]);
            var firstFixedTime = ParseFloat(starts.First(row =>
                row["recordType"] == "WAVE_ADD" && row["systemName"] == SystemName)["unityFixedTime"]);
            var waves = starts
                .Where(row => row["recordType"] == "WAVE_ADD" && row["systemName"] == SystemName)
                .Select(row => new CapturedWave(
                    ParseInt(row["waveId"]),
                    string.IsNullOrEmpty(row["eventSongSeconds"])
                        ? 0f
                        : ParseFloat(row["eventSongSeconds"]),
                    string.IsNullOrEmpty(row["callbackSongSeconds"])
                        ? 0f
                        : ParseFloat(row["callbackSongSeconds"]),
                    ParseInt(row["fixedSequence"]),
                    ParseFloat(row["target"]),
                    ParseFloat(row["step"]),
                    ParseFloat(row["floatPropagation"]),
                    ParseFloat(row["flexSpeed"])))
                .ToList();
            var destinations = starts
                .Where(row => row["recordType"] == "DEST_STATE" && row["systemName"] == SystemName)
                .Select(row => new CapturedDestination(
                    ParseInt(row["fixedSequence"]),
                    ParseInt(row["ring"]),
                    ParseFloat(row["target"]),
                    ParseFloat(row["flexSpeed"])))
                .ToList();
            var samples = states
                .Where(row => row["recordType"] == "HALF_BEAT" && row["systemName"] == SystemName)
                .Select(row => new CapturedSample(
                    ParseInt(row["fixedSequence"]),
                    ParseInt(row["ring"]),
                    ParseFloat(row["sampleBeat"]),
                    ParseFloat(row["currentRotation"]),
                    ParseFloat(row["destinationRotation"]),
                    ParseFloat(row["rotationSpeed"]),
                    ParseFloat(row["rotationMomentum"])))
                .ToList();

            Assert.That(waves, Has.Count.EqualTo(25));
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(samples, Is.Not.Empty);
            return new CapturedFixture(firstFixedTime, fixedDeltaTime, waves, destinations, samples);
        }

        private static JSONNode[] LoadRingEvents(string fixtureFolder)
        {
            var root = JSON.Parse(LoadFixtureText(fixtureFolder, "ExpertPlusStandard.dat"));
            return root["basicBeatmapEvents"].AsArray.Children
                .Where(node => node["et"].AsInt == 8)
                .ToArray();
        }

        private static int GetPredictedFirstAssignmentFrame(float beat, JSONNode bpmInfo, float fixedDeltaTime)
        {
            var songSeconds = GetSongSeconds(beat, bpmInfo);
            return TrackLaneRingsRotationEffect.GetFirstAssignmentFrame((float)songSeconds, fixedDeltaTime);
        }

        private static int GetRenderAliasedAssignmentFrame(
            float beat,
            JSONNode bpmInfo,
            float fixedDeltaTime,
            float refreshRate,
            float firstFixedTime)
        {
            var songSeconds = GetSongSeconds(beat, bpmInfo);
            // Anchor render divisions to the session's first fixed tick so refresh-grid
            // aliases preserve the captured Unity timeline origin without event timing input.
            var absoluteSeconds = firstFixedTime + songSeconds;
            var callbackSeconds = Math.Ceiling(absoluteSeconds * refreshRate) / refreshRate;
            return TrackLaneRingsRotationEffect.GetFirstAssignmentFrame(
                (float)(callbackSeconds - firstFixedTime),
                fixedDeltaTime);
        }

        private static double GetSongSeconds(float beat, JSONNode bpmInfo)
        {
            var frequency = bpmInfo["_songFrequency"].AsDouble;
            foreach (var region in bpmInfo["_regions"].AsArray.Children)
            {
                var startBeat = region["_startBeat"].AsDouble;
                var endBeat = region["_endBeat"].AsDouble;
                if (beat < startBeat || beat > endBeat)
                    continue;

                var progress = (beat - startBeat) / (endBeat - startBeat);
                var startSample = region["_startSampleIndex"].AsDouble;
                var endSample = region["_endSampleIndex"].AsDouble;
                return (startSample + ((endSample - startSample) * progress)) / frequency;
            }

            Assert.Fail($"No BPMInfo region contains beat {beat:R}.");
            return -1d;
        }

        private static void AssertMapMatchesCapturedWaves(string fixtureFolder, IReadOnlyList<CapturedWave> waves)
        {
            var info = JSON.Parse(LoadFixtureText(fixtureFolder, "Info.dat"));
            Assert.That(info["_songName"].Value, Is.EqualTo("RingRotationTesting"));
            Assert.That(info["_environmentName"].Value, Is.EqualTo("KaleidoscopeEnvironment"));
            Assert.That(
                info["_difficultyBeatmapSets"][0]["_difficultyBeatmaps"][0]["_beatmapFilename"].Value,
                Is.EqualTo("ExpertPlusStandard.dat"));

            var bpmInfo = JSON.Parse(LoadFixtureText(fixtureFolder, "BPMInfo.dat"));
            Assert.That(bpmInfo["_songFrequency"].AsInt, Is.EqualTo(48000));
            Assert.That(bpmInfo["_regions"].AsArray.Count, Is.EqualTo(2));

            var root = JSON.Parse(LoadFixtureText(fixtureFolder, "ExpertPlusStandard.dat"));
            var events = root["basicBeatmapEvents"].AsArray.Children
                .Where(node => node["et"].AsInt == 8)
                .ToArray();
            Assert.That(events.Length, Is.EqualTo(waves.Count - 1));
            for (var i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                var wave = waves[i + 1];
                Assert.That(evt["customData"]["step"].AsFloat, Is.EqualTo(wave.Step).Within(0.0001f));
                Assert.That(evt["customData"]["prop"].AsFloat, Is.EqualTo(wave.Propagation).Within(0.0001f));
                Assert.That(evt["customData"]["speed"].AsFloat, Is.EqualTo(wave.Speed).Within(0.0001f));
            }
        }

        private static List<Dictionary<string, string>> ParseCsv(string fixtureFolder, string fileName)
        {
            var lines = LoadFixtureText(fixtureFolder, fileName).Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            var headers = SplitCsvLine(lines[0]);
            var rows = new List<Dictionary<string, string>>(lines.Length - 1);
            for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                var values = SplitCsvLine(lines[lineIndex]);
                var row = new Dictionary<string, string>(headers.Count);
                for (var i = 0; i < headers.Count; i++)
                    row[headers[i]] = i < values.Count ? values[i] : string.Empty;
                rows.Add(row);
            }
            return rows;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var values = new List<string>();
            var start = 0;
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    quoted = !quoted;
                }
                else if (line[i] == ',' && !quoted)
                {
                    values.Add(line.Substring(start, i - start).Trim('"'));
                    start = i + 1;
                }
            }
            values.Add(line.Substring(start).Trim('"'));
            return values;
        }

        private static string LoadFixtureText(string fixtureFolder, string fileName)
        {
            var relativePath = FixtureRoot.Substring("Assets/".Length) + fixtureFolder + "/" + fileName;
            var path = Path.Combine(Application.dataPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(path), Is.True, $"Missing ring rotation fixture {path}.");
            return File.ReadAllText(path);
        }

        private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);
        private static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        private readonly struct CapturedFixture
        {
            public CapturedFixture(
                float firstFixedTime,
                float fixedDeltaTime,
                List<CapturedWave> waves,
                List<CapturedDestination> destinations,
                List<CapturedSample> samples)
            {
                FirstFixedTime = firstFixedTime;
                FixedDeltaTime = fixedDeltaTime;
                Waves = waves;
                Destinations = destinations;
                Samples = samples;
            }

            public float FirstFixedTime { get; }
            public float FixedDeltaTime { get; }
            public List<CapturedWave> Waves { get; }
            public List<CapturedDestination> Destinations { get; }
            public List<CapturedSample> Samples { get; }
        }

        private readonly struct CapturedWave
        {
            public CapturedWave(
                int waveId,
                float eventSeconds,
                float callbackSeconds,
                int fixedSequence,
                float target,
                float step,
                float propagation,
                float speed)
            {
                WaveId = waveId;
                EventSeconds = eventSeconds;
                CallbackSeconds = callbackSeconds;
                FixedSequence = fixedSequence;
                Target = target;
                Step = step;
                Propagation = propagation;
                Speed = speed;
            }

            public int WaveId { get; }
            public float EventSeconds { get; }
            public float CallbackSeconds { get; }
            public int FixedSequence { get; }
            public float Target { get; }
            public float Step { get; }
            public float Propagation { get; }
            public float Speed { get; }
        }

        private readonly struct CapturedDestination
        {
            public CapturedDestination(int fixedSequence, int ring, float destination, float speed)
            {
                FixedSequence = fixedSequence;
                Ring = ring;
                Destination = destination;
                Speed = speed;
            }

            public int FixedSequence { get; }
            public int Ring { get; }
            public float Destination { get; }
            public float Speed { get; }
        }

        private readonly struct CapturedSample
        {
            public CapturedSample(int fixedSequence, int ring, float beat, float current, float destination, float speed, float momentum)
            {
                FixedSequence = fixedSequence;
                Ring = ring;
                Beat = beat;
                Current = current;
                Destination = destination;
                Speed = speed;
                Momentum = momentum;
            }

            public int FixedSequence { get; }
            public int Ring { get; }
            public float Beat { get; }
            public float Current { get; }
            public float Destination { get; }
            public float Speed { get; }
            public float Momentum { get; }
        }
    }
}
