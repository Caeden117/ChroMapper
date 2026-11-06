// Remove this helper and return callers to regular Action events once Unity's Mono avoids expensive
// invocation-list copies on subscription changes. Confirm that with a dense-map allocation profile first.
//
// Adapted from TMPro.FastAction in Unity uGUI.
// uGUI Copyright © 2015-2020 Unity Technologies ApS.
// Licensed under the Unity Companion License for Unity-dependent projects:
// https://unity3d.com/legal/licenses/unity_companion_license

using System;
using System.Collections.Generic;

public sealed class FastAction
{
    private readonly LinkedList<Action> callbacks = new();
    private readonly Dictionary<Action, LinkedListNode<Action>> callbackNodes;
    private readonly Stack<LinkedListNode<Action>> availableNodes;

    public FastAction(int capacity = 0)
    {
        availableNodes = new Stack<LinkedListNode<Action>>(capacity);
        callbackNodes = new Dictionary<Action, LinkedListNode<Action>>((int)Math.Ceiling(capacity * 1.5));

        // LinkedList cannot reserve capacity, so reserve its nodes separately for pooled beatmap objects.
        for (var index = 0; index < capacity; index++)
            availableNodes.Push(new LinkedListNode<Action>(null));
    }

    public void Add(Action callback)
    {
        if (callbackNodes.ContainsKey(callback))
            return;

        var node = availableNodes.Count > 0
            ? availableNodes.Pop()
            : new LinkedListNode<Action>(null);
        node.Value = callback;
        callbacks.AddLast(node);
        callbackNodes.Add(callback, node);
    }

    public void Remove(Action callback)
    {
        if (callbackNodes.TryGetValue(callback, out var node))
        {
            callbackNodes.Remove(callback);
            callbacks.Remove(node);
            node.Value = null;
            availableNodes.Push(node);
        }
    }

    public void Call()
    {
        var node = callbacks.First;
        while (node != null)
        {
            node.Value();
            node = node.Next;
        }
    }
}
