using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GridOrderController : MonoBehaviour
{
    private static Dictionary<int, List<GridChild>> allChilds = new Dictionary<int, List<GridChild>>();

    private static bool dirty = false;

    [SerializeField] private GridRotationController gridRotationController;

    public static int GetSizeForOrder(int order)
    {
        if (allChilds.TryGetValue(order, out var childs))
        {
            return Mathf.CeilToInt(childs.Any() ? childs.Max(x => x.Size) : 0);
        }
        return 0;
    }

    public static void RegisterChild(GridChild child)
    {
        if (allChilds.TryGetValue(child.Order, out var grids))
        {
            grids.Add(child);
        }
        else
        {
            allChilds[child.Order] = new List<GridChild>() { child };
            RefreshChildDictionary();
        }
    }

    public static void DeregisterChild(GridChild child)
    {
        if (allChilds.TryGetValue(child.Order, out var grids))
        {
            grids.Remove(child);
            if (grids.Count == 0)
            {
                allChilds.Remove(child.Order);
                RefreshChildDictionary();
            }
        }
    }

    public static void MarkDirty() => dirty = true;

    public static void RefreshChildDictionary()
    {
        allChilds = allChilds.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);
        MarkDirty();
    }

    private void Start()
    {
        gridRotationController.ObjectRotationChangedEvent += MarkDirty;
    }

    private void OnDestroy()
    {
        gridRotationController.ObjectRotationChangedEvent -= MarkDirty;
    }

    private void LateUpdate()
    {
        if (!dirty) return; //We do not want this updating every frame so that's why we have a Dirty system.
        dirty = false;
        float childX = 0;
        if (allChilds.Any(x => x.Key < 0))
        {
            if (allChilds.TryGetValue(0, out var centerGridChilds))
            {
                childX -= centerGridChilds.Max(x => x.Size);
            }

            childX -= 1;
            foreach (var kvp in allChilds.Where(x => x.Key < 0))
            {
                childX -= Mathf.Ceil(kvp.Value.Max(x => x.Size));
                childX -= 1;
            }
        }
        foreach (var kvp in allChilds)
        {
            if (kvp.Key == 0 || (kvp.Key > 0 && childX < 0))
            {
                childX = 0;
            }
            kvp.Value.RemoveAll(x => x == null);
            foreach (GridChild child in kvp.Value)
            {
                child.transform.eulerAngles = new Vector3(child.transform.eulerAngles.x, transform.eulerAngles.y,
                        child.transform.eulerAngles.z);
                float x = childX + child.LocalOffset.x;
                Vector3 side = transform.right.normalized * x;
                Vector3 up = transform.up.normalized * child.LocalOffset.y;
                Vector3 forward = transform.forward.normalized * child.LocalOffset.z;
                Vector3 total = side + up + forward;
                child.transform.position = transform.position + total;
            }
            childX += Mathf.Ceil(kvp.Value.Any() ? kvp.Value.Max(x => x.Size) + 1: 0);
        }
    }
}
