using System;
using UnityEngine;

public class EditorScaleController : MonoBehaviour {

    public static float EditorScale = 4;
    public static Action<float> EditorScaleChangedEvent;

    private float PreviousEditorScale = -1;

    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Transform[] scalingOffsets;
    private BeatmapObjectContainerCollection[] collections;
    [SerializeField] private AudioTimeSyncController atsc;

    public void UpdateEditorScale(object value)
    {
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
        Settings.NotifyBySettingName("EditorScale", UpdateEditorScale);
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
        PreviousEditorScale = EditorScale = Settings.Instance.EditorScale;
        if (false) //TODO replace with new setting
        {
            float bps = 60f / BeatSaberSongContainer.Instance.song.beatsPerMinute;
            float halfJumpDuration = 4;

            float songNoteJumpSpeed = BeatSaberSongContainer.Instance.difficultyData.noteJumpMovementSpeed;
            float songStartBeatOffset = BeatSaberSongContainer.Instance.difficultyData.noteJumpStartBeatOffset;

            while (songNoteJumpSpeed * bps * halfJumpDuration > 18) halfJumpDuration /= 2;

            halfJumpDuration += songStartBeatOffset;

            if (halfJumpDuration < 1) halfJumpDuration = 1;

            float jumpDuration = bps * halfJumpDuration * 2;
            float jumpDistance = songNoteJumpSpeed * jumpDuration;

            //these can also be simplified out
            //Vector3 moveStartPos = Vector3.zero + Vector3.forward * (((200 * 1) + jumpDistance) * 0.5f);
            //Vector3 moveEndPos = Vector3.forward * jumpDistance * 0.5f;
            //Vector3 jumpEndPos = -Vector3.forward * jumpDistance * 0.5f;

            //jumpEndPos - moveEndPos can be simplified to jumpDistance
            //(jumpEndPos - moveEndPos).magnitude / jumpDuration can be simplified to just the note jump speed
            EditorScale = songNoteJumpSpeed;
        }
        Apply();
	}

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("EditorScale");
    }
}
