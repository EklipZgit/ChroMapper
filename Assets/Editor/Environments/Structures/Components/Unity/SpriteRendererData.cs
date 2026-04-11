using System.Linq;
using UnityEngine;

public class SpriteRendererData : EnvironmentComponentData<SpriteRenderer>
{
    public string Name;
    public string TextureName;
    public Vector2 Size;
    public string[] Materials;

    public override void SearchAndFillComponents(GameObject self, SpriteRenderer comp, CreateContainer container) =>
        comp.sharedMaterials = Materials.Select(x => container.Library.Materials.Lookup[x]).ToArray();

    public override void CopyTo(SpriteRenderer comp) => comp.size = Size;
}
