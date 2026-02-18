using UnityEngine;
using UnityEngine.InputSystem;

public class PlatformToggleDisableableObjects : MonoBehaviour, CMInput.IPlatformDisableableObjectsActions
{
    private EnvironmentDescriptor descriptor;

    // Start is called before the first frame update
    // private void Start() => LoadInitialMap.OnPlatformLoaded += PlatformLoaded;

    // private void OnDestroy() => LoadInitialMap.OnPlatformLoaded -= PlatformLoaded;

    public void OnTogglePlatformObjects(InputAction.CallbackContext context)
    {
        // if (context.performed) UpdateDisableableObjects();
    }

    private void PlatformLoaded(EnvironmentDescriptor obj) => descriptor = obj;
    // public void UpdateDisableableObjects() => descriptor.ToggleDisablableObjects();
}
