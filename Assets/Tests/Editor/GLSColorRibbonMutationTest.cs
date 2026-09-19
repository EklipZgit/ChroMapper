using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.Editor
{
    // GLSColorRibbonMutationTest keeps the requested edit/undo matrix compact while every case still exercises all node-easing permutations.
    public class GLSColorRibbonMutationTest : GLSColorRibbonTestBase
    {
        private const int SyntheticPrimaryGroupId = 1000;
        private const int SyntheticSecondaryGroupId = 1001;

        // Resolve a physical color lane plus one fixture lane on the same page so outer paste/shift has valid metadata.
        private static int PrimaryGroupId => GetColorLanePair().Primary;
        private static int SecondaryGroupId => GetColorLanePair().Secondary;

        private static readonly Color colorA = new(0.91f, 0.13f, 0.21f, 0.71f);
        private static readonly Color colorB = new(0.17f, 0.83f, 0.29f, 0.59f);
        private static readonly Color colorC = new(0.23f, 0.31f, 0.94f, 0.43f);

        private static readonly int[] easingValues =
        {
            (int)EaseType.Linear,
            (int)EaseType.None,
            (int)EaseType.InQuadratic
        };

        private StackTraceLogType originalWarningStackTrace;

        // GLSColorRibbonMutationTest seeks through thousands of preview samples; omit repeated localization-warning stack traces while retaining each warning and every error.
        [OneTimeSetUp]
        public void SuppressRepeatedWarningStackTraces()
        {
            originalWarningStackTrace = Application.GetStackTraceLogType(LogType.Warning);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        }

        // GLSColorRibbonMutationTest restores the editor's warning diagnostics after the fixture completes.
        [OneTimeTearDown]
        public void RestoreWarningStackTraces() =>
            Application.SetStackTraceLogType(LogType.Warning, originalWarningStackTrace);

        // Keep synthetic track insertion order stable across all 27 easing iterations, then remove it after the NUnit case.
        [TearDown]
        public void RemoveSyntheticColorTracks()
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            if (runtime == null)
            {
                return;
            }

            runtime.TrackDefinitions.Gls.Remove(SyntheticPrimaryGroupId);
            runtime.TrackDefinitions.Gls.Remove(SyntheticSecondaryGroupId);
        }

        // MovementCases represents the four requested chronological destinations for each production edit path and both GLS views.
        private static IEnumerable<TestCaseData> MovementCases()
        {
            foreach (var view in Enum.GetValues(typeof(RibbonView)).Cast<RibbonView>())
            foreach (var operation in Enum.GetValues(typeof(MovementOperation)).Cast<MovementOperation>())
            foreach (var destination in Enum.GetValues(typeof(ChronologicalDestination)).Cast<ChronologicalDestination>())
            {
                yield return new TestCaseData(view, operation, destination)
                    .SetName($"{view}_{operation}_{destination}_KeepsRibbonAndPreviewSynchronized");
            }
        }

        // PositionalCases applies deletion independently to the moved node in each of the four requested resulting orders.
        private static IEnumerable<TestCaseData> PositionalCases()
        {
            foreach (var view in Enum.GetValues(typeof(RibbonView)).Cast<RibbonView>())
            foreach (var position in Enum.GetValues(typeof(ChronologicalDestination)).Cast<ChronologicalDestination>())
            {
                yield return new TestCaseData(view, position)
                    .SetName($"{view}_DeleteAndUndo_{position}_KeepsRibbonAndPreviewSynchronized");
            }
        }

        // HoverCases covers every color-node wheel mutation that can alter the live GLS result or its ribbon presentation.
        private static IEnumerable<TestCaseData> HoverCases()
        {
            foreach (var view in Enum.GetValues(typeof(RibbonView)).Cast<RibbonView>())
            foreach (var mutation in Enum.GetValues(typeof(HoverMutation)).Cast<HoverMutation>())
            foreach (var position in Enum.GetValues(typeof(ChronologicalDestination)).Cast<ChronologicalDestination>())
            {
                yield return new TestCaseData(view, mutation, position)
                    .SetName($"{view}_{mutation}_{position}_KeepsRibbonAndPreviewSynchronized");
            }
        }

        // Every movement case asserts the authored order, the production light result, and the already-rendered ribbon before and after undo.
        [TestCaseSource(nameof(MovementCases))]
        public void MovingColorNodeKeepsRibbonAndPreviewSynchronized(
            RibbonView view,
            MovementOperation operation,
            ChronologicalDestination destination)
        {
            // GLSColorRibbonMutationTest uses two inner filter lanes; keep both physical IDs valid throughout the easing matrix.
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            // MovingColorNodeKeepsRibbonAndPreviewSynchronized reuses one authored topology because undo restores it before the next easing permutation.
            var scenario = CreateMovementScenario(view, operation, destination, GetEasingTriples().First());
            try
            {
                foreach (var easings in GetEasingTriples())
                {
                    ApplyScenarioEasings(easings);
                    AssertScenario(scenario, preview, "before edit");

                    if (operation == MovementOperation.CutPaste)
                    {
                        CutMovedObject(scenario);
                        AssertScenario(scenario, preview, "after cut");
                        PasteMovedObject(scenario);
                        AssertExpectedOrder(scenario, ExpectedOrder(destination));
                        AssertScenario(scenario, preview, "after paste");

                        Undo();
                        AssertScenario(scenario, preview, "after undoing paste");
                        Undo();
                    }
                    else
                    {
                        ApplyMovement(scenario, operation);
                        AssertExpectedOrder(scenario, ExpectedOrder(destination));
                        AssertScenario(scenario, preview, "after edit");
                        Undo();
                    }

                    AssertExpectedOrder(
                        scenario,
                        operation == MovementOperation.LaneShift
                            ? ExpectedOrder(destination).Replace(scenario.MovedLabel.ToString(), string.Empty)
                            : "ABC");
                    AssertScenario(scenario, preview, "after final undo");
                }
            }
            finally
            {
                CleanupScenario();
            }
        }

        // PartialBoxTimeShiftKeepsBoxEventsChronological proves a partial-lane MoveSelection rewrites the box's
        // serialized event order; OrderedEvents lazily self-heals, but GLSColorTimeline's box.Events[^1] reads
        // and saved JSON output consume the raw array order.
        [Test]
        public void PartialBoxTimeShiftKeepsBoxEventsChronological()
        {
            var scenario = CreateInnerScenario("ABC", GetEasingTriples().First(), 'A');
            try
            {
                SelectMovedObject(scenario);
                UnityEngine.Object.FindAnyObjectByType<SelectionController>().MoveSelection(5f);

                var group = FindScenarioGroups(scenario.GroupId).Single();
                var events = group.Boxes[0].Events;
                Assert.That(events.Length, Is.EqualTo(3));
                for (var index = 1; index < events.Length; index++)
                {
                    Assert.That(
                        events[index].RelativeJsonTime,
                        Is.GreaterThanOrEqualTo(events[index - 1].RelativeJsonTime),
                        $"box.Events[{index}] lost chronological order after the partial-box shift.");
                }

                Assert.That(GetLabel(events[^1]), Is.EqualTo('A'));
            }
            finally
            {
                CleanupScenario();
            }
        }

        // RibbonVisibilitySettingRefreshesLoadedContainersImmediately proves both GLS views react to the named setting event without scrolling or rebuilding the map.
        [TestCase(RibbonView.Inner)]
        [TestCase(RibbonView.Outer)]
        public void RibbonVisibilitySettingRefreshesLoadedContainersImmediately(RibbonView view)
        {
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            var scenario = view == RibbonView.Inner
                ? CreateInnerScenario("ABC", GetEasingTriples().First())
                : CreateOuterScenario("ABC", GetEasingTriples().First());
            try
            {
                AssertScenario(scenario, preview, "before visibility setting");
                var controllers = GetLoadedColorRibbonControllers(view, scenario.GroupId);
                var initialVisibility = controllers.ToDictionary(
                    controller => controller,
                    controller => controller.gameObject.activeSelf);
                Assert.That(initialVisibility.Values, Has.Some.True, "The scenario needs at least one visible ribbon.");

                Settings.Instance.VisualizeGLSLightTransitions = false;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    nameof(Settings.VisualizeGLSLightTransitions),
                    false);
                Assert.That(controllers, Has.All.Matches<LightGradientController>(
                    controller => !controller.gameObject.activeSelf));

                Settings.Instance.VisualizeGLSLightTransitions = true;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    nameof(Settings.VisualizeGLSLightTransitions),
                    true);
                foreach (var pair in initialVisibility)
                {
                    Assert.That(pair.Key.gameObject.activeSelf, Is.EqualTo(pair.Value));
                }
            }
            finally
            {
                Settings.Instance.VisualizeGLSLightTransitions = true;
                Settings.ManuallyNotifySettingUpdatedEvent(
                    nameof(Settings.VisualizeGLSLightTransitions),
                    true);
                CleanupScenario();
            }
        }

        // Snapshot only the loaded controllers owned by the active synthetic lane so unrelated fixture ribbons cannot mask a missed live refresh.
        private static List<LightGradientController> GetLoadedColorRibbonControllers(RibbonView view, int groupId)
        {
            var controllers = new List<LightGradientController>();
            if (view == RibbonView.Inner)
            {
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                foreach (var container in collection.LoadedContainers.Values.OfType<GLSEventContainer>())
                {
                    if (container.EventData?.EventBoxGroupData?.ID != groupId)
                    {
                        continue;
                    }

                    controllers.Add(container.LightGradientController);
                    controllers.Add(container.IncomingLightGradientController);
                }
            }
            else
            {
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
                var owners = new HashSet<GLSGroupContainer>(
                    collection.LoadedContainers
                        .Where(pair => pair.Key is BaseLightColorEventBoxGroup group && group.ID == groupId)
                        .Select(pair => pair.Value)
                        .OfType<GLSGroupContainer>());
                foreach (var container in UnityEngine.Object.FindObjectsByType<GLSGroupContainer>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (!owners.Contains(container) && !owners.Contains(container.DragTarget))
                    {
                        continue;
                    }

                    controllers.Add(container.lightGradientController);
                    controllers.Add(container.IncomingLightGradientController);
                }
            }

            return controllers.Where(controller => controller != null).Distinct().ToList();
        }

        // DeleteAndUndoColorNode checks each first/middle/last topology produced by the four movement destinations without sharing an undo action.
        [TestCaseSource(nameof(PositionalCases))]
        public void DeleteAndUndoColorNode(
            RibbonView view,
            ChronologicalDestination position)
        {
            // GLSColorRibbonMutationTest uses two inner filter lanes; keep both physical IDs valid throughout the easing matrix.
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            // DeleteAndUndoColorNode reuses its restored map objects instead of rebuilding the scene-backed GLS scenario 27 times.
            var scenario = CreatePositionalScenario(view, position, GetEasingTriples().First());
            try
            {
                foreach (var easings in GetEasingTriples())
                {
                    ApplyScenarioEasings(easings);
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "before delete");

                    DeleteMovedObject(scenario);
                    AssertScenario(scenario, preview, "after delete");

                    Undo();
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "after undoing delete");
                }
            }
            finally
            {
                CleanupScenario();
            }
        }

        // HoverMutatingColorNodeAndUndo covers brightness, both strobe values, strobe fade, and easing on inner and outer preview nodes.
        [TestCaseSource(nameof(HoverCases))]
        public void HoverMutatingColorNodeAndUndo(
            RibbonView view,
            HoverMutation mutation,
            ChronologicalDestination position)
        {
            // GLSColorRibbonMutationTest uses two inner filter lanes; keep both physical IDs valid throughout the easing matrix.
            var preview = InitializeColorPreview(PrimaryGroupId, minimumLightCount: 2);
            // HoverMutatingColorNodeAndUndo keeps its hover-independent topology alive across easing permutations after each undo restores the node.
            var scenario = CreatePositionalScenario(view, position, GetEasingTriples().First());
            HoverInputSession inputSession = null;
            try
            {
                // HoverMutatingColorNodeAndUndo owns one isolated Input System session for all 27 easing permutations instead of recreating devices for every wheel tick.
                inputSession = new HoverInputSession();
                foreach (var easings in GetEasingTriples())
                {
                    ApplyScenarioEasings(easings);
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "before hover mutation");

                    var originalEvent = ApplyHoverMutation(scenario, mutation, inputSession);
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "after hover mutation");

                    Undo();
                    AssertColorEventPropertiesEqual(originalEvent, GetMovedObject(scenario), "after undoing hover mutation");
                    AssertExpectedOrder(scenario, ExpectedOrder(position));
                    AssertScenario(scenario, preview, "after undoing hover mutation");
                }
            }
            finally
            {
                inputSession?.Dispose();
                CleanupScenario();
            }
        }

        // GetEasingTriples exhausts None, Linear, and one nonlinear easing independently for all three nodes without multiplying NUnit fixtures.
        private static IEnumerable<EasingTriple> GetEasingTriples()
        {
            foreach (var first in easingValues)
            foreach (var second in easingValues)
            foreach (var third in easingValues)
                yield return new EasingTriple(first, second, third);
        }

        // The three ribbon mutation tests replace only their fixture groups when changing easing, then discard setup actions so the tested edit remains the sole undo step.
        private static void ApplyScenarioEasings(EasingTriple easings)
        {
            var groups = FindScenarioGroups(PrimaryGroupId)
                .Concat(FindScenarioGroups(SecondaryGroupId))
                .ToArray();
            foreach (var group in groups)
            {
                var replacement = BeatmapFactory.Clone(group) as BaseLightColorEventBoxGroup;
                var changed = false;
                foreach (var evt in replacement.Boxes.SelectMany(box => box.Events))
                {
                    if (!TryGetLabel(evt, out var label))
                    {
                        continue;
                    }

                    var easing = label switch
                    {
                        'A' => easings.A,
                        'B' => easings.B,
                        _ => easings.C
                    };
                    if (evt.Easing == easing)
                    {
                        continue;
                    }

                    evt.Easing = easing;
                    changed = true;
                }

                if (changed)
                {
                    GLSCommonCommand.TriggerModifyEventBoxAction(
                        group,
                        replacement,
                        ActionMergeType.ModifyGLSEventEasing);
                }
            }

            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
        }

        // CreateMovementScenario starts time movements from ABC and gives lane shifts an adjacent source lane feeding the requested destination order.
        private static MutationScenario CreateMovementScenario(
            RibbonView view,
            MovementOperation operation,
            ChronologicalDestination destination,
            EasingTriple easings)
        {
            if (operation == MovementOperation.LaneShift)
            {
                return CreateLaneShiftScenario(view, destination, easings);
            }

            var scenario = view == RibbonView.Inner
                ? CreateInnerScenario("ABC", easings, MovedLabel(destination))
                : CreateOuterScenario("ABC", easings, MovedLabel(destination));
            scenario.DestinationTime = DestinationTime(destination);
            return scenario;
        }

        // CreatePositionalScenario realizes the post-movement topology first so delete and hover cases cover the moved node at every requested position.
        private static MutationScenario CreatePositionalScenario(
            RibbonView view,
            ChronologicalDestination position,
            EasingTriple easings) => view == RibbonView.Inner
                ? CreateInnerScenario(ExpectedOrder(position), easings)
                : CreateOuterScenario(ExpectedOrder(position), easings);

        // CreateInnerScenario authors all nodes into one filter lane and opens that group through the production provider.
        private static MutationScenario CreateInnerScenario(string order, EasingTriple easings)
            => CreateInnerScenario(order, easings, order is "ACB" or "CAB" ? 'C' : 'A');

        // The explicit moved label distinguishes backward edits that start from the same ABC chronology as forward edits.
        private static MutationScenario CreateInnerScenario(string order, EasingTriple easings, char movedLabel)
        {
            // Inner selection, deletion, and paste commands are enabled only on the Event Box editing tab.
            UnityEngine.Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
            var group = CreateGroup(1f, PrimaryGroupId, 0, CreateOrderedEvents(order, easings));
            SpawnGroup(group);
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = group;
            return new MutationScenario(
                RibbonView.Inner,
                movedLabel,
                group.ID);
        }

        // CreateOuterScenario uses one zero-offset event per group so outer preview nodes form the same chronological transition timeline as inner nodes.
        private static MutationScenario CreateOuterScenario(string order, EasingTriple easings)
            => CreateOuterScenario(order, easings, order is "ACB" or "CAB" ? 'C' : 'A');

        // The explicit moved label keeps ABC backward scenarios targeted at C before the drag or time shift occurs.
        private static MutationScenario CreateOuterScenario(string order, EasingTriple easings, char movedLabel)
        {
            // Outer group commands and preview containers are owned by the GLS editing tab.
            UnityEngine.Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.GLS;
            // A nonzero authored preview opacity is what enables each outer node's represented-event ribbon.
            Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
            ConfigureOuterColorPage();
            for (var index = 0; index < order.Length; index++)
            {
                var label = order[index];
                var group = CreateGroup(2f + (index * 2f), PrimaryGroupId, 0, CreateEvent(label, easings));
                SpawnGroup(group);
            }

            return new MutationScenario(
                RibbonView.Outer,
                movedLabel,
                PrimaryGroupId);
        }

        // CreateLaneShiftScenario places the moved node beside the destination lane while retaining both affected live-light timelines for assertions.
        private static MutationScenario CreateLaneShiftScenario(
            RibbonView view,
            ChronologicalDestination destination,
            EasingTriple easings)
        {
            // Lane-shift selection uses the same tab-specific edit permissions as direct movement.
            UnityEngine.Object.FindAnyObjectByType<EditModeContext>().EditingMode = view == RibbonView.Inner
                ? EditingMode.EventBox
                : EditingMode.GLS;
            // Outer lane-shift cases require the same visible represented-event preview as ordinary outer cases.
            if (view == RibbonView.Outer)
            {
                Settings.Instance.GLSOuterTrackGhostNodeOpacity = 0.5f;
                ConfigureOuterColorPage();
            }

            var movedLabel = destination is ChronologicalDestination.BackwardBetween or ChronologicalDestination.BackwardFirst
                ? 'C'
                : 'A';
            var destinationOrder = ExpectedOrder(destination).Replace(movedLabel.ToString(), string.Empty);
            var movedTime = LaneDestinationTime(destination);

            if (view == RibbonView.Inner)
            {
                var destinationEvents = CreateEventsAtFinalOrderSlots(
                    ExpectedOrder(destination),
                    movedLabel,
                    easings,
                    1f);
                var sourceEvent = CreateEvent(movedLabel, easings);
                sourceEvent.RelativeJsonTime = movedTime - 1f;
                var group = new BaseLightColorEventBoxGroup
                {
                    JsonTime = 1f,
                    ID = PrimaryGroupId,
                    Boxes =
                    {
                        CreateBox(0, destinationEvents),
                        CreateBox(1, new[] { sourceEvent })
                    }
                };
                group.NormalizeLoadedEventConflicts();
                SpawnGroup(group);
                var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
                provider.LastContext = null;
                provider.GroupContext = group;
                return new MutationScenario(view, movedLabel, PrimaryGroupId) { LaneDirection = -1 };
            }

            for (var index = 0; index < destinationOrder.Length; index++)
            {
                var destinationLabel = destinationOrder[index];
                var finalIndex = ExpectedOrder(destination).IndexOf(destinationLabel);
                SpawnGroup(CreateGroup(
                    2f + (finalIndex * 2f),
                    PrimaryGroupId,
                    0,
                    CreateEvent(destinationLabel, easings)));
            }

            // Outer lanes select the light group, not a different fixture light; keeping the same filter proves
            // Ctrl+Left/Right changes the group timeline instead of accidentally changing which light is sampled.
            SpawnGroup(CreateGroup(movedTime, SecondaryGroupId, 0, CreateEvent(movedLabel, easings)));
            // Track definitions preserve dictionary order, which can reverse after teardown/reinsertion; derive the
            // actual Ctrl+Arrow direction that moves the synthetic source lane into the primary destination lane.
            var pair = GetColorLanePair();
            var colorLaneIds = UnityEngine.Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions.Gls
                .Where(entry => entry.Value.ColorTrack && entry.Value.Group == pair.Group)
                .Select(entry => entry.Key)
                .ToArray();
            var laneDirection = Array.IndexOf(colorLaneIds, pair.Primary)
                - Array.IndexOf(colorLaneIds, pair.Secondary);
            return new MutationScenario(view, movedLabel, PrimaryGroupId) { LaneDirection = laneDirection };
        }

        // ApplyMovement routes each matrix row through the editor operation users invoke instead of rebuilding visual caches in the test.
        private static void ApplyMovement(MutationScenario scenario, MovementOperation operation)
        {
            switch (operation)
            {
                case MovementOperation.AltDrag:
                    AltDragMovedObject(scenario);
                    break;
                case MovementOperation.TimeShift:
                    SelectMovedObject(scenario);
                    UnityEngine.Object.FindAnyObjectByType<SelectionController>()
                        .MoveSelection(scenario.DestinationTime - GetMovedObject(scenario).JsonTime, true);
                    break;
                case MovementOperation.LaneShift:
                    SelectMovedObject(scenario);
                    UnityEngine.Object.FindAnyObjectByType<SelectionController>()
                        .ShiftSelection(scenario.LaneDirection, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        // AltDragMovedObject uses the same placement StartDrag/FinishDrag path as the Alt-held mouse gesture.
        private static void AltDragMovedObject(MutationScenario scenario)
        {
            var moved = GetMovedObject(scenario);
            if (scenario.View == RibbonView.Inner)
            {
                var collection = BeatmapObjectContainerCollection
                    .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent);
                // The test scene retains inactive placement templates; select the initialized instance bound to this collection.
                var placement = UnityEngine.Object.FindObjectsByType<GLSEventColorPlacement>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None)
                    .Where(candidate => ReferenceEquals(candidate.ObjectContainerCollection, collection))
                    .OrderByDescending(candidate => candidate.isActiveAndEnabled)
                    .FirstOrDefault();
                Assert.That(placement, Is.Not.Null);
                // Build the drag hit container exactly as PlacementInputSystem does; pooled loaded containers may be
                // absent when the shared test playhead has just sampled the final ribbon interval.
                placement.Initialize(null);
                var dragContainer = collection.CreateContainer() as GLSEventContainer;
                Assert.That(dragContainer, Is.Not.Null);
                dragContainer.ObjectData = moved;
                dragContainer.Setup();
                dragContainer.UpdateGridPosition();
                Assert.That(placement.StartDrag(dragContainer.gameObject), Is.Not.Null);
                placement.DraggedObjectData.RelativeJsonTime = scenario.DestinationTime
                    - placement.DraggedObjectData.EventBoxGroupData.JsonTime;
                placement.DraggedObjectData.RecomputeSongBpmTime();
                placement.FinishDrag();
                return;
            }

            // Resolve the authoritative map-owned parent because replacement actions may leave a child back-reference
            // pointing at the pre-normalized group instance even though the child itself is current.
            var group = GetOwningGroup(moved);
            var groupCollection = GetGroupCollection();
            var groupPlacement = GetOuterColorPlacement(groupCollection);
            // Build the hit container directly because the shared test viewport may have pooled the authored outer node.
            var groupContainer = groupCollection.CreateContainer() as GLSGroupContainer;
            Assert.That(groupContainer, Is.Not.Null);
            groupContainer.ObjectData = group;
            groupContainer.Setup();
            Assert.That(groupPlacement.StartDrag(groupContainer.gameObject), Is.Not.Null);
            groupPlacement.DraggedObjectData.JsonTime = scenario.DestinationTime;
            RecomputeGroupEventTimes(groupPlacement.DraggedObjectData);
            groupPlacement.FinishDrag();
        }

        // CutMovedObject invokes the production selection cut so its delete action remains independently undoable from paste.
        private static void CutMovedObject(MutationScenario scenario)
        {
            SelectMovedObject(scenario);
            UnityEngine.Object.FindAnyObjectByType<SelectionController>().Copy(true);
        }

        // PasteMovedObject supplies the same queued hover destination consumed by the production lane-aware paste path.
        private static void PasteMovedObject(MutationScenario scenario)
        {
            if (scenario.View == RibbonView.Inner)
            {
                var placement = UnityEngine.Object.FindAnyObjectByType<GLSEventColorPlacement>();
                var group = FindScenarioGroups(scenario.GroupId).Single();
                placement.State = PlacementState.Active;
                placement.QueuedData.EventBoxGroupData = group;
                placement.QueuedData.EventBoxData = group.Boxes[0];
                placement.QueuedData.BoxIndex = 0;
                placement.QueuedData.RelativeJsonTime = scenario.DestinationTime - group.JsonTime;
                placement.QueuedData.RecomputeSongBpmTime();
            }
            else
            {
                var placement = GetOuterColorPlacement(GetGroupCollection());
                placement.State = PlacementState.Active;
                placement.QueuedData.ID = scenario.GroupId;
                placement.QueuedData.JsonTime = scenario.DestinationTime;
            }

            UnityEngine.Object.FindAnyObjectByType<SelectionController>().Paste();
        }

        // DeleteMovedObject uses selection deletion for both an owned inner event and its single-event outer group equivalent.
        private static void DeleteMovedObject(MutationScenario scenario)
        {
            SelectMovedObject(scenario);
            UnityEngine.Object.FindAnyObjectByType<SelectionController>().Delete();
        }

        // ApplyHoverMutation drives the authored modifier-and-wheel composite through an isolated input runtime and production hover controller.
        private static BaseLightColorBase ApplyHoverMutation(
            MutationScenario scenario,
            HoverMutation mutation,
            HoverInputSession inputSession)
        {
            var moved = GetMovedObject(scenario);
            var originalEvent = BeatmapFactory.Clone(moved) as BaseLightColorBase;
            GameObject containerObject = null;
            GameObject controllerObject = null;
            try
            {
                containerObject = new GameObject($"{scenario.View} GLS ribbon hover target");
                controllerObject = new GameObject($"{scenario.View} GLS ribbon hover controller");
                CMInput.IGLSColorObjectsActions controller;
                if (scenario.View == RibbonView.Inner)
                {
                    UnityEngine.Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.EventBox;
                    var container = containerObject.AddComponent<GLSEventContainer>();
                    container.VisualSettings = GetInitializedVisualSettings();
                    container.EventData = moved;
                    SetHighlightedWithoutVisualRefresh(container);
                    var innerController = controllerObject.AddComponent<TestGLSEventColorInputController>();
                    innerController.IsHovering = true;
                    innerController.HoveredObject = container;
                    innerController.RaycastTarget = container;
                    innerController.SetPrecision(UnityEngine.Object.FindAnyObjectByType<ScrollPrecisionController>());
                    controller = innerController;
                }
                else
                {
                    UnityEngine.Object.FindAnyObjectByType<EditModeContext>().EditingMode = EditingMode.GLS;
                    var container = containerObject.AddComponent<GLSGroupContainer>();
                    container.VisualSettings = GetInitializedVisualSettings();
                    // The outer hover action must target the authoritative map-owned parent after fixture placement.
                    container.EventBoxGroupData = GetOwningGroup(moved);
                    container.PreviewEventData = moved;
                    var outerController = controllerObject.AddComponent<TestGLSGroupColorInputController>();
                    outerController.IsHovering = true;
                    outerController.HoveredObject = container;
                    outerController.RaycastTarget = container;
                    controller = outerController;
                }

                inputSession.Input.GLSColorObjects.Disable();
                inputSession.Input.GLSColorObjects.SetCallbacks(controller);
                inputSession.Input.GLSColorObjects.Enable();
                SendHoverScroll(inputSession.Fixture, inputSession.Keyboard, inputSession.Mouse, mutation);
                AssertHoverMutationApplied(originalEvent, GetMovedObject(scenario), mutation);
            }
            finally
            {
                if (controllerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(controllerObject);
                }

                if (containerObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(containerObject);
                }
            }

            return originalEvent;
        }

        // HoverMutatingColorNodeAndUndo isolates native device state once per NUnit case and restores the shared action map after disposing fixture-owned actions.
        private sealed class HoverInputSession : IDisposable
        {
            private readonly CMInput sharedInput;
            private readonly bool sharedMapWasEnabled;

            public HoverInputSession()
            {
                sharedInput = CMInputCallbackInstaller.InputInstance;
                Assert.That(sharedInput, Is.Not.Null);
                sharedMapWasEnabled = sharedInput.GLSColorObjects.enabled;
                sharedInput.GLSColorObjects.Disable();
                Fixture = new InputTestFixture();
                Fixture.Setup();
                Input = new CMInput();
                Keyboard = InputSystem.AddDevice<Keyboard>();
                Mouse = InputSystem.AddDevice<Mouse>();
            }

            public InputTestFixture Fixture { get; }
            public CMInput Input { get; }
            public Keyboard Keyboard { get; }
            public Mouse Mouse { get; }

            public void Dispose()
            {
                Input.GLSColorObjects.Disable();
                Input.Dispose();
                Fixture.TearDown();
                if (sharedMapWasEnabled)
                {
                    sharedInput.GLSColorObjects.Enable();
                }
            }
        }

        // AssertHoverMutationApplied prevents an unchanged map and equally stale consumers from passing the parity assertion.
        private static void AssertHoverMutationApplied(
            BaseLightColorBase original,
            BaseLightColorBase edited,
            HoverMutation mutation)
        {
            switch (mutation)
            {
                case HoverMutation.Brightness:
                    Assert.That(edited.Brightness, Is.Not.EqualTo(original.Brightness));
                    break;
                case HoverMutation.StrobeFrequency:
                    Assert.That(
                        edited.Frequency != original.Frequency
                        || edited.ChromaStrobeInterval != original.ChromaStrobeInterval,
                        Is.True);
                    break;
                case HoverMutation.StrobeBrightness:
                    Assert.That(edited.StrobeBrightness, Is.Not.EqualTo(original.StrobeBrightness));
                    break;
                case HoverMutation.StrobeFade:
                    Assert.That(edited.StrobeFade, Is.Not.EqualTo(original.StrobeFade));
                    break;
                case HoverMutation.Easing:
                    Assert.That(
                        edited.Easing != original.Easing
                        || edited.ChromaColorEasing != original.ChromaColorEasing,
                        Is.True);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null);
            }
        }

        // AssertColorEventPropertiesEqual proves one undo restores every field touched by any hover-scroll chord.
        private static void AssertColorEventPropertiesEqual(
            BaseLightColorBase expected,
            BaseLightColorBase actual,
            string checkpoint)
        {
            Assert.That(actual.Brightness, Is.EqualTo(expected.Brightness), checkpoint);
            Assert.That(actual.Frequency, Is.EqualTo(expected.Frequency), checkpoint);
            Assert.That(actual.ChromaStrobeInterval, Is.EqualTo(expected.ChromaStrobeInterval), checkpoint);
            Assert.That(actual.StrobeBrightness, Is.EqualTo(expected.StrobeBrightness), checkpoint);
            Assert.That(actual.StrobeFade, Is.EqualTo(expected.StrobeFade), checkpoint);
            Assert.That(actual.Easing, Is.EqualTo(expected.Easing), checkpoint);
            Assert.That(actual.ChromaColorEasing, Is.EqualTo(expected.ChromaColorEasing), checkpoint);
        }

        // SendHoverScroll queues only fixture-owned devices and resets the scroll axis before restoring the shared runtime.
        private static void SendHoverScroll(
            InputTestFixture inputFixture,
            Keyboard keyboard,
            Mouse mouse,
            HoverMutation mutation)
        {
            // Queue the complete chord as one device state; separate queued key events overwrite earlier modifiers.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(HoverModifiers(mutation).ToArray()));
            InputSystem.Update();
            inputFixture.Set(mouse.scroll, new Vector2(0f, 1f), queueEventOnly: true);
            InputSystem.Update();
            inputFixture.Set(mouse.scroll, Vector2.zero, queueEventOnly: true);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
        }

        // HoverModifiers mirrors the five authored GLS color wheel composites without depending on a host keyboard.
        private static IEnumerable<Key> HoverModifiers(HoverMutation mutation) => mutation switch
        {
            HoverMutation.Brightness => new[] { Key.LeftAlt },
            HoverMutation.StrobeFrequency => new[] { Key.LeftAlt, Key.LeftCtrl },
            HoverMutation.StrobeBrightness => new[] { Key.LeftAlt, Key.LeftCtrl, Key.LeftShift },
            HoverMutation.StrobeFade => new[] { Key.LeftShift },
            HoverMutation.Easing => new[] { Key.LeftCtrl, Key.LeftShift },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null)
        };

        // AssertScenario samples every interval without requesting a manager refresh or ribbon/pool reconstruction.
        private void AssertScenario(
            MutationScenario scenario,
            GlsColorScenarioContext preview,
            string checkpoint)
        {
            AssertAllCurrentGlsIntervalsMatchExpected(
                preview,
                $"{scenario.View}, moved {scenario.MovedLabel}, {checkpoint}");
        }

        // AssertExpectedOrder separately proves the user-visible edit happened so mutually stale preview and ribbon caches cannot agree and pass.
        private static void AssertExpectedOrder(MutationScenario scenario, string expected)
        {
            // Materialize the map snapshot before NUnit unwinds the failed case and cleanup restores prior groups.
            var mapSnapshot = BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups
                .Where(group => group.Boxes.SelectMany(box => box.Events).Any(evt => TryGetLabel(evt, out _)))
                .Select(group => $"{group.ID}@{group.JsonTime}:"
                    + string.Join("/", group.Boxes.Select(box => $"f{box.IndexFilter.Param0}={string.Concat(box.Events.Select(GetLabel))}")))
                .ToArray();
            var actual = FindScenarioGroups(scenario.GroupId)
                .SelectMany(group => group.Boxes)
                .Where(box => box.IndexFilter.Param0 == 0)
                .SelectMany(box => box.Events)
                .OrderBy(evt => evt.EventBoxGroupData.JsonTime + evt.RelativeJsonTime)
                .Select(GetLabel)
                .ToArray();
            Assert.That(
                string.Concat(actual),
                Is.EqualTo(expected),
                $"{scenario}; map snapshot: {string.Join(", ", mapSnapshot)}");
        }

        // SelectMovedObject resolves replacement clones by their stable test color before invoking selection-owned editor actions.
        private static void SelectMovedObject(MutationScenario scenario)
        {
            SelectionController.DeselectAll();
            var moved = GetMovedObject(scenario);
            // Outer editing selects the map-owned group; child ownership back-references can lag replacement normalization.
            BaseObject selectedObject = scenario.View == RibbonView.Inner ? moved : GetOwningGroup(moved);
            SelectionController.Select(
                selectedObject,
                true,
                false,
                false);
            // A simulated keyboard edit is meaningful only if the same production selection gate accepted its target.
            Assert.That(
                SelectionController.SelectedObjects.Contains(selectedObject),
                Is.True,
                $"Selection rejected {selectedObject.GetType().Name} from {selectedObject.ObjectType} collection.");
        }

        // GetOwningGroup derives ownership from authoritative map membership rather than a potentially stale child back-reference.
        private static BaseLightColorEventBoxGroup GetOwningGroup(BaseLightColorBase evt) =>
            BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups.Single(group => group.Boxes
                .SelectMany(box => box.Events)
                .Any(candidate => ReferenceEquals(candidate, evt)));

        // GetMovedObject reacquires the current clone after every group replacement and undo.
        private static BaseLightColorBase GetMovedObject(MutationScenario scenario)
        {
            var groups = scenario.View == RibbonView.Inner
                ? FindScenarioGroups(scenario.GroupId)
                : FindScenarioGroups(PrimaryGroupId).Concat(FindScenarioGroups(SecondaryGroupId));
            var events = groups.SelectMany(group => group.Boxes)
                .SelectMany(box => box.Events)
                .ToArray();
            var matches = events.Where(evt => TryGetLabel(evt, out var label) && label == scenario.MovedLabel).ToArray();
            Assert.That(
                matches.Length,
                Is.EqualTo(1),
                $"Expected one moved {scenario.MovedLabel} event, found {matches.Length}; fixture events: "
                + string.Join(", ", events.Select(evt => $"{evt.CustomColor}@{evt.JsonTime}")));
            return matches[0];
        }

        // CreateGroup creates one authored filter lane and normalizes all ownership before it enters editor collections.
        private static BaseLightColorEventBoxGroup CreateGroup(
            float jsonTime,
            int groupId,
            int lightId,
            params BaseLightColorBase[] events)
        {
            var group = new BaseLightColorEventBoxGroup
            {
                JsonTime = jsonTime,
                ID = groupId,
                Boxes = { CreateBox(lightId, events) }
            };
            group.NormalizeLoadedEventConflicts();
            return group;
        }

        // CreateBox selects one light explicitly so lane-shift cases can verify both the source and destination simulation caches.
        private static BaseLightColorEventBox CreateBox(int lightId, IEnumerable<BaseLightColorBase> events) => new()
        {
            IndexFilter = new BaseIndexFilter
            {
                Type = (int)IndexFilterType.StepAndOffset,
                Param0 = lightId,
                Param1 = 0
            },
            Events = events.OrderBy(evt => evt.RelativeJsonTime).ToArray()
        };

        // CreateOrderedEvents maps each identity to its fixed chronological slot while preserving its independently permuted easing.
        private static BaseLightColorBase[] CreateOrderedEvents(string order, EasingTriple easings) => order
            .Select((label, index) =>
            {
                var evt = CreateEvent(label, easings);
                evt.RelativeJsonTime = 1f + (index * 2f);
                return evt;
            })
            .ToArray();

        // CreateEventsAtFinalOrderSlots leaves the moved node in its adjacent lane while preserving its eventual neighbors' beats.
        private static BaseLightColorBase[] CreateEventsAtFinalOrderSlots(
            string finalOrder,
            char excluded,
            EasingTriple easings,
            float relativeOffset) => finalOrder
            .Select((label, index) => (label, index))
            .Where(entry => entry.label != excluded)
            .Select(entry =>
            {
                var evt = CreateEvent(entry.label, easings);
                evt.RelativeJsonTime = relativeOffset + (entry.index * 2f);
                return evt;
            })
            .ToArray();

        // CreateEvent writes its custom color exactly as a loaded map does so production clone paths retain identity.
        private static BaseLightColorBase CreateEvent(char label, EasingTriple easings)
        {
            var evt = new BaseLightColorBase
            {
                Color = (int)LightColor.White,
                Brightness = label switch { 'A' => 0.35f, 'B' => 0.7f, _ => 1.1f },
                StrobeBrightness = 0.5f,
                CustomColor = label switch { 'A' => colorA, 'B' => colorB, _ => colorC },
                Easing = label switch { 'A' => easings.A, 'B' => easings.B, _ => easings.C }
            };
            evt.WriteCustom();
            return evt;
        }

        // Fixture placement uses the real placement action so the action-driven GLS light manager and rendered collection receive identical data.
        private static void SpawnGroup(BaseLightColorEventBoxGroup group) =>
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(
                    group,
                    Array.Empty<BaseObject>(),
                    "Placed a GLS ribbon mutation fixture."),
                true);

        // RecomputeGroupEventTimes mirrors outer drag hover updates before FinishDrag publishes the moved group.
        private static void RecomputeGroupEventTimes(BaseLightColorEventBoxGroup group)
        {
            foreach (var evt in group.Boxes.SelectMany(box => box.Events))
                evt.RecomputeSongBpmTime();
        }

        // Undo keeps every checkpoint on the public action-container path used by the editor shortcut.
        private static void Undo() => UnityEngine.Object.FindAnyObjectByType<BeatmapActionContainer>().Undo();

        // CleanupScenario removes only uniquely colored fixture groups and actions between in-test easing iterations without reloading the map.
        private static void CleanupScenario()
        {
            SelectionController.DeselectAll();
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSEventGridProvider>();
            provider.LastContext = null;
            provider.GroupContext = null;
            var groups = FindScenarioGroups(PrimaryGroupId).Concat(FindScenarioGroups(SecondaryGroupId)).ToArray();
            // Fixture removal also uses an action so incremental live-light state is empty before the next easing permutation.
            if (groups.Length > 0)
            {
                BeatmapActionContainer.AddAction(new SelectionDeletedAction(groups), true);
            }

            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            SelectionController.CopiedObjects.Clear();
        }

        // Production placement rewrites unsupported group custom data, so fixture ownership uses clone-stable event colors instead.
        private static IEnumerable<BaseLightColorEventBoxGroup> FindScenarioGroups(int groupId) =>
            BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups
                .Where(group => group.ID == groupId && group.Boxes
                    .SelectMany(box => box.Events)
                    .Any(evt => TryGetLabel(evt, out _)));

        // GetGroupCollection centralizes the authoritative outer color collection used by every setup and cleanup operation.
        private static GLSGroupColorGridContainer GetGroupCollection() =>
            BeatmapObjectContainerCollection.GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);

        // GetColorLanePair adds a test-only adjacent lane because the compact shared environment has one color lane per page.
        private static (int Primary, int Secondary, string Group) GetColorLanePair()
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(runtime, Is.Not.Null);
            const string fixturePage = "GLS ribbon mutation fixtures";
            if (!runtime.TrackDefinitions.Gls.ContainsKey(SyntheticPrimaryGroupId))
            {
                runtime.TrackDefinitions.Gls.Add(
                    SyntheticPrimaryGroupId,
                    new TrackDefinitionGLS
                    {
                        ID = SyntheticPrimaryGroupId,
                        Group = fixturePage,
                        Name = "GLS ribbon mutation source",
                        ColorTrack = true
                    });
            }

            if (!runtime.TrackDefinitions.Gls.ContainsKey(SyntheticSecondaryGroupId))
            {
                runtime.TrackDefinitions.Gls.Add(
                    SyntheticSecondaryGroupId,
                    new TrackDefinitionGLS
                    {
                        ID = SyntheticSecondaryGroupId,
                        Group = fixturePage,
                        Name = "GLS ribbon mutation destination",
                        ColorTrack = true
                    });
            }

            return (SyntheticPrimaryGroupId, SyntheticSecondaryGroupId, fixturePage);
        }

        // ConfigureOuterColorPage mirrors opening the page that contains both fixture lanes before paste or lane shift.
        private static void ConfigureOuterColorPage()
        {
            var pair = GetColorLanePair();
            var provider = UnityEngine.Object.FindAnyObjectByType<GLSGroupGridProvider>();
            Assert.That(provider, Is.Not.Null);
            provider.CurrentGroup = pair.Group;
        }

        // GetOuterColorPlacement rejects inactive templates wired to other collections.
        private static GLSGroupColorPlacement GetOuterColorPlacement(GLSGroupColorGridContainer collection)
        {
            var placement = UnityEngine.Object.FindObjectsByType<GLSGroupColorPlacement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(candidate => ReferenceEquals(candidate.ObjectContainerCollection, collection))
                .OrderByDescending(candidate => candidate.isActiveAndEnabled)
                .FirstOrDefault();
            Assert.That(placement, Is.Not.Null);
            return placement;
        }

        // Serialization can round-trip color channels with minor float differences, so labels use a tight component tolerance.
        private static char GetLabel(BaseLightColorBase evt)
        {
            if (TryGetLabel(evt, out var label))
            {
                return label;
            }

            Assert.Fail($"Unrecognized GLS mutation-test color {evt.CustomColor}.");
            return '?';
        }

        // TryGetLabel provides a non-asserting predicate for locating fixture-owned groups after production replacement actions.
        private static bool TryGetLabel(BaseLightColorBase evt, out char label)
        {
            if (ColorsApproximatelyEqual(evt.CustomColor, colorA))
            {
                label = 'A';
                return true;
            }

            if (ColorsApproximatelyEqual(evt.CustomColor, colorB))
            {
                label = 'B';
                return true;
            }

            if (ColorsApproximatelyEqual(evt.CustomColor, colorC))
            {
                label = 'C';
                return true;
            }

            label = default;
            return false;
        }

        // ColorsApproximatelyEqual is strict enough that authored shared-map colors cannot accidentally become fixture ownership.
        private static bool ColorsApproximatelyEqual(Color? actual, Color expected) =>
            actual.HasValue
            && Mathf.Abs(actual.Value.r - expected.r) < 0.0001f
            && Mathf.Abs(actual.Value.g - expected.g) < 0.0001f
            && Mathf.Abs(actual.Value.b - expected.b) < 0.0001f
            && Mathf.Abs(actual.Value.a - expected.a) < 0.0001f;

        // GetInitializedVisualSettings supplies the normal container teardown dependency to synthetic hover targets.
        private static VisualSettingsSO GetInitializedVisualSettings() => UnityEngine.Object
            .FindObjectsByType<ObjectContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(container => container.VisualSettings)
            .First(settings => settings != null);

        // SetHighlightedWithoutVisualRefresh preserves hover ownership without invoking renderer dependencies on data-only containers.
        private static void SetHighlightedWithoutVisualRefresh(ObjectContainer container)
        {
            var field = typeof(ObjectContainer).GetField("highlighted", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(container, true);
        }

        // MovedLabel chooses the original first node for forward edits and original last node for backward edits.
        private static char MovedLabel(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween or ChronologicalDestination.ForwardLast => 'A',
            ChronologicalDestination.BackwardBetween or ChronologicalDestination.BackwardFirst => 'C',
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        // DestinationTime expresses each requested drag/paste/shift endpoint independently from its operation mechanism.
        private static float DestinationTime(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween => 5f,
            ChronologicalDestination.ForwardLast => 7f,
            ChronologicalDestination.BackwardBetween => 3f,
            ChronologicalDestination.BackwardFirst => 1f,
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        // LaneDestinationTime uses the final order's existing beat slot because the moved node starts on another lane and cannot conflict there.
        private static float LaneDestinationTime(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween => 4f,
            ChronologicalDestination.ForwardLast => 6f,
            ChronologicalDestination.BackwardBetween => 4f,
            ChronologicalDestination.BackwardFirst => 2f,
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        // ExpectedOrder records the four exact chronology changes from the request.
        private static string ExpectedOrder(ChronologicalDestination destination) => destination switch
        {
            ChronologicalDestination.ForwardBetween => "BAC",
            ChronologicalDestination.ForwardLast => "BCA",
            ChronologicalDestination.BackwardBetween => "ACB",
            ChronologicalDestination.BackwardFirst => "CAB",
            _ => throw new ArgumentOutOfRangeException(nameof(destination), destination, null)
        };

        // NUnit exposes these values through public test signatures, so their nested types must be equally accessible.
        public enum RibbonView
        {
            Inner,
            Outer
        }

        // Movement case arguments remain public so NUnit can construct every generated structural row.
        public enum MovementOperation
        {
            AltDrag,
            TimeShift,
            CutPaste,
            LaneShift
        }

        // Destination case arguments remain public so NUnit can construct every generated chronology row.
        public enum ChronologicalDestination
        {
            ForwardBetween,
            ForwardLast,
            BackwardBetween,
            BackwardFirst
        }

        // Hover case arguments remain public so NUnit can construct every generated physical-input row.
        public enum HoverMutation
        {
            Brightness,
            StrobeFrequency,
            StrobeBrightness,
            StrobeFade,
            Easing
        }

        // EasingTriple keeps the full 3x3x3 node permutation available as one loop value and useful failure label.
        private readonly struct EasingTriple
        {
            public EasingTriple(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }

            public int A { get; }
            public int B { get; }
            public int C { get; }
        }

        // MutationScenario retains only clone-stable identity and intended destination data; every object lookup remains authoritative.
        private sealed class MutationScenario
        {
            public MutationScenario(RibbonView view, char movedLabel, int groupId)
            {
                View = view;
                MovedLabel = movedLabel;
                GroupId = groupId;
            }

            public RibbonView View { get; }
            public char MovedLabel { get; }
            public int GroupId { get; }
            public float DestinationTime { get; set; }
            public int LaneDirection { get; set; }

            public override string ToString() => $"{View} moved={MovedLabel} group={GroupId}";
        }

        // TestGLSEventColorInputController keeps production callback logic while replacing only physical raycast acquisition.
        private sealed class TestGLSEventColorInputController : BeatmapGLSEventColorInputController
        {
            public GLSEventContainer RaycastTarget;

            public void SetPrecision(ScrollPrecisionController precision) => ScrollPrecisionController = precision;

            protected override bool RaycastFirstObject(out GLSEventContainer firstObject)
            {
                firstObject = RaycastTarget;
                return firstObject != null;
            }
        }

        // TestGLSGroupColorInputController targets a real preview event while retaining the outer controller's mutation path.
        private sealed class TestGLSGroupColorInputController : BeatmapGLSGroupColorInputController
        {
            public GLSGroupContainer RaycastTarget;

            protected override bool RaycastFirstObject(out GLSGroupContainer firstObject)
            {
                firstObject = RaycastTarget;
                return firstObject != null;
            }
        }
    }
}
