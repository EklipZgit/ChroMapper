using UnityEngine;

public class HydraulicCarJump : MonoBehaviour
{
    public int EventType = 8;
    public int[] EventValues;
    public Vector3 Impulse;
    public float Randomness = 0.1f;
    public Vector3 Position;
    public float MinDelayBetweenEvents = 0.5f;
    public Rigidbody Rigidbody;
}
