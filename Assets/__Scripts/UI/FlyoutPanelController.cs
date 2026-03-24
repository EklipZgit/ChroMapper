using System.Collections;
using UnityEngine;

// im TIRED of the same implementation copy/pasted, so here we are
public class FlyoutPanelController : MonoBehaviour
{
    // yeah im caching that shit for micro-optimization purposes, fight me
    private static readonly WaitForEndOfFrame endOfFrame = new();

    [SerializeField] private RectTransform panel;

    [Tooltip("The offset from the closed position to the open position. The closed position is the anchored position of the panel in the editor.")]
    [SerializeField] private Vector2 openOffset;

    [SerializeField] private float flyoutDuration = 0.4f;

    private Canvas parentCanvas; // For optimization purposes, we disable the canvas when the panel is closed, so we need to cache it
    private Vector2 startingAnchoredPosition;
    private Vector2 endingAnchoredPosition;
    private Coroutine activeCoroutine;

    public void Open()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        activeCoroutine = StartCoroutine(UpdateGroup(true));
    }

    public void Close()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }

        activeCoroutine = StartCoroutine(UpdateGroup(false));
    }

    public void UpdateEndingOffset(Vector2 newOffset) => endingAnchoredPosition = startingAnchoredPosition + newOffset;

    private void Start()
    {
        parentCanvas = panel.GetComponentInParent<Canvas>();
        startingAnchoredPosition = panel.anchoredPosition;
        endingAnchoredPosition = startingAnchoredPosition + openOffset;

        // Start with the panel closed
        panel.anchoredPosition = startingAnchoredPosition;
        parentCanvas.enabled = false;
    }

    private IEnumerator UpdateGroup(bool enabled)
    {
        var dest = enabled ? endingAnchoredPosition : startingAnchoredPosition;
        var og = enabled ? startingAnchoredPosition : endingAnchoredPosition;
        var t = 0f;

        // Set initial state - show canvas if opening
        panel.anchoredPosition = og;
        if (enabled) parentCanvas.enabled = true;

        while (t < flyoutDuration)
        {
            t += Time.deltaTime;

            // Some easing was apparent in the original version of the flyout, so I added a quintic ease here.
            panel.anchoredPosition = Vector2.Lerp(og, dest, Easing.Quintic.InOut(t / flyoutDuration));

            yield return endOfFrame;
        }

        // Set final state - hide canvas if closing
        panel.anchoredPosition = dest;
        if (!enabled) parentCanvas.enabled = false;
    }
}
