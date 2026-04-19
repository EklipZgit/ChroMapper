using UnityEngine;

public class HideOnEditModeChange : MonoBehaviour
{
    [SerializeField] private EditModeContext editModeContext;
    [SerializeField] private GameObject target;
    [SerializeField] private EditingMode visible;

    private void Start()
    {
        editModeContext.OnEditModeChanged += HandleEditModeChanged;
        HandleEditModeChanged(editModeContext.EditingMode);
    }

    private void OnDestroy() => editModeContext.OnEditModeChanged -= HandleEditModeChanged;

    private void HandleEditModeChanged(EditingMode mode) => target.SetActive((visible & mode) > 0);
}
