using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DisableActionsField : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private static readonly Type[] whitelistTypes = { typeof(CMInput.IDialogBoxActions) };

    private static readonly Type[] interfaceTypes =
        typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface && !whitelistTypes.Contains(x)).ToArray();

    public void OnDeselect(BaseEventData eventData) => OnDeselect();
    public void OnSelect(BaseEventData eventData) => OnSelect();
    private void OnDestroy() => OnDeselect();

    public void OnSelect() => StartCoroutine(WaitToEnable());
    public void OnDeselect() => CMInputCallbackInstaller.ClearDisabledActionMaps(GetType(), interfaceTypes);

    private IEnumerator WaitToEnable()
    {
        yield return new WaitForEndOfFrame();
        CMInputCallbackInstaller.DisableActionMaps(GetType(), interfaceTypes);
    }
}
