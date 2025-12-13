using UnityEngine;

public class LightObject : MonoBehaviour
{
    public Renderer Renderer;
    public float Multiply = 1f;

    protected MaterialPropertyBlock Mpb;

    private static readonly int colorId = Shader.PropertyToID("_Color");

    protected virtual void Start() => Mpb = new MaterialPropertyBlock();

    public void UpdateLighting(Color color)
    {
        Mpb.SetColor(colorId, ModifyColor(color));
        Renderer.SetPropertyBlock(Mpb);
    }

    public virtual void UpdateBoostState(bool boost) { }

    protected virtual Color ModifyColor(Color color)
    {
        color.a *= Multiply;
        return color;
    }
}
