using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class BPMTapperController : MonoBehaviour, CMInput.IBPMTapperActions
{
    public static bool IsActive;
    
    [FormerlySerializedAs("_bpmText")] [SerializeField] private TextMeshProUGUI bpmText;
    [SerializeField] private FlyoutPanelController flyoutPanelController;

    private readonly List<float> taps = new List<float>();

    private bool isTapping;
    private float t1;

    private float timeSinceLastTap;

    public void Reset()
    {
        isTapping = false;
        StopAllCoroutines();
        bpmText.text = "Tap...";
        taps.Clear();
    }

    private void Start() => bpmText.text = "";

    public void OnToggleBPMTapper(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (UIMode.SelectedMode != UIModeType.Normal) return;

            flyoutPanelController.Open();
        }
    }

    public void Close() => flyoutPanelController.Close();

    public void Tap()
    {
        timeSinceLastTap = 0;
        if (!isTapping)
        {
            isTapping = true;

            StartCoroutine(WaitForReset());

            bpmText.text = "Tap...";

            t1 = Time.time;
        }
        else
        {
            var dist = Time.time - t1;
            t1 = Time.time;
            taps.Add(dist);
            bpmText.text = Math.Round(CalculateBpm(), 2).ToString();
        }
    }

    private float CalculateBpm()
    {
        var avg = taps.Average();
        return 1000 / (avg * 1000) * 60;
    }

    private IEnumerator WaitForReset()
    {
        while (timeSinceLastTap < 3)
        {
            timeSinceLastTap += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        Reset();
    }
}
