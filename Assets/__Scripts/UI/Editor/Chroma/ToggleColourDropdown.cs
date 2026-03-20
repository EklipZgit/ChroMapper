using UnityEngine;
using UnityEngine.Serialization;

public class ToggleColourDropdown : MonoBehaviour
{
    [FormerlySerializedAs("ColourDropdown")] [SerializeField] private RectTransform colourDropdown;
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
