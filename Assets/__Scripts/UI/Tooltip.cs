using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Serialization;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [FormerlySerializedAs("tooltip")] public LocalizedString LocalizedTooltip;

    [FormerlySerializedAs("tooltipOverride")] [HideInInspector] public string TooltipOverride;

    [FormerlySerializedAs("advancedTooltip")] public string AdvancedTooltip;

    public float AppearDelay;

    public bool TooltipActive;

    [Tooltip("Action map name to look up the hotkey from (e.g. 'Edit Mode')")]
    [SerializeField] public string HotkeyActionMap;

    [Tooltip("Input action name within the map to display as a hotkey hint (e.g. 'GLSEdit')")]
    [SerializeField] public string HotkeyActionName;

    private Coroutine routine;

    private void OnDisable() => OnPointerExit(null);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (routine == null)
            routine = StartCoroutine(TooltipRoutine(AppearDelay));

        TooltipActive = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        PersistentUI.Instance.HideTooltip();
        TooltipActive = false;
    }

    private IEnumerator TooltipRoutine(float timeToWait)
    {
        var tooltipTextResult = TooltipOverride;
        if (string.IsNullOrEmpty(TooltipOverride)) 
            tooltipTextResult = LocalizedTooltip.GetLocalizedString();

        var hotkey = GetHotkeyDisplayString();
        if (!string.IsNullOrEmpty(hotkey))
            tooltipTextResult = $"{tooltipTextResult} [{hotkey}]";

        PersistentUI.Instance.SetTooltip(tooltipTextResult, AdvancedTooltip);
        yield return new WaitForSeconds(timeToWait);
        PersistentUI.Instance.ShowTooltip();
    }

    private string GetHotkeyDisplayString()
    {
        if (string.IsNullOrEmpty(HotkeyActionName)) 
            return null;
        var input = CMInputCallbackInstaller.InputInstance;
        if (input == null)
            return null;

        InputAction action = null;
        if (!string.IsNullOrEmpty(HotkeyActionMap))
        {
            var map = input.asset.FindActionMap(HotkeyActionMap);
            action = map?.FindAction(HotkeyActionName);
        }
        else
        {
            action = input.asset.FindAction(HotkeyActionName);
        }

        if (action == null) 
            return null;

        var displayString = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
        return string.IsNullOrEmpty(displayString) ? null : displayString;
    }
}
