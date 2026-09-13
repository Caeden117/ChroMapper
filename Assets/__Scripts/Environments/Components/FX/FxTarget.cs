using UnityEngine;

public abstract class FxTarget : MonoBehaviour
{
    public abstract void SetValue(int group, int id, float value);
    public abstract void TriggerValue(int group, int id, float value);
}
