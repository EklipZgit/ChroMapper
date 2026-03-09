using UnityEngine;

[CreateAssetMenu(fileName = "VisualModelSO", menuName = "Graphics/Create Visual Model")]
public class VisualModelSO : ScriptableObject
{
    public GameObject Prefab;
    public Mesh Collider;
    public bool DisableAux; // this refer to arrow/dot, can be for other entity

    private void OnValidate()
    {
        if (Collider == null && Prefab != null)
            Collider = Prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
    }

    public static VisualModelSO Create(GameObject prefab, string prefix)
    {
        var so = CreateInstance<VisualModelSO>();
        so.Prefab = prefab;
        so.name = $"{prefix}_{prefab.name}";
        return so;
    }
}
