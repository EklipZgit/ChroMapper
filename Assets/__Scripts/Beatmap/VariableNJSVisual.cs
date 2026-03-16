using UnityEngine;

public class VariableNJSVisual : MonoBehaviour
{
    private static readonly int editorDistanceID = Shader.PropertyToID("_EditorDistance");
    [SerializeField] private VariableNJSProvider provider;

    private void OnEnable() => provider.OnChanged += UpdateVisual;
    private void OnDisable() => provider.OnChanged -= UpdateVisual;
    private void UpdateVisual() => Shader.SetGlobalFloat(editorDistanceID, provider.HalfJumpDistance);
}
