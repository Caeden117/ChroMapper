using System;
using System.Collections.Generic;
using UnityEngine;

public class TextureIntSwitch : MonoBehaviour
{
    [SerializeField] public GenericCallbackEventEffect Effect;

    [SerializeField] public MaterialPropertyBlockController MpbController;
    [SerializeField] public string TexturePropertyName;

    [SerializeField] public int DefaultIndex;
    [SerializeField] public TextureValueTuple[] TextureValueTuples;

    private int texturePropertyId;
    private Dictionary<int, Texture> valueToTextureMap;

    protected void Start()
    {
        texturePropertyId = Shader.PropertyToID(TexturePropertyName);
        valueToTextureMap = new Dictionary<int, Texture>();
        foreach (var tuple in TextureValueTuples) valueToTextureMap[tuple.Value] = tuple.Texture;
        Effect.OnStateChanged += HandleStateChanged;
        var p = Effect.GetCurrentState();
        if (p.index != -1) HandleStateChanged(p);
    }

    protected void OnDestroy() => Effect.OnStateChanged -= HandleStateChanged;

    private void HandleStateChanged((int index, BasicEventStateData state) data) =>
        SetTextureByValue(data.state.Base.Value);

    private void SetTextureByValue(int value)
    {
        if (!valueToTextureMap.TryGetValue(value, out var texture)) texture = valueToTextureMap[DefaultIndex];

        MpbController.Mpb.SetTexture(texturePropertyId, texture);
        MpbController.ApplyChanges();
    }

    [Serializable]
    public struct TextureValueTuple
    {
        public int Value;
        public Texture Texture;
    }
}
