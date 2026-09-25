using UnityEngine;

namespace Beatmap.Shared
{
    // BasicEventFixedDurationParityTest: the 1.44.1 ColorSO assets give a highlight a
    // larger alpha than a steady red/blue light. Keep CM's existing steady-light brightness
    // while reproducing that contrast; its different light materials are not calibrated to
    // the game's absolute ColorSO alpha.
    public static class BasicEventLightIntensity
    {
        private const float NormalAlpha = 0.7490196f;
        private const float BoostNormalAlpha = 0.8f;

        public static float HighlightToNormalRatio(bool isWhite, bool boost) =>
            isWhite ? 1f : 1f / (boost ? BoostNormalAlpha : NormalAlpha);

        public static Color ApplyHighlight(Color color, bool isWhite, bool boost)
        {
            color.a *= HighlightToNormalRatio(isWhite, boost);
            return color;
        }
    }
}
