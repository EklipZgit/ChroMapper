using System;
using UnityEngine;

public abstract class LightController : MonoBehaviour
{
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);

    public LightKind Kind;
    public int Type;
    public int ID;

    protected static readonly int ColorId = Shader.PropertyToID("_Color");

    protected bool HasInitialized;
    protected MaterialPropertyBlock Mpb;
    protected Color Color;

    protected virtual void OnValidate()
    {
        Color = new(0f, 0.5f, 1f);
        Start();
    }

    public void Start()
    {
        Mpb = new();
        HasInitialized = Initialize();
        SetColor(Color);
    }

    protected abstract bool Initialize();
    public abstract void SetColor(Color color);

    public enum LightKind : byte
    {
        Basic,
        Group
    }
}
