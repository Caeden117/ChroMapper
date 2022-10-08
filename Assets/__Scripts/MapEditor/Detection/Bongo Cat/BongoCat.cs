using Beatmap.Enums;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class BongoCat : MonoBehaviour
{
    [SerializeField] private BongoCatPreset[] bongoCats;
    [SerializeField] private Transform noteGridHeight;
    [FormerlySerializedAs("Larm")] [SerializeField] private bool larm;
    [FormerlySerializedAs("Rarm")] [SerializeField] private bool rarm;

    private SpriteRenderer comp;
    private BongoCatPreset selectedBongoCat;
    private int oldBongoCatValue = 0;

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

    public void TriggerArm(BaseNote baseNote, NoteGridContainer container)
    {
        //Ignore bombs here to improve performance.
        if (Settings.Instance.BongoCat == -1 || baseNote.Type == (int)NoteType.Bomb) return;
        var next = container.UnsortedObjects.Find(x => x.Time > baseNote.Time &&
                                                       ((BaseNote)x).Type == baseNote.Type);
        var timer = 0.125f;
        if (!(next is null))
        {
            var half = container.AudioTimeSyncController.GetSecondsFromBeat((next.Time - baseNote.Time) / 2f);
            timer = next != null
                ? Mathf.Clamp(half, 0.05f, 0.2f)
                : 0.125f; // clamp to accommodate sliders and long gaps between notes
        }

        switch (baseNote.Type)
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
