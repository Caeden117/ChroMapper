using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialParameters : MonoBehaviour
{
    [SerializeField] public Vector3 flashShaderOffset = Vector3.zero;
    [SerializeField] public Vector3 fadeShaderOffset = Vector3.zero;
    [SerializeField] public float defaultFadeSize = 0f;
    [SerializeField] public float boostEventFadeSize = 0f;
}
