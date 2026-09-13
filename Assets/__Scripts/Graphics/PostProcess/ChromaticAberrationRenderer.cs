using System;
using UnityEngine;
using UnityEngine.Rendering;

// Records chromatic aberration after bloom has composited the scene.
public class ChromaticAberrationRenderer : MonoBehaviour
{
    [SerializeField] private Shader chromaticAberrationShader;

    [Space]
    // User-facing normalized intensity; the shader expects a smaller sampling offset.
    [SerializeField, Range(0f, 1f)] private float intensity = 0.1f;

    private static readonly int chromaticAberrationId = Shader.PropertyToID("_ChromaticAberration");
    private static readonly int bloomTexelSizeId = Shader.PropertyToID("_BloomTexelSize");

    private Material caMaterial;
    private bool caEnabled = true;
    private bool settingsCallbackSubscribed;

    public bool IsReady => isActiveAndEnabled && caEnabled && caMaterial != null;

    private void OnEnable()
    {
        if (!settingsCallbackSubscribed)
        {
            Settings.NotifyBySettingName(nameof(Settings.ChromaticAberration), UpdateChromaticAberration);
            settingsCallbackSubscribed = true;
        }
        UpdateChromaticAberration(Settings.Instance.ChromaticAberration);
    }

    private void OnDisable()
    {
        if (!settingsCallbackSubscribed) return;
        Settings.StopNotifyingBySettingName(nameof(Settings.ChromaticAberration), UpdateChromaticAberration);
        settingsCallbackSubscribed = false;
    }

    private void Start()
    {
        caMaterial = new Material(chromaticAberrationShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        UpdateChromaticAberration(Settings.Instance.ChromaticAberration);
    }

    public void UpdateChromaticAberration(object o) => caEnabled = Convert.ToBoolean(o);

    private void OnDestroy()
    {
        OnDisable();
        if (caMaterial != null) Destroy(caMaterial);
    }

    public void RecordRender(
        CommandBuffer commandBuffer,
        RenderTargetIdentifier source,
        RenderTargetIdentifier destination,
        int sourceWidth,
        int sourceHeight)
    {
        // The shader reads source dimensions from zw and derives its sample count from them.
        caMaterial.SetFloat(chromaticAberrationId, intensity * 0.05f);
        caMaterial.SetVector(
            bloomTexelSizeId,
            new Vector4(1f / sourceWidth, 1f / sourceHeight, sourceWidth, sourceHeight));
        commandBuffer.Blit(source, destination, caMaterial);
    }
}
