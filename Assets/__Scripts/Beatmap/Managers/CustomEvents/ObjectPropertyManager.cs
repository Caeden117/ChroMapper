using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class ObjectPropertyManager
{
    public InstantiateObjectPrefabManager InstantiateObjectPrefabManager;
    public VivifyAssetBundleManager VivifyAssetBundleManager;
    public CustomEventPropertyTimeline PropertyTimeline;

    private readonly List<TriggerHistory> triggerHistories = new();
    public readonly VivifyTextureBindings TextureBindings = new();

    public void AssignMaterial(CustomEventStateData state)
    {
        var data = state.Base.Data;
        if (!VivifyAssetBundleManager.AssetPathToMaterial.TryGetValue(data["asset"], out var material)) return;
        var easing = CustomEventValueParser.GetEasing(data);

        foreach (var child in data["properties"].Children)
        {
            var id = (string)child["id"];
            var propertyId = Shader.PropertyToID(id);
            var key = "property:" + propertyId;
            var value = child["value"];
            switch ((string)child["type"])
            {
                case "Texture":
                    var texture = VivifyAssetBundleManager.AssetPathToTexture.GetValueOrDefault(value);
                    PropertyTimeline.Register(state, material, key,
                        () => material.GetTexture(propertyId), v => material.SetTexture(propertyId, v), _ => texture);
                    break;
                case "Keyword":
                    var keyword = id;
                    var keywordValue = CustomEventValueParser.Float(value, allowBoolean: true);
                    if (keywordValue == null) break;
                    PropertyTimeline.Register(state, material, "keyword:" + keyword,
                        () => material.IsKeywordEnabled(keyword),
                        enabled =>
                        {
                            if (enabled) material.EnableKeyword(keyword);
                            else material.DisableKeyword(keyword);
                        }, t => keywordValue(t) >= 1f, easing);
                    break;
                case "Float":
                    RegisterFloat(state, material, key, value,
                        () => material.GetFloat(propertyId), v => material.SetFloat(propertyId, v), easing);
                    break;
                case "Vector":
                    var vector = PointDefinitionParser.Get(value);
                    if (vector != null)
                        PropertyTimeline.Register(state, material, key,
                            () => material.GetVector(propertyId), v => material.SetVector(propertyId, v),
                            vector.GetVector4, easing);
                    break;
                case "Color":
                    var color = CustomEventValueParser.Color(value);
                    if (color != null)
                        PropertyTimeline.Register(state, material, key,
                            () => material.GetColor(propertyId), v => material.SetColor(propertyId, v),
                            color, easing);
                    break;
            }
        }
    }

    public void AssignGlobal(CustomEventStateData state)
    {
        var data = state.Base.Data;
        var easing = CustomEventValueParser.GetEasing(data);
        foreach (var child in data["properties"].Children)
        {
            var propertyId = Shader.PropertyToID(child["id"]);
            var key = "property:" + propertyId;
            var value = child["value"];
            switch ((string)child["type"])
            {
                case "Texture":
                    var texture = VivifyAssetBundleManager.AssetPathToTexture.GetValueOrDefault(value);
                    var textureName = (string)child["id"];
                    PropertyTimeline.Register<Texture>(state, null, key, null,
                        v => TextureBindings.Set(this, textureName, v), _ => texture,
                        restore: () => TextureBindings.Remove(this, textureName));
                    break;
                case "Keyword":
                    var keyword = (string)child["id"];
                    var keywordValue = CustomEventValueParser.Float(value, allowBoolean: true);
                    if (keywordValue == null) break;
                    PropertyTimeline.Register(state, null, "keyword:" + keyword,
                        () => Shader.IsKeywordEnabled(keyword),
                        enabled =>
                        {
                            if (enabled) Shader.EnableKeyword(keyword);
                            else Shader.DisableKeyword(keyword);
                        }, t => keywordValue(t) >= 1f, easing);
                    break;
                case "Float":
                    RegisterFloat(state, null, key, value,
                        () => Shader.GetGlobalFloat(propertyId), v => Shader.SetGlobalFloat(propertyId, v), easing);
                    break;
                case "Vector":
                    var vector = PointDefinitionParser.Get(value);
                    if (vector != null)
                        PropertyTimeline.Register(state, null, key,
                            () => Shader.GetGlobalVector(propertyId), v => Shader.SetGlobalVector(propertyId, v),
                            vector.GetVector4, easing);
                    break;
                case "Color":
                    var color = CustomEventValueParser.Color(value);
                    if (color != null)
                        PropertyTimeline.Register(state, null, key,
                            () => Shader.GetGlobalColor(propertyId), v => Shader.SetGlobalColor(propertyId, v),
                            color, easing);
                    break;
            }
        }
    }

    public void AssignAnimator(CustomEventStateData state)
    {
        var data = state.Base.Data;
        var easing = CustomEventValueParser.GetEasing(data);
        foreach (var child in data["properties"].Children)
        {
            var id = (string)child["id"];
            var key = "parameter:" + id;
            var type = (string)child["type"];
            var value = child["value"];
            var sample = type == "Bool" && value.IsNumber ? null
                : CustomEventValueParser.Float(value, allowBoolean: type == "Bool");
            foreach (var obj in InstantiateObjectPrefabManager.GetObjectById(data["id"], state))
            {
                foreach (var animator in obj.Animators)
                {
                    switch (type)
                    {
                        case "Trigger":
                            triggerHistories.Add(new TriggerHistory(state, animator, id));
                            if (value != null && value.AsBool) animator.SetTrigger(id);
                            else animator.ResetTrigger(id);
                            break;
                        case "Float" when sample != null:
                            PropertyTimeline.Register(state, animator, key,
                                () => animator.GetFloat(id), v => animator.SetFloat(id, v), sample, easing);
                            break;
                        case "Integer" when sample != null:
                            PropertyTimeline.Register(state, animator, key,
                                () => animator.GetInteger(id), v => animator.SetInteger(id, v),
                                t => (int)sample(t), easing);
                            break;
                        case "Bool" when sample != null:
                            PropertyTimeline.Register(state, animator, key,
                                () => animator.GetBool(id), v => animator.SetBool(id, v),
                                t => sample(t) >= 1f, easing);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>Removes one event's properties; sample the timeline again to apply remaining owners.</summary>
    public void RemoveEvent(CustomEventStateData state)
    {
        PropertyTimeline.Remove(state);
        for (var i = triggerHistories.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(triggerHistories[i].Owner, state)) continue;
            triggerHistories[i].Restore();
            triggerHistories.RemoveAt(i);
        }
    }

    /// <summary>Restores trigger commands; the shared property timeline resets separately.</summary>
    public void Reset()
    {
        for (var i = triggerHistories.Count - 1; i >= 0; i--) triggerHistories[i].Restore();
        triggerHistories.Clear();
    }

    /// <summary>Restores a target's trigger history and forgets its bindings before timeline cleanup and pooling.</summary>
    public void RemoveTarget(UnityEngine.Object target)
    {
        for (var i = triggerHistories.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(triggerHistories[i].Animator, target)) continue;
            triggerHistories[i].Restore();
            triggerHistories.RemoveAt(i);
        }
    }

    private void RegisterFloat(CustomEventStateData state, UnityEngine.Object target, string key, JSONNode value,
        Func<float> read, Action<float> write, Func<float, float> easing)
    {
        var sample = CustomEventValueParser.Float(value);
        if (sample != null) PropertyTimeline.Register(state, target, key, read, write, sample, easing);
    }

    private sealed class TriggerHistory
    {
        public readonly CustomEventStateData Owner;
        public readonly Animator Animator;
        private readonly string property;
        private readonly bool value;

        public TriggerHistory(CustomEventStateData owner, Animator animator, string property)
        {
            Owner = owner;
            Animator = animator;
            this.property = property;
            value = animator.GetBool(property);
        }

        public void Restore()
        {
            if (Animator == null) return;
            if (value) Animator.SetTrigger(property);
            else Animator.ResetTrigger(property);
        }
    }
}
