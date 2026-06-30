using TMPro;
using UnityEngine;

public class Rotation90DegreesWarningController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rotationDisplay;

    public void HandleRotationChanged(float rotation)
    {
        if (BeatSaberSongContainer.Instance.MapDifficultyInfo.Characteristic == "90Degree")
            rotationDisplay.color = rotation is < -45f or > 45f ? Color.red : Color.white;
    }
}
