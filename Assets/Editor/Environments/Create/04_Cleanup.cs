using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class EnvironmentSceneCreator
{
    private static void Cleanup(Scene scene, EnvironmentData data)
    {
        TraverseAndRemove(scene.GetRootGameObjects(), data);
        var descriptor = GameObject.Find("Environment").GetComponent<EnvironmentDescriptor>();
        descriptor.ChromaIDMarkers = scene
            .GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<ChromaIDMarker>(true))
            .ToList();
    }

    private static void TraverseAndRemove(GameObject[] gos, EnvironmentData data)
    {
        foreach (var go in gos)
        {
            TraverseAndRemove(GetChildren(go), data);
            CheckParametricAndRemove(go, data);
            if ((go.TryGetComponent<ChromaIDMarker>(out var cim) && cim.MarkUse)
                || !go.GetComponents<Component>().All(x => x is Transform or ChromaIDMarker)
                || go.transform.childCount > 0)
                continue;

            Object.DestroyImmediate(go);
        }

        return;

        // messy enumerator, wcyd
        GameObject[] GetChildren(GameObject go)
        {
            var objects = new List<GameObject>();
            var c = go.transform.childCount;
            for (var i = 0; i < c; i++) objects.Add(go.transform.GetChild(i).gameObject);
            return objects.ToArray();
        }
    }

    private static void CheckParametricAndRemove(GameObject go, EnvironmentData data)
    {
        var marker = go.GetComponent<ChromaIDMarker>();
        if (marker == null) return;

        var envObject = data.Objects.First(d => d.ChromaID == marker.ChromaID);

        if (envObject.Components.ParametricBoxController != null && go.GetComponent<ParametricBoxLight>() == null)
        {
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        }
        else if (envObject.Components.Parametric3SliceSpriteController != null
            && go.GetComponent<ParametricSpriteLight>() == null)
        {
            Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        }
    }
}
