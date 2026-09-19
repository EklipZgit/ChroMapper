using System;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    // SongBoundaryTest and GLSSongBoundaryTest share scene-backed input and drag setup so lane coverage cannot diverge.
    public abstract class SongBoundaryTestBase : TestBase
    {
        protected SelectionController Selection;
        protected AudioTimeSyncController Atsc;
        private BasePlacement[] placements;
        private int originalSnapping;
        private readonly System.Collections.Generic.HashSet<ObjectType> touchedObjectTypes = new();

        // Song limits must use the map's BPM conversion rather than assuming the clip length is measured in JSON beats.
        protected float FinalBeat => (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(
            Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

        // SongBoundaryTest establishes the shared map once because each case records and removes every object collection it authors.
        [OneTimeSetUp]
        public void SetUpBoundaryFixtureMap() => TestUtils.ResetSharedMapState();

        // SongBoundaryTest avoids the full metadata/map reset and validation pass for each of its hundreds of parameter cases.
        protected override void ResetSharedMapState()
        {
        }

        // A retained hover or clipboard can silently choose a different paste branch, so each boundary case starts idle.
        [SetUp]
        public void SetUpSongBoundaries()
        {
            // SongBoundaryTest performance coverage starts an empty ownership set instead of sweeping unrelated collections after the case.
            touchedObjectTypes.Clear();
            Selection = Object.FindAnyObjectByType<SelectionController>();
            Atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            originalSnapping = Atsc.GridMeasureSnapping;
            Atsc.GridMeasureSnapping = 1;
            placements = Object.FindObjectsByType<BasePlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var placement in placements)
                placement.State = PlacementState.Idle;
            SelectionController.CopiedObjects.Clear();
            Assert.That(FinalBeat, Is.GreaterThan(10f), "The shared clip must have room for isolated source and boundary ranges.");
        }

        // Failed assertions must not leak an active hover or clipboard into another lane's regression case.
        protected override void BeforeCleanup()
        {
            foreach (var placement in placements)
                placement.State = PlacementState.Idle;
            SelectionController.CopiedObjects.Clear();
            Atsc.GridMeasureSnapping = originalSnapping;
            base.BeforeCleanup();
        }

        // ArcTest.UpdateArcMultiplier needs a live beat-two container after this fixture. Synchronous test setup resets
        // the audio cursor but cannot run LateUpdate, so restore the derived pool cursor along with our song-end cursor.
        protected override void AfterCleanup()
        {
            Atsc.MoveToJsonTime(0f);
            foreach (var collection in Object.FindObjectsByType<BeatmapObjectContainerCollection>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetField(collection, "previousAtscBeat", 0f);
                SetField(collection, "previousChunk", 0);
            }
            base.AfterCleanup();
        }

        // Selection and paste filter by editor mode, so exercise the lane's real mode rather than bypassing that filter.
        protected void SetMode(EditingMode mode) =>
            Object.FindAnyObjectByType<EditModeContext>().EditingMode = mode;

        // Use authoritative collections even for objects outside the visual window; boundary edits must not depend on pooling.
        protected T Spawn<T>(T obj) where T : BaseObject
        {
            // SongBoundaryTest performance coverage records parent and child collections once at fixture insertion time.
            touchedObjectTypes.Add(obj.ObjectType);
            if (obj is BaseEventBoxGroup)
            {
                touchedObjectTypes.Add(ObjectType.GLSEvent);
            }
            obj.SetMap(BeatSaberSongContainer.Instance.Map);
            obj.RecomputeSongBpmTime();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);
            Assert.That(collection, Is.Not.Null);
            collection.SpawnObject(obj, false, false, true);
            // InRangeShiftAndPastePreserveTiming exposed that raw insertion skips preview-state registration; publish the normal placement action.
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(obj, Array.Empty<BaseObject>(), "Placed song boundary fixture."));
            return obj;
        }

        // SongBoundaryTest performance coverage cleans only objects this case authored; TestBase retains full cleanup elsewhere.
        protected override void CleanupTestObjects() => CleanupUtils.CleanupObjects(touchedObjectTypes);

        // Shift boundary tests must traverse the authored modifier composite, not only call MoveSelection directly.
        protected void ShiftWithKeyboard(bool forward) =>
            SendShortcut(Key.LeftShift, forward ? Key.UpArrow : Key.DownArrow);

        // Paste boundary tests retain the production Copy/Paste callbacks while avoiding host keyboard focus.
        protected void CopyWithKeyboard() => SendShortcut(Key.LeftCtrl, Key.C);
        protected void PasteWithKeyboard() => SendShortcut(Key.LeftCtrl, Key.V);

        // Isolate same-frame modifier processing so neither native devices nor shared callbacks can execute a second edit.
        private void SendShortcut(Key modifier, Key key)
        {
            var shared = CMInputCallbackInstaller.InputInstance;
            var enabledMaps = shared.asset.actionMaps.Where(map => map.enabled).ToArray();
            shared.Disable();
            var fixture = new InputTestFixture();
            fixture.Setup();
            var input = new CMInput();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                input.ModifyingSelection.SetCallbacks(Selection);
                input.Selecting.SetCallbacks(Selection);
                input.ModifyingSelection.Enable();
                input.Selecting.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(modifier));
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(modifier, key));
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
            }
            finally
            {
                input.Dispose();
                fixture.TearDown();
                Selection.OnActivateShiftinTime(default);
                Selection.OnActivateShiftinPlace(default);
                foreach (var map in enabledMaps)
                    map.Enable();
            }
        }

        // Scene prefabs can contain inactive siblings; prefer the initialized placement used by the live editor.
        protected BasePlacement FindPlacement(Type type)
        {
            var placement = placements.Where(candidate => candidate.GetType() == type)
                .OrderByDescending(candidate => candidate.isActiveAndEnabled).FirstOrDefault();
            Assert.That(placement, Is.Not.Null, $"Missing {type.Name} in the test scene.");
            return placement;
        }

        // Exercise StartDrag/UpdateState/FinishDrag with real prefab containers without relying on screen size or mouse focus.
        protected void DragToBeat(BasePlacement placement, BaseObject obj, float targetBeat,
            IndicatorType indicatorType = IndicatorType.Head)
        {
            Assert.That(placement.CanClickAndDrag, Is.True);
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType);
            var container = collection.CreateContainer();
            var surface = new GameObject("Song boundary drag surface");
            var hitObject = new GameObject("Song boundary drag hit");
            hitObject.transform.SetParent(surface.transform);
            var oldBounds = placement.Bounds;
            try
            {
                container.ObjectData = obj;
                container.Setup();
                container.UpdateGridPosition();
                ObjectContainer target = container;
                if (placement is ArcIndicatorPlacement)
                {
                    target = container.GetComponentsInChildren<ArcIndicatorContainer>(true)
                        .Single(indicator => indicator.IndicatorType == indicatorType);
                }
                else if (placement is ChainIndicatorPlacement)
                {
                    // The chain prefab has both link and sphere tail handles for the same endpoint; either exercises its tail transfer.
                    target = container.GetComponentsInChildren<ChainIndicatorContainer>(true)
                        .First(indicator => indicator.IndicatorType == indicatorType);
                }
                if (obj is not BaseEventBoxGroup)
                    placement.Initialize(null);
                Assert.That(placement.StartDrag(target.gameObject), Is.Not.Null);
                placement.Bounds = new Bounds(new Vector3(32f, 2f, 0f), new Vector3(64f, 4f, 1000f));
                var localPoint = placement.PlacementTrack.InverseTransformPoint(target.transform.position);
                if (obj is BaseGLSEvent)
                    localPoint.x = 0.5f;
                if (obj is BaseEvent evt)
                {
                    var labels = Object.FindAnyObjectByType<CreateEventTypeLabels>();
                    var lane = labels.EventToLaneId(evt);
                    Assert.That(lane, Is.GreaterThanOrEqualTo(0), "Basic event test lane must be visible.");
                    localPoint.x = lane + 0.5f;
                }
                var songBeat = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(targetBeat);
                localPoint.z = songBeat * EditorScaleController.EditorScale;
                if (placement.AdjustZScale)
                    localPoint.z = (localPoint.z * BeatmapConstant.LaneSize) + BeatmapConstant.ZOffset;
                var worldPoint = placement.PlacementTrack.TransformPoint(localPoint);
                placement.UpdateState(new Intersections.IntersectionHit(hitObject,
                    new Bounds(Vector3.zero, Vector3.one), new Ray(worldPoint, Vector3.forward), 0f),
                    PlacementInputState.Drag);
                placement.FinishDrag();
                Assert.That(placement.IsDragging, Is.False, "Releasing a boundary drag must retire its state.");
            }
            finally
            {
                if (placement.IsDragging)
                    placement.FinishDrag();
                placement.Bounds = oldBounds;
                Object.DestroyImmediate(container.gameObject);
                Object.DestroyImmediate(surface);
            }
        }

        // Tests configure the exact serialized paste placement; inherited generic fields must resolve without a production test seam.
        protected static T GetField<T>(object target, string name) => (T)FindField(target, name).GetValue(target);
        protected static void SetField(object target, string name, object value) => FindField(target, name).SetValue(target, value);

        // A missing fixture field should fail as setup, not masquerade as a song-boundary assertion.
        private static FieldInfo FindField(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            throw new AssertionException($"Missing field {target.GetType().Name}.{name}");
        }
    }
}
