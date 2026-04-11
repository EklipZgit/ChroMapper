using System.Linq;
using UnityEngine;

public class
    FloatTextureProcessor3DMaterialSwitchEffectTargetData : EnvironmentComponentData<TextureProcessor3DMaterialSwitchFx>
{
    public string[] MaterialArray;

    public Vector2 ValueBounds = new(-10f, 10f);

    public string[] GridElementControllers;
    public int MaterialIndex;

    public override void SearchAndFillComponents(
        GameObject self,
        TextureProcessor3DMaterialSwitchFx comp,
        CreateContainer container)
    {
        comp.MaterialArray = MaterialArray.Select(x => container.Library.Materials.Lookup[x]).ToArray();
        comp.GridElementControllers = GridElementControllers
            .Select(x => container.GetGameObjectOrNull(x, self).GetComponent<GridElementController>())
            .ToArray();
    }

    public override void CopyTo(TextureProcessor3DMaterialSwitchFx comp)
    {
        comp.ValueBounds = ValueBounds;
        comp.MaterialIndex = MaterialIndex;
    }
}
