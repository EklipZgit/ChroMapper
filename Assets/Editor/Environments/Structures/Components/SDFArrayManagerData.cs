using System.Linq;
using UnityEngine;

public class SDFArrayManagerData : EnvironmentComponentData<SDFArrayManager>
{
    public string[] SDFPointArray;

    public override void SearchAndFillComponents(GameObject self, SDFArrayManager comp, CreateContainer container)
    {
        comp.SDFPointArray =
            SDFPointArray
                .Select(o => container.GetGameObjectOrNull(o, self).GetComponent<SDFPoint>())
                .ToArray();
    }

    public override void CopyTo(SDFArrayManager comp) { }
}
