﻿using UnityEngine;
using SimpleJSON;

public abstract class RotatingLightsBase : MonoBehaviour {
    public abstract void UpdateOffset(int Speed, float Rotation, bool RotateForwards, JSONNode customData = null);

    public abstract bool IsOverrideLightGroup();
}
