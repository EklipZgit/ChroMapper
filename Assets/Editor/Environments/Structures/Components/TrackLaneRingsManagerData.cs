using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackLaneRingsManagerData : EnvironmentComponentData<TrackLaneRingsManager>
{
    public string[] Rings;
    public float RingPositionZStep;
    public bool SpawnAsChildren;

    public override void SearchAndFillComponents(GameObject self, TrackLaneRingsManager comp, CreateContainer container)
    {
        if (Rings is null)
            comp.Rings = new List<TrackLaneRing>();
        else
        {
            comp.Rings = Rings
                .Select((r, i) =>
                {
                    var tlr = container.ChromaIdObjects[r].AddComponent<TrackLaneRing>();
                    tlr.ParentManager = comp;
                    return tlr;
                })
                .ToList();
        }
    }

    public override void CopyTo(TrackLaneRingsManager comp)
    {
        comp.RingPositionStep = RingPositionZStep;
        comp.SpawnAsChildren = SpawnAsChildren;
    }
}
