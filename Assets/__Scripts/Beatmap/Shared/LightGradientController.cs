using Beatmap.Shared;
using UnityEngine;

public class LightGradientController : MonoBehaviour
{
    private static readonly int colorA = Shader.PropertyToID("_ColorA");
    private static readonly int colorB = Shader.PropertyToID("_ColorB");
    private static readonly int easingId = Shader.PropertyToID("_EasingID");

    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialPropertyBlock;
    private float ribbonLength;

    public void UpdateGradientData(ChromaLightGradient gradient)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();

        materialPropertyBlock.SetColor(colorA, gradient.StartColor);
        materialPropertyBlock.SetColor(colorB, gradient.EndColor);
        materialPropertyBlock.SetInt(easingId, Easing.EasingShaderId(gradient.EasingType));
        
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    // note: 4/3rds magic number comes from the fact that events are 0.75m in size
    public void UpdateDuration(float duration)
    {
        ribbonLength = duration * EditorScaleController.EditorScale * (4f / 3);
        transform.localPosition = new Vector3(
            0,
            -0.5f + 0.005f,
            0);
        transform.localScale = new Vector3(ribbonLength, 1, 1);
    }

    public void SetVisible(bool visible)
    {
        meshRenderer.enabled = visible;
    }
}
