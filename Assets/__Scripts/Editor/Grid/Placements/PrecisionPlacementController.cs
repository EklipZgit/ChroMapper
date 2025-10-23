using UnityEngine;

public class PrecisionPlacementController : MonoBehaviour
{
    public static bool IsEnabled;

    [SerializeField] private IntersectionCollider intersectionCollider;
    [SerializeField] private Renderer regularMesh;
    [SerializeField] private Renderer expandedMesh;

    private static readonly int position = Shader.PropertyToID("_MousePosition");
    private MaterialPropertyBlock materialPropertyBlock;

    private void Start()
    {
        TogglePrecisionPlacement(false);
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    public void TogglePrecisionPlacement(bool toggle)
    {
        if (toggle == IsEnabled) return;
        IsEnabled = toggle;

        if (toggle && Settings.Instance.PrecisionPlacementMode != PrecisionPlacementMode.Off)
        {
            expandedMesh.gameObject.SetActive(true);
            intersectionCollider.Size = expandedMesh.transform.localScale;
        }
        else
        {
            expandedMesh.gameObject.SetActive(false);
            intersectionCollider.Size = regularMesh.transform.localScale;
        }

        intersectionCollider.HardRefresh();
    }

    public void UpdateMousePosition(Vector3 mousePosition)
    {
        materialPropertyBlock.SetVector(position, mousePosition);
        expandedMesh.SetPropertyBlock(materialPropertyBlock);
    }
}
