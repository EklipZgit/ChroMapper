using UnityEngine;

[ExecuteAlways]
public class PlanarReflection : MonoBehaviour
{
    [SerializeField] public MirrorRendererSO MirrorRenderer;
    [SerializeField] public Material MirrorMaterial;
    [SerializeField] public Material NoMirrorMaterial;
    [SerializeField] public MeshRenderer Renderer;
    [SerializeField] public Transform PlaneTransform;

    private static readonly int textureId = Shader.PropertyToID("_ReflectionTex");

    // Reflection textures are temporary and remain cached only for the rendered frame.
    private void Update() => MirrorRenderer.PrepareForNextFrame();

    private void OnWillRenderObject()
    {
        if (!enabled || !Renderer || !Renderer.enabled) return;

        var position = PlaneTransform.position;
        var up = PlaneTransform.up;
        // Move the clip plane behind the visible surface to avoid clipping the mirror itself.
        var texture = MirrorRenderer.RenderMirrorTexture(Camera.current, position - (up * 0.001f), up);
        if (texture == null)
        {
            if (Renderer.sharedMaterial != NoMirrorMaterial) Renderer.sharedMaterial = NoMirrorMaterial;
            Renderer.sharedMaterial.SetTexture(textureId, Texture2D.blackTexture);
            return;
        }

        if (Renderer.sharedMaterial != MirrorMaterial) Renderer.sharedMaterial = MirrorMaterial;
        Renderer.sharedMaterial.SetTexture(textureId, texture);
    }
}
