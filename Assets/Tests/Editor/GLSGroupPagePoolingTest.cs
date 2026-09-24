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

        // ScrollingShowsEveryGlsNodeBeforeRibbonWork checks the scrub refresh itself, before a later frame can drain deferred work.
        [TestCase(GlsKind.Color)]
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void ScrollingShowsEveryGlsNodeBeforeRibbonWork(GlsKind kind)
        {
            var group = SpawnGroup(kind, FirstGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);

            try
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
                collection.RefreshPool(0f, 20f, true);

                var owner = (GLSGroupContainer)collection.LoadedContainers[group];
                Assert.That(owner.gameObject.activeInHierarchy, Is.True,
                    "The first node must be visible in the scrub frame.");
                Assert.That(owner.PreviewEventData, Is.SameAs(group.OrderedEvents[0]));
                var ghosts = GetPreviewGhosts(owner);
                Assert.That(ghosts, Has.Count.EqualTo(1));
                Assert.That(ghosts[0].gameObject.activeInHierarchy, Is.True,
                    "Later GLS nodes must appear in the same scrub frame, before ribbon work runs.");
                Assert.That(ghosts[0].PreviewEventData, Is.SameAs(group.OrderedEvents[1]));
            }
            finally
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            }
        }

        // ColdScrubConfiguresRetainedColorSourceImmediately covers a ribbon source loaded after the ordinary start-time pool pass.
        [Test]
        public void ColdScrubConfiguresRetainedColorSourceImmediately()
        {
            var source = SpawnColorGroup(FirstGroupId, 0f, 0f, 40f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);

            try
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", true);
                collection.RefreshPool(19f, 25f);

                Assert.That(collection.LoadedContainers.TryGetValue(source, out var loaded), Is.True,
                    "The source group must be retained for its crossing color ribbon.");
                var owner = (GLSGroupContainer)loaded;
                Assert.That(owner.PreviewEventData, Is.SameAs(source.OrderedEvents[0]),
                    "A cold scrub must bind the retained node before the refresh returns.");
                Assert.That(owner.gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                SetPrivateField(collection, "deferAutomaticPreviewConfiguration", false);
            }
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

        // ResetForPoolClearsEveryExternallyVisibleOwnerState catches stale ghost opacity, ribbons, interaction, and data identity together.
        [Test]
        public void ResetForPoolClearsEveryExternallyVisibleOwnerState()
        {
            var group = SpawnGroup(GlsKind.Color, FirstGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var owner = (GLSGroupContainer)collection.LoadedContainers[group];
            var cachedGhost = GetPreviewGhosts(owner).Single();
            var alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
            var translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

            owner.Selected = true;
            owner.Highlighted = true;
            owner.Dragged = true;
            owner.GlsLightCount = 91;
            owner.lightGradientController.SetVisible(true);
            owner.IncomingLightGradientController.SetVisible(true);
            owner.MpbController.Mpb.SetFloat(alwaysTranslucent, 1f);
            owner.MpbController.Mpb.SetFloat(translucentAlpha, 0.2f);
            SetPrivateField(owner, "isPreviewGhost", true);

            // Reproduce the collection lifecycle: ObjectData is cleared before the type-specific reset runs.
            owner.ObjectData = null;
            owner.ResetForPool();

            Assert.That(GetPrivateField<bool>(owner, "isPreviewGhost"), Is.False,
                "A collection-owned primary must never retain a preview-ghost role.");
            Assert.That(owner.MpbController.Mpb.GetFloat(alwaysTranslucent), Is.Zero,
                "A recycled primary must clear the shader's forced-translucency branch.");
            Assert.That(owner.MpbController.Mpb.GetFloat(translucentAlpha), Is.EqualTo(1f));
            Assert.That(owner.lightGradientController.gameObject.activeSelf, Is.False);
            Assert.That(owner.IncomingLightGradientController.gameObject.activeSelf, Is.False);
            Assert.That(GetPrivateField<Transform>(owner, "previewGhostRoot").gameObject.activeSelf, Is.False,
                "The separately-parented preview root must stop rendering as soon as its owner enters the pool.");
            Assert.That(owner.Selected, Is.False);
            Assert.That(owner.Highlighted, Is.False);
            Assert.That(owner.Dragged, Is.False);
            Assert.That(owner.GlsLightCount, Is.Zero);
            Assert.That(owner.EventBoxGroupData, Is.Null);
            Assert.That(owner.PreviewEventData, Is.Null);
            Assert.That(owner.DragTarget, Is.SameAs(owner));
            Assert.That(owner.ProcessPreviewNodeConfigurationStep(), Is.True,
                "No abandoned preview configuration may survive a pool reset.");
            Assert.That(GetPreviewGhosts(owner), Has.Count.EqualTo(1),
                "Resetting transient state must preserve the expensive preview capacity.");
            Assert.That(GetPreviewGhosts(owner).Single(), Is.SameAs(cachedGhost));
            Assert.That(GetPrivateField<bool>(owner, "reusePreviewCapacityOnNextConfigure"), Is.True,
                "The next owner should reuse its inactive preview slot instead of allocating another Unity object.");
        }

        // RecycledPreviewNodesReceiveTheirRoleOnEveryBind prevents both primary dithering and solid child ghosts after page reuse.
        [TestCase(GlsKind.Color)]
        [TestCase(GlsKind.Rotation)]
        [TestCase(GlsKind.Translation)]
        [TestCase(GlsKind.FloatFX)]
        public void RecycledPreviewNodesReceiveTheirRoleOnEveryBind(GlsKind kind)
        {
            var first = SpawnGroup(kind, FirstGroupId);
            var second = SpawnGroup(kind, SecondGroupId);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(first.ObjectType);
            collection.RefreshPool(0f, 20f, true);
            var firstOwner = (GLSGroupContainer)collection.LoadedContainers[first];
            var firstGhost = GetPreviewGhosts(firstOwner).Single();
            var alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
            var translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

            // Seed the exact leaked roles/material values that a pool boundary must overwrite authoritatively.
            SetPrivateField(firstOwner, "isPreviewGhost", true);
            firstOwner.MpbController.Mpb.SetFloat(alwaysTranslucent, 1f);
            firstOwner.MpbController.Mpb.SetFloat(translucentAlpha, 0.2f);
            SetPrivateField(firstGhost, "isPreviewGhost", false);
            firstGhost.MpbController.Mpb.SetFloat(alwaysTranslucent, 0f);
            firstGhost.MpbController.Mpb.SetFloat(translucentAlpha, 1f);

            // Rebind this exact cached owner so unrelated fixture pool depth cannot select a different valid container.
            firstOwner.ObjectData = null;
            firstOwner.ResetForPool();
            firstOwner.ObjectData = second;
            firstOwner.GlsLightCount = 17;
            firstOwner.ConfigurePreviewNodes(_ => false);

            var secondOwner = firstOwner;
            var secondGhost = GetPreviewGhosts(secondOwner).Single();
            Assert.That(secondOwner, Is.SameAs(firstOwner));
            Assert.That(secondGhost, Is.SameAs(firstGhost));
            Assert.That(GetPrivateField<bool>(secondOwner, "isPreviewGhost"), Is.False);
            Assert.That(GetPrivateField<bool>(secondGhost, "isPreviewGhost"), Is.True);
            Assert.That(secondOwner.MpbController.Mpb.GetFloat(alwaysTranslucent), Is.Zero);
            Assert.That(secondOwner.MpbController.Mpb.GetFloat(translucentAlpha), Is.EqualTo(1f));
            Assert.That(secondGhost.MpbController.Mpb.GetFloat(alwaysTranslucent), Is.EqualTo(1f));
            Assert.That(secondGhost.MpbController.Mpb.GetFloat(translucentAlpha),
                Is.EqualTo(Mathf.Clamp01(Settings.Instance.GLSOuterTrackGhostNodeOpacity)));
            Assert.That(secondOwner.PreviewEventData, Is.SameAs(second.OrderedEvents[0]));
            Assert.That(secondGhost.PreviewEventData, Is.SameAs(second.OrderedEvents[1]));
            Assert.That(secondOwner.Selected, Is.False);
            Assert.That(secondOwner.Highlighted, Is.False);
            Assert.That(secondOwner.Dragged, Is.False);
            Assert.That(secondGhost.Selected, Is.False);
            Assert.That(secondGhost.Highlighted, Is.False);
            Assert.That(secondGhost.Dragged, Is.False);
        }

        // QueuedPreviewConfigurationsPrioritizeEarlierGroups keeps deferred color ribbons ordered after their nodes appear synchronously.
        [Test]
        public void QueuedPreviewConfigurationsPrioritizeEarlierGroups()
        {
            var earlier = SpawnColorGroup(FirstGroupId, 4f, 0f, 1f);
            var later = SpawnColorGroup(FirstGroupId, 12f, 0f, 1f);
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
            collection.RefreshPool(0f, 20f, true);
            var earlierOwner = (GLSGroupContainer)collection.LoadedContainers[earlier];
            var laterOwner = (GLSGroupContainer)collection.LoadedContainers[later];

            InvokePrivate(collection, "CancelPendingPreviewConfigurations");
            // A forced appearance refresh dirties both already-visible ribbon owners without delaying either node.
            InvokePrivate(collection, "SchedulePreviewConfiguration", laterOwner, true);
            InvokePrivate(collection, "SchedulePreviewConfiguration", earlierOwner, true);

            Assert.That(collection.NextPendingPreviewConfiguration.EventBoxGroupData, Is.SameAs(earlier),
                "The next visible node must be chosen by ascending song time, not queue insertion order.");
        }

        // PreviewSchedulerBatchesCheapWorkUnits confirms non-color nodes leave no deferred ribbon work after appearing.
        [Test]
        public void PreviewSchedulerBatchesCheapWorkUnits()
        {
            var originalVSync = QualitySettings.vSyncCount;
            var originalTargetFrameRate = Application.targetFrameRate;
            try
            {
                // Give this deterministic direct invocation a generous frame deadline instead of inheriting test-runner work.
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 10;
                var group = SpawnGroup(GlsKind.Rotation, FirstGroupId);
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupRotationGridContainer>(group.ObjectType);
                collection.RefreshPool(0f, 20f, true);
                var owner = (GLSGroupContainer)collection.LoadedContainers[group];

                InvokePrivate(collection, "CancelPendingPreviewConfigurations");
                SetPrivateField(owner, "configuredPrimaryPreviewEvent", null);
                InvokePrivate(collection, "SchedulePreviewConfiguration", owner, false);
                InvokePrivate(collection, "ProcessNextPreviewConfiguration");

                Assert.That(collection.NextPendingPreviewConfiguration, Is.Null,
                    "Rotation nodes must already be complete and must not occupy the color ribbon queue.");
            }
            finally
            {
                QualitySettings.vSyncCount = originalVSync;
                Application.targetFrameRate = originalTargetFrameRate;
            }
        }

        // PreviewSchedulerUsesTheConfiguredFrameDeadline bounds a dense ribbon queue without delaying any visible node.
        [Test]
        public void PreviewSchedulerUsesTheConfiguredFrameDeadline()
        {
            var originalVSync = QualitySettings.vSyncCount;
            var originalTargetFrameRate = Application.targetFrameRate;
            try
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 1000;
                // More than one frame's maximum work units makes the bound deterministic across fast and slow test hosts.
                var offsets = Enumerable.Range(0, 32).Select(index => index * 0.25f).ToArray();
                var group = SpawnColorGroup(FirstGroupId, 4f, offsets);
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupColorGridContainer>(group.ObjectType);
                collection.RefreshPool(0f, 20f, true);
                var owner = (GLSGroupContainer)collection.LoadedContainers[group];

                InvokePrivate(collection, "CancelPendingPreviewConfigurations");
                // Force every color node to request a ribbon so one frame cannot drain the capped queue.
                InvokePrivate(collection, "SchedulePreviewConfiguration", owner, true);
                Assert.That(collection.NextPendingPreviewConfiguration, Is.SameAs(owner));
                InvokePrivate(collection, "ProcessNextPreviewConfiguration");

                Assert.That(collection.NextPendingPreviewConfiguration, Is.SameAs(owner),
                    "One frame must leave later ribbons queued after its bounded upload slice.");
            }
            finally
            {
                QualitySettings.vSyncCount = originalVSync;
                Application.targetFrameRate = originalTargetFrameRate;
            }
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
        private static void InvokePrivate(object target, string methodName, params object[] arguments)
        {
            var type = target.GetType();
            MethodInfo method = null;
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, arguments);
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

        // Pool-state regressions read the compiled lifecycle state that has no user-facing accessor.
        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
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
