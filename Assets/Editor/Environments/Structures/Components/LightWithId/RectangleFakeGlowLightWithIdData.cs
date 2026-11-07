using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class RectangleFakeGlowLightWithIdData : EnvironmentComponentData<RectangleFakeGlowLightController>
{
    public override int Priority => 1;

    // The geometry and LightWithId records describe one CM controller, not two independently updating lights.
    public override void SpawnComponent(GameObject self, Component retainedComponent = null) =>
        base.SpawnComponent(self, self.GetComponent<RectangleFakeGlowLightController>());

    [JsonProperty("lightId")] public int Id;

    public float MinAlpha;
    public float AlphaMultiplier = 1f;

    public override void FillComponents(
        GameObject self,
        RectangleFakeGlowLightController comp,
        CreateContainer container)
    {
        comp.MpbController = self.GetComponent<MaterialPropertyBlockController>();
        if (comp.MpbController == null)
        {
            comp.MpbController = self.AddComponent<MaterialPropertyBlockController>();
            comp.MpbController.Add(self.GetComponentsInChildren<Renderer>());
        }

        var envObject =
            container.Data.Objects.First(y =>
                y.ChromaID == container.ChromaIdObjects.First(x => x.Value == self).Key);
        envObject.Components.RectangleFakeGlow[0].FillComponents(self, comp, container);
        comp.MinAlpha = MinAlpha;
        comp.AlphaMultiplier = AlphaMultiplier;
    }
}
