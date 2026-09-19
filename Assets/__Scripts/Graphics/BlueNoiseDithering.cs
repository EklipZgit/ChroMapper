using UnityEngine;

public sealed class BlueNoiseDithering : ScriptableObject
{
    [SerializeField] private Texture2D noiseTexture;

    private static readonly int noiseParamsId = Shader.PropertyToID("_GlobalBlueNoiseParams");
    private static readonly int globalNoiseTextureId = Shader.PropertyToID("_GlobalBlueNoiseTex");

    /// <summary>
    /// Publishes the noise texture and UV tiling for the supplied render dimensions in pixels.
    /// Stereo callers supply eye-texture dimensions rather than the window size.
    /// </summary>
    public void SetBlueNoiseShaderParams(int cameraPixelWidth, int cameraPixelHeight)
    {
        Shader.SetGlobalVector(
            noiseParamsId,
            new Vector4(
                cameraPixelWidth / (float)noiseTexture.width,
                cameraPixelHeight / (float)noiseTexture.height,
                0f,
                0f));
        Shader.SetGlobalTexture(globalNoiseTextureId, noiseTexture);
    }
}
