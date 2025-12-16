using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    private const int maxBloomfogPasses = 5;

    [SerializeField] private Camera bloomfogCamera;
    [SerializeField] private Shader blurShader;

    private Camera editorCamera;
    private Material blurMaterial;
    private RenderTexture[] bloomfogPassRTs = new RenderTexture[maxBloomfogPasses];

    private int realBloomfogPasses = maxBloomfogPasses;
    private int cachedScreenWidth = 0;
    private int cachedScreenHeight = 0;

    public void AssignToCamera(CameraController activeCamera)
    {
        editorCamera = activeCamera.Camera;
        transform.SetParent(activeCamera.transform, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        bloomfogCamera.fieldOfView = editorCamera.fieldOfView;
    }

    private void Start()
    {
        bloomfogCamera.enabled = false;
        Camera.onPreRender += OnCameraPreRender;
        LoadInitialMap.OnPlatformLoaded += HandlePlatformLoaded;

        blurMaterial = new Material(blurShader);

        Settings.NotifyBySettingName(nameof(Settings.HighQualityBloom), (_) => RegenerateRenderTexture());
        Settings.NotifyBySettingName(nameof(Settings.CameraFOV), (fov) => bloomfogCamera.fieldOfView = (float)fov);

        UpdateBloomFogParams(0f, 25f, -50f, 0.00025f);
        Shader.SetGlobalFloat("_BloomfogBrightness", 0.1f);
        Shader.EnableKeyword("ENABLE_BLOOM_FOG");

        RegenerateRenderTexture();
    }

    private void Update()
    {
        if (cachedScreenWidth != Screen.width || cachedScreenHeight != Screen.height)
        {
            RegenerateRenderTexture();
        }
    }

    // Render bloomfog and perform blur passes before the active editor camera renders
    // This ensures the main render has up-to-date bloomfog texture
    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != editorCamera) return;

        // Render bloomfog to first RT
        bloomfogCamera.Render();

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

    private void HandlePlatformLoaded(PlatformDescriptor descriptor)
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

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        var width = cachedScreenWidth / quality;
        var height = cachedScreenHeight / quality;

        // Enforce maximum resolution of 512 while keeping aspect ratio
        // TODO: Beat Saber/ArcViewer uses square 512x512 and uses shader to sample correctly
        var aspect = (float)width / height;
        if (aspect >= 1)
        {
            width = Mathf.Clamp(width, 2, 512);
            height = Mathf.Clamp(Mathf.RoundToInt(width / aspect), 2, 512);
        }
        else
        {
            height = Mathf.Clamp(height, 2, 512);
            width = Mathf.Clamp(Mathf.RoundToInt(height * aspect), 2, 512);
        }

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

        bloomfogCamera.targetTexture = bloomfogPassRTs[0];

        // TODO(Caeden): Calculate screen ratio properly once texture is 512x512
        Shader.SetGlobalVector("_CustomFogTextureToScreenRatio", new Vector4(1, 1, 0, 0));
        Shader.SetGlobalTexture("_BloomPrePassTexture", bloomfogPassRTs[0]);
    }
}
