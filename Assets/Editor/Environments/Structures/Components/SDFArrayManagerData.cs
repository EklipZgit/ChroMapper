using System.Linq;
using UnityEngine;

public class SDFArrayManagerData : EnvironmentComponentData<SDFArrayManager>
{
    public int[] SDFPointArray;

    public override void FillComponents(GameObject self, SDFArrayManager comp, CreateContainer container) =>
        comp.SDFPointArray = SDFPointArray.Select(container.GetComponentOrNull<SDFPoint>).ToArray();
}
