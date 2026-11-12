using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class WeekndLampTranslationTest : TestBase
    {
        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator FreshMapInitializesBuildingsAndReloadResetsTranslationEdits()
        {
            yield return LoadEmptyWeekndMap();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            AssertBuildingHeight(context, -5f);
            var manager = context.Descriptor.LightTranslationGroupEffectManager;
            Assert.That(manager.InsertData(CreateTranslationGroup(20, 1f)), Is.True);
            var leftBuildings = context.Descriptor.ChromaIDMarkers
                .Where(
                    marker => marker.ChromaID.Contains("BuildingsLeft") && marker.name.StartsWith("TranslationWrapper"))
                .ToArray();
            Assert.That(leftBuildings.Length, Is.EqualTo(3));
            foreach (var building in leftBuildings)
                Assert.That(building.transform.localPosition.y, Is.EqualTo(0f).Within(0.0001f), building.ChromaID);

            yield return LoadEmptyWeekndMap();
            AssertBuildingHeight(Object.FindAnyObjectByType<BeatmapRuntimeContext>(), -5f);
        }

        [UnityTest]
        public IEnumerator FreshMapInitializesBothLampRingAssembliesBeforeAnyTranslationEdits()
        {
            yield return LoadEmptyWeekndMap();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            AssertAssemblyHeight(context, 7f);

            context.Descriptor.LightTranslationGroupEffectManager.Refresh();
            AssertAssemblyHeight(context, 7f);
            context.Atsc.MoveToJsonTime(20f);
            context.Atsc.MoveToJsonTime(0f);
            AssertAssemblyHeight(context, 7f);
        }

        [UnityTest]
        public IEnumerator IndividualRingEditsRetainAssemblyHeightAndEmptyReloadResetsIt()
        {
            yield return LoadEmptyWeekndMap();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var manager = context.Descriptor.LightTranslationGroupEffectManager;
            var ringGroup = CreateTranslationGroup(29, 1f);
            Assert.That(manager.InsertData(ringGroup), Is.True);
            AssertAssemblyHeight(context, 7f);
            var wrapper = context.Descriptor.ChromaIDMarkers.Single(
                marker => marker.ChromaID ==
                    "TheWeekndEnvironment.[0]Environment.[28]LampsGroupLeft.[0]LampsSpawner.[0]Lamp.[1]LampRingSet.[0]LampRingWrapper");
            Assert.That(wrapper.transform.localPosition.y, Is.EqualTo(0.5f).Within(0.0001f));

            Assert.That(manager.InsertData(CreateTranslationGroup(2, 1f)), Is.True);
            AssertAssemblyHeight(context, 13f, 7f);
            yield return LoadEmptyWeekndMap();
            context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            AssertAssemblyHeight(context, 7f);
        }

        private static BaseLightTranslationEventBoxGroup CreateTranslationGroup(int id, float translation)
        {
            var group = new BaseLightTranslationEventBoxGroup
            {
                ID = id,
                JsonTime = 0f,
                Boxes =
                {
                    new BaseLightTranslationEventBox
                    {
                        Axis = (int)Axis.Y,
                        IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                        Events = new[] { new BaseLightTranslationBase { Translation = translation } }
                    }
                }
            };
            group.SetMap(BeatSaberSongContainer.Instance.Map);
            group.RecomputeSongBpmTime();
            return group;
        }

        private static IEnumerator LoadEmptyWeekndMap() => TestUtils.ReloadMap(
            3,
            new JSONObject { ["version"] = "3.3.0" },
            beatsPerMinute: 120f,
            environmentName: "TheWeekndEnvironment");

        private static void AssertAssemblyHeight(
            BeatmapRuntimeContext context,
            float leftHeight,
            float? rightHeight = null)
        {
            var assemblies = context.Descriptor.ChromaIDMarkers
                .Where(marker => marker.name == "LampRingSet")
                .ToArray();
            Assert.That(assemblies.Length, Is.EqualTo(8));
            foreach (var assembly in assemblies)
            {
                var height = assembly.ChromaID.Contains("LampsGroupLeft")
                    ? leftHeight
                    : rightHeight ?? leftHeight;
                Assert.That(assembly.transform.localPosition.y, Is.EqualTo(height).Within(0.0001f), assembly.ChromaID);
            }
        }

        private static void AssertBuildingHeight(BeatmapRuntimeContext context, float height)
        {
            var buildings = context.Descriptor.ChromaIDMarkers
                .Where(
                    marker => marker.name.StartsWith("TranslationWrapper")
                        && (marker.ChromaID.Contains("BuildingsLeft")
                            || marker.ChromaID.Contains("BuildingsRight")
                            || marker.ChromaID.Contains("PeripheralBuildings")))
                .ToArray();
            Assert.That(buildings.Length, Is.EqualTo(8));
            foreach (var building in buildings)
                Assert.That(building.transform.localPosition.y, Is.EqualTo(height).Within(0.0001f), building.ChromaID);
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
