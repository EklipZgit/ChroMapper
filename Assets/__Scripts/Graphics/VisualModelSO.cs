using UnityEngine;

[CreateAssetMenu(fileName = "VisualModelSO", menuName = "Graphics/Create Visual Model")]
public class VisualModelSO : ScriptableObject
{
    public GameObject Prefab;
    public Mesh Collider;

    private void OnValidate()
    {
        if (Collider == null && Prefab != null)
            Collider = Prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
    }
}
