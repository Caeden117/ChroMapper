using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
///     Code taken from Beat Saber, which provides deltaTime, fixedDeltaTime, and interpolation.
/// </summary>
public class TimeHelper : MonoBehaviour
{
    private static readonly int timeHelperOffsetId = Shader.PropertyToID("_TimeHelperOffset");
    private static readonly int timeId = Shader.PropertyToID("_Time");

    // A 90 Hz headset cadence gives preview callbacks one stable clock across editor frame rates.
    private const float PreviewCallbackRate = 90f;
    // Keeping the integer render index authoritative prevents repeated float additions from
    // drifting callback and rendered-state boundaries apart on long maps.
    private const float PreviewBoundaryTolerance = 0.00001f;
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

    // Beat Saber dispatches zero-ahead beatmap callbacks on the first render LateUpdate
    // whose song clock has reached the event; 90 Hz is the editor's deterministic cadence.
    public static int GetPreviewRenderIndex(float songSeconds) =>
        Mathf.CeilToInt((songSeconds * PreviewCallbackRate) - PreviewBoundaryTolerance);

    // Derive seconds from the same integer index used by rendering so the two preview
    // clocks cannot independently round opposite ways at an exact 90 Hz boundary.
    public static float GetPreviewCallbackSeconds(float songSeconds) =>
        GetPreviewRenderIndex(songSeconds) / PreviewCallbackRate;
}
