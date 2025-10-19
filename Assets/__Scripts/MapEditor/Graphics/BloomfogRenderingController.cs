using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    private const int maxBloomfogPasses = 7;

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
        blurMaterial = new Material(blurShader);

        Settings.NotifyBySettingName(nameof(Settings.HighQualityBloom), (_) => RegenerateRenderTexture());
        Settings.NotifyBySettingName(nameof(Settings.CameraFOV), (fov) => bloomfogCamera.fieldOfView = (float)fov);

        RegenerateRenderTexture();
    }

    private void Update()
    {
        if (cachedScreenWidth != Screen.width || cachedScreenHeight != Screen.height)
        {
            RegenerateRenderTexture();
        }
    }

    private void OnPostRender()
    {
        // Downscale
        blurMaterial.SetFloat("_BloomfogAlpha", 1);
        for (var i = 0; i < realBloomfogPasses - 1; i++)
        {
            blurMaterial.SetTexture("_BloomfogPrevTex", bloomfogPassRTs[i]);
            Graphics.Blit(bloomfogPassRTs[i], bloomfogPassRTs[i + 1], blurMaterial);
        }

        // Upscale
        for (var i = realBloomfogPasses - 1; i > 0; i--)
        {
            blurMaterial.SetFloat("_BloomfogAlpha", Mathf.Pow(0.5f, (float)i / realBloomfogPasses));
            blurMaterial.SetTexture("_BloomfogPrevTex", bloomfogPassRTs[i]);
            Graphics.Blit(bloomfogPassRTs[i], bloomfogPassRTs[i - 1], blurMaterial);
        }
    }

    private void OnDestroy()
    {
        ClearRenderTextures();
        Settings.ClearSettingNotifications(nameof(Settings.HighQualityBloom));
        Settings.ClearSettingNotifications(nameof(Settings.CameraFOV));
    }

    private void ClearRenderTextures()
    {
        foreach (var rt in bloomfogPassRTs)
        {
            if (rt != null) rt.Release();
        }
    }

    private void RegenerateRenderTexture()
        => RegenerateRenderTexture(Settings.Instance.HighQualityBloom ? 1 : 2);

    private void RegenerateRenderTexture(int quality)
    {
        ClearRenderTextures();

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        var width = cachedScreenWidth / quality;
        var height = cachedScreenHeight / quality;

        realBloomfogPasses = 0;

        // Create render textures for each pass
        for (var i = 0; i < maxBloomfogPasses; i++)
        {
            // Stop if the texture is too small
            if (width < 2 || height < 2) break;

            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            rt.Create();
            bloomfogPassRTs[i] = rt;

            realBloomfogPasses++;
            width /= 2;
            height /= 2;
        }

        bloomfogCamera.targetTexture = bloomfogPassRTs[0];

        Shader.SetGlobalTexture("_BloomfogTex", bloomfogPassRTs[0]);
    }
}
