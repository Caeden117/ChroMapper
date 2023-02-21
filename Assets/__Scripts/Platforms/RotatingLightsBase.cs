using Beatmap.Base;
using UnityEngine;

public abstract class RotatingLightsBase : MonoBehaviour
{
    public abstract void UpdateOffset(bool isLeftEvent, BaseEvent evt);

    public abstract bool IsOverrideLightGroup();
}
