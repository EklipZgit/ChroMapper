using System;
using UnityEngine;
using UnityEngine.UI;

public class EditModeViewController : MonoBehaviour
{
    [SerializeField] private EditModeContext editContext;
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private EnumPicker enumPicker;

    private void Start()
    {
        enumPicker.Initialize(typeof(EditingModeNoFlag));
        enumPicker.OnClick += HandleOnClick;
        editContext.OnEditModeChanged += HandleEditModeChanged;
        enumPicker.Select(Enum.Parse<EditingModeNoFlag>(editContext.EditingMode.ToString()));
    }

    private void OnDestroy()
    {
        enumPicker.OnClick -= HandleOnClick;
        editContext.OnEditModeChanged -= HandleEditModeChanged;
    }

    private void HandleOnClick(Enum enumMode) =>
        editContext.EditingMode = (EditingMode)(1 << (int)(EditingModeNoFlag)enumMode);

    private void HandleEditModeChanged(EditingMode obj) =>
        enumPicker.Select(Enum.Parse<EditingModeNoFlag>(editContext.EditingMode.ToString()));
}
