using UnityEngine;

public class LightObject : MonoBehaviour
{
    public Renderer Renderer;
    public float Multiply = 1f;

    protected MaterialPropertyBlock Mpb;

    protected static readonly int colorId = Shader.PropertyToID("_Color");

    protected bool HasInitialized;

    protected virtual void Start()
    {
        if (Renderer == null) Renderer = GetComponent<Renderer>();
        Mpb = new MaterialPropertyBlock();
        HasInitialized = Renderer != null;
        SetColor(new Color(0f, 0f, 0f, 0f));
    }

    public virtual void SetColor(Color color)
    {
        if (!HasInitialized) return;
        color.a *= Multiply;
        Mpb.SetColor(colorId, color);
        Renderer.SetPropertyBlock(Mpb);
    }
}
