using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class RectangleFakeGlowLightWithIdData : EnvironmentComponentData<RectangleFakeGlowLightController>
{
    [JsonProperty("lightId")] public int Id;

    public float MinAlpha;
    public float AlphaMultiplier = 1f;

    public override void FillComponents(
        GameObject self,
        RectangleFakeGlowLightController comp,
        CreateContainer container)
    {
        comp.MpbController = self.GetComponent<MaterialPropertyBlockController>();
        var envObject =
            container.Data.Objects.First(y =>
                y.ChromaID == container.ChromaIdObjects.First(x => x.Value == self).Key);
        envObject.Components.RectangleFakeGlow[0].FillComponents(self, comp, container);
        comp.MinAlpha = MinAlpha;
        comp.AlphaMultiplier = AlphaMultiplier;
    }
}
