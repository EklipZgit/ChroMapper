using UnityEngine;

public class GLSColorShiftRowView : MonoBehaviour
{
    [SerializeField] private DropdownComponent targetDropdown;
    [SerializeField] private TextBoxFloatComponent valueInput;
    [SerializeField] private DropdownComponent easingDropdown;
    [SerializeField] private ToggleComponent lightToggle;
    [SerializeField] private ButtonComponent removeButton;

    public DropdownComponent TargetDropdown => targetDropdown;
    public TextBoxFloatComponent ValueInput => valueInput;
    public DropdownComponent EasingDropdown => easingDropdown;
    public ToggleComponent LightToggle => lightToggle;
    public ButtonComponent RemoveButton => removeButton;
}
