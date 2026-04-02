using UnityEngine.InputSystem;

public static class CallbackContextExtensions
{
    public static int GetScrollDirection(this InputAction.CallbackContext context) =>
        context.ReadValue<float>() > 0 ? 1 : -1;

    public static int GetScrollDirection(this InputAction.CallbackContext context, bool invert) =>
        (context.ReadValue<float>() > 0) ^ invert ? 1 : -1;
}
