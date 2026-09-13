// Exercise the real editor input and ownership paths without adding production-only boundary test hooks.
using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // All four GLS families share the boundary contract, including parent-owned inner nodes and outer child extents.
    public class GLSSongBoundaryTest : SongBoundaryTestBase
    {
        // Named axes make every generated case identify its GLS family and the precise boundary it exercises.
        public enum GlsKind { Color, Rotation, Translation, FloatFX }
        public enum PasteOverflow { BeforeSome, BeforeAll, AfterSome, AfterAll }
        public enum LowerShift { SomeBeforeGroup, AllBeforeGroup, LandsAtGroupStart }
        // Idle inner paste anchors at relative zero, so test a fitting range ending at song end without requesting an impossible parent rebase.
        public enum InnerPasteStart { NoHover, NoHoverEndingAtSongEnd, HoverAtStart, HoverBeforeStart }
        public enum OuterDragTarget { BeforeSong, ChildrenBeyondEnd, EntireGroupBeyondEnd }

        // Restore the real scene definition after each case rather than mutating the environment asset's shared lists.
        private BeatmapRuntimeContext runtime;
        private GLSGroupGridProvider groupProvider;
        private GLSEventGridProvider innerProvider;
        private BeatmapActionContainer actions;
        private TracksDefinitionSO originalTracks;
        private TracksDefinitionSO testTracks;
        private string originalPage;

        // Real GetNewObjects rejects unavailable GLS tracks, so install a valid three-lane definition for every family.
        [SetUp]
        public void ConfigureGlsTracks()
        {
            runtime = GetField<BeatmapRuntimeContext>(Selection, "beatmapRuntimeContext");
            groupProvider = GetField<GLSGroupGridProvider>(Selection, "glsGroupGridProvider");
            innerProvider = GetField<GLSEventGridProvider>(Selection, "glsEventGridProvider");
            actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            originalTracks = runtime.TracksDefinition;
            originalPage = groupProvider.CurrentGroup;
            testTracks = ScriptableObject.CreateInstance<TracksDefinitionSO>();
            testTracks.Copy(originalTracks);
            SetField(testTracks, "glsEntries", Enumerable.Range(1, 3).Select(id => new TrackDefinitionGLS
            {
                ID = id,
                Name = "Boundary lane " + id,
                Group = "Song boundary tests",
                ColorTrack = true,
                RotationTracks = new[] { true, true, true },
                TranslationTracks = new[] { true, true, true },
                FloatFXTrack = true
            }).ToList());
            testTracks.Initialize();
            runtime.TracksDefinition = testTracks;
            runtime.NotifyTracksDefinition();
            groupProvider.SetGroupPage("Song boundary tests");
            Assert.That(FinalBeat, Is.GreaterThan(32f), "The shared test song must fit the untouched source groups.");
        }

        // Tracks are scene state, so failed assertions must not leave subsequent fixtures on the synthetic GLS page.
        [TearDown]
        public void RestoreGlsTracks()
        {
            if (runtime != null && originalTracks != null)
            {
                runtime.TracksDefinition = originalTracks;
                runtime.NotifyTracksDefinition();
                groupProvider.SetGroupPage(originalPage);
            }
            if (testTracks != null)
            {
                Object.DestroyImmediate(testTracks);
            }
        }

        // The earlier parent owns the latest child: checking only parent beats or clamping each parent loses spacing.
        [Test]
        public void ShiftOuterGroupsTranslatesWholeSelectionToSongBoundary(
            [Values] GlsKind kind, [Values] bool forward)
        {
            SetMode(EditingMode.GLS);
            var firstBeat = forward ? FinalBeat - 5.5f : 0.25f;
            var secondBeat = firstBeat + 2.75f;
            var firstOffsets = new[] { new[] { 0f }, new[] { 4.75f } };
            var secondOffsets = new[] { new[] { 0f, 0.5f } };
            var first = PlaceGroup(kind, 1, firstBeat, firstOffsets);
            var second = PlaceGroup(kind, 2, secondBeat, secondOffsets);
            var untouched = PlaceGuard(kind);
            SelectOnly(first, second);

            ShiftWithKeyboard(forward);

            var delta = forward ? 0.75f : -0.25f;
            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(2));
            AssertRoundTrip(() => AssertState(delta), () => AssertState(0f));

            // Every phase checks backing groups rather than pooled visuals, including the unselected group's identity.
            void AssertState(float appliedDelta)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(3));
                AssertGroup(Group(kind, 1), kind, firstBeat + appliedDelta, firstOffsets);
                AssertGroup(Group(kind, 2), kind, secondBeat + appliedDelta, secondOffsets);
                Assert.That(Group(kind, 2).JsonTime - Group(kind, 1).JsonTime, Is.EqualTo(2.75f).Within(0.0001f));
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // A later unselected sibling must not reduce the translation available to the selected inner-node range.
        [Test]
        public void ShiftInnerNodesTranslatesSelectionToSongEndWithoutMovingParent([Values] GlsKind kind)
        {
            SetMode(EditingMode.EventBox);
            var groupBeat = FinalBeat - 8f;
            var original = new[] { new[] { 4.5f, 7.5f }, new[] { 7.75f } };
            var moved = new[] { new[] { 5f, 8f }, new[] { 7.75f } };
            var group = PlaceGroup(kind, 1, groupBeat, original);
            var untouched = PlaceGuard(kind);
            OpenGroup(group);
            SelectOnly(group.ReadOnlyBoxes[0].ReadOnlyEvents.Cast<BaseObject>().ToArray());

            ShiftWithKeyboard(true);

            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(2));
            AssertRoundTrip(() => AssertState(moved), () => AssertState(original));

            // Parent replacement must preserve the unselected sibling's beat and rebuild every child owner in both action directions.
            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertInnerGroup(kind, 1, groupBeat, offsets);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Preserve the existing atomic lower-limit rejection; landing exactly at relative zero is still a legal shift.
        [Test]
        public void ShiftInnerNodesRetainsExistingGroupStartRestriction(
            [Values] GlsKind kind, [Values] LowerShift scenario)
        {
            SetMode(EditingMode.EventBox);
            var offsets = scenario == LowerShift.LandsAtGroupStart
                ? new[] { 1f, 3f }
                : new[] { 0.25f, scenario == LowerShift.AllBeforeGroup ? 0.5f : 3.25f };
            var original = new[] { offsets, new[] { 6f } };
            var group = PlaceGroup(kind, 1, 8f, original);
            var untouched = PlaceGuard(kind);
            OpenGroup(group);
            var selected = group.ReadOnlyBoxes[0].ReadOnlyEvents.Cast<BaseObject>().ToArray();
            SelectOnly(selected);

            ShiftWithKeyboard(false);

            if (scenario == LowerShift.LandsAtGroupStart)
            {
                AssertRoundTrip(
                    () => AssertState(new[] { new[] { 0f, 2f }, new[] { 6f } }),
                    () => AssertState(original));
            }
            else
            {
                AssertState(original);
                Assert.That(Group(kind, 1), Is.SameAs(group), "Rejection must not replace or rebase the parent.");
                CollectionAssert.AreEquivalent(selected, SelectionController.SelectedObjects);
                AssertNoAction();
                AssertState(original);
            }

            // Even when only one selected node would be negative, none of its selected or unselected siblings may move.
            void AssertState(float[][] expected)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertInnerGroup(kind, 1, 8f, expected);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Copy first, then offset its relative clipboard beats to reach both negative-song scenarios without a negative cursor.
        [Test]
        public void PasteOuterGroupsTranslatesCompleteChildExtentAndPreservesClipboard(
            [Values] GlsKind kind, [Values] PasteOverflow overflow, [Values] bool hovered)
        {
            SetMode(EditingMode.GLS);
            var firstOffsets = new[] { new[] { 0f }, new[] { 4f } };
            var secondOffsets = new[] { new[] { 0f, 0.5f } };
            var first = PlaceGroup(kind, 1, 8f, firstOffsets);
            var second = PlaceGroup(kind, 2, 10f, secondOffsets);
            var untouched = PlaceGuard(kind);
            SelectOnly(first, second);
            CopyWithKeyboard();
            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(2));
            var atEnd = overflow == PasteOverflow.AfterSome || overflow == PasteOverflow.AfterAll;
            var allOutside = overflow == PasteOverflow.BeforeAll || overflow == PasteOverflow.AfterAll;
            var clipboardOffset = atEnd ? (allOutside ? 2f : 0f) : (allOutside ? -8f : -1f);
            foreach (var copied in SelectionController.CopiedObjects.Cast<BaseEventBoxGroup>())
            {
                copied.JsonTime += clipboardOffset;
                copied.RecomputeSongBpmTime();
            }
            var clipboard = ClipboardSnapshot();
            var anchor = atEnd ? (allOutside ? FinalBeat : FinalBeat - 3f) : 0f;
            Atsc.MoveToJsonTime(hovered ? 8f : anchor);
            ConfigureOuterPaste(kind, hovered, anchor);
            var attemptedNodes = SelectionController.CopiedObjects.Cast<BaseEventBoxGroup>()
                .SelectMany(g => g.ReadOnlyBoxes.SelectMany(b => b.ReadOnlyEvents)
                    .Select(e => anchor + g.JsonTime + e.RelativeJsonTime)).ToArray();
            AssertOutsideScenario(attemptedNodes, atEnd, allOutside);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            Assert.That(SelectionController.SelectedObjects.Count, Is.EqualTo(2));
            var expectedFirstBeat = atEnd ? FinalBeat - 4f : 0f;
            AssertRoundTrip(() => AssertState(true), () => AssertState(false));

            // Paste must keep source groups, clipboard beats, inter-parent spacing, and child offsets unchanged across undo/redo.
            void AssertState(bool pasted)
            {
                var groups = Groups(kind);
                Assert.That(groups.Length, Is.EqualTo(pasted ? 5 : 3));
                Assert.That(groups.Any(g => ReferenceEquals(g, first)), Is.True);
                Assert.That(groups.Any(g => ReferenceEquals(g, second)), Is.True);
                AssertGroup(first, kind, 8f, firstOffsets);
                AssertGroup(second, kind, 10f, secondOffsets);
                if (pasted)
                {
                    var copies = groups.Where(g => !ReferenceEquals(g, first)
                        && !ReferenceEquals(g, second) && !ReferenceEquals(g, untouched)).ToArray();
                    Assert.That(copies.Length, Is.EqualTo(2));
                    AssertGroup(copies.Single(g => g.ID == 1), kind, expectedFirstBeat, firstOffsets);
                    AssertGroup(copies.Single(g => g.ID == 2), kind, expectedFirstBeat + 2f, secondOffsets);
                }
                AssertGuard(kind, untouched);
                CollectionAssert.AreEqual(clipboard, ClipboardSnapshot());
                AssertSelectedOwnership();
            }
        }

        // Hovered inner paste has room to translate backward inside its existing parent, for either partial or complete overflow.
        [Test]
        public void PasteInnerNodesTranslatesCompleteSelectionToSongEndWithoutRebasing(
            [Values] GlsKind kind, [Values] bool allOutside)
        {
            SetMode(EditingMode.EventBox);
            var sourceOffsets = new[] { new[] { 0f, 3f }, new[] { 6f } };
            var source = PlaceGroup(kind, 1, 8f, sourceOffsets);
            var groupBeat = FinalBeat - 8f;
            var original = new[] { Array.Empty<float>(), new[] { 0.5f } };
            var target = PlaceGroup(kind, 2, groupBeat, original);
            var untouched = PlaceGuard(kind);
            CopyInnerNodes(source);
            var clipboard = ClipboardSnapshot();
            OpenGroup(target);
            Atsc.MoveToJsonTime(8f);
            var relativeAnchor = allOutside ? 10f : 7f;
            ConfigureInnerPaste(kind, target, true, relativeAnchor);
            AssertOutsideScenario(new[] { groupBeat + relativeAnchor, groupBeat + relativeAnchor + 3f }, true, allOutside);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            AssertRoundTrip(
                () => AssertState(new[] { new[] { 5f, 8f }, new[] { 0.5f } }),
                () => AssertState(original));

            // The destination parent and its unselected child stay fixed; only the copied node range translates to end at FinalBeat.
            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(3));
                // Paste's existing action selects its replacement group; validate ownership without changing that selection policy.
                AssertInnerGroup(kind, 2, groupBeat, offsets, true);
                Assert.That(Group(kind, 1), Is.SameAs(source));
                AssertGroup(source, kind, 8f, sourceOffsets);
                AssertGuard(kind, untouched);
                CollectionAssert.AreEqual(clipboard, ClipboardSnapshot());
                AssertSelectedOwnership();
            }
        }

        // Non-hovered inner paste starts at its parent, not the song cursor; preserve the existing per-node lower clamp for negative hover.
        [Test]
        public void PasteInnerNodesRetainsNonnegativeGroupStartBehavior(
            [Values] GlsKind kind, [Values] InnerPasteStart scenario)
        {
            SetMode(EditingMode.EventBox);
            var sourceOffsets = new[] { new[] { 0f, 3f }, new[] { 6f } };
            var source = PlaceGroup(kind, 1, 2f, sourceOffsets);
            var original = new[] { Array.Empty<float>(), new[] { 0.5f } };
            // An idle paste can land its latest node exactly at end while keeping its earliest node at the unchanged group start.
            var groupBeat = scenario == InnerPasteStart.NoHoverEndingAtSongEnd ? FinalBeat - 3f : 12f;
            var target = PlaceGroup(kind, 2, groupBeat, original);
            var untouched = PlaceGuard(kind);
            CopyInnerNodes(source);
            var clipboard = ClipboardSnapshot();
            OpenGroup(target);
            Atsc.MoveToJsonTime(FinalBeat);
            // Both idle cases must bypass the queued hover value and retain the real group-start anchoring branch.
            var hovered = scenario == InnerPasteStart.HoverAtStart || scenario == InnerPasteStart.HoverBeforeStart;
            ConfigureInnerPaste(kind, target, hovered, scenario == InnerPasteStart.HoverBeforeStart ? -1f : 0f);
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();

            PasteWithKeyboard();

            var lastOffset = scenario == InnerPasteStart.HoverBeforeStart ? 2f : 3f;
            AssertRoundTrip(
                () => AssertState(new[] { new[] { 0f, lastOffset }, new[] { 0.5f } }),
                () => AssertState(original));

            // Lower-limit expectations deliberately retain today's clamp, rather than imposing a new rebase or spacing policy.
            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(3));
                // Preserve SelectionPastedAction's existing outer-group selection while testing the unchanged lower time restriction.
                AssertInnerGroup(kind, 2, groupBeat, offsets, true);
                Assert.That(Group(kind, 1), Is.SameAs(source));
                AssertGroup(source, kind, 2f, sourceOffsets);
                AssertGuard(kind, untouched);
                CollectionAssert.AreEqual(clipboard, ClipboardSnapshot());
                AssertSelectedOwnership();
            }
        }

        // Alt-drag edits one outer group; its entire child range must fit even when only the children cross song end.
        [Test]
        public void AltLeftDragOuterGroupUsesMaximumChildOffsetAtSongBoundary(
            [Values] GlsKind kind, [Values] OuterDragTarget destination)
        {
            SetMode(EditingMode.GLS);
            var offsets = new[] { new[] { 0f, 1f }, new[] { 4.75f } };
            var group = PlaceGroup(kind, 1, 8f, offsets);
            var untouched = PlaceGuard(kind);
            SelectOnly(group);
            var target = destination == OuterDragTarget.BeforeSong
                ? -4f
                : destination == OuterDragTarget.ChildrenBeyondEnd ? FinalBeat - 1f : FinalBeat + 4f;
            var expected = destination == OuterDragTarget.BeforeSong ? 0f : FinalBeat - 4.75f;

            // Outer placements need their real track provider; Initialize(null) cannot establish a valid GLS lane.
            var placement = FindPlacement(OuterPlacement(kind).GetType());
            var provider = groupProvider.IdToTracks[group.ID].GetComponent<PlacementProvider>();
            Assert.That(provider, Is.Not.Null);
            placement.Initialize(provider);
            DragToBeat(placement, group, target);

            AssertRoundTrip(() => AssertState(expected), () => AssertState(8f));

            // The same relative child offsets must survive both action directions; unrelated outer groups cannot follow the drag.
            void AssertState(float beat)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertGroup(Group(kind, 1), kind, beat, offsets);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Inner alt-drag clamps at song end but restores its original offset when dropped before the existing parent.
        [Test]
        public void AltLeftDragInnerNodeRetainsParentAndLowerLimitBehavior(
            [Values] GlsKind kind, [Values] bool atEnd)
        {
            SetMode(EditingMode.EventBox);
            var original = new[] { new[] { 1f }, new[] { 3f } };
            var group = PlaceGroup(kind, 1, 8f, original);
            var untouched = PlaceGuard(kind);
            OpenGroup(group);
            // Alt-drag does not require selection; isolate restoration of the dragged node from unrelated selection gestures.
            SelectOnly();

            DragToBeat(FindPlacement(InnerPlacement(kind).GetType()), group.ReadOnlyBoxes[0].ReadOnlyEvents[0],
                atEnd ? FinalBeat + 4f : 7f);

            if (atEnd)
            {
                AssertRoundTrip(
                    () => AssertState(new[] { new[] { FinalBeat - 8f }, new[] { 3f } }),
                    () => AssertState(original));
            }
            else
            {
                AssertState(original);
                AssertNoAction();
                AssertState(original);
            }

            // Restoring a rejected drag must not leak its transient negative node through history, selection, or box ownership.
            void AssertState(float[][] offsets)
            {
                Assert.That(Groups(kind).Length, Is.EqualTo(2));
                AssertInnerGroup(kind, 1, 8f, offsets);
                AssertGuard(kind, untouched);
                AssertSelectedOwnership();
            }
        }

        // Keep source selection out of the action stack so one undo must reverse the complete tested operation atomically.
        private static void SelectOnly(params BaseObject[] objects)
        {
            SelectionController.DeselectAll();
            foreach (var obj in objects)
            {
                SelectionController.Select(obj, true, false, false);
            }
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        // Clearing retirement metadata mirrors existing GLS fixtures and prevents a previous group's deferred replacement from winning.
        private void OpenGroup(BaseEventBoxGroup group)
        {
            innerProvider.LastContext = null;
            innerProvider.GroupContext = group;
        }

        // Copy real parent-owned nodes before changing contexts; direct clipboard construction would miss ownership-sensitive Copy behavior.
        private void CopyInnerNodes(BaseEventBoxGroup source)
        {
            OpenGroup(source);
            SelectOnly(source.ReadOnlyBoxes[0].ReadOnlyEvents.Cast<BaseObject>().ToArray());
            CopyWithKeyboard();
            Assert.That(SelectionController.CopiedObjects.Count, Is.EqualTo(2));
            SelectionController.DeselectAll();
        }

        // SelectionController reads serialized placements, which need not be the first inactive sibling found in the scene.
        private BasePlacement OuterPlacement(GlsKind kind) => GetField<BasePlacement>(Selection, kind switch
        {
            GlsKind.Color => "glsGroupColorPlacement",
            GlsKind.Rotation => "glsGroupRotationPlacement",
            GlsKind.Translation => "glsGroupTranslationPlacement",
            GlsKind.FloatFX => "glsGroupFloatFXPlacement",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

        // The four inner placements share an ObjectType but each has its own serialized paste queue and group type.
        private BasePlacement InnerPlacement(GlsKind kind) => GetField<BasePlacement>(Selection, kind switch
        {
            GlsKind.Color => "glsEventColorPlacement",
            GlsKind.Rotation => "glsEventRotationPlacement",
            GlsKind.Translation => "glsEventTranslationPlacement",
            GlsKind.FloatFX => "glsEventFloatFXPlacement",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

        // Model a hovered outer lane without replacing GetNewObjects: its real track mapping and queued beat still decide the paste.
        private void ConfigureOuterPaste(GlsKind kind, bool hovered, float beat)
        {
            var placement = OuterPlacement(kind);
            var queued = GetField<BaseEventBoxGroup>(placement, "QueuedData");
            queued.ID = 1;
            queued.JsonTime = beat;
            queued.SetMap(BeatSaberSongContainer.Instance.Map);
            queued.RecomputeSongBpmTime();
            placement.State = hovered ? PlacementState.Active : PlacementState.Idle;
        }

        // Inner paste uses a relative hover offset, while Idle intentionally leaves the historical group-start anchor intact.
        private void ConfigureInnerPaste(GlsKind kind, BaseEventBoxGroup group, bool hovered, float relativeBeat)
        {
            var placement = InnerPlacement(kind);
            var queued = GetField<BaseGLSEvent>(placement, "QueuedData");
            queued.EventBoxGroupData = group;
            queued.EventBoxData = group.ReadOnlyBoxes[0];
            queued.BoxIndex = 0;
            queued.RelativeJsonTime = relativeBeat;
            queued.SetMap(BeatSaberSongContainer.Instance.Map);
            queued.RecomputeSongBpmTime();
            placement.State = hovered ? PlacementState.Active : PlacementState.Idle;
        }

        // Normalize typed groups before Spawn/selection so tests exercise editing, not incomplete synthetic ownership or time setup.
        private static BaseEventBoxGroup PlaceGroup(GlsKind kind, int id, float beat, params float[][] offsets)
        {
            BaseEventBoxGroup group = kind switch
            {
                GlsKind.Color => new BaseLightColorEventBoxGroup(),
                GlsKind.Rotation => new BaseLightRotationEventBoxGroup(),
                GlsKind.Translation => new BaseLightTranslationEventBoxGroup(),
                GlsKind.FloatFX => new BaseVfxEventEventBoxGroup(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            group.JsonTime = beat;
            group.ID = id;
            for (var lane = 0; lane < offsets.Length; lane++)
            {
                BaseEventBox box;
                switch (group)
                {
                    case BaseLightColorEventBoxGroup color:
                        var colorBox = new BaseLightColorEventBox();
                        color.Boxes.Add(colorBox);
                        box = colorBox;
                        break;
                    case BaseLightRotationEventBoxGroup rotation:
                        var rotationBox = new BaseLightRotationEventBox { Axis = lane };
                        rotation.Boxes.Add(rotationBox);
                        box = rotationBox;
                        break;
                    case BaseLightTranslationEventBoxGroup translation:
                        var translationBox = new BaseLightTranslationEventBox { Axis = lane };
                        translation.Boxes.Add(translationBox);
                        box = translationBox;
                        break;
                    case BaseVfxEventEventBoxGroup floatFx:
                        var floatBox = new BaseVfxEventEventBox();
                        floatFx.Boxes.Add(floatBox);
                        box = floatBox;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(kind));
                }
                var nodes = offsets[lane].Select((offset, index) => CreateNode(kind, offset, lane * 10 + index + 1)).ToArray();
                box.SetEvents(nodes);
            }
            // Normalization is typed on the generic base and must run before any node enters the live child collection.
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
            return Spawn(group);
        }

        // Distinct payloads expose dropped, reordered, or accidentally overwritten nodes even if their final beat range looks right.
        private static BaseGLSEvent CreateNode(GlsKind kind, float offset, float marker) => kind switch
        {
            GlsKind.Color => new BaseLightColorBase { RelativeJsonTime = offset, Brightness = marker },
            GlsKind.Rotation => new BaseLightRotationBase { RelativeJsonTime = offset, Rotation = marker },
            GlsKind.Translation => new BaseLightTranslationBase { RelativeJsonTime = offset, Translation = marker },
            GlsKind.FloatFX => new BaseFxEventFloat { RelativeJsonTime = offset, Value = marker },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        // Keep an unselected parent and child well away from every boundary operation to catch accidental whole-map translation.
        private BaseEventBoxGroup PlaceGuard(GlsKind kind) => PlaceGroup(kind, 3, FinalBeat / 2f, new[] { 0.75f });

        // LoadedObjects exposes backing collection data in this API, not just currently visible/pool-resident containers.
        private static BaseEventBoxGroup[] Groups(GlsKind kind) => BeatmapObjectContainerCollection.GetCollectionForType(kind switch
        {
            GlsKind.Color => ObjectType.GLSColor,
            GlsKind.Rotation => ObjectType.GLSRotation,
            GlsKind.Translation => ObjectType.GLSTranslation,
            GlsKind.FloatFX => ObjectType.GLSFloatFx,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }).LoadedObjects.Cast<BaseEventBoxGroup>().ToArray();

        // Single-ID lookup catches duplicate parents left behind by an incomplete replacement action.
        private static BaseEventBoxGroup Group(GlsKind kind, int id) => Groups(kind).Single(g => g.ID == id);

        // Check absolute/relative time, payload, type, and exact owners together so a boundary fix cannot mask stale child references.
        private static void AssertGroup(BaseEventBoxGroup group, GlsKind kind, float beat, float[][] offsets)
        {
            Assert.That(group.JsonTime, Is.EqualTo(beat).Within(0.0001f));
            Assert.That(group.ReadOnlyBoxes.Count, Is.EqualTo(offsets.Length));
            for (var lane = 0; lane < offsets.Length; lane++)
            {
                var box = group.ReadOnlyBoxes[lane];
                Assert.That(box.ReadOnlyEvents.Count, Is.EqualTo(offsets[lane].Length));
                for (var index = 0; index < offsets[lane].Length; index++)
                {
                    var node = box.ReadOnlyEvents[index];
                    Assert.That(node.GetType(), Is.EqualTo(CreateNode(kind, 0f, 0f).GetType()));
                    Assert.That(node.RelativeJsonTime, Is.EqualTo(offsets[lane][index]).Within(0.0001f));
                    Assert.That(node.JsonTime, Is.EqualTo(beat + offsets[lane][index]).Within(0.0001f));
                    Assert.That(node.RelativeJsonTime, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(node.EventBoxGroupData, Is.SameAs(group));
                    Assert.That(node.EventBoxData, Is.SameAs(box));
                    Assert.That(node.BoxIndex, Is.EqualTo(lane));
                    var marker = node switch
                    {
                        BaseLightColorBase color => color.Brightness,
                        BaseLightRotationBase rotation => rotation.Rotation,
                        BaseLightTranslationBase translation => translation.Translation,
                        BaseFxEventFloat floatFx => floatFx.Value,
                        _ => throw new ArgumentOutOfRangeException(nameof(kind))
                    };
                    Assert.That(marker, Is.EqualTo(lane * 10 + index + 1));
                }
            }
        }

        // Child actions must publish the live context; paste retains its existing group-selection policy rather than gaining an unrelated regression.
        private void AssertInnerGroup(GlsKind kind, int id, float beat, float[][] offsets, bool isPaste = false)
        {
            var group = Group(kind, id);
            Assert.That(innerProvider.GroupContext, Is.SameAs(group));
            AssertGroup(group, kind, beat, offsets);
            if (!isPaste)
            {
                Assert.That(SelectionController.SelectedObjects.OfType<BaseEventBoxGroup>(), Is.Empty);
            }
        }

        // Unselected objects must retain reference identity as well as their complete parent/child timing and value data.
        private void AssertGuard(GlsKind kind, BaseEventBoxGroup original)
        {
            Assert.That(Group(kind, 3), Is.SameAs(original));
            AssertGroup(original, kind, FinalBeat / 2f, new[] { new[] { 0.75f } });
            Assert.That(SelectionController.SelectedObjects.Contains(original), Is.False);
        }

        // Selection must never retain nodes from a retired group or groups that are absent from the authoritative collection.
        private static void AssertSelectedOwnership()
        {
            foreach (var selected in SelectionController.SelectedObjects)
            {
                var owner = selected is BaseGLSEvent node ? node.EventBoxGroupData : selected as BaseEventBoxGroup;
                Assert.That(owner, Is.Not.Null);
                var stored = BeatmapObjectContainerCollection.GetCollectionForType(owner.ObjectType).LoadedObjects;
                Assert.That(stored.Any(obj => ReferenceEquals(obj, owner)), Is.True);
                if (selected is BaseGLSEvent child)
                {
                    Assert.That(child.EventBoxData, Is.SameAs(owner.ReadOnlyBoxes[child.BoxIndex]));
                    Assert.That(child.EventBoxData.ReadOnlyEvents.Any(evt => ReferenceEquals(evt, child)), Is.True);
                }
            }
        }

        // Assert the intended some/all precondition explicitly so fixture changes cannot silently weaken overflow coverage.
        private void AssertOutsideScenario(float[] attempted, bool atEnd, bool allOutside)
        {
            var count = attempted.Count(beat => atEnd ? beat > FinalBeat : beat < 0f);
            Assert.That(count, Is.GreaterThan(0));
            // Count equality distinguishes total overflow without relying on a conditional between different NUnit constraint types.
            Assert.That(count == attempted.Length, Is.EqualTo(allOutside));
        }

        // Clipboard objects are mutable: snapshot both computed absolute beats and serialized data to detect paste-side mutation.
        private static string[] ClipboardSnapshot() => SelectionController.CopiedObjects
            .Select(obj => obj.JsonTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + obj.ToJson())
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();

        // Exactly one action must restore and replay the complete edit, not require one undo per selected parent or inner node.
        private void AssertRoundTrip(Action assertApplied, Action assertOriginal)
        {
            assertApplied();
            var action = actions.Undo();
            Assert.That(action, Is.Not.Null);
            assertOriginal();
            Assert.That(actions.Undo(), Is.Null, "The edit must be one atomic action.");
            Assert.That(actions.Redo(), Is.SameAs(action));
            assertApplied();
            Assert.That(actions.Redo(), Is.Null, "No extra edits may remain on the redo stack.");
        }

        // Lower-limit rejection/restoration must not publish a negative intermediate state that undo or redo could resurrect.
        private void AssertNoAction()
        {
            Assert.That(actions.Undo(), Is.Null);
            Assert.That(actions.Redo(), Is.Null);
        }
    }
}
