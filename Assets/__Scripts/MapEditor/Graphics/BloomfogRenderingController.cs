using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    [SerializeField] private Camera bloomfogCamera;
    [SerializeField] private Camera editorCamera;
    [SerializeField] private Shader upsampleShader;

    private Material upsampleMaterial;
    private RenderTexture bloomfogPrePassTexture;
    private RenderTexture bloomfogRenderTexture;

    private int cachedScreenWidth = 0;
    private int cachedScreenHeight = 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            RegenerateRenderTexture(Settings.Instance.HighQualityBloom ? 1 : 2);
        }
    }
#endif

    private void Start()
    {
        if (bloomfogCamera == null)
        {
            Debug.LogError("Bloomfog Camera is not assigned.");
            return;
        }

        upsampleMaterial = new Material(upsampleShader);

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
        if (bloomfogRenderTexture == null || bloomfogPrePassTexture == null) return;

        Graphics.Blit(bloomfogPrePassTexture, bloomfogRenderTexture, upsampleMaterial);
    }

    private void OnDestroy()
    {
        if (bloomfogPrePassTexture != null)
        {
            bloomfogPrePassTexture.Release();
        }
        if (bloomfogRenderTexture != null)
        {
            bloomfogRenderTexture.Release();
        }
    }

    private void RegenerateRenderTexture(int quality = 1)
    {
        if (bloomfogPrePassTexture != null)
        {
            bloomfogPrePassTexture.Release();
        }
        if (bloomfogRenderTexture != null)
        {
            bloomfogRenderTexture.Release();
        }

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        bloomfogPrePassTexture = new(cachedScreenWidth / quality, cachedScreenHeight / quality, 16);
        bloomfogPrePassTexture.filterMode = FilterMode.Trilinear;
        bloomfogPrePassTexture.antiAliasing = Settings.Instance.CameraAA;
        bloomfogPrePassTexture.useMipMap = true;
        bloomfogPrePassTexture.Create();
        bloomfogCamera.targetTexture = bloomfogPrePassTexture;
        upsampleMaterial.SetTexture("_BloomfogPrePassTex", bloomfogPrePassTexture);

        bloomfogRenderTexture = new(cachedScreenWidth / quality, cachedScreenHeight / quality, 16);
        bloomfogRenderTexture.Create();
        Shader.SetGlobalTexture("_BloomfogTex", bloomfogRenderTexture);
    }
}
