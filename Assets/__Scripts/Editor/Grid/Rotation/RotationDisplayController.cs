using TMPro;
using UnityEngine;

public class RotationDisplayController : MonoBehaviour
{
    [SerializeField] private LaneRotationProvider laneRotationProvider;
    [SerializeField] private TextMeshProUGUI display;

    // Start is called before the first frame update
    private void Start()
    {
        laneRotationProvider.OnPlaybackChanged += HandleRotationChanged;
    }

    private void OnDestroy() => laneRotationProvider.OnPlaybackChanged -= HandleRotationChanged;

    private void HandleRotationChanged(float rotation)
    {
        display.text = Settings.Instance.Reset360DisplayOnCompleteTurn
            ? $"{BetterModulo(rotation, 360)}°"
            : $"{rotation}°";
    }

    private static float BetterModulo(float x, float m) => ((x % m) + m) % m; //thanks stackoverflow
}
