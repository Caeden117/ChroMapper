using UnityEngine;

namespace Beatmap.Shared
{
    public static class BasicEventLightIntensity
    {
        // The shipped 1.44.1 normal ColorSO assets multiply alpha before dispatch to every light adapter.
        private const float NormalAlpha = 0.7490196f;
        private const float BoostNormalAlpha = 0.8f;

        public static Color ApplyNative(Color color, bool isWhite, bool boost, bool isHighlight)
        {
            if (!isWhite && !isHighlight)
                color.a *= boost ? BoostNormalAlpha : NormalAlpha;

            return color;
        }
    }
}
