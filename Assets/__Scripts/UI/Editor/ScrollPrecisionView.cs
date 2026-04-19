using System;
using UnityEngine;

public class ScrollPrecisionView : MonoBehaviour
{
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private SliderComponent slider;

    private void Start()
    {
        scrollPrecisionController.OnPrecisionChanged += HandlePrecisionChanged;
        slider
            .WithSliderParams(0, ScrollPrecisionController.MaxPrecision - 1, 1)
            .OnValueChanged(HandleSliderChanged);
        HandlePrecisionChanged(scrollPrecisionController.CurrentPrecision);
    }

    private void OnDestroy() => scrollPrecisionController.OnPrecisionChanged -= HandlePrecisionChanged;

    private void HandlePrecisionChanged(ScrollPrecision precision) => slider.SetValueWithoutNotify((float)precision);

    private void HandleSliderChanged(float value) =>
        scrollPrecisionController.CurrentPrecision = (ScrollPrecision)Mathf.RoundToInt(value);
}
