using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

namespace Tests.Util
{
    public class PlaceUtils
    {
        public static void PlaceNote(
            NotePlacement notePlacement, BaseNote note)
        {
            notePlacement.QueuedData = note;
            notePlacement.RoundedJsonTime = notePlacement.QueuedData.JsonTime;
            notePlacement.HandleApply();
        }

        public static void PlaceWall(
            ObstaclePlacement obstaclePlacement, BaseObstacle obstacle)
        {
            obstaclePlacement.QueuedData = obstacle;
            obstaclePlacement.RoundedJsonTime = obstaclePlacement.QueuedData.JsonTime;
            obstaclePlacement.PlacementVisualContainer.SetScale(new Vector3(0, 0,
                obstaclePlacement.QueuedData.Duration * EditorScaleController.EditorScale));
            obstaclePlacement.HandleApply(); // Starts placement
            obstaclePlacement.HandleApply(); // Completes placement
        }

        public static void PlaceEvents(EventPlacement eventPlacement, IEnumerable<BaseEvent> events, bool precRotation = false)
        {
            foreach (var evt in events)
            {
                PlaceEvent(eventPlacement, evt, precRotation);
            }
        }

        public static void PlaceEvent(
            EventPlacement eventPlacement, BaseEvent evt, bool precRotation = false)
        {
            eventPlacement.QueuedData = evt;
            eventPlacement.queuedValue = eventPlacement.QueuedData.Value;
            eventPlacement.queuedFloatValue = eventPlacement.QueuedData.FloatValue;
            eventPlacement.RoundedJsonTime = eventPlacement.QueuedData.JsonTime;

            eventPlacement.HandleApply();
        }

        public static void PlaceRotationEvent(
            RotationEventPlacement rotationEventPlacement, BaseRotationEvent evt, bool precRotation = false)
        {
            rotationEventPlacement.QueuedData = evt;
            rotationEventPlacement.QueuedRotation = rotationEventPlacement.QueuedData.Rotation;
            rotationEventPlacement.RoundedJsonTime = rotationEventPlacement.QueuedData.JsonTime;

            rotationEventPlacement.HandleApply();
        }

        public static void PlaceNJSEvent(
            NJSEventPlacement njsEventPlacement, BaseNJSEvent evt)
        {
            njsEventPlacement.QueuedData = evt;
            njsEventPlacement.HandleApplyNoDialogue();
        }

        public static void PlaceArc(
            ArcPlacement arcPlacement, BaseArc arc)
        {
            arcPlacement.QueuedData = arc;
            arcPlacement.RoundedJsonTime = arcPlacement.QueuedData.JsonTime;
            arcPlacement.HandleApply();
        }

        public static void PlaceChain(
            ChainPlacement chainPlacement, BaseChain chain)
        {
            chainPlacement.QueuedData = chain;
            chainPlacement.RoundedJsonTime = chainPlacement.QueuedData.JsonTime;
            chainPlacement.HandleApply();
        }
    }
}
