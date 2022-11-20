using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabEditorData : MonoBehaviour
{
    [Header("This is for simply cloning objects because I don't know reverse engineering..")]
    public Vector3 StartPosition = new Vector3(0, 0, 0);
    public Vector3 PositionDelta = new Vector3(0, 0, 1);
    [Tooltip("Doesn't take affect under RearrangeAll option")]
    public int RepeatTimes = 24;
    [Tooltip("If cloned objects' indices are greater or equal than this range, their light component will be deleted.")]
    public int LightRange = 999;
    public enum CloneOptions
    {
        CloneFirstAndDeleteOthers,
        CloneFirstOnly,
        RearrangeAll,
    }
    public CloneOptions Option;

    public void Apply()
    {
        if (Option == CloneOptions.CloneFirstAndDeleteOthers)
        {
            int i = 0;
            var toDelete = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (i != 0) toDelete.Add(child.gameObject);
                ++i;
            }
            foreach (var child in toDelete)
            {
                DestroyImmediate(child);
            }

        }
        if (Option == CloneOptions.CloneFirstOnly || Option == CloneOptions.CloneFirstAndDeleteOthers)
        {
            Clone(transform.GetChild(0).gameObject);
        }
        RearrangeAll();
    }

    private void Clone(GameObject child)
    {
        for (int i = 0; i < RepeatTimes - 1; ++i)
        {
            var obj = Instantiate(child, transform);
            if (i >= LightRange)
            {
                foreach (var light in obj.GetComponentsInChildren<LightingEvent>())
                {
                    DestroyImmediate(light.gameObject);
                }
            }
        }
    }

    private void RearrangeAll()
    {
        var pos = StartPosition;
        foreach (Transform child in transform)
        {
            child.localPosition = pos;
            pos += PositionDelta;
        }
    }
}
