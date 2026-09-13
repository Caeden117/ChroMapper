using UnityEngine;

public class SwitchGameObjectFx : FxTarget
{
    [SerializeField] public GameObject GameObjectA;
    [SerializeField] public GameObject GameObjectB;

    public override void SetValue(int group, int id, float value) => SetFloat(value);
    public override void TriggerValue(int group, int id, float value) => SetFloat(value);

    private void SetFloat(float value)
    {
        if (Mathf.Approximately(value, 0f))
        {
            GameObjectA.SetActive(true);
            GameObjectB.SetActive(false);
        }
        else
        {
            GameObjectA.SetActive(false);
            GameObjectB.SetActive(true);
        }
    }
}
