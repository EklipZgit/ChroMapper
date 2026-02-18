using UnityEngine;

[ExecuteAlways]
public class PlanarReflection : MonoBehaviour
{
    public MirrorRendererSO MirrorRenderer;
    public MeshRenderer Renderer;
    public Transform PlaneTransform;

    private static readonly int texturePropertyId = Shader.PropertyToID("_ReflectionTex");

    private void Update() => MirrorRenderer.PrepareForNextFrame();

    private void OnWillRenderObject()
    {
        if (!enabled || !Renderer || !Renderer.enabled) return;

        var position = PlaneTransform.position;
        var up = PlaneTransform.up;
        var texture = MirrorRenderer.RenderMirrorTexture(Camera.current, position - (up * 0.001f), up);
        if (texture == null)
        {
            Renderer.sharedMaterial.SetTexture(texturePropertyId, Texture2D.blackTexture);
            return;
        }
        Renderer.sharedMaterial.SetTexture(texturePropertyId, texture);
    }
}
