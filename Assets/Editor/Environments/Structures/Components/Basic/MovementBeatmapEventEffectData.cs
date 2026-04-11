using System.Linq;
using UnityEngine;

public class MovementBeatmapEventEffectData : EnvironmentComponentData<Movement>
{
    public string EventType;
    public float TransitionSpeed;
    public MovementDataComponent[] MovementData;
    public string[] Transforms;

    public class MovementDataComponent
    {
        public Vector3 LocalPositionOffset;
    }

    public override void SearchAndFillComponents(GameObject self, Movement comp, CreateContainer container)
    {
        comp.Transforms = Transforms
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.transform : null)
            .Where(y => y != null)
            .ToArray();
        foreach (var t in comp.Transforms) t.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
    }

    public override void CopyTo(Movement comp)
    {
        comp.TransitionSpeed = TransitionSpeed;
        comp.MovementData = MovementData.Select(x => x.LocalPositionOffset).ToArray();
    }
}
