using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    public class PlaceUtils
    {
        private static readonly Dictionary<Type, BasePlacement> placementCache = new();
        private static BeatmapActionContainer actionContainerCache;

        public static TObject Place<TObject>(TObject objectData) where TObject : BaseObject
        {
            var placedObject = CloneForPlacement(objectData);

            switch (placedObject)
            {
                case BaseNote note:
                    PlaceNote(note);
                    break;
                case BaseObstacle obstacle:
                    PlaceWall(obstacle);
                    break;
                case BaseRotationEvent rotationEvent:
                    PlaceRotationEvent(rotationEvent);
                    break;
                case BaseNJSEvent njsEvent:
                    PlaceNJSEvent(njsEvent);
                    break;
                case BaseBpmEvent bpmEvent:
                    PlaceBpmEvent(bpmEvent);
                    break;
                case BaseEvent evt:
                    PlaceEvent(evt);
                    break;
                case BaseArc arc:
                    PlaceArc(arc);
                    break;
                case BaseChain chain:
                    PlaceChain(chain);
                    break;
                default:
                    throw new ArgumentException($"Unsupported placement object type: {objectData.GetType().Name}");
            }

            return placedObject;
        }

        public static IEnumerable<TObject> Place<TObject>(IEnumerable<TObject> objects) where TObject : BaseObject
        {
            return objects.Select(Place).ToList();
        }

        public static void Delete<TObject>(
            TObject objectData,
            bool triggersAction = true,
            bool refreshesPool = true,
            string comment = "No comment.",
            bool inCollectionOfDeletes = false,
            bool deselect = true)
            where TObject : BaseObject
        {
            var collection = BeatmapObjectContainerCollection
                .GetCollectionForType<BeatmapObjectContainerCollection<TObject>,
                    TObject>();
            if (collection == null)
                throw new InvalidOperationException($"Could not find collection for {typeof(TObject).Name}.");

            collection.DeleteObject(
                objectData,
                triggersAction,
                refreshesPool,
                comment,
                inCollectionOfDeletes,
                deselect);
        }

        public static void Delete<TObject>(IEnumerable<TObject> objects)
            where TObject : BaseObject
        {
            foreach (var objectData in objects) Delete(objectData);
        }

        public static IEnumerable<BaseObject> Undo()
        {
            return GetActionObjects(GetActionContainer().Undo(), false);
        }

        public static IEnumerable<TObject> Undo<TObject>()
            where TObject : BaseObject
        {
            return Undo().OfType<TObject>();
        }

        public static IEnumerable<BaseObject> Redo()
        {
            return GetActionObjects(GetActionContainer().Redo(), true);
        }

        public static IEnumerable<TObject> Redo<TObject>()
            where TObject : BaseObject
        {
            return Redo().OfType<TObject>();
        }

        private static IEnumerable<BaseObject> GetActionObjects(BeatmapAction action, bool redo)
        {
            return action switch
            {
                null => Enumerable.Empty<BaseObject>(),
                ActionCollectionAction collectionAction => collectionAction.Actions.SelectMany(childAction =>
                    GetActionObjects(childAction, redo)),
                BeatmapObjectModifiedCollectionAction modifiedCollectionAction => redo
                    ? modifiedCollectionAction.EditedObjects
                    : modifiedCollectionAction.OriginalObjects,
                BeatmapObjectUpdatedAction updatedAction => new[]
                {
                    redo ? updatedAction.EditedObject : updatedAction.OriginalObject
                },
                BeatmapObjectModifiedWithConflictingAction modifiedWithConflictingAction => GetModifiedActionObject(
                        modifiedWithConflictingAction,
                        redo)
                    .Concat(
                        redo
                            ? Enumerable.Empty<BaseObject>()
                            : NonNullObjects(modifiedWithConflictingAction.ConflictingObjects)),
                BeatmapObjectModifiedAction modifiedAction => GetModifiedActionObject(modifiedAction, redo),
                BeatmapObjectPlacementAction placementAction => redo
                    ? placementAction.Data
                    : NonNullObjects(placementAction.RemovedConflictObjects),
                BeatmapObjectDeletionAction deletionAction => redo
                    ? Enumerable.Empty<BaseObject>()
                    : deletionAction.Data,
                StrobeGeneratorGenerationAction strobeAction => redo
                    ? strobeAction.Data
                    : NonNullObjects(strobeAction.ConflictingData),
                _ => action.Data ?? Enumerable.Empty<BaseObject>()
            };
        }

        private static IEnumerable<BaseObject> GetModifiedActionObject(BeatmapObjectModifiedAction action, bool redo)
        {
            return new[] { redo ? action.EditedObject : action.OriginalObject };
        }

        private static IEnumerable<BaseObject> NonNullObjects(IEnumerable<BaseObject> objects)
        {
            return objects?.Where(obj => obj != null) ?? Enumerable.Empty<BaseObject>();
        }

        private static TObject CloneForPlacement<TObject>(TObject objectData) where TObject : BaseObject
        {
            var originalCustomData = objectData.CustomData.Clone();
            objectData.WriteCustom();
            var placedObject = BeatmapFactory.Clone(objectData);
            objectData.SetCustomData(originalCustomData);

            return placedObject;
        }

        private static void PlaceNote(BaseNote note)
        {
            var notePlacement = GetPlacement<NotePlacement>();
            notePlacement.QueuedData = note;
            notePlacement.RoundedJsonTime = notePlacement.QueuedData.JsonTime;
            notePlacement.HandleApply();
        }

        private static void PlaceWall(BaseObstacle obstacle)
        {
            var obstaclePlacement = GetPlacement<ObstaclePlacement>();
            obstaclePlacement.QueuedData = obstacle;
            obstaclePlacement.RoundedJsonTime = obstaclePlacement.QueuedData.JsonTime;
            obstaclePlacement.PlacementVisualContainer.SetScale(
                new Vector3(
                    0,
                    0,
                    obstaclePlacement.QueuedData.Duration * EditorScaleController.EditorScale));
            obstaclePlacement.HandleApply(); // Starts placement
            obstaclePlacement.HandleApply(); // Completes placement
        }

        private static void PlaceEvent(BaseEvent evt)
        {
            var eventPlacement = GetPlacement<EventPlacement>();
            eventPlacement.QueuedData = evt;
            eventPlacement.queuedValue = eventPlacement.QueuedData.Value;
            eventPlacement.queuedFloatValue = eventPlacement.QueuedData.FloatValue;
            eventPlacement.RoundedJsonTime = eventPlacement.QueuedData.JsonTime;

            eventPlacement.HandleApply();
        }

        private static void PlaceRotationEvent(BaseRotationEvent evt)
        {
            var rotationEventPlacement = GetPlacement<RotationEventPlacement>();
            rotationEventPlacement.QueuedData = evt;
            rotationEventPlacement.QueuedRotation = rotationEventPlacement.QueuedData.Rotation;
            rotationEventPlacement.RoundedJsonTime = rotationEventPlacement.QueuedData.JsonTime;

            rotationEventPlacement.HandleApply();
        }

        private static void PlaceNJSEvent(BaseNJSEvent evt)
        {
            var njsEventPlacement = GetPlacement<NJSEventPlacement>();
            njsEventPlacement.QueuedData = evt;
            njsEventPlacement.HandleApplyNoDialogue();
        }

        private static void PlaceBpmEvent(BaseBpmEvent evt)
        {
            var bpmChangePlacement = GetPlacement<BPMChangePlacement>();
            bpmChangePlacement.QueuedData = evt;
            bpmChangePlacement.RoundedJsonTime = bpmChangePlacement.QueuedData.JsonTime;
            bpmChangePlacement.ObjectContainerCollection.SpawnObject(evt, out var conflicting);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(evt, conflicting, $"Placed a BPM Event at time {evt.JsonTime}"));
        }

        private static void PlaceArc(BaseArc arc)
        {
            var arcPlacement = GetPlacement<ArcPlacement>();
            arcPlacement.QueuedData = arc;
            arcPlacement.RoundedJsonTime = arcPlacement.QueuedData.JsonTime;
            arcPlacement.HandleApply();
        }

        private static void PlaceChain(BaseChain chain)
        {
            var chainPlacement = GetPlacement<ChainPlacement>();
            chainPlacement.QueuedData = chain;
            chainPlacement.RoundedJsonTime = chainPlacement.QueuedData.JsonTime;
            chainPlacement.HandleApply();
        }

        private static TPlacement GetPlacement<TPlacement>() where TPlacement : BasePlacement
        {
            if (!placementCache.TryGetValue(typeof(TPlacement), out var placement) || !placement)
            {
                placement = Object.FindAnyObjectByType<TPlacement>();
                if (!placement) throw new InvalidOperationException($"Could not find {typeof(TPlacement).Name}.");

                placementCache[typeof(TPlacement)] = placement;
            }

            return (TPlacement)placement;
        }

        private static BeatmapActionContainer GetActionContainer()
        {
            if (actionContainerCache) return actionContainerCache;
            actionContainerCache = Object.FindFirstObjectByType<BeatmapActionContainer>();
            return !actionContainerCache
                ? throw new InvalidOperationException("Could not find BeatmapActionContainer.")
                : actionContainerCache;
        }
    }
}