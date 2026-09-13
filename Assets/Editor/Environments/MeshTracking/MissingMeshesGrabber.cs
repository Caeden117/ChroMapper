using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class MissingMeshesGrabber
{
    private const string editorPath = "Assets/Editor/Environments";

    // Filter V2 and V3 envs
    private static readonly string[] V2Envs = new[]
    {
        "BigMirrorEnvironment",
        "BillieEnvironment",
        "BTSEnvironment",
        "CrabRaveEnvironment",
        "DefaultEnvironment",
        "DragonsEnvironment",
        "FitBeatEnvironment",
        "GagaEnvironment",
        "GreenDayEnvironment",
        "GreenDayGrenadeEnvironment",
        "HalloweenEnvironment",
        "InterscopeEnvironment",
        "KaleidoscopeEnvironment",
        "KDAEnvironment",
        "LinkinParkEnvironment",
        "MonstercatEnvironment",
        "NiceEnvironment",
        "OriginsEnvironment",
        "PanicEnvironment",
        "RocketEnvironment",
        "SkrillexEnvironment",
        "TimbalandEnvironment",
        "TriangleEnvironment"
    };

    private static readonly string[] V3Envs = new[]
    {
        "BritneyEnvironment",
        "ColliderEnvironment",
        "DaftPunkEnvironment",
        "Dragons2Environment",
        "EDMEnvironment",
        "HipHopEnvironment",
        "LatticeEnvironment",
        "LinkinPark2Environment",
        "LizzoEnvironment",
        "MetallicaEnvironment",
        "Monstercat2Environment",
        "Panic2Environment",
        "PyroEnvironment",
        "QueenEnvironment",
        "RockMixtapeEnvironment",
        "TheRollingStonesEnvironment",
        "TheSecondEnvironment",
        "TheWeekndEnvironment",
        "WeaveEnvironment"
    };

    [MenuItem("Environment/Tools/Gather V2 Missing Meshes")]
    public static void GatherV2() => GatherMissingMeshes(false);

    [MenuItem("Environment/Tools/Gather V3 Missing Meshes")]
    public static void GatherV3() => GatherMissingMeshes(true);

    private static void GatherMissingMeshes(bool isV3)
    {
        var library =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(PathUtils.Combine(editorPath, "EnvironmentLibrarySO.asset"));

        List<JsonMesh> meshes = new();
        foreach (var meshInfo in library.Meshes.list)
        {
            if (!meshInfo.Environments.Any(x => isV3 ? V3Envs.Contains(x) : V2Envs.Contains(x))
                || meshInfo.Mesh != null)
                continue;

            meshes.Add(
                new JsonMesh
                {
                    Hash = meshInfo.Hash,
                    Names = meshInfo.Names,
                    Envs = meshInfo.Environments,
                    BoundsCenter = ToFloatArr(meshInfo.BoundsCenter),
                    BoundsSize = ToFloatArr(meshInfo.BoundsSize),
                    elementId = library.Meshes.list.IndexOf(meshInfo)
                });
        }

        // Write missing meshes data to file
        using (var stream = new FileStream(
            PathUtils.Combine(editorPath, "MeshTracking", "MissingMeshes.json"),
            FileMode.Create))
        {
            using (var writer = new StreamWriter(stream))
                writer.Write(JsonConvert.SerializeObject(meshes, Formatting.Indented));
        }
    }

    private class JsonMesh
    {
        public string Hash;
        public List<string> Names;
        public List<string> Envs;
        public float[] BoundsSize;
        public float[] BoundsCenter;
        public int elementId; // The element index of the mesh in the Mesh library
    }

    private static float[] ToFloatArr(Vector3 arr) => new float[3] { arr.x, arr.y, arr.z };
}
