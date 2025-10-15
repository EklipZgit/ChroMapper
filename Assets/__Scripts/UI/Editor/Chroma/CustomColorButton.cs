using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CustomColorButton : MonoBehaviour, IPointerClickHandler
{
    public Image image;

    public event Action OnRightClick;
    public event Action OnMiddleClick;

    public void OnPointerClick(PointerEventData data)
    {
        switch (data.button)
        {
            case PointerEventData.InputButton.Middle:
                OnMiddleClick?.Invoke();
                break;
            case PointerEventData.InputButton.Right:
                OnRightClick?.Invoke();
                break;
        }
    }
}
