using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RightButtonPanel : MonoBehaviour
{
    [SerializeField] private FlyoutPanelController flyoutPanelController;

    private bool isActive;

    public void TogglePanel()
    {
        isActive = !isActive;

        if (isActive)
        {
            flyoutPanelController.Open();
        }
        else
        {
            flyoutPanelController.Close();
        }
    }
}
