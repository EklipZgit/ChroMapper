using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Explicit keeps this memory diagnostic out of batch runs: the triple heavy-map reload plus the
    // per-assembly static-field census take minutes and the leaked maps stall every following fixture.
    // Run on demand with -TestFilter 'WorldCavesInMemDiagTest'.
    [Explicit]
    public class WorldCavesInMemDiagTest : TestBase
    {
        private static void DumpMemory(string tag)
        {
            System.GC.Collect();
            var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            var materials = Resources.FindObjectsOfTypeAll<Material>();
            var meshes = Resources.FindObjectsOfTypeAll<Mesh>();
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            var clipBytes = clips.Sum(c => (long)c.samples * c.channels * sizeof(float));
            Debug.Log($"[Mem] {tag}: managed={GC.GetTotalMemory(true) / 1048576}MB " +
                $"totalAlloc={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576}MB " +
                $"reserved={UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1048576}MB " +
                $"clips={clips.Length} ({clipBytes / 1048576}MB) materials={materials.Length} " +
                $"meshes={meshes.Length} gameObjects={objects.Length}");
        }

        // Census every static field in the app assemblies: collections, delegate chains, and any
        // live references to map objects/JSON trees — a retained map graph shows up here.
        private static void DumpStatics(string tag)
        {
            var lines = new List<string>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name.StartsWith("System") || name.StartsWith("mscorlib") || name.StartsWith("netstandard")
                    || name.StartsWith("UnityEngine") || name.StartsWith("UnityEditor")
                    || name.StartsWith("Unity.") || name.StartsWith("nunit") || name.StartsWith("Newtonsoft"))
                    continue;
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var type in types)
                {
                    foreach (var field in type.GetFields(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        object value;
                        try { value = field.GetValue(null); } catch { continue; }
                        if (value == null) continue;
                        var desc = Describe(value);
                        if (desc != null)
                            lines.Add($"    {type.FullName}.{field.Name}: {desc}");
                    }
                }
            }

            Debug.Log($"[Statics] {tag}:\n" + string.Join("\n", lines.OrderBy(l => l)));
        }

        private static string Describe(object value)
        {
            switch (value)
            {
                case Delegate d:
                    var n = d.GetInvocationList().Length;
                    return n > 1 ? $"delegate[{n}]" : null;
                case IDictionary dict:
                    return dict.Count > 0 ? $"dict[{dict.Count}]" : null;
                case ICollection col:
                    return col.Count > 0 ? $"col[{col.Count}]" : null;
                case BaseDifficulty:
                    return "BaseDifficulty";
                case BaseObject:
                    return "BaseObject";
                case JSONNode node:
                    return $"JSONNode[{node.Count}]";
                default:
                    return null;
            }
        }

        [UnityTest]
        public IEnumerator MeasureReloadFootprint()
        {
            var fixture = File.ReadAllText(Path.Combine(
                Application.dataPath, "Tests", "Fixtures", "WorldCavesInEnvironmentEssence.json"));
            var loadedMaps = new List<WeakReference<BaseDifficulty>>();

            DumpMemory("before any load");
            for (var i = 0; i < 3; i++)
            {
                yield return TestUtils.ReloadMap(
                    2,
                    JSON.Parse(fixture),
                    beatsPerMinute: 124,
                    environmentName: "TimbalandEnvironment",
                    songLengthSeconds: 260);
                loadedMaps.Add(new WeakReference<BaseDifficulty>(BeatSaberSongContainer.Instance.Map));
                DumpMemory($"after heavy load {i + 1}");
                DumpStatics($"after heavy load {i + 1}");
            }

            for (var i = 0; i < 2; i++)
            {
                yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
                DumpMemory($"after empty-map reload {i + 1}");
                DumpStatics($"after empty-map reload {i + 1}");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (var i = 0; i < loadedMaps.Count; i++)
            {
                if (loadedMaps[i].TryGetTarget(out var stale))
                {
                    // A stale map equal to Instance.Map means the last load never replaced the
                    // reference; anything else means a hidden root retains a superseded graph.
                    Debug.Log($"[StaleMap] heavy map {i + 1} is STILL ALIVE" +
                        (ReferenceEquals(stale, BeatSaberSongContainer.Instance.Map)
                            ? " — still BeatSaberSongContainer.Instance.Map"
                            : " — held by something other than Instance.Map"));
                }
                else
                {
                    Debug.Log($"[StaleMap] heavy map {i + 1} collected");
                }
            }

            Assert.Pass();
        }

        // The reloads above leave the mapper on a new map object while the shared baseline still points
        // at an earlier scene's map; the next class's ResetSharedMapState would reinstall that dead map and
        // split it from the collections' aliased lists (BPMTest saw SongBpmTime collapse to JsonTime).
        // Restore and recapture the canonical empty map so following fixtures stay consistent.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
