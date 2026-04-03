using UnityEngine;

public class VisualOutlineController : VisualController
{
    public VisualModelController VModelController;

    public MeshRenderer Renderer;
    public MeshFilter MeshFilter;
    public bool ReplaceCollider;
    public IntersectionCollider Collider;

    private MaterialPropertyBlock mpb;

    public void Start()
    {
        if (MeshFilter != null)
        {
            VModelController.OnMeshChanged += HandleMeshChanged;
            if (VModelController.Actives.Count > 0)
            {
                HandleMeshChanged(
                    VModelController.Actives[0].OutlineMesh.sharedMesh,
                    VModelController.Actives[0].GameObject.transform);
            }
        }

        if (Collider != null && ReplaceCollider)
        {
            VModelController.OnColliderChanged += HandleColliderChanged;
            if (VModelController.Actives.Count > 0) HandleColliderChanged(VModelController.Actives[0].ColliderMesh);
        }
    }

    public void OnDestroy()
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
