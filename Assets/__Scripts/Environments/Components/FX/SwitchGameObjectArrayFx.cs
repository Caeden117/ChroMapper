using System;
using UnityEngine;

public class SwitchGameObjectArrayFx : FxTarget
{
    [SerializeField] public GameObjectActivation[] GameObjects;

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        var first = false;
        for (var i = GameObjects.Length - 1; i >= 0; i--)
        {
            var gameObjectActivation = GameObjects[i];
            if (!first && value >= gameObjectActivation.Threshold)
            {
                first = true;
                gameObjectActivation.GameObject.SetActive(true);
            }
            else
                gameObjectActivation.GameObject.SetActive(false);
        }
    }

    [Serializable]
    public struct GameObjectActivation
    {
        public float Threshold;
        public GameObject GameObject;
    }
}
