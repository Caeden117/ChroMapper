using SimpleJSON;

// BeatToTheFuture and ChroMapper must interpret the map opt-in identically so an old-speed preview does not diverge from gameplay.
public static class RingPropagationCompatibility
{
    public const float OldMappedVisualMultiplier = 0.6f;
    public const string MappedForOldPropagationKey = "mappedForOldRingPropagationSpeed";
    public const string V2MappedForOldPropagationKey = "_mappedForOldRingPropagationSpeed";

    public static bool HasOldPropagationDeclaration(JSONNode customData) =>
        IsTrue(customData, MappedForOldPropagationKey)
        || IsTrue(customData, V2MappedForOldPropagationKey);

    public static bool IsMappedForOldPropagation(
        JSONNode difficultyCustomData,
        JSONNode levelCustomData,
        JSONNode beatmapFileCustomData) =>
        HasOldPropagationDeclaration(difficultyCustomData)
        || HasOldPropagationDeclaration(levelCustomData)
        || HasOldPropagationDeclaration(beatmapFileCustomData);

    public static float ApplyVisualMultiplier(
        float propagation,
        bool hasExplicitPropagation,
        bool mappedForOldPropagation) =>
        hasExplicitPropagation && mappedForOldPropagation
            ? propagation * OldMappedVisualMultiplier
            : propagation;

    private static bool IsTrue(JSONNode customData, string key) =>
        customData != null
        && customData.HasKey(key)
        && customData[key].AsBool;
}
