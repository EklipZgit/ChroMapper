using System;
using UnityEngine;

public class DeleteToolController : MonoBehaviour
{
    public static event Action OnDeleteToolActivated;
    public static bool IsActive { get; private set; }

    public static void UpdateDeletion(bool active)
    {
        IsActive = active;
        OnDeleteToolActivated?.Invoke();
    }

    public void ToggleDeletion() => UpdateDeletion(!IsActive);
}
