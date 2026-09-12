namespace Beatmap.Constants
{
    public static class RingEventHeightConstants
    {
        // Ring zoom: 100% height at this step value; 300% height at 3x this value.
        public const float RingZoomHeightScaleStep = 2f;

        // Ring zoom box height cap as a multiplier of the normal block height.
        public const float RingZoomHeightMaxMultiplier = 3.0f;

        // Ring rotation: 0 degrees -> 0 height; this value -> 100% normal height.
        public const float RingRotationHeightScaleDegrees = 120.0f;

        // Ring rotation box height cap as a multiplier of the normal block height.
        public const float RingRotationHeightMaxMultiplier = 3.0f;
    }
}
