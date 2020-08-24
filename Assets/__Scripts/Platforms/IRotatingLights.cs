using UnityEngine;
using SimpleJSON;

public abstract class IRotatingLights : MonoBehaviour {
    public abstract void UpdateOffset(int Speed, float Rotation, bool RotateForwards, JSONNode customData = null);

    public abstract bool IsOverrideLightGroup();
}
