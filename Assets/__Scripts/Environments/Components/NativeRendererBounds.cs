using UnityEngine;

public class NativeRendererBounds : MonoBehaviour
{
    [SerializeField] private Renderer target;
    [SerializeField] private Bounds localBounds;

    public void Configure(Renderer renderer, Bounds bounds)
    {
        target = renderer;
        localBounds = bounds;
        target.localBounds = localBounds;
    }

    // Renderer overrides are not stored with the renderer in a scene, so restore the imported values on load.
    private void Awake() => target.localBounds = localBounds;
}
