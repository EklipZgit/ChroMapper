using UnityEngine;

public class VisualOutlineController : VisualController
{
    private static readonly int colorId = Shader.PropertyToID("_Color");
    public VisualModelController VModelController;

    public MeshRenderer Renderer;
    public MeshFilter MeshFilter;
    public bool ReplaceCollider;
    public IntersectionCollider Collider;

    private MaterialPropertyBlock mpb;

    public void OnEnable()
    {
        if (MeshFilter != null) VModelController.OnMeshChanged += HandleMeshChanged;
        if (Collider != null && ReplaceCollider) VModelController.OnColliderChanged += HandleColliderChanged;
    }

    public void OnDisable()
    {
        VModelController.OnMeshChanged -= HandleMeshChanged;
        VModelController.OnColliderChanged -= HandleColliderChanged;
    }

    private void HandleMeshChanged(Mesh mesh, Transform source)
    {
        if (mesh == MeshFilter.sharedMesh) return;

        var target = Renderer.transform;

        // TODO: it's bad but i need someway to match mesh with selection, maybe use matrix?
        target.SetParent(source.parent, false);
        target.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
        target.localScale = source.localScale;
        target.SetParent(transform, true);

        MeshFilter.sharedMesh = mesh;
    }

    private void HandleColliderChanged(Mesh mesh)
    {
        if (mesh == Collider.Mesh) return;
        Collider.Mesh = mesh;
        Collider.HardRefresh();
    }
}
