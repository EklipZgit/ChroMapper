using System.Linq;
using UnityEngine;

public class ColorArrayLightWithIdsData : EnvironmentComponentData<ColorArrayLightsController>
{
    public ColorArrayLightWithIdData[] ColorArrayLightWithIds;
    public MaterialControllerData MaterialController;
    public string[] MaterialPropertyBlockControllers;
    public string ColorsArrayPropertyName = "_ColorsArray";
    public string ColorsArrayOffsetPropertyName = "_ColorsArrayOffset";

    public override void SearchAndFillComponents(
        GameObject self,
        ColorArrayLightsController comp,
        CreateContainer container)
    {
        comp.Material = container.Library.Materials.Lookup[MaterialController.Material];
        comp.MpbControllers = MaterialPropertyBlockControllers
            .Select(x => container.GetGameObjectOrNull(x, self).GetComponent<MaterialPropertyBlockController>())
            .ToArray();
    }

    public override void CopyTo(ColorArrayLightsController comp)
    {
        comp.ColorArrayData = ColorArrayLightWithIds
            .Select(data =>
            {
                var d = comp.gameObject.AddComponent<ColorArrayData>();
                d.Index = data.Index;
                return d;
            })
            .ToArray();

        comp.ColorsArrayPropertyName = ColorsArrayPropertyName;
        comp.ColorsArrayOffsetPropertyName = ColorsArrayOffsetPropertyName;
    }

    public class MaterialControllerData
    {
        public string Material;
    }

    public class ColorArrayLightWithIdData
    {
        public int Index;
    }
}
