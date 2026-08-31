using UnityEngine;

// Serialized ring-zoom settings consumed by TrackLaneRingsPositionEffect.
// Snapshot evaluation owns movement; the former duplicate Apply path is no longer used.
public class TrackLaneRingsPositionSpawner : MonoBehaviour
{
    public TrackLaneRingsManager RingManager;
    // Environment scenes host the evaluator separately and serialize its binding here.
    public TrackLaneRingsPositionEffect EffectManager;
    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;

    private void Awake()
    {
        // Existing environment scenes serialize the zoom effect on this spawner; removing the
        // reference left the effect's Visual null, so every snapshot returned without moving rings.
        if (EffectManager != null)
            EffectManager.Visual = this;
    }
}
