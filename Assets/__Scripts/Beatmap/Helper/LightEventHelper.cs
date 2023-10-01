using Beatmap.Enums;

public static class LightEventHelper
{
    public static bool IsBlueFromValue(int value) =>
        value == (int)LightValue.BlueOn || value == (int)LightValue.BlueFlash ||
        value == (int)LightValue.BlueFade || value == (int)LightValue.BlueTransition;
}
