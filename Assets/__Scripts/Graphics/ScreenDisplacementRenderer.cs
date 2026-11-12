using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
// Shares the native draw order between displacement cores and later foreground sprites.
public sealed class ScreenDisplacementRenderer : MonoBehaviour
{
    private const string displacementShaderName = "ChroMapper/Object/Obstacle Distortion";

    private static readonly List<ScreenDisplacementRenderer> renderers = new();
    private static bool displacementEnabled = true;
    private static Shader displacementShader;

    [SerializeField, Range(0, 31)] private int displacementLayer = 31;
    // Native spawn sprites sort after obstacle cores and depth-test against the core's finished surface.
    [SerializeField] private bool renderAfterDisplacement;

    private Renderer targetRenderer;
    private MeshFilter meshFilter;
    private int originalLayer;
    private bool usesDisplacementLayer;
    private int registryIndex = -1;

    public Renderer TargetRenderer => targetRenderer;
    public MeshFilter MeshFilter => meshFilter;
    internal bool RequiresScreenCopy => !renderAfterDisplacement;
    public bool IsReady =>
        displacementEnabled
        && usesDisplacementLayer
        && isActiveAndEnabled
        && targetRenderer != null
        && targetRenderer.enabled;

    public static IReadOnlyList<ScreenDisplacementRenderer> Renderers => renderers;

    public static void SetEnabled(bool enabled)
    {
        displacementEnabled = enabled;
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.RefreshLayer();
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        foreach (var renderer in renderers)
        {
            renderer.registryIndex = -1;
        }
        renderers.Clear();
        displacementEnabled = true;
        displacementShader = Shader.Find(displacementShaderName);
    }

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        originalLayer = gameObject.layer;
    }

    private void OnEnable()
    {
        registryIndex = renderers.Count;
        renderers.Add(this);
        RefreshLayer();
    }

    // Material owners notify shader switches so pooled walls need no per-frame material polling.
    public void RefreshMaterial() => RefreshLayer();

    private void OnDisable()
    {
        Unregister();
        RestoreLayer();
    }

    private void OnDestroy()
    {
        Unregister();
        RestoreLayer();
    }

    private void Unregister()
    {
        if (registryIndex < 0)
        {
            return;
        }

        // Wall pools enable and disable thousands of renderers. Drawing sorts its own copy,
        // so removing a renderer can swap in the last entry without shifting the registry.
        var lastIndex = renderers.Count - 1;
        var last = renderers[lastIndex];
        renderers[registryIndex] = last;
        last.registryIndex = registryIndex;
        renderers.RemoveAt(lastIndex);
        registryIndex = -1;
    }

    private void RefreshLayer()
    {
        if (!displacementEnabled)
        {
            RestoreLayer();
            return;
        }

        var material = targetRenderer.sharedMaterial;
        var shouldUseDisplacementLayer =
            renderAfterDisplacement || (material != null && material.shader == displacementShader);
        if (shouldUseDisplacementLayer == usesDisplacementLayer)
        {
            return;
        }

        usesDisplacementLayer = shouldUseDisplacementLayer;
        gameObject.layer = usesDisplacementLayer ? displacementLayer : originalLayer;
    }

    private void RestoreLayer()
    {
        if (!usesDisplacementLayer)
        {
            return;
        }
        gameObject.layer = originalLayer;
        usesDisplacementLayer = false;
    }
}
