using System.Linq;
using UnityEngine;

public class MeshRendererSwitchEventEffectData : EnvironmentComponentData<MeshRendererSwitch>
{
    public string EventType;
    public string[] ActivateOnBoostRenderers;
    public string[] DeactivateOnBoostRenderers;

    public override void SearchAndFillComponents(GameObject self, MeshRendererSwitch comp, CreateContainer container)
    {
        comp.NormalRenderers = DeactivateOnBoostRenderers
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.GetComponent<Renderer>() : null)
            .Where(y => y != null)
            .Select(g =>
            {
                g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToArray();
        comp.BoostRenderers = ActivateOnBoostRenderers
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.GetComponent<Renderer>() : null)
            .Where(y => y != null)
            .Select(g =>
            {
                g.gameObject.GetComponent<ChromaIDMarker>().MarkUse = true;
                return g;
            })
            .ToArray();
    }

    public override void CopyTo(MeshRendererSwitch comp) { }
}
