using System.Linq;
using UnityEngine;

public class SpriteRendererData : EnvironmentComponentData<SpriteRenderer>
{
    public string Name;
    public string Texture;
    public Vector2 Size;
    public string[] Materials;

    public override void FillComponents(GameObject self, SpriteRenderer comp, CreateContainer container)
    {
        comp.sharedMaterials = Materials.Select(x => container.Library.Materials.GetSafe(x)).ToArray();
        comp.size = Size;
    }
}
