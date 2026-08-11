using UnityEngine;

// Serialized ring-zoom settings consumed by TrackLaneRingsPositionEffect.
// Snapshot evaluation owns movement; the former duplicate Apply path is no longer used.
public class TrackLaneRingsPositionSpawner : MonoBehaviour
{
    public TrackLaneRingsManager RingManager;
    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;
}
