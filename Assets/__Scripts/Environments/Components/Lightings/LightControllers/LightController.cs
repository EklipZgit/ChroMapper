using System;
using UnityEngine;

public abstract class LightController : MonoBehaviour, IEnvironmentComponentUpdate
{
    public LightKind Kind;
    public int Type;
    public int ID;

    // GeneratedGeometryUsesNativeEventAlphaAcrossSeeks: Chroma-generated preset geometry consumes
    // the native ColorSO alpha factors (normal .7490196, boosted .8), while imported legacy scene
    // lights keep CM's normalized calibration. Construction-time role flag, not per-frame
    // discovery; left serialized so a cloned controller keeps its role.
    public bool UseNativeEventAlpha;

    public virtual bool IsPhysical => false;

    protected static readonly int ColorId = Shader.PropertyToID("_Color");

    // Public so TubeBloomAnimator's deferred push can refresh only controllers that completed initialization.
    // HeliovFullMapLaserParityTest.Beat41LaserBeamRendersOnCompleteMap: without NonSerialized,
    // Unity's Instantiate copies a runtime-set HasInitialized=true onto every cloned enhancement
    // light, so the clone's Start() skips Initialize() and never caches hasBoxLight/hasSpriteLight —
    // Refresh then silently skips the physical box, its MPB _Color stays transparent, and the
    // authored beam renders black (user's dark Heliov report).
    [NonSerialized]
    public bool HasInitialized;
    protected MaterialPropertyBlock Mpb;
    [NonSerialized] public Color Color = new(0f, 0f, 0f, 0f);

    protected virtual void OnValidate()
    {
        if (!Application.isEditor || Application.isPlaying) return;
        HasInitialized = false;
        Color = new Color(0f, 0.5f, 1f);
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Start();
        };
    #endif
    }

    public void Start()
    {
        Mpb = new MaterialPropertyBlock();
        if (!HasInitialized)
        {
            HasInitialized = Initialize();
            if (!HasInitialized && this is not LightSink)
                Debug.LogError(
                    $"[LightController] Initialize() returned false on '{name}' ({GetType().Name}). Light will not function.");
        }

        SetColor(Color);
    }

    protected abstract bool Initialize();
    public abstract void SetColor(Color color);
    public virtual void SetColor(Color color, LightColorEventStateData evt, float time) => SetColor(color);

    public enum LightKind : byte
    {
        Basic,
        Group
    }

    public virtual bool ShouldInclude => false;
    public virtual bool ShouldRefresh => false;
    public virtual void Refresh() { }
}
