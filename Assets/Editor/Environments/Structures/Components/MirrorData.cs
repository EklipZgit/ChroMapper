using UnityEngine;

public class MirrorData : EnvironmentComponentData<PlanarReflection>
{
    public string Renderer;
    public string MirrorMaterial;
    public string NoMirrorMaterial;
    public string ReflectionPlaneTransform;

    public override void SearchAndFillComponents(GameObject self, PlanarReflection comp, CreateContainer container)
    {
        comp.MirrorRenderer = container.Library.MirrorRenderer;
        comp.MirrorMaterial = container.Library.Materials.Lookup[MirrorMaterial];
        comp.NoMirrorMaterial = container.Library.Materials.Lookup[NoMirrorMaterial];
        comp.Renderer = container.GetGameObjectOrNull(Renderer, self).GetComponent<MeshRenderer>();
        comp.PlaneTransform = container.GetGameObjectOrNull(ReflectionPlaneTransform, self)
            .transform;
    }

    public override void CopyTo(PlanarReflection comp)
    {
    }
}
