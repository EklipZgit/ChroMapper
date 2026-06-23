using TMPro;
using UnityEngine;

public class RotationDisplayController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private LaneRotationProvider laneRotationProvider;
    [SerializeField] private Rotation90DegreesWarningController rotation90DegreesWarningController;
    [SerializeField] private TextMeshProUGUI display;

    // Start is called before the first frame update
    private void Start()
    {
        atsc.OnPlayToggled += HandlePlayToggled;
        laneRotationProvider.OnEditChanged += HandleEditRotationChanged;
        laneRotationProvider.OnPlaybackChanged += HandleRotationChanged;
    }

    private void OnDestroy()
    {
        atsc.OnPlayToggled -= HandlePlayToggled;
        laneRotationProvider.OnEditChanged -= HandleEditRotationChanged;
        laneRotationProvider.OnPlaybackChanged -= HandleRotationChanged;
    }

    private void HandlePlayToggled(bool toggle)
    {
        if (toggle)
            SetText(laneRotationProvider.PlaybackRotation);
        else
        {
            SetText(
                BeatSaberSongContainer.Instance.Map.MajorVersion < 4
                    ? laneRotationProvider.PlaybackRotation
                    : laneRotationProvider.EditRotation);
        }
    }

    private void HandleEditRotationChanged(float rotation)
    {
        if (atsc.IsPlaying || BeatSaberSongContainer.Instance.Map.MajorVersion < 4) return;
        SetText(rotation);
    }

    private void HandleRotationChanged(float rotation)
    {
        if (!atsc.IsPlaying || (!atsc.IsPlaying && BeatSaberSongContainer.Instance.Map.MajorVersion >= 4)) return;
        SetText(rotation);
    }

    private void SetText(float rotation)
    {
        rotation90DegreesWarningController.HandleRotationChanged(rotation);
        display.text = Settings.Instance.Reset360DisplayOnCompleteTurn
            ? $"{BetterModulo(rotation, 360)}°"
            : $"{rotation}°";
    }

    private static float BetterModulo(float x, float m) => ((x % m) + m) % m; //thanks stackoverflow
}
