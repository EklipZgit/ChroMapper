using UnityEngine;
using UnityEngine.EventSystems;

public class AudioPreviewMouseInputHandler : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private SongInfoEditUI songInfoEditUI;
    [SerializeField] private RectTransform audioPreviewRectTransform;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(audioPreviewRectTransform, eventData.position,
                eventData.enterEventCamera)) return;
        
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(audioPreviewRectTransform, eventData.position,
                eventData.pressEventCamera, out var localMousePos)) return;
        
        var mouseHorizontalPosition = localMousePos.x - audioPreviewRectTransform.rect.position[0];
        var audioPreviewWidth = audioPreviewRectTransform.rect.size[0];

        var clickPositionPercent = Mathf.Clamp01(mouseHorizontalPosition /audioPreviewWidth);
                
        songInfoEditUI.OnAudioPreviewClicked(clickPositionPercent, eventData.button);
    }
}

