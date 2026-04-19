using UnityEngine;

public class ToggleColourDropdown : MonoBehaviour
{
    [SerializeField] private RectTransform colourDropdown;
    [SerializeField] private FlyoutPanelController flyoutPanelController;

    public bool Visible;

    public void ToggleDropdown(bool visible)
    {
        Visible = visible;

        if (Visible)
        {
            flyoutPanelController.Open();
        }
        else
        {
            flyoutPanelController.Close();
        }
    }
}
