using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    private const int bloomFogResolution = 512;
    private const int maxBloomfogPasses = 5;

    [SerializeField] private Shader blurShader;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private BloomfogRendererSO bloomfogRenderer;

    private Camera activeCamera;
    private Material blurMaterial;
    private RenderTexture[] bloomfogPassRTs = new RenderTexture[maxBloomfogPasses];

    private int realBloomfogPasses = maxBloomfogPasses;

    public void AssignToCamera(CameraController activeCamera)
        => this.activeCamera = activeCamera.Camera;

    private void Start()
    {
        Camera.onPreRender += OnCameraPreRender;
        context.OnEnvironmentChanged += HandleEnvironmentLoaded;

        blurMaterial = new Material(blurShader);

        Settings.NotifyBySettingName(nameof(Settings.HighQualityBloom), (_) => RegenerateRenderTexture());

        bloomfogRenderer.Initialize();
        UpdateBloomFogParams(0f, 25f, -50f, 0.00025f);
        Shader.SetGlobalFloat("_BloomfogBrightness", 0.1f);
        Shader.EnableKeyword("ENABLE_BLOOM_FOG");

        RegenerateRenderTexture();
    }

    // Render bloomfog and perform blur passes before the active editor camera renders
    // This ensures the main render has up-to-date bloomfog texture
    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera) return;

        // Render bloomfog to first RT
        bloomfogRenderer.RenderToTexture(activeCamera, bloomfogPassRTs[0], out var textureToScreenRatio);
        Shader.SetGlobalVector("_CustomFogTextureToScreenRatio", textureToScreenRatio);

        // Downscale
        blurMaterial.SetFloat("_BloomfogAlpha", 1);
        blurMaterial.SetFloat("_BloomfogBlurRadius", 0);
        for (var i = 0; i < realBloomfogPasses - 1; i++)
        {
            blurMaterial.SetTexture("_BloomfogPrevTex", bloomfogPassRTs[i]);
            Graphics.Blit(bloomfogPassRTs[i], bloomfogPassRTs[i + 1], blurMaterial);
        }

        // Upscale
        var passes = 0;
        for (var i = realBloomfogPasses - 1; i > 0; i--)
        {
            //blurMaterial.SetFloat("_BloomfogAlpha", Mathf.Pow(0.5f, (float)i / realBloomfogPasses));
            blurMaterial.SetFloat("_BloomfogAlpha", Mathf.Lerp(1.2f, 0.25f, (float)passes / realBloomfogPasses));
            blurMaterial.SetFloat("_BloomfogBlurRadius", passes);
            blurMaterial.SetTexture("_BloomfogPrevTex", bloomfogPassRTs[i]);
            Graphics.Blit(bloomfogPassRTs[i], bloomfogPassRTs[i - 1], blurMaterial);
            passes++;
        }
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        if (descriptor == null) return;
        UpdateBloomFogParams(
            descriptor.BloomFogParams.Offset,
            descriptor.BloomFogParams.Height,
            descriptor.BloomFogParams.StartY,
            descriptor.BloomFogParams.Attenuation);
    }

    private void OnDestroy()
    {
        bloomfogRenderer.Release();
        Camera.onPreRender -= OnCameraPreRender;
        ClearRenderTextures();
        Settings.ClearSettingNotifications(nameof(Settings.HighQualityBloom));
        Settings.ClearSettingNotifications(nameof(Settings.CameraFOV));
    }

    private void UpdateBloomFogParams(float offset, float height, float startY, float attenuation)
    {
        Shader.SetGlobalFloat("_CustomFogOffset", offset);
        Shader.SetGlobalFloat("_CustomFogHeightFogStartY", startY);
        Shader.SetGlobalFloat("_CustomFogHeightFogHeight", height);
        Shader.SetGlobalFloat("_CustomFogAttenuation", attenuation);
    }

    private void ClearRenderTextures()
    {
        foreach (var rt in bloomfogPassRTs)
        {
            if (rt != null) rt.Release();
        }
    }

    private void RegenerateRenderTexture() => RegenerateRenderTexture(Settings.Instance.HighQualityBloom ? 1 : 2);

    private void RegenerateRenderTexture(int quality)
    {
        ClearRenderTextures();

        var width = bloomFogResolution / quality;
        var height = bloomFogResolution / quality;

        realBloomfogPasses = 0;

        // Create render textures for each pass
        for (var i = 0; i < maxBloomfogPasses; i++)
        {
            // Stop if the texture is too small
            if (width < 2 || height < 2) break;

            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat)
            {
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
            bloomfogPassRTs[i] = rt;

            realBloomfogPasses++;
            width /= 2;
            height /= 2;
        }

        Shader.SetGlobalTexture("_BloomPrePassTexture", bloomfogPassRTs[0]);
    }
}
