using System.Linq;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public class VivifyPropertyStateTest
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void RgbColorsAreOpaqueAndRgbaPreservesAuthoredAlpha(bool materialTarget, bool rgba)
        {
            using var assets = new VivifyTestAssets();
            var timeline = new CustomEventPropertyTimeline();
            var properties = new ObjectPropertyManager { PropertyTimeline = timeline, VivifyAssetBundleManager = assets.Bundle };
            const string global = "_VivifyTestColor";
            var baseline = new Color(.1f, .2f, .3f, .4f);
            var originalGlobal = Shader.GetGlobalColor(global);
            assets.Material.SetColor("_Color", baseline);
            Shader.SetGlobalColor(global, baseline);
            try
            {
                var value = rgba ? "[1,0.25,0.5,0.6]" : "[1,0.25,0.5]";
                var state = VivifyTestAssets.State(materialTarget ? "SetMaterialProperty" : "SetGlobalProperty",
                    "{\"asset\":\"" + VivifyTestAssets.MaterialPath + "\",\"properties\":[{\"id\":\""
                    + (materialTarget ? "_Color" : global) + "\",\"type\":\"Color\",\"value\":" + value + "}]}");
                if (materialTarget) properties.AssignMaterial(state);
                else properties.AssignGlobal(state);
                timeline.Sample(0);
                AssertColor(Read(), new Color(1, .25f, .5f, rgba ? .6f : 1));
                timeline.Remove(state);
                AssertColor(Read(), baseline);
            }
            finally
            {
                timeline.Reset();
                Shader.SetGlobalColor(global, originalGlobal);
            }
            Color Read() => materialTarget ? assets.Material.GetColor("_Color") : Shader.GetGlobalColor(global);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GlobalTextureRestoresBaselineWhenOwnersReleaseInEitherOrder(bool resourceFirst)
        {
            using var assets = new VivifyTestAssets();
            const string name = "_VivifyTestTexture";
            var original = Shader.GetGlobalTexture(name);
            var baseline = assets.Own(new Texture2D(1, 1));
            var authored = assets.Own(new Texture2D(1, 1));
            var screen = assets.Own(new RenderTexture(4, 4, 0));
            screen.Create();
            assets.Bundle.AssetPathToTexture.Add("authored", authored);
            var timeline = new CustomEventPropertyTimeline();
            var properties = new ObjectPropertyManager { PropertyTimeline = timeline, VivifyAssetBundleManager = assets.Bundle };
            var resourceOwner = new object();
            try
            {
                Shader.SetGlobalTexture(name, baseline);
                properties.TextureBindings.Set(resourceOwner, name, screen);
                var state = VivifyTestAssets.State("SetGlobalProperty",
                    "{\"properties\":[{\"id\":\"" + name + "\",\"type\":\"Texture\",\"value\":\"authored\"}]}");
                properties.AssignGlobal(state);
                timeline.Sample(0);
                Assert.That(Shader.GetGlobalTexture(name), Is.SameAs(authored));
                properties.TextureBindings.Set(resourceOwner, name, screen);
                Assert.That(Shader.GetGlobalTexture(name), Is.SameAs(screen), "Render preparation publishes screen textures each frame.");
                if (resourceFirst)
                {
                    properties.TextureBindings.Remove(resourceOwner, name);
                    Assert.That(Shader.GetGlobalTexture(name), Is.SameAs(authored));
                    timeline.Reset();
                }
                else
                {
                    timeline.Reset();
                    Assert.That(Shader.GetGlobalTexture(name), Is.SameAs(screen));
                    properties.TextureBindings.Remove(resourceOwner, name);
                }
                Assert.That(Shader.GetGlobalTexture(name), Is.SameAs(baseline));
                properties.TextureBindings.Reset();
                Assert.That(Shader.GetGlobalTexture(name), Is.SameAs(baseline));
            }
            finally
            {
                timeline.Reset();
                properties.TextureBindings.Reset();
                Shader.SetGlobalTexture(name, original);
            }
        }

        [Test]
        public void OldAnimatorPropertiesDoNotBindToALaterPooledInstanceWithTheSameId()
        {
            using var assets = new VivifyTestAssets();
            assets.CreatePrefab();
            var timeline = new CustomEventPropertyTimeline();
            var instances = new InstantiateObjectPrefabManager { VivifyAssetBundleManager = assets.Bundle };
            var properties = new ObjectPropertyManager { InstantiateObjectPrefabManager = instances, PropertyTimeline = timeline };
            instances.BeforeTargetReleased = target => { properties.RemoveTarget(target); timeline.RemoveTarget(target); };
            try
            {
                instances.InstantiateVivifyObject(Spawn(0));
                var first = instances.GetObjectById("reused").Single();
                var state = VivifyTestAssets.State("SetAnimatorProperty",
                    "{\"id\":\"reused\",\"properties\":[{\"id\":\"Value\",\"type\":\"Float\",\"value\":[[0,0],[7,1]]}]}", 1, 4);
                properties.AssignAnimator(state);
                timeline.Sample(5);
                Assert.That(first.Animators[0].GetFloat("Value"), Is.EqualTo(7));
                instances.RemoveVivifyObjectById(VivifyTestAssets.State("DestroyObject", "{\"id\":\"reused\"}", 5));
                instances.InstantiateVivifyObject(Spawn(6));
                var replacement = instances.GetObjectById("reused").Single();
                properties.AssignAnimator(state);
                timeline.Sample(10);
                Assert.That(replacement.Animators[0].GetFloat("Value"), Is.Zero,
                    "Re-registering an old event must not affect a later lifetime, including a pooled Animator.");
            }
            finally { properties.Reset(); timeline.Reset(); instances.Reset(); }
            CustomEventStateData Spawn(float beat) => VivifyTestAssets.State("InstantiatePrefab",
                "{\"asset\":\"" + VivifyTestAssets.PrefabPath + "\",\"id\":\"reused\"}", beat);
        }

        [TestCase(1, 0)]
        [TestCase(3, 7)]
        public void EqualBeatAnimatorBindingsRespectSpawnSequence(int eventSequence, float expected)
        {
            using var assets = new VivifyTestAssets();
            assets.CreatePrefab();
            var timeline = new CustomEventPropertyTimeline();
            var instances = new InstantiateObjectPrefabManager { VivifyAssetBundleManager = assets.Bundle };
            var properties = new ObjectPropertyManager { InstantiateObjectPrefabManager = instances, PropertyTimeline = timeline };
            try
            {
                instances.InstantiateVivifyObject(VivifyTestAssets.State("InstantiatePrefab",
                    "{\"asset\":\"" + VivifyTestAssets.PrefabPath + "\",\"id\":\"reused\"}", 5, sequence: 2));
                properties.AssignAnimator(VivifyTestAssets.State("SetAnimatorProperty",
                    "{\"id\":\"reused\",\"properties\":[{\"id\":\"Value\",\"type\":\"Float\",\"value\":7}]}", 5, sequence: eventSequence));
                timeline.Sample(5);
                Assert.That(instances.GetObjectById("reused").Single().Animators[0].GetFloat("Value"), Is.EqualTo(expected));
            }
            finally { properties.Reset(); timeline.Reset(); instances.Reset(); }
        }

        [TestCase("")]
        [TestCase(",\"value\":false")]
        [TestCase(",\"value\":true")]
        public void TriggerCommandsPreserveAuthoredJsonAndApplyTheirBooleanValue(string value)
        {
            using var assets = new VivifyTestAssets();
            assets.CreatePrefab();
            var instances = new InstantiateObjectPrefabManager { VivifyAssetBundleManager = assets.Bundle };
            var properties = new ObjectPropertyManager { InstantiateObjectPrefabManager = instances };
            try
            {
                instances.InstantiateVivifyObject(VivifyTestAssets.State("InstantiatePrefab",
                    "{\"asset\":\"" + VivifyTestAssets.PrefabPath + "\",\"id\":\"reused\"}"));
                var animator = instances.GetObjectById("reused").Single().Animators[0];
                animator.SetTrigger("Kill");
                var state = VivifyTestAssets.State("SetAnimatorProperty",
                    "{\"id\":\"reused\",\"properties\":[{\"id\":\"Kill\",\"type\":\"Trigger\"" + value + "}]}");
                var source = state.Base.Data.ToString();
                properties.AssignAnimator(state);
                Assert.That(animator.GetBool("Kill"), Is.EqualTo(value.EndsWith("true")));
                Assert.That(state.Base.Data.ToString(), Is.EqualTo(source));
            }
            finally { properties.Reset(); instances.Reset(); }
        }

        private static void AssertColor(Color actual, Color expected)
        {
            for (var component = 0; component < 4; component++)
                Assert.That(actual[component], Is.EqualTo(expected[component]).Within(.0001f), "Color component " + component);
        }
    }
}
