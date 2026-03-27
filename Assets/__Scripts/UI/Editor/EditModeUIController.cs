using System;
using UnityEngine;
using UnityEngine.UI;

public class EditModeUIController : MonoBehaviour
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
        editContext.EditingMode = (EditingMode)(1 << ((int)(EditingModeNoFlag)enumMode - 1));

    private void HandleEditModeChanged(EditingMode obj) =>
        enumPicker.Select(Enum.Parse<EditingModeNoFlag>(editContext.EditingMode.ToString()));
}
