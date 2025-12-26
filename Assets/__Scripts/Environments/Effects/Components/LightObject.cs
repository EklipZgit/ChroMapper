using UnityEngine;

public class LightObject : MonoBehaviour
{
    public Renderer Renderer;
    public float Multiply = 1f;

    protected MaterialPropertyBlock Mpb;

    protected static readonly int colorId = Shader.PropertyToID("_Color");

    protected virtual void Start() => Mpb = new MaterialPropertyBlock();

    public virtual void UpdateLighting(Color color)
    {
        Mpb.SetColor(colorId, ModifyColor(color));
        Renderer.SetPropertyBlock(Mpb);
    }

    protected virtual Color ModifyColor(Color color)
    {
        color.a *= Multiply;
        return color;
    }
}
