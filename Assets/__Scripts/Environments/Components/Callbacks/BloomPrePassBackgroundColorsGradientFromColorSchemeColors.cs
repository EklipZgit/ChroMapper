using System;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientFromColorSchemeColors : MonoBehaviour
{
    [SerializeField] public ColorBoostEffect Effect;
    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;

    [SerializeField] public BloomPrePassBackgroundColorsGradient BloomPrePassBackgroundColorsGradient;
    [SerializeField] public Element[] Elements;

    protected void Start()
    {
        Effect.OnStateChanged += HandleColorBoostChanged;
        SetColorsToElements();
    }

    protected void OnDestroy() => Effect.OnStateChanged -= HandleColorBoostChanged;

    private void HandleColorBoostChanged(bool boost) => SetColorsToElements();

    private void SetColorsToElements()
    {
        for (var i = 0; i < BloomPrePassBackgroundColorsGradient.Elements.Length && i < Elements.Length; i++)
        {
            if (Elements[i].LoadFromColorScheme)
            {
                Elements[i].Color = Elements[i].EnvironmentColor switch
                {
                    EnvironmentColor.Color0 => ColorSchemeProvider.ColorScheme.EnvironmentLeftColor
                        * Elements[i].Intensity,
                    EnvironmentColor.Color1 => ColorSchemeProvider.ColorScheme.EnvironmentRightColor
                        * Elements[i].Intensity,
                    EnvironmentColor.Color0Boost => ColorSchemeProvider.ColorScheme.EnvironmentLeftBoostColor
                        * Elements[i].Intensity,
                    EnvironmentColor.Color1Boost => ColorSchemeProvider.ColorScheme.EnvironmentRightBoostColor
                        * Elements[i].Intensity,
                    _ => Elements[i].Color
                };
            }

            BloomPrePassBackgroundColorsGradient.Elements[i].Color = Elements[i].Color;
        }

        BloomPrePassBackgroundColorsGradient.UpdateGradientTexture();
    }

    [Serializable]
    public class Element
    {
        public bool LoadFromColorScheme;
        public EnvironmentColor EnvironmentColor;
        public float Intensity;
        public Color Color;
    }

    public enum EnvironmentColor
    {
        Color0,
        Color1,
        Color0Boost,
        Color1Boost
    }
}
