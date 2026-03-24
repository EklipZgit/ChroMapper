using UnityEngine;

public class StrobeGeneratorUIDropdown : MonoBehaviour
{
    [SerializeField] private FlyoutPanelController flyoutPanelController;

    public bool IsActive;

    public void ToggleDropdown(bool visible)
    {
        IsActive = visible;
        
        if (IsActive)
        {
            flyoutPanelController.Open();
        }
        else
        {
            flyoutPanelController.Close();
        }
    }
}
