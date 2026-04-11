using System.Linq;
using UnityEngine;

public class
    BloomPrePassBackgroundNonLightInstancedGroupRendererData : EnvironmentComponentData<
    BloomPrePassBackgroundNonLightInstancedGroupRenderer>
{
    public int ExecutionTimeType;

    public string[] Renderers;
    public SupportedPropertyComponent[] SupportedProperties;

    public class SupportedPropertyComponent
    {
        public int PropertyType;
        public string PropertyName;
    }

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundNonLightInstancedGroupRenderer comp,
        CreateContainer container)
    {
        comp.Renderers =
            Renderers
                .Select(x =>
                    container.GetGameObjectOrNull(x, self).GetComponent<BloomPrePassBackgroundNonLightRenderer>())
                .ToArray();
    }

    public override void CopyTo(BloomPrePassBackgroundNonLightInstancedGroupRenderer comp)
    {
        comp.ExecutionTimeType = (BloomPrePassNonLightPass.ExecutionTime)ExecutionTimeType;
        comp.SupportedProperties = SupportedProperties
            .Select(x => new BloomPrePassBackgroundNonLightInstancedGroupRenderer.SupportedProperty
            {
                PropertyType = (BloomPrePassBackgroundNonLightInstancedGroupRenderer.PropertyType)x.PropertyType,
                PropertyName = x.PropertyName
            })
            .ToArray();
    }
}
