using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Explicit keeps this animation-state diagnostic out of batch runs: it reloads the heaviest fixture
    // in the suite purely to dump track state. Run on demand with -TestFilter 'WorldCavesInRunwayDiagTest'.
    [Explicit]
    public class WorldCavesInRunwayDiagTest : TestBase
    {
        [UnityTest]
        public IEnumerator DumpRunwayTrackState()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(Path.Combine(
                    Application.dataPath, "Tests", "Fixtures", "WorldCavesInEnvironmentEssence.json"))),
                beatsPerMinute: 124,
                environmentName: "TimbalandEnvironment",
                songLengthSeconds: 260);

            var track = Object
                .FindObjectsByType<TrackAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(t => t.Track.name == "TrackConstructionParent1");

            Debug.Log($"[Diag] track enabled={track.enabled} children={track.CachedChildren.Length} " +
                $"props=[{string.Join(", ", track.AnimatedProperties.Keys)}]");

            foreach (var kv in track.AnimatedProperties)
            {
                if (kv.Value is AnimateProperty<Vector3> prop)
                {
                    var desc = string.Join(" | ", prop.PointDefinitions.Select(pd =>
                        $"start={pd.StartTime} dur={pd.Duration} p0={pd.Points.FirstOrDefault()?.Value}"));
                    Debug.Log($"[Diag] prop {kv.Key}: {desc}");
                    Debug.Log($"[Diag] prop {kv.Key} eval(0)={prop.GetLerpedValue(0)} " +
                        $"eval(6)={prop.GetLerpedValue(6)} eval(20)={prop.GetLerpedValue(20)}");
                }
            }

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Debug.Log($"[Diag] atsc time={(atsc != null ? atsc.CurrentJsonTime : -999f)} " +
                $"trackPos={track.Track.ObjectParentTransform.position} " +
                $"selfPos={track.Track.SelfTransform.position}");

            yield return null;
            yield return null;
            Debug.Log($"[Diag] after frames: trackPos={track.Track.ObjectParentTransform.position} " +
                $"selfPos={track.Track.SelfTransform.position} enabled={track.enabled}");

            var enhanced = Object
                .FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .SelectMany(container => container.GetComponents<ObjectAnimator>())
                .Where(animator => animator.LocalTarget != null)
                .ToList();
            foreach (var animator in enhanced)
            {
                var container = animator.GetComponent<GeometryContainer>();
                var trackName = container != null && container.EnvironmentEnhancement != null
                    ? container.EnvironmentEnhancement.Track
                    : null;
                if (trackName == "TrackConstruction1" || trackName == "TrackMirror1")
                {
                    Debug.Log($"[Diag] clone '{animator.LocalTarget.name}' track=" +
                        $"{trackName} pos={animator.LocalTarget.position}");
                }
            }

            Assert.Pass();
        }

        // This test leaves the mapper on the Timbaland fixture map while the shared baseline still points at
        // an earlier scene's map; the next class's ResetSharedMapState would install that dead map and split it
        // from the collections' aliased lists (BPMTest saw SongBpmTime collapse to JsonTime). Restore and
        // recapture the canonical empty map so following fixtures stay consistent.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
