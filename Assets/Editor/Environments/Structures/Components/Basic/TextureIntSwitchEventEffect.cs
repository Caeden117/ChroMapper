using System.Linq;
using UnityEngine;

public class TextureIntSwitchEventEffectData : EnvironmentComponentData<TextureIntSwitch>
{
    public int MaterialPropertyBlockController;
    public string TexturePropertyName;
    public EnvironmentEventType EventType;
    public int DefaultIndex;
    public TextureValueTuple[] TextureValueTuples;

    public override void FillComponents(
        GameObject self,
        TextureIntSwitch comp,
        CreateContainer container)
    {
        comp.Effect =
            container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
                EventType);

        comp.MpbController = container
            .GetComponentOrNull<MaterialPropertyBlockController>(MaterialPropertyBlockController);

        comp.TextureValueTuples = TextureValueTuples
            .Select(x =>
                new TextureIntSwitch.TextureValueTuple
                {
                    Value = x.Value, Texture = container.Library.Textures.Lookup[x.Texture]
                })
            .ToArray();
        comp.TexturePropertyName = TexturePropertyName;
        comp.DefaultIndex = DefaultIndex;
    }

    public class TextureValueTuple
    {
        public int Value;
        public string Texture;
    }
}
