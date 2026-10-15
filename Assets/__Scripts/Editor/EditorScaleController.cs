using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorScaleController : MonoBehaviour, CMInput.IEditorScaleActions
{
    private const float keybindMultiplyValue = 1.25f;
    private const float baseBpm = 160;

    public static float EditorScale = 4;
    public static event Action<float> OnEditorScaleChanged;

    [SerializeField] private AudioTimeSyncController atsc;

    private float currentBpm = baseBpm;

    private float previousEditorScale = -1;

    // Use this for initialization
    private void Start()
    {
        currentBpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
        SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale); // seems weird but it does what we need
        Settings.NotifyBySettingName("EditorScale", UpdateEditorScale);
        Settings.NotifyBySettingName("EditorScaleBPMIndependent", RecalcEditorScale);
        Settings.NotifyBySettingName("NoteJumpSpeedForEditorScale", SetAccurateEditorScale);
        UIMode.OnUIModeSwitched += UpdateByUIMode;
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("EditorScale");
        Settings.ClearSettingNotifications("EditorScaleBPMIndependent");
        Settings.ClearSettingNotifications("NoteJumpSpeedForEditorScale");
        UIMode.OnUIModeSwitched -= UpdateByUIMode;
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

        if (!Mathf.Approximately(previousEditorScale, EditorScale)) Apply();
    }

    private void RecalcEditorScale(object obj) => UpdateEditorScale(Settings.Instance.EditorScale);

    private void SetAccurateEditorScale(object obj)
    {
        var accurateNjs = (bool)obj;
        if (accurateNjs)
        {
            var bps = 60f / currentBpm;
            var songNoteJumpSpeed = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
            EditorScale = songNoteJumpSpeed * bps / 0.6f; // / 0.6 = Correct accurate NJS scale, per discussion w/ Kival and GalaxyMaster
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
            case UIModeType.HideUI:
            case UIModeType.HideGrids:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);
                break;
            case UIModeType.Preview:
            case UIModeType.Playing:
                SetAccurateEditorScale(true);
                break;
        }
    }

    private void Apply()
    {
        BeatmapObjectContainerCollection.UpdateAllGridPositions();

        OnEditorScaleChanged?.Invoke(EditorScale);
        previousEditorScale = EditorScale;

        atsc.MoveToSongBpmTime(atsc.CurrentSongBpmTime);
    }
}
