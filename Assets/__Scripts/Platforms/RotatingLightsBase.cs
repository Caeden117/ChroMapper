using SimpleJSON;
using UnityEngine;

public abstract class RotatingLightsBase : MonoBehaviour
{
    public abstract void UpdateOffset(bool isLeftEvent, int speed, float rotation, bool rotateForwards,
        JSONNode customData = null);

    public abstract void UpdateZPosition();

    public abstract bool IsOverrideLightGroup();
}
