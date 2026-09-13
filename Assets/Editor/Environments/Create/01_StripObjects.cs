using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public partial class EnvironmentSceneCreator
{
    private static Dictionary<string, GameObject> StripObjects(Scene scene, EnvironmentData data)
    {
        var existingObjects = new Dictionary<string, GameObject>();
        var validObjects = data.Objects.Select(x => x.ChromaID).ToHashSet();
        TraverseAndStrip(scene.GetRootGameObjects());

        return existingObjects;

        void TraverseAndStrip(GameObject[] gos)
        {
            foreach (var go in gos)
            {
                var marker = go.GetComponent<ChromaIDMarker>();
                if (marker == null || !validObjects.Contains(marker.ChromaID))
                {
                    Object.DestroyImmediate(go);
                    continue;
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                foreach (var component in go.GetComponents<Component>().Reverse())
                {
                    if (component is not (Transform or ChromaIDMarker)) Object.DestroyImmediate(component);
                }

                existingObjects.Add(marker.ChromaID, go);
                TraverseAndStrip(GetChildren(go));
            }
        }

        // messy enumerator, wcyd
        GameObject[] GetChildren(GameObject go)
        {
            var objects = new List<GameObject>();
            var c = go.transform.childCount;
            for (var i = 0; i < c; i++) objects.Add(go.transform.GetChild(i).gameObject);
            return objects.ToArray();
        }
    }
}
