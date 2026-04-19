using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ScrollableInput : MonoBehaviour, IScrollHandler
{
    public event Action<Vector2> OnScrolled;

    public void OnScroll(PointerEventData eventData)
    {
        // TODO: we need to change this alt somewhere else
        if (Keyboard.current.altKey.isPressed) OnScrolled?.Invoke(eventData.scrollDelta);
    }
}
