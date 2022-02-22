public static class SpawnParameterHelper
{
    public static float CalculateHalfJumpDuration(float noteJumpSpeed, float startBeatOffset, float bpm) {
        var halfJumpDuration = 4f;
        var num = 60 / bpm;

        while (noteJumpSpeed * num * halfJumpDuration > 17.999f)
            halfJumpDuration /= 2;

        halfJumpDuration += startBeatOffset;

        if (halfJumpDuration < 0.25f) halfJumpDuration = 0.25f;

        return halfJumpDuration;
    }

    public static float CalculateJumpDistance(float noteJumpSpeed, float startBeatOffset, float bpm)
    {
        var num = 60 / bpm;
        return CalculateHalfJumpDuration(noteJumpSpeed, startBeatOffset, bpm) * num * noteJumpSpeed * 2;
    }
}
