using UnityEngine;

public class HydraulicCarJumpEffectData : EnvironmentComponentData<HydraulicCarJump>
{
    public string Event;
    public int[] EventValues;
    public Vector3 Impulse;
    public float Randomness = 0.1f;
    public Vector3 Position;
    public float MinDelayBetweenEvents = 0.5f;
    public string Rigidbody;

    public override void SearchAndFillComponents(GameObject self, HydraulicCarJump comp, CreateContainer container) =>
        comp.Rigidbody = container.GetGameObjectOrNull(Rigidbody, self).GetComponent<Rigidbody>();

    public override void CopyTo(HydraulicCarJump comp)
    {
        comp.EventValues = EventValues;
        comp.Impulse = Impulse;
        comp.Randomness = Randomness;
        comp.Position = Position;
        comp.MinDelayBetweenEvents = MinDelayBetweenEvents;
    }
}
