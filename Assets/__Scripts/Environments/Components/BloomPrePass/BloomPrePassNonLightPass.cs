using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BloomPrePassNonLightPass : MonoBehaviour
{
    public enum ExecutionTime
    {
        None,
        BeforeBlur,
        AfterBlur
    }

    [SerializeField] public ExecutionTime ExecutionTimeType;

    public static readonly List<BloomPrePassNonLightPass> BloomPrePassAfterBlurList = new();
    public static readonly List<BloomPrePassNonLightPass> BloomPrePassBeforeBlurList = new();
    private ExecutionTime registeredExecutionTime;

    protected virtual void OnEnable() => Register();
    protected virtual void OnDisable() => Unregister();

    protected void Register()
    {
        if (registeredExecutionTime == ExecutionTimeType) return;
        if (registeredExecutionTime != 0) Unregister();

        switch (ExecutionTimeType)
        {
            case ExecutionTime.BeforeBlur:
                BloomPrePassBeforeBlurList.Add(this);
                break;
            case ExecutionTime.AfterBlur:
                BloomPrePassAfterBlurList.Add(this);
                break;
            case ExecutionTime.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        registeredExecutionTime = ExecutionTimeType;
    }

    protected void Unregister()
    {
        switch (registeredExecutionTime)
        {
            case ExecutionTime.BeforeBlur:
                BloomPrePassBeforeBlurList.Remove(this);
                break;
            case ExecutionTime.AfterBlur:
                BloomPrePassAfterBlurList.Remove(this);
                break;
            case ExecutionTime.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        registeredExecutionTime = ExecutionTime.None;
    }

    protected virtual void OnValidate()
    {
        if (isActiveAndEnabled)
            Register();
        else
            Unregister();
    }

    public abstract void Render(RenderTexture dest, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix);
}
