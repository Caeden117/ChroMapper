using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditorScaleController : MonoBehaviour, CMInput.IEditorScaleActions {
    private const float keybindMultiplyValue = 1.25f;

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
        EditorScale = (float)Convert.ChangeType(value, typeof(float));
        if (PreviousEditorScale != EditorScale) Apply();
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
        PreviousEditorScale = EditorScale = Settings.Instance.EditorScale;
        if (Settings.Instance.NoteJumpSpeedForEditorScale)
        {
            float bps = 60f / BeatSaberSongContainer.Instance.song.beatsPerMinute;
            float songNoteJumpSpeed = BeatSaberSongContainer.Instance.difficultyData.noteJumpMovementSpeed;

            // When doing the math, it turns out that this all cancels out into what you see
            // We don't know where the hell 5/3 comes from, yay for magic numbers
            EditorScale = (5 / 3f) * songNoteJumpSpeed * bps;
        }
        Settings.NotifyBySettingName("EditorScale", UpdateEditorScale);
        Apply();
	}

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("EditorScale");
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
