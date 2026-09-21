using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    // GLSGroupPagePoolingTest covers visual-pool ownership when the outer GLS page changes.
    public class GLSGroupPagePoolingTest : TestBase
    {
        private const int FirstGroupId = 910001;
        private const int SecondGroupId = 910002;
        private const string FirstPage = "GLS page pool first";
        private const string SecondPage = "GLS page pool second";

        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider provider;
        private TrackDefinitionsSO originalTracks;
        private TrackDefinitionsSO testTracks;

        protected override EditingMode InitialEditingMode => EditingMode.GLS;

        // Install two equivalent GLS lanes on different pages so only page membership controls materialization.
        [SetUp]
        public void ConfigurePages()
        {
            runtime = UnityEngine.Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            provider = UnityEngine.Object.FindAnyObjectByType<GLSGroupGridProvider>();
            originalTracks = runtime.TrackDefinitions;
            testTracks = ScriptableObject.CreateInstance<TrackDefinitionsSO>();
            SetGlsEntries(testTracks, new List<TrackDefinitionGLS>
            {
                CreateTrack(FirstGroupId, FirstPage),
                CreateTrack(SecondGroupId, SecondPage)
            });
            testTracks.Initialize();
            runtime.TrackDefinitions = testTracks;
            runtime.NotifyTrackDefinitions();
            provider.SetGroupPage(FirstPage);
        }

        // Restore the shared scene definitions after each case so page state cannot leak into unrelated fixtures.
        [TearDown]
        public void RestorePages()
        {
            if (runtime != null && originalTracks != null)
            {
                runtime.TrackDefinitions = originalTracks;
                runtime.NotifyTrackDefinitions();
            }

            if (testTracks != null)
            {
                UnityEngine.Object.DestroyImmediate(testTracks);
            }
        }

        // RefreshPoolMaterializesOnlyTheSelectedPage covers every outer GLS node family without scanning inactive pages.
        [TestCase(GlsKind.Color)]
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void RefreshPoolMaterializesOnlyTheSelectedPage(GlsKind kind)
        {
            var first = SpawnGroup(kind, FirstGroupId);
            var second = SpawnGroup(kind, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(first.ObjectType);

            collection.RefreshPool(0f, 20f, true);

            Assert.That(collection.LoadedContainers.ContainsKey(first), Is.True);
            Assert.That(collection.LoadedContainers.ContainsKey(second), Is.False,
                "An inactive GLS page must remain data-only instead of allocating nodes or ribbons.");
        }

        // SwitchingPagesRebindsThePooledOwnerAndItsGhostsBeforeTheyCanRender prevents a deferred old-page root from appearing on the new page.
        [Test]
        public void SwitchingPagesRebindsThePooledOwnerAndItsGhostsBeforeTheyCanRender()
        {
            var first = SpawnGroup(GlsKind.Color, FirstGroupId);
            var second = SpawnGroup(GlsKind.Color, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(first.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var firstOwner = (GLSGroupContainer)collection.LoadedContainers[first];
            Assert.That(GetPreviewGhosts(firstOwner).Count, Is.EqualTo(1));

            firstOwner.Highlighted = true;
            firstOwner.Dragged = true;
            provider.SetGroupPage(SecondPage);

            Assert.That(collection.LoadedContainers.Count, Is.EqualTo(1));
            Assert.That(collection.LoadedContainers.ContainsKey(first), Is.False);
            Assert.That(collection.LoadedContainers.TryGetValue(second, out var loadedSecond), Is.True);
            var secondOwner = (GLSGroupContainer)loadedSecond;
            Assert.That(secondOwner.transform.parent,
                Is.SameAs(provider.IdToTracks[SecondGroupId].Track.ObjectParentTransform));
            Assert.That(secondOwner.Highlighted, Is.False);
            Assert.That(secondOwner.Dragged, Is.False);
            foreach (var ghost in GetPreviewGhosts(secondOwner))
            {
                Assert.That(ghost.EventBoxGroupData, Is.SameAs(second));
                // Ghosts remain under the owner's shared activation root, whose parent must move to the selected lane.
                Assert.That(ghost.transform.parent.parent,
                    Is.SameAs(provider.IdToTracks[SecondGroupId].Track.ObjectParentTransform));
                Assert.That(ghost.Highlighted, Is.False);
                Assert.That(ghost.Dragged, Is.False);
            }
        }

        // InactiveColorPagesKeepTimelineDataWithoutMaterializingVisuals separates light calculation from ribbon ownership.
        [Test]
        public void InactiveColorPagesKeepTimelineDataWithoutMaterializingVisuals()
        {
            var active = SpawnGroup(GlsKind.Color, FirstGroupId);
            var inactive = SpawnGroup(GlsKind.Color, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(active.ObjectType);

            collection.RefreshPool(0f, 20f, true);

            var inactiveNode = (BaseLightColorBase)inactive.OrderedEvents[0];
            Assert.That(GLSEventCommon.GetColorTimeline(inactiveNode, 4), Is.Not.Null,
                "Inactive-page color events must remain available to the global light timeline.");
            Assert.That(collection.LoadedContainers.ContainsKey(inactive), Is.False,
                "Timeline indexing must not require an inactive page's ribbon owner or preview nodes.");
        }

        // RemovingAllPagesUnloadsMaterializedGroups covers environments whose replacement definitions contain no GLS lanes.
        [Test]
        public void RemovingAllPagesUnloadsMaterializedGroups()
        {
            var group = SpawnGroup(GlsKind.Color, FirstGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            Assert.That(collection.LoadedContainers.ContainsKey(group), Is.True);

            SetGlsEntries(testTracks, new List<TrackDefinitionGLS>());
            testTracks.Initialize();
            runtime.NotifyTrackDefinitions();

            Assert.That(collection.LoadedContainers.Count, Is.EqualTo(0));
            Assert.That(provider.ActiveGlsTrackIds, Is.Empty);
        }

        // RapidScrubUsesTheLatestWindowWhileAReboundOwnerIsStillQueued reproduces nodes staying absent until play/pause refreshes the pool.
        [Test]
        public void RapidScrubUsesTheLatestWindowWhileAReboundOwnerIsStillQueued()
        {
            var initial = SpawnColorGroup(FirstGroupId, 0f, 0f, 1f);
            var spanning = SpawnColorGroup(FirstGroupId, 20f, 0f, 40f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.RefreshPool(0f, 5f, true);
            Assert.That(collection.LoadedContainers.ContainsKey(initial), Is.True);

            // The first automatic scrub rebinds an owner and processes only its root-hide work unit.
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
            collection.RefreshPool(19f, 25f);
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            InvokePrivate(collection, "ProcessNextPreviewConfiguration");

            // A second scrub keeps that parent for its far-offset node and must supersede the queued first window.
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
            collection.RefreshPool(55f, 65f);
            SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            for (var step = 0; step < 20; step++)
            {
                InvokePrivate(collection, "ProcessNextPreviewConfiguration");
            }

            Assert.That(collection.LoadedContainers.TryGetValue(spanning, out var loaded), Is.True);
            var owner = (GLSGroupContainer)loaded;
            Assert.That(GetPreviewGhosts(owner).Any(ghost =>
                    Mathf.Approximately(ghost.PreviewEventData.RelativeJsonTime, 40f)),
                Is.True,
                "The queued preview request must use the latest scrub window without waiting for play/pause.");
        }

        // RebindingAnOwnerDoesNotPositionResetGhostSlotsBeforeConfiguration reproduces the deployed UpdateGridPosition NRE.
        [Test]
        public void RebindingAnOwnerDoesNotPositionResetGhostSlotsBeforeConfiguration()
        {
            var first = SpawnColorGroup(FirstGroupId, 0f, 0f, 1f);
            var replacement = SpawnColorGroup(FirstGroupId, 40f, 0f, 1f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.RefreshPool(0f, 5f, true);
            var owner = (GLSGroupContainer)collection.LoadedContainers[first];
            var ghost = GetPreviewGhosts(owner).Single();

            // Map/pool teardown can reset a cached child before its collection owner is rebound on the next refresh.
            ghost.ResetForPool();
            owner.ResetForPool();
            owner.ObjectData = replacement;

            Assert.DoesNotThrow(owner.UpdateGridPosition,
                "A collection owner must not position cached child slots until configuration rebinds their event data.");
        }

        // CreateTrack gives both pages identical capabilities so type-specific filtering cannot influence the result.
        private static TrackDefinitionGLS CreateTrack(int id, string page) => new()
        {
            ID = id,
            Group = page,
            Name = page,
            ColorTrack = true,
            RotationTracks = new[] { true, true, true },
            TranslationTracks = new[] { true, true, true },
            FloatFXTrack = true
        };

        // SpawnGroup authors two preview offsets so page switches exercise both the collection owner and a pooled ghost.
        private static BaseEventBoxGroup SpawnGroup(GlsKind kind, int id)
        {
            BaseEventBoxGroup group = kind switch
            {
                GlsKind.Color => new BaseLightColorEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightColorEventBox
                        {
                            Events = new[]
                            {
                                new BaseLightColorBase { RelativeJsonTime = 0f, Brightness = 1f },
                                new BaseLightColorBase { RelativeJsonTime = 1f, Brightness = 1f }
                            }
                        }
                    }
                },
                GlsKind.Rotation => new BaseLightRotationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightRotationEventBox
                        {
                            Events = new[]
                            {
                                new BaseLightRotationBase { RelativeJsonTime = 0f },
                                new BaseLightRotationBase { RelativeJsonTime = 1f }
                            }
                        }
                    }
                },
                GlsKind.Translation => new BaseLightTranslationEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseLightTranslationEventBox
                        {
                            Events = new[]
                            {
                                new BaseLightTranslationBase { RelativeJsonTime = 0f },
                                new BaseLightTranslationBase { RelativeJsonTime = 1f }
                            }
                        }
                    }
                },
                GlsKind.FloatFX => new BaseVfxEventEventBoxGroup
                {
                    Boxes =
                    {
                        new BaseVfxEventEventBox
                        {
                            Events = new[]
                            {
                                new BaseFxEventFloat { RelativeJsonTime = 0f },
                                new BaseFxEventFloat { RelativeJsonTime = 1f }
                            }
                        }
                    }
                },
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            group.ID = id;
            group.JsonTime = 4f;
            Normalize(group);
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, out _, false, false, true);
            return group;
        }

        // SpawnColorGroup creates a long-lived parent whose preview offsets can cross multiple rapid scrub windows.
        private static BaseLightColorEventBoxGroup SpawnColorGroup(
            int id,
            float beat,
            params float[] offsets)
        {
            var box = new BaseLightColorEventBox();
            box.SetEvents(offsets.Select(offset => (BaseGLSEvent)new BaseLightColorBase
            {
                RelativeJsonTime = offset,
                Brightness = 1f
            }).ToArray());
            var group = new BaseLightColorEventBoxGroup
            {
                ID = id,
                JsonTime = beat,
                Boxes = { box }
            };
            group.NormalizeLoadedEventConflicts();
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType)
                .SpawnObject(group, out _, false, false, true);
            return group;
        }

        // Normalize establishes the same owner references and ordered preview index as production deserialization.
        private static void Normalize(BaseEventBoxGroup group)
        {
            switch (group)
            {
                case BaseLightColorEventBoxGroup color:
                    color.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightRotationEventBoxGroup rotation:
                    rotation.NormalizeLoadedEventConflicts();
                    break;
                case BaseLightTranslationEventBoxGroup translation:
                    translation.NormalizeLoadedEventConflicts();
                    break;
                case BaseVfxEventEventBoxGroup floatFx:
                    floatFx.NormalizeLoadedEventConflicts();
                    break;
            }
        }

        // Read the owner's maintained preview list so assertions do not scan unrelated pooled scene objects.
        private static List<GLSGroupContainer> GetPreviewGhosts(GLSGroupContainer owner)
        {
            var field = typeof(GLSGroupContainer).GetField(
                "previewGhosts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (List<GLSGroupContainer>)field.GetValue(owner);
        }

        // TrackDefinitionsSO serializes its source list privately; populate that production input before Initialize.
        private static void SetGlsEntries(TrackDefinitionsSO tracks, List<TrackDefinitionGLS> entries)
        {
            var field = typeof(TrackDefinitionsSO).GetField(
                "glsEntries",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(tracks, entries);
        }

        // Queue regressions drive one compiled private work unit at a time without depending on frame timing.
        private static void InvokePrivate(object target, string methodName)
        {
            var type = target.GetType();
            MethodInfo method = null;
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        // Queue regressions toggle only the production automatic-refresh mode around public pool refreshes.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        public enum GlsKind
        {
            Color,
            Rotation,
            Translation,
            FloatFX
        }
    }
}
