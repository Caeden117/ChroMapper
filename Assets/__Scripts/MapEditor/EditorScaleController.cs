using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorScaleController : MonoBehaviour, CMInput.IEditorScaleActions
{
    private const float keybindMultiplyValue = 1.25f;
    private const float baseBpm = 160;

    public static float EditorScale = 4;
    public static Action<float> EditorScaleChangedEvent;

    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Transform[] scalingOffsets;
    [SerializeField] private AudioTimeSyncController atsc;
    private BeatmapObjectContainerCollection[] collections;
    private float currentBpm = baseBpm;

    private float previousEditorScale = -1;

    // Use this for initialization
    private void Start()
    {
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
        currentBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale); // seems weird but it does what we need
        Settings.NotifyBySettingName("EditorScale", UpdateEditorScale);
        Settings.NotifyBySettingName("EditorScaleBPMIndependent", RecalcEditorScale);
        Settings.NotifyBySettingName("NoteJumpSpeedForEditorScale", SetAccurateEditorScale);
        UIMode.UIModeSwitched += UpdateByUIMode;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("EditorScale");
        Settings.ClearSettingNotifications("EditorScaleBPMIndependent");
        Settings.ClearSettingNotifications("NoteJumpSpeedForEditorScale");
        UIMode.UIModeSwitched -= UpdateByUIMode;
    }

    public void OnDecreaseEditorScale(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Settings.Instance.EditorScale /= keybindMultiplyValue;
        Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
    }

    public void OnIncreaseEditorScale(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Settings.Instance.EditorScale *= keybindMultiplyValue;
        Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
    }

    public void UpdateEditorScale(object value)
    {
        if (Settings.Instance.NoteJumpSpeedForEditorScale) return;

        var setting = (float)value;
        if (Settings.Instance.EditorScaleBPMIndependent)
            EditorScale = setting * baseBpm / currentBpm;
        else
            EditorScale = setting;

        if (previousEditorScale != EditorScale) Apply();
    }

    private void RecalcEditorScale(object obj) => UpdateEditorScale(Settings.Instance.EditorScale);

    private void SetAccurateEditorScale(object obj)
    {
        var enabled = (bool)obj;
        if (enabled)
        {
            var bps = 60f / currentBpm;
            var songNoteJumpSpeed = BeatSaberSongContainer.Instance.DifficultyData.NoteJumpMovementSpeed;

            // When doing the math, it turns out that this all cancels out into what you see
            // We don't know where the hell 5/3 comes from, yay for magic numbers
            EditorScale = 5 / 3f * songNoteJumpSpeed * bps;
            Apply();
        }
        else
        {
            UpdateEditorScale(Settings.Instance.EditorScale);
        }
    }

    private void UpdateByUIMode(UIModeType mode)
    {
        switch (mode)
        {
            case UIModeType.Normal:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);
                break;
            case UIModeType.HideUI:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);
                break;
            case UIModeType.HideGrids:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);
                break;
            case UIModeType.Preview:
                SetAccurateEditorScale(true);
                break;
            case UIModeType.Playing:
                SetAccurateEditorScale(true);
                break;
        }
    }

    private void Apply()
    {
        foreach (var collection in collections)
        {
            foreach (var b in collection.LoadedContainers.Values)
                b.UpdateGridPosition();
        }

        atsc.MoveToTimeInSeconds(atsc.CurrentSeconds);
        EditorScaleChangedEvent?.Invoke(EditorScale);
        previousEditorScale = EditorScale;
        foreach (var offset in scalingOffsets)
            offset.localScale = new Vector3(offset.localScale.x, offset.localScale.y, 8 * EditorScale);
    }
}
