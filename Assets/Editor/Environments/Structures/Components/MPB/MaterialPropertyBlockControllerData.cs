using System.Linq;
using UnityEngine;

public class MaterialPropertyBlockControllerData : EnvironmentComponentData<MaterialPropertyBlockController>
{
    public string[] Renderers;

    public override void FillComponents(
        GameObject self,
        MaterialPropertyBlockController comp,
        CreateContainer container)
    {
        comp.Renderers = Renderers
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.GetComponent<Renderer>() : null)
            .Where(y => y != null)
            .Select(g =>
            {
                g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToList();
    }
}
