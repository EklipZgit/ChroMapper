using UnityEngine;

public class CustomBoundingBoxData : EnvironmentComponentData<NativeRendererBounds>
{
    public Vector3 CustomBoundCenter;
    public Vector3 CustomBoundSize;

    public override void FillComponents(GameObject self, NativeRendererBounds comp, CreateContainer container)
    {
        // Native CustomBoundingBox stores extents, and doubles them when constructing the renderer's bounds.
        comp.Configure(self.GetComponent<MeshRenderer>(), new Bounds(CustomBoundCenter, CustomBoundSize * 2f));
    }
}
