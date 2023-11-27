using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class BongoCat : MonoBehaviour
{
    [SerializeField] private BongoCatPreset[] bongoCats;
    [SerializeField] private Transform noteGridHeight;
    [FormerlySerializedAs("Larm")][SerializeField] private bool larm;
    [FormerlySerializedAs("Rarm")][SerializeField] private bool rarm;

    private SpriteRenderer comp;
    private BongoCatPreset selectedBongoCat;

    private float larmTimeout;
    private float rarmTimeout;

    private void Start()
    {
        selectedBongoCat = bongoCats[0];

        comp = GetComponent<SpriteRenderer>();

        Settings.NotifyBySettingName(nameof(BongoCat), UpdateBongoCatState);

        UpdateBongoCatState(Settings.Instance.BongoCat);
    }

    private void Update()
    {
        larmTimeout -= Time.deltaTime;
        rarmTimeout -= Time.deltaTime;
        if (larmTimeout < 0) larm = false;
        if (rarmTimeout < 0) rarm = false;

        comp.sprite = larm
            ? rarm
                ? selectedBongoCat.LeftDownRightDown
                : selectedBongoCat.LeftDownRightUp
            : rarm
                ? selectedBongoCat.LeftUpRightDown
                : selectedBongoCat.LeftUpRightUp;
    }

    private void UpdateBongoCatState(object obj)
    {
        switch (Settings.Instance.BongoCat)
        {
            case -1:
                comp.enabled = enabled = false;
                break;
            default:
                selectedBongoCat = bongoCats[Settings.Instance.BongoCat];
                comp.enabled = enabled = true;
                break;
        }

        var x = transform.localPosition.x;

        transform.localPosition = new Vector3(
            x,
            noteGridHeight.lossyScale.z + selectedBongoCat.YOffset,
            0);

        transform.localScale = selectedBongoCat.Scale;
    }

    public void TriggerArm(BaseNote note, NoteGridContainer container)
    {
        //Ignore bombs here to improve performance.
        if (Settings.Instance.BongoCat == -1 || note.Type == (int)NoteType.Bomb) return;

        // TODO(Caeden): This can be optimized:
        //   - Pass note idx through the caller (DingOnNotePassingGrid? should be a direct callback subscriber tbh)
        //   - Manually march forward until the next object that matches our predicate is found
        var next = container.MapObjects.Find(x => x.JsonTime > note.JsonTime && x.Type == note.Type);
        
        var timer = 0.125f;
        if (next is not null)
        {
            // clamp to accommodate sliders and long gaps between notes
            var half = (next.SongBpmTime - note.SongBpmTime) * 60f / BeatSaberSongContainer.Instance.Song.BeatsPerMinute / 2f;
            timer = Mathf.Clamp(half, 0.05f, 0.2f);
        }

        switch (note.Type)
        {
            case (int)NoteType.Red:
                larm = true;
                larmTimeout = timer;
                break;
            case (int)NoteType.Blue:
                rarm = true;
                rarmTimeout = timer;
                break;
        }
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications(nameof(Settings.BongoCat));
    }
}
