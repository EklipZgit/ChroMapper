using UnityEngine;

namespace Tests.Infrastructure
{
    // Batch tests do not render the game view automatically. Render after the production animation loops.
    [DefaultExecutionOrder(10000)]
    internal sealed class CameraRenderAfterLateUpdate : MonoBehaviour
    {
        internal Camera TargetCamera;
        internal System.Action BeforeRender;

        private void LateUpdate()
        {
            BeforeRender?.Invoke();
            TargetCamera.Render();
            enabled = false;
        }
    }
}
