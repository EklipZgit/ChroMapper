using System;
using UnityEngine;

public class ParametricBoxLight : MonoBehaviour
{
    public Renderer Renderer;

    public float AlphaStart = 1f;
    public float AlphaEnd = 1f;
    public float AlphaMultiplier = 1f;
    public float Width = 1f;
    public float WidthStart = 1f;
    public float WidthEnd = 1f;
    public float Center = 0.5f;
    public float Height = 1f;
    public float Length = 1f;
    public float MinAlpha;

    public bool UseCollision;
    public float CollisionHeight;
    public bool UpdateTransform = true;

    private Transform tr;
    private MaterialPropertyBlock mpb;
    private Color color;
    private bool hasInitialized;
    private static readonly int colorId = Shader.PropertyToID("_Color");
    private static readonly int alphaWidthId = Shader.PropertyToID("_AlphaWidth");

    private void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        color = new Color(0f, 0.5f, 1f);
        Start();
    }

    protected void Awake()
    {
        InitIfNeeded();
        if (Renderer == null) Debug.LogError($"[ParametricBoxLight] Renderer is null in Awake on '{name}'.");
        else Renderer.enabled = false;
    }

    private void Start() => SetColor(color);
    protected void OnEnable() { if (Renderer != null) Renderer.enabled = true; else Debug.LogError($"[ParametricBoxLight] Renderer is null in OnEnable on '{name}'."); }
    protected void OnDisable() { if (Renderer != null) Renderer.enabled = false; else Debug.LogError($"[ParametricBoxLight] Renderer is null in OnDisable on '{name}'."); }

    public void InitIfNeeded()
    {
        if (hasInitialized) return;
        tr = transform;
        mpb ??= new MaterialPropertyBlock();
        if (Renderer == null) Renderer = GetComponent<Renderer>();
        if (Renderer == null) Debug.LogWarning($"[ParametricBoxLight] Renderer is still null after InitIfNeeded on '{name}'. Assign Renderer in the Inspector or ensure a Renderer is on this GameObject.");
        hasInitialized = true;
    }

    public void SetColor(Color col)
    {
        color = col;
        InitIfNeeded();
        if (!hasInitialized) return;

        var height = UseCollision ? Mathf.Min(CollisionHeight, Height) : Height;
        if (UpdateTransform)
        {
            tr.localScale = new Vector3(Width * 0.5f, height * 0.5f, Length * 0.5f);
            tr.localPosition = new Vector3(0f, (0.5f - Center) * height, 0f);
        }

        var newCol = color;
        newCol.a *= AlphaMultiplier;
        if (newCol.a < MinAlpha) newCol.a = MinAlpha;

        var alphaEnd = Mathf.Lerp(AlphaStart, AlphaEnd, Mathf.InverseLerp(0f, Height, height));
        if (mpb == null)
        {
            Debug.LogWarning($"[ParametricBoxLight] mpb was null on '{name}' during SetColor — reinitializing (likely a domain-reload timing issue).");
            mpb = new MaterialPropertyBlock();
        }
        if (Renderer == null)
        {
            Debug.LogError($"[ParametricBoxLight] Renderer is null on '{name}' — no Renderer component found on this GameObject.");
            return;
        }
        mpb.SetColor(colorId, color);
        mpb.SetVector(alphaWidthId, new Vector4(AlphaStart, alphaEnd, WidthStart, WidthEnd));
        Renderer.SetPropertyBlock(mpb);
    }
}
