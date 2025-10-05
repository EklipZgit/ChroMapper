using UnityEngine;
using UnityEngine.EventSystems;

public class SongTimelineHandleController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField] private SongTimelineController timeline;
    [SerializeField] private TimelineInputPlaybackController tipc;

    public void OnPointerDown(PointerEventData fucku)
    {
        tipc.PointerDown();
        timeline.IsClicked = true;
        timeline.UpdateSongTimelineSlider(timeline.ClickedSliderValue);
    }

    public void OnPointerUp(PointerEventData fucku)
    {
        // wait, why are we updating this on release?
        // timeline.UpdateSongTimelineSlider();
        timeline.IsClicked = false;
        tipc.PointerUp();
    }
}
