using UnityEngine;

/// <summary>
///     Code taken from Beat Saber, which provides deltaTime, fixedDeltaTime, and interpolation.
/// </summary>
public class TimeHelper : MonoBehaviour
{
    // A 90 Hz headset cadence gives preview callbacks one stable clock across editor frame rates.
    private const float PreviewCallbackRate = 90f;
    // Keeping the integer render index authoritative prevents repeated float additions from
    // drifting callback and rendered-state boundaries apart on long maps.
    private const float PreviewBoundaryTolerance = 0.00001f;
    private float accumulator;
    public static float DeltaTime { get; private set; }
    public static float FixedDeltaTime { get; private set; }
    public static float InterpolationFactor { get; private set; }

    private void Awake()
    {
        FixedDeltaTime = Time.fixedDeltaTime;
        accumulator += FixedDeltaTime;
    }

    private void Update()
    {
        DeltaTime = Time.deltaTime;
        accumulator += DeltaTime;
        InterpolationFactor = accumulator / FixedDeltaTime;
    }

    private void FixedUpdate()
    {
        FixedDeltaTime = Time.fixedDeltaTime;
        accumulator -= FixedDeltaTime;
    }

    // Beat Saber dispatches zero-ahead beatmap callbacks on the first render LateUpdate
    // whose song clock has reached the event; 90 Hz is the editor's deterministic cadence.
    public static int GetPreviewRenderIndex(float songSeconds) =>
        Mathf.CeilToInt((songSeconds * PreviewCallbackRate) - PreviewBoundaryTolerance);

    // Derive seconds from the same integer index used by rendering so the two preview
    // clocks cannot independently round opposite ways at an exact 90 Hz boundary.
    public static float GetPreviewCallbackSeconds(float songSeconds) =>
        GetPreviewRenderIndex(songSeconds) / PreviewCallbackRate;

}
