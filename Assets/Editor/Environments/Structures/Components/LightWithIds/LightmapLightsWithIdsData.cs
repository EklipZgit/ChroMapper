using System;
using System.Linq;
using UnityEngine;

public class LightmapLightsWithIdsData : EnvironmentComponentData<LightmapsLightsController>
{
    public float MaxTotalIntensity = 1f;
    public LightIntensitiesWithIdData[] LightIntensityData;

    public override void SearchAndFillComponents(
        GameObject self,
        LightmapsLightsController comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(LightmapsLightsController comp)
    {
        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var d = comp.gameObject.AddComponent<LightmapsIntensityData>();
                d.Intensity = data.Intensity;
                d.BakeId = Enum.Parse<LightConstants.BakeId>(data.BakeId);
                d.Weight = data.Weight;
                return d;
            })
            .ToArray();

        comp.MaxTotalIntensity = MaxTotalIntensity;
    }

    public class LightIntensitiesWithIdData
    {
        public float Intensity;
        public string BakeId;
        public float Weight;
    }
}
