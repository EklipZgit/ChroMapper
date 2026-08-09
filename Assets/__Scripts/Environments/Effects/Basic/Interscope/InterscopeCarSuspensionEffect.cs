using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class InterscopeCarSuspensionEffect : InterscopeCarEventEffect
{
    [SerializeField] private float contractDistance = 0.35f;
    [SerializeField] private float expandDistance = 0.45f;

    private SpringJoint frontWheelSpringJoint;

    public override int[] ListeningEventTypes => new[]
    {
        (int)EventTypeValue.Event16, (int)EventTypeValue.Event17
    };

    protected override void Start()
    {
        base.Start();

        // ok so like this is a pretty jank way to do it but im lazy
        frontWheelSpringJoint = GetComponentsInChildren<SpringJoint>()
            .Where(x => x.connectedBody.name.Contains("FrontWheel"))
            .FirstOrDefault();
    }

    protected override void OnCarGroupTriggered(BaseEvent @event)
    {
        if (@event.Type == (int)EventTypeValue.Event16)
        {
            frontWheelSpringJoint.minDistance = frontWheelSpringJoint.maxDistance = expandDistance;
            CarRigidbody.WakeUp();
        }
        else
        {
            frontWheelSpringJoint.minDistance = frontWheelSpringJoint.maxDistance = contractDistance;
            CarRigidbody.WakeUp();
        }
    }
}
