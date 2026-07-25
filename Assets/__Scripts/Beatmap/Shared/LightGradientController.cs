using Beatmap.Shared;
using UnityEngine;

public class LightGradientController : MonoBehaviour
{
    private static readonly int colorA = Shader.PropertyToID("_ColorA");
    private static readonly int colorB = Shader.PropertyToID("_ColorB");
    private static readonly int easingId = Shader.PropertyToID("_EasingID");
    private static readonly int useHsvId = Shader.PropertyToID("_UseHSV");

    [SerializeField] private MeshRenderer meshRenderer;

    private MaterialPropertyBlock materialPropertyBlock;
    private float ribbonLength;

    public void UpdateGradientData(ChromaLightGradient gradient, bool useHsv = false)
    {
        materialPropertyBlock ??= new MaterialPropertyBlock();

        materialPropertyBlock.SetColor(colorA, gradient.StartColor);
        materialPropertyBlock.SetColor(colorB, gradient.EndColor);
        materialPropertyBlock.SetInt(easingId, Easing.EasingShaderId(gradient.EasingType));
        // Match Basic Light runtime interpolation when a transition requests HSV color lerping.
        materialPropertyBlock.SetInt(useHsvId, useHsv ? 1 : 0);
        
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
        // Ribbon prefab children start inactive, so enabling only their renderer cannot make a GLS transition visible.
        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
        meshRenderer.enabled = visible;
    }
}
