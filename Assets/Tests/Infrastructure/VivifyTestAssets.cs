using System;
using System.Collections.Generic;
using System.Reflection;
using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    internal sealed class VivifyTestAssets : IDisposable
    {
        internal const string PrefabPath = "tests/vivify.prefab";
        internal const string MaterialPath = "tests/vivify.mat";
        private readonly List<Object> owned = new();
        internal VivifyAssetBundleManager Bundle { get; }
        internal Material Material { get; }
        internal AnimatorController Controller { get; }

        internal VivifyTestAssets(VivifyAssetBundleManager bundle = null)
        {
            if (bundle == null)
            {
                var root = Own(new GameObject("Vivify test assets"));
                root.SetActive(false);
                bundle = root.AddComponent<VivifyAssetBundleManager>();
            }
            Bundle = bundle;
            Material = Own(new Material(Shader.Find("Hidden/Tests/VivifyProperties")));
            Material.SetFloat("_Value", .25f);
            Bundle.AssetPathToMaterial.Add(MaterialPath, Material);
            Controller = Own(new AnimatorController());
            Controller.AddLayer("Base Layer");
            var machine = Own(Controller.layers[0].stateMachine);
            machine.defaultState = Own(machine.AddState("Idle"));
            Controller.AddParameter("Value", AnimatorControllerParameterType.Float);
            Controller.AddParameter("Kill", AnimatorControllerParameterType.Trigger);
        }

        internal T Own<T>(T obj) where T : Object
        {
            owned.Add(obj);
            return obj;
        }

        internal VivifyObject CreatePrefab(AudioTimeSyncController clock = null)
        {
            var root = Own(new GameObject("Vivify test prefab"));
            root.SetActive(false);
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = Controller;
            var prefab = root.AddComponent<VivifyObject>();
            prefab.AssetPath = PrefabPath;
            prefab.Atsc = clock;
            prefab.Animators = new[] { animator };
            prefab.SetDefault();
            Bundle.AssetPathToPrefab.Add(PrefabPath, prefab);
            return prefab;
        }

        internal static CustomEventStateData State(string type, string json, float start = 0,
            float duration = 0, int sequence = 0)
        {
            // These isolated manager tests supply timing directly, without a loaded map/version singleton.
            var evt = new BaseCustomEvent { Type = type, JsonTime = start, DataDuration = duration };
            typeof(BaseCustomEvent).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(evt, JSON.Parse(json));
            return new CustomEventStateData(evt)
            {
                StartTime = start, StartSecondTime = start, EndTimeAnimation = start + duration,
                Sequence = sequence
            };
        }

        public void Dispose()
        {
            Bundle.AssetPathToMaterial.Remove(MaterialPath);
            Bundle.AssetPathToPrefab.Remove(PrefabPath);
            for (var i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }
    }
}
