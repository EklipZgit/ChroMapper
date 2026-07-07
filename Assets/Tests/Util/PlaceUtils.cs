using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

namespace Tests.Util
{
    public class PlaceUtils
    {
        private static readonly Dictionary<Type, BasePlacement> PlacementCache = new();

        public static TObject Place<TObject>(TObject objectData) where TObject : BaseObject
        {
            var placedObject = BeatmapFactory.Clone(objectData);

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

        public static List<TObject> Place<TObject>(IEnumerable<TObject> objects) where TObject : BaseObject =>
            objects.Select(Place).ToList();

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
            obstaclePlacement.PlacementVisualContainer.SetScale(new Vector3(0, 0,
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
            if (!PlacementCache.TryGetValue(typeof(TPlacement), out var placement) || !placement)
            {
                placement = UnityEngine.Object.FindAnyObjectByType<TPlacement>();
                if (!placement) throw new InvalidOperationException($"Could not find {typeof(TPlacement).Name}.");

                PlacementCache[typeof(TPlacement)] = placement;
            }

            return (TPlacement)placement;
        }
    }
}
