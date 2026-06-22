using TMPro;
using UnityEngine;

public class Rotation90DegreesWarningController : MonoBehaviour
{
    [SerializeField] private LaneRotationProvider laneRotationProvider;
    [SerializeField] private TextMeshProUGUI rotationDisplay;

    private void Start()
    {
        if (BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic == "90Degree")
            laneRotationProvider.OnPlaybackChanged += HandleRotationChanged;
    }

    private void OnDestroy()
    {
        if (BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic == "90Degree")
            laneRotationProvider.OnPlaybackChanged -= HandleRotationChanged;
    }

    private void HandleRotationChanged(float rotation) =>
        rotationDisplay.color = rotation is < -45f or > 45f ? Color.red : Color.white;
}
