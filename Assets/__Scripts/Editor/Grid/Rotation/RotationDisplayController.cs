using TMPro;
using UnityEngine;

public class RotationDisplayController : MonoBehaviour
{
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private TextMeshProUGUI display;

    // Start is called before the first frame update
    private void Start()
    {
        gameObject.SetActive(rotationCallback.IsActive);
        rotationCallback.OnRotationChanged += OnRotationChanged;
    }

    private void OnDestroy() => rotationCallback.OnRotationChanged -= OnRotationChanged;

    private void OnRotationChanged(bool natural, float rotation)
    {
        if (Settings.Instance.Reset360DisplayOnCompleteTurn)
            display.text = $"{BetterModulo(rotation, 360)}°";
        else
            display.text = $"{rotation}°";
    }

    private float BetterModulo(float x, float m) => ((x % m) + m) % m; //thanks stackoverflow
}
