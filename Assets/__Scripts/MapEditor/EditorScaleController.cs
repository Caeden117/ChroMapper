using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorScaleController : MonoBehaviour, CMInput.IEditorScaleActions {
    private const float keybindMultiplyValue = 1.25f;
    private const float baseBPM = 160;
    private float currentBPM = baseBPM;

    public static float EditorScale = 4;
    public static Action<float> EditorScaleChangedEvent;

    private float PreviousEditorScale = -1;

    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Transform[] scalingOffsets;
    private BeatmapObjectContainerCollection[] collections;
    [SerializeField] private AudioTimeSyncController atsc;

    public void UpdateEditorScale(object value)
    {
        if (Settings.Instance.NoteJumpSpeedForEditorScale) return;

        float setting = (float)value;
        if (Settings.Instance.EditorScaleBPMIndependent)
        {
            EditorScale = setting * baseBPM / currentBPM;
        }
        else
        {
            EditorScale = setting;
        }

        if (PreviousEditorScale != EditorScale) Apply();
    }

    private void RecalcEditorScale(object obj)
    {
        UpdateEditorScale(Settings.Instance.EditorScale);
    }

    private void SetAccurateEditorScale(object obj)
    {
        bool enabled = (bool)obj;
        if (enabled)
        {
            float bps = 60f / currentBPM;
            float songNoteJumpSpeed = BeatSaberSongContainer.Instance.difficultyData.noteJumpMovementSpeed;

            // When doing the math, it turns out that this all cancels out into what you see
            // We don't know where the hell 5/3 comes from, yay for magic numbers
            EditorScale = (5 / 3f) * songNoteJumpSpeed * bps;
            Apply();
        }
        else
        {
            UpdateEditorScale(Settings.Instance.EditorScale);
        }
    }

    private void UpdateByUIMode(object mode)
    {
        UIModeType selectedMode = (UIModeType)mode;
        switch (selectedMode)
        {
            case UIModeType.NORMAL:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);
                break;
            case UIModeType.HIDE_UI:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);       
                break;
            case UIModeType.HIDE_GRIDS:
                SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale);
                break;
            case UIModeType.PREVIEW:              
                SetAccurateEditorScale(true);
                break;
            case UIModeType.PLAYING:             
                SetAccurateEditorScale(true);
                break;
        }
    }

    private void Apply()
    {
        foreach (BeatmapObjectContainerCollection collection in collections)
        {
            foreach (BeatmapObjectContainer b in collection.LoadedContainers.Values)
            {
                b.UpdateGridPosition();
            }
        }
        atsc.MoveToTimeInSeconds(atsc.CurrentSeconds);
        EditorScaleChangedEvent?.Invoke(EditorScale);
        PreviousEditorScale = EditorScale;
        foreach (Transform offset in scalingOffsets)
            offset.localScale = new Vector3(offset.localScale.x, offset.localScale.y, 8 * EditorScale);
    }

	// Use this for initialization
	void Start () {
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
        currentBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;
        SetAccurateEditorScale(Settings.Instance.NoteJumpSpeedForEditorScale); // seems weird but it does what we need
        Settings.NotifyBySettingName("EditorScale", UpdateEditorScale);
        Settings.NotifyBySettingName("EditorScaleBPMIndependent", RecalcEditorScale);
        Settings.NotifyBySettingName("NoteJumpSpeedForEditorScale", SetAccurateEditorScale);
        UIMode.NotifyOnUIModeChange(UpdateByUIMode);
	}

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("EditorScale");
        Settings.ClearSettingNotifications("EditorScaleBPMIndependent");
        Settings.ClearSettingNotifications("NoteJumpSpeedForEditorScale");
        UIMode.ClearUIModeNotifications();
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
}
