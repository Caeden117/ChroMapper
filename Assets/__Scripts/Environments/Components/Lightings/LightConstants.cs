using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LightConstants
{
    public enum BakeId
    {
        A = 1,
        B,
        C,
        D,
        E,
        F
    }

    public static readonly IReadOnlyList<BakeId> AllBakeIds = Enum.GetValues(typeof(BakeId)).Cast<BakeId>().ToList();
    public const int BaseLightId = 25;
    public const string LightmapLightBakeIdPrefix = "_LightmapLightBakeId";
    public const string LightProbeLightBakeIdPrefix = "_LightProbeLightBakeId";

    public static int GetLightmapLightBakeIdPropertyId(BakeId bakeId) =>
        Shader.PropertyToID($"{LightmapLightBakeIdPrefix}{bakeId}");

    public static int GetLightProbeLightBakeIdPropertyId(BakeId bakeId) =>
        Shader.PropertyToID($"{LightProbeLightBakeIdPrefix}{bakeId}");

    public static int GetComputeFieldPropertyId(string fieldName) => Shader.PropertyToID(fieldName ?? "");
}
