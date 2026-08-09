using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    private const int prefilterPass = 0;
    private const int downscalePass = 0;
    private const int upscalePass = 1;
    private const int finalUpscalePass = 2;

    private const int bloomFogResolution = 512;
    private const int maxBloomfogPasses = 16;

    [SerializeField] private Shader blurShader;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private BloomfogRendererSO bloomfogRenderer;
    [Space]
    // Changing these v does nothing, they're set in the Mapper scene itself which overrides this
    [SerializeField] private float bloomIntensity = 0.4f;
    [SerializeField] private float bloomRadius = 16f;
    [SerializeField] private float pyramidWeightsParam = 0.2f;
    [SerializeField] private float downIntensityOffset = 1f;
    [SerializeField] private float firstUpscaleBrightness = 1f;
    [SerializeField] private float finalUpscaleBrightness = 1f;
    // Changing these ^ does nothing, they're set in the Mapper scene itself which overrides this
    /*
        Open Assets/__Scenes/03_Mapper.unity.
        In the Hierarchy panel, search for Bloomfog Renderer.
        Select it.
        In the Inspector panel, find the Bloomfog Rendering Controller component.
        Edit the two fields there, then save the scene with Ctrl+S.
    */

    private Camera activeCamera;
    private Material blurMaterial;

    private RenderTexture bloomfogRaw = null;
    private RenderTexture bloomfogTex = null;
    private readonly Level[] bloomfogPasses = new Level[maxBloomfogPasses];

    public void AssignToCamera(CameraController activeCamera) => this.activeCamera = activeCamera.Camera;

    private void Start()
    {
        Camera.onPreRender += OnCameraPreRender;
        context.OnEnvironmentLoaded += HandleEnvironmentLoaded;

        blurMaterial = new Material(blurShader);

        Settings.NotifyBySettingName(nameof(Settings.HighQualityBloom), (_) => RegenerateRenderTexture());

        bloomfogRenderer.Initialize();
        // TODO verify against game
        UpdateBloomFogParams(1000f, 0f, 25f, -50f, 0.00025f);
        // TODO verify against game?
        Shader.SetGlobalFloat("_BloomfogBrightness", 0.1f);
        Shader.EnableKeyword("BLOOM_FOG");

        RegenerateRenderTexture();
    }

    // Render bloomfog and perform blur passes before the active editor camera renders
    // This ensures the main render has up-to-date bloomfog texture
    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera) return;

        // Render bloomfog to raw texture
        bloomfogRenderer.RenderToTexture(activeCamera, bloomfogRaw, out var textureToScreenRatio);
        Shader.SetGlobalVector("_CustomFogTextureToScreenRatio", textureToScreenRatio);

        // Beat Saber does an initial downscale so we mimic that here
        // Low quality bloom will do another downscale on top of that,
        var qualityDownscale = Settings.Instance.HighQualityBloom ? 2 : 4;

        // Gather descriptor for temporary render textures
        var descriptor = new RenderTextureDescriptor
        {
            width = bloomfogTex.width / qualityDownscale,
            height = bloomfogTex.height / qualityDownscale,
            volumeDepth = 1,
            msaaSamples = 1,
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            colorFormat = RenderTextureFormat.ARGBFloat,
            depthBufferBits = 0,
            sRGB = false,
            useMipMap = false,
            autoGenerateMips = false,
            enableRandomWrite = false
        };

        // Determine number of passes based on resolution and radius
        var bloomfogPassFloat = Mathf.Log(Mathf.Max(descriptor.width, descriptor.height), 2f) + Mathf.Min(bloomRadius, 10f) - 10f;
        var unclampedBloomfogPasses = Mathf.FloorToInt(bloomfogPassFloat);
        var realBloomfogPasses = Mathf.Clamp(unclampedBloomfogPasses, 1, maxBloomfogPasses);
        var blurRadius = 0.5f + bloomfogPassFloat - unclampedBloomfogPasses;

        // Set up downscale parameters
        blurMaterial.SetFloat("_BloomfogCombineDst", 1);
        blurMaterial.SetFloat("_BloomfogCombineSrc", bloomIntensity);
        blurMaterial.SetFloat("_BloomfogBlurRadius", blurRadius);

        // Downscale
        var downscaleSrc = bloomfogRaw;
        for (var i = 0; i < realBloomfogPasses; i++)
        {
            // Pass 0 is prefilter, rest are downscale
            var pass = i == 0 ? prefilterPass : downscalePass;

            // Allocate temporary render texture for this pass
            bloomfogPasses[i].down = RenderTexture.GetTemporary(descriptor);
            if (i > 0)
            {
                bloomfogPasses[i].up = RenderTexture.GetTemporary(descriptor);
            }

            // Apply blur pass
            blurMaterial.SetTexture("_BloomfogSrcTex", downscaleSrc);
            Graphics.Blit(downscaleSrc, bloomfogPasses[i].down, blurMaterial, pass);

            // Next source is current destination
            downscaleSrc = bloomfogPasses[i].down;

            // Downscale for next iteration
            descriptor.width /= 2;
            descriptor.height /= 2;
        }

        // Set last downsample texture for auto exposure
        blurMaterial.SetTexture("_BloomfogGlobalIntensityTex", downscaleSrc);

        // Upscale
        var upscaleSrc = bloomfogPasses[realBloomfogPasses - 1].down;
        for (var i = realBloomfogPasses - 2; i >= 0; i--)
        {
            var srcStrength = Mathf.Min(1f, Mathf.Pow(bloomIntensity * (i + 1f) / (realBloomfogPasses - 1f), pyramidWeightsParam));
            var dstStrength = Mathf.Min(1f, 1 + downIntensityOffset - srcStrength);
            var brightness = 1f;

            if (i == 0)
            {
                brightness = finalUpscaleBrightness;
            }
            else if (i == realBloomfogPasses - 2)
            {
                brightness = firstUpscaleBrightness;
            }

            blurMaterial.SetFloat("_BloomfogCombineSrc", srcStrength * brightness);
            blurMaterial.SetFloat("_BloomfogCombineDst", dstStrength * brightness);
            blurMaterial.SetTexture("_BloomfogPrevTex", bloomfogPasses[i].down);
            blurMaterial.SetTexture("_BloomfogSrcTex", upscaleSrc);

            var upscaleDst = (i == 0) ? bloomfogTex : bloomfogPasses[i].up;
            var shaderPass = (i == 0) ? finalUpscalePass : upscalePass;

            Graphics.Blit(upscaleSrc, upscaleDst, blurMaterial, shaderPass);

            // Update for next iteration - we cant release here as we still need the texture in prevTexture
            upscaleSrc = upscaleDst;
        }

        // Release all temporary render textures
        for (var i = 0; i < realBloomfogPasses; i++)
        {
            RenderTexture.ReleaseTemporary(bloomfogPasses[i].down);
            RenderTexture.ReleaseTemporary(bloomfogPasses[i].up);
        }
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        if (descriptor == null) return;
        UpdateBloomFogParams(
            descriptor.BloomFogParams.AutoExposureLimit,
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

    private void UpdateBloomFogParams(
        float autoExposureLimit,
        float offset,
        float height,
        float startY,
        float attenuation)
    {
        blurMaterial.SetFloat("_AutoExposureLimit", autoExposureLimit);
        Shader.SetGlobalFloat("_CustomFogOffset", offset);
        Shader.SetGlobalFloat("_CustomFogHeightFogStartY", startY);
        Shader.SetGlobalFloat("_CustomFogHeightFogHeight", height);
        Shader.SetGlobalFloat("_CustomFogAttenuation", attenuation);
    }

    private void ClearRenderTextures()
    {
        if (bloomfogRaw != null)
        {
            bloomfogRaw.Release();
        }

        if (bloomfogTex != null)
        {
            bloomfogTex.Release();
        }
    }

    private void RegenerateRenderTexture() => RegenerateRenderTexture(Settings.Instance.HighQualityBloom ? 1 : 2);

    private void RegenerateRenderTexture(int quality)
    {
        ClearRenderTextures();

        var width = bloomFogResolution / quality;
        var height = bloomFogResolution / quality;

        bloomfogTex = new RenderTexture(width, height, 0, RenderTextureFormat.RGB111110Float)
        {
            name = "Bloomfog Final Texture",
            filterMode = FilterMode.Bilinear
        };
        bloomfogTex.Create();

        bloomfogRaw = new RenderTexture(width, height, 0, RenderTextureFormat.RGB111110Float)
        {
            name = "Bloomfog Raw Texture",
            filterMode = FilterMode.Bilinear,
        };
        bloomfogRaw.Create();

        Shader.SetGlobalTexture("_BloomPrePassTexture", bloomfogTex);
    }

    private struct Level
    {
        internal RenderTexture down;
        internal RenderTexture up;
    }
}
