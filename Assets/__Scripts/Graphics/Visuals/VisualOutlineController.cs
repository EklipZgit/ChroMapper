using UnityEngine;

public class VisualOutlineController : VisualController
{
    public VisualModelController VModelController;

    public MeshRenderer Renderer;
    public MeshFilter MeshFilter;
    public bool ReplaceCollider;
    public IntersectionCollider Collider;
    private bool selectionMarkReplace;
    private bool selected;

    public bool Selected
    {
        get => selected;
        set
        {
            if (selected == value) return;
            selected = value;
            if (Renderer != null) Renderer.enabled = selected;
        }
    }

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

    private void HandleMeshChanged(Mesh obj, Transform t)
    {
        var scale = t.lossyScale;
        var target = Renderer.transform;
        target.localScale = scale;
        target.localRotation = t.localRotation;
        MeshFilter.sharedMesh = obj;
    }

    private void HandleColliderChanged(Mesh obj)
    {
        Collider.Mesh = obj;
        Collider.HardRefresh();
    }
}
