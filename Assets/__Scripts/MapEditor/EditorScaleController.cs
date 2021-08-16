using System;
using UnityEngine;

public class EditorScaleController : MonoBehaviour {
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
        if (Settings.Instance.NoteJumpSpeedForEditorScale)
        {
            float bps = 60f / BeatSaberSongContainer.Instance.song.beatsPerMinute;
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
        Settings.NotifyBySettingName("EditorScale", UpdateEditorScale);
        Settings.NotifyBySettingName("EditorScaleBPMIndependent", RecalcEditorScale);
	}

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("EditorScale");
        Settings.ClearSettingNotifications("EditorScaleBPMIndependent");
    }
}
