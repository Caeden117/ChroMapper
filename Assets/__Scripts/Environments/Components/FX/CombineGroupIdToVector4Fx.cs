using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombineGroupIdToVector4Fx : FxTarget
{
    [SerializeField] public string PropertyName;
    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public Vector4 DefaultValue;

    [SerializeField] public LightGroupToIndex[] LightGroupsToIndices;

    private bool didReceiveEventThisFrame;
    private Dictionary<int, int> groupIdToIndex;
    private int propertyId;
    private Vector4 data;

    private void Awake()
    {
        propertyId = Shader.PropertyToID(PropertyName);
        groupIdToIndex = LightGroupsToIndices.ToDictionary(
            item => item.GroupId,
            item => item.Index);
        data = DefaultValue;
    }

    public override void SetValue(int group, int id, float value)
    {
        if (!groupIdToIndex.TryGetValue(group, out var value2)) return;
        data[value2] = value;
        didReceiveEventThisFrame = true;
    }

    public override void TriggerValue(int group, int id, float value)
    {
        if (!groupIdToIndex.TryGetValue(group, out var value2)) return;
        data[value2] = value;
        didReceiveEventThisFrame = true;
    }

    private void HandleBeatmapCallbacksControllerDidProcessAllCallbacksThisFrame()
    {
        if (!didReceiveEventThisFrame) return;
        didReceiveEventThisFrame = false;
        MpbController.Mpb.SetVector(propertyId, data);
        MpbController.ApplyChanges();
    }

    [Serializable]
    public struct LightGroupToIndex
    {
        public int GroupId;
        public int Index;
    }
}
