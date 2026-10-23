using System.Collections.Generic;
using Beatmap.Containers;
using UnityEngine;

public class ModelUpdateQueueManager : MonoBehaviour
{
    private readonly HashSet<ObjectContainer> containerQueue = new();

    private void LateUpdate() => Flush();

    /// <summary>Applies pending model replacements before the owning bundle is unloaded.</summary>
    public void Flush()
    {
        if (containerQueue.Count == 0) return;

        foreach (var obj in containerQueue)
            if (obj != null) obj.HandleModelChanged();
        containerQueue.Clear();
    }

    public void Add(ObjectContainer container) => containerQueue.Add(container);
}
