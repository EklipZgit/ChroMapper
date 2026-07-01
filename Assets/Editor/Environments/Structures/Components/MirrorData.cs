using UnityEngine;

public class MirrorData : EnvironmentComponentData<PlanarReflection>
{
    public int Renderer;
    public string MirrorMaterial;
    public string NoMirrorMaterial;
    public int ReflectionPlaneTransform;

    public override void FillComponents(GameObject self, PlanarReflection comp, CreateContainer container)
    {
        comp.MirrorRenderer = container.Library.MirrorRenderer;
        comp.MirrorMaterial = container.Library.Materials.GetSafe(MirrorMaterial);
        comp.NoMirrorMaterial = container.Library.Materials.GetSafe(NoMirrorMaterial);
        comp.Renderer = container.GetComponentOrNull<MeshRenderer>(Renderer);
        comp.PlaneTransform = container.GetComponentOrNull<Transform>(ReflectionPlaneTransform);
    }
}
