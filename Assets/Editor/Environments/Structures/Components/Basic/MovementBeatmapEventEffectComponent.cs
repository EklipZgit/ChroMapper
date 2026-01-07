using System.Linq;
using UnityEngine;

public class MovementBeatmapEventEffectComponent
{
    public bool IsEnabled;

    public string EventType;
    public float TransitionSpeed;
    public MovementDataComponent[] MovementData;
    public string[] Transforms;

    public class MovementDataComponent
    {
        public Vector3 LocalPositionOffset;
    }

    public void CopyTo(Movement target)
    {
        target.TransitionSpeed = TransitionSpeed;
        target.MovementData = MovementData.Select(x => x.LocalPositionOffset).ToArray();
    }
}
