using System;
using UnityEngine;

public class PlacementViewController : MonoBehaviour
{
    [SerializeField] private EditModeContext editModeContext;

    [SerializeField] private GameObject[] gameplayTargets;
    [SerializeField] private GameObject[] basicEventTargets;
    [SerializeField] private GameObject[] glsTargets;

    private void Start() => editModeContext.OnEditModeChanged += HandleEditModeChanged;
    private void OnDestroy() => editModeContext.OnEditModeChanged += HandleEditModeChanged;

    private void HandleEditModeChanged(EditingMode mode)
    {
        foreach (var go in gameplayTargets) go.SetActive(editModeContext.EditingMode.HasFlag(EditingMode.Gameplay));
        foreach (var go in basicEventTargets) go.SetActive(editModeContext.EditingMode.HasFlag(EditingMode.BasicEvent));
        foreach (var go in glsTargets)
        {
            go.SetActive(
                editModeContext.EditingMode.HasFlag(EditingMode.GLS)
                | editModeContext.EditingMode.HasFlag(EditingMode.EventBox));
        }
    }
}
