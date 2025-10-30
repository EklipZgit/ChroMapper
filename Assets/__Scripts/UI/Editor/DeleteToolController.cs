using System;
using UnityEngine;

public class DeleteToolController : MonoBehaviour
{
    public static event Action OnDeleteToolActivated;
    public static bool IsActive { get; private set; }

    public void UpdateDeletion(bool enabled)
    {
        IsActive = enabled;
        if (enabled) OnDeleteToolActivated?.Invoke();
    }

    public void ToggleDeletion() => UpdateDeletion(!IsActive);
}
