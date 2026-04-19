using System;
using UnityEngine;

public class ToggleableViewController : MonoBehaviour
{
    [SerializeField] private GameObject[] showTargets = Array.Empty<GameObject>();
    [SerializeField] private GameObject[] extendTargets = Array.Empty<GameObject>();

    public void Show(bool active)
    {
        foreach (var showTarget in showTargets) showTarget.SetActive(active);
    }

    public void Extend(bool active)
    {
        foreach (var extendTarget in extendTargets) extendTarget.SetActive(active);
    }
}
