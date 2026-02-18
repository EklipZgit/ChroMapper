using System;
using UnityEngine;

public abstract class LightController : MonoBehaviour, IEnvironmentComponentUpdate
{
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);

    public LightKind Kind;
    public int Type;
    public int ID;

    public virtual bool IsPhysical => false;

    protected static readonly int ColorId = Shader.PropertyToID("_Color");

    protected bool HasInitialized;
    protected MaterialPropertyBlock Mpb;
    [NonSerialized] public Color Color;

    protected virtual void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        HasInitialized = false;
        Color = new(0f, 0.5f, 1f);
        Start();
    }

    public void Start()
    {
        Mpb = new();
        if (!HasInitialized) HasInitialized = Initialize();
        SetColor(Color);
    }

    protected abstract bool Initialize();
    public abstract void SetColor(Color color);

    public enum LightKind : byte
    {
        Basic,
        Group
    }

    public virtual bool ShouldInclude => false;
    public virtual bool ShouldRefresh => false;
    public virtual void Refresh() { }
}
