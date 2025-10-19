using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    private const int maxBloomfogPasses = 7;

    [SerializeField] private Camera bloomfogCamera;
    [SerializeField] private Camera editorCamera;
    [SerializeField] private Shader blurShader;

    private Material blurMaterial;

    private RenderTexture[] bloomfogPassRTs = new RenderTexture[maxBloomfogPasses];

    private int realBloomfogPasses = maxBloomfogPasses;
    private int cachedScreenWidth = 0;
    private int cachedScreenHeight = 0;

    private void Start()
    {
        if (bloomfogCamera == null)
        {
            Debug.LogError("Bloomfog Camera is not assigned.");
            return;
        }

        blurMaterial = new Material(blurShader);

        RegenerateRenderTexture(Settings.Instance.HighQualityBloom ? 1 : 2);
    }

    private void Update()
    {
        if (cachedScreenWidth != Screen.width || cachedScreenHeight != Screen.height)
        {
            RegenerateRenderTexture(Settings.Instance.HighQualityBloom ? 1 : 2);
        }

        // lazy
        bloomfogCamera.fieldOfView = editorCamera.fieldOfView;
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

    private void OnDestroy() => ClearRenderTextures();

    private void ClearRenderTextures()
    {
        foreach (var rt in bloomfogPassRTs)
        {
            if (rt != null) rt.Release();
        }
    }

    private void RegenerateRenderTexture(int quality = 1)
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
