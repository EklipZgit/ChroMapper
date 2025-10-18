using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrecisionPlacementGridController : MonoBehaviour
{
    [SerializeField] private IntersectionCollider intersectionCollider;
    [SerializeField] private Renderer regularMesh;
    [SerializeField] private Renderer expandedMesh;

    private bool isEnabled = true;
    private static readonly int position = Shader.PropertyToID("_MousePosition");

    private void Start() => TogglePrecisionPlacement(false);

    public void TogglePrecisionPlacement(bool isVisible)
    {
        if (isEnabled == isVisible) return;
        isEnabled = isVisible;

        if (isVisible && Settings.Instance.PrecisionPlacementMode != PrecisionPlacementMode.Off)
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

    public void UpdateMousePosition(Vector3 mousePosition) =>
        expandedMesh.sharedMaterial.SetVector(position, mousePosition);
}
