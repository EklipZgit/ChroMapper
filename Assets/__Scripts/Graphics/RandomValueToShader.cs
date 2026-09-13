using UnityEngine;

public sealed class RandomValueToShader : ScriptableObject
{
    private static readonly int randomValueId = Shader.PropertyToID("_GlobalRandomValue");

    private System.Random random;
    private int lastFrameNumber = -1;

    public void SetRandom(System.Random randomSource) => random = randomSource;

    /// <summary>
    /// Publishes one random shader value per frame, shared by all cameras using this asset.
    /// </summary>
    public void SetRandomValueToShaders()
    {
        var frameNumber = Time.frameCount;
        if (lastFrameNumber == frameNumber) return;

        random ??= new System.Random();
        Shader.SetGlobalFloat(randomValueId, (float)random.NextDouble());
        lastFrameNumber = frameNumber;
    }
}
