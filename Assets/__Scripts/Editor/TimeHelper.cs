using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
///     Code taken from Beat Saber, which provides deltaTime, fixedDeltaTime, and interpolation.
/// </summary>
public class TimeHelper : MonoBehaviour
{
    private static readonly int timeHelperOffsetId = Shader.PropertyToID("_TimeHelperOffset");
    private static readonly int timeId = Shader.PropertyToID("_Time");

    private float accumulator;
    private float currentTime;
    private int baseFrameCount;
    private bool externallyControlled;
    private bool shouldResetAccumulator;

    public static float DeltaTime { get; private set; }
    public static float FixedDeltaTime { get; private set; }
    public static float InterpolationFactor { get; private set; }
    public static TimeHelper Instance { get; private set; }
    public Vector4 TimeHelperOffset { get; private set; }
    public float CurrentTime => currentTime;

    private void Awake()
    {
        Instance = this;
        FixedDeltaTime = Time.fixedDeltaTime;
        shouldResetAccumulator = true;
        SetTime(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        DeltaTime = Time.deltaTime;
        accumulator += DeltaTime;
        if (externallyControlled)
            ApplyTime(currentTime);
        else
            currentTime += DeltaTime;
        InterpolationFactor = accumulator / FixedDeltaTime;
    }

    private void FixedUpdate()
    {
        if (shouldResetAccumulator || FixedDeltaTime != Time.fixedDeltaTime)
        {
            accumulator = 0f;
            shouldResetAccumulator = false;
        }
        else
        {
            accumulator -= FixedDeltaTime;
        }

        FixedDeltaTime = Time.fixedDeltaTime;
    }

    public int GetFrameCount() => Time.frameCount - baseFrameCount;

    public void SetTime(float time)
    {
        externallyControlled = false;
        baseFrameCount = Time.frameCount;
        shouldResetAccumulator = true;
        ApplyTime(time);
    }

    internal void SynchronizeTime(float time, bool resetFrameState)
    {
        externallyControlled = true;
        if (resetFrameState && !Mathf.Approximately(currentTime, time))
        {
            baseFrameCount = Time.frameCount;
            shouldResetAccumulator = true;
        }

        ApplyTime(time);
    }

    private void ApplyTime(float time)
    {
        currentTime = time;
        TimeHelperOffset = EncodeTimeAsVector(time - GetShaderTimeValue());
        Shader.SetGlobalVector(timeHelperOffsetId, TimeHelperOffset);
    }

    public void SetCommandBufferTimeProperties(CommandBuffer commandBuffer)
    {
        commandBuffer.SetGlobalVector(timeHelperOffsetId, TimeHelperOffset);
        commandBuffer.SetGlobalVector(timeId, EncodeTimeAsVector(GetShaderTimeValue()));
    }

    public static Vector4 EncodeTimeAsVector(float time) =>
        new(time / 20f, time, time * 2f, time * 3f);

    public static float GetShaderTimeValue() => Time.time;
}
