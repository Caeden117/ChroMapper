using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class DingOnNotePassingGrid : MonoBehaviour
{
    public static Dictionary<int, bool> NoteTypeToDing = new Dictionary<int, bool>
    {
        {BeatmapNote.NoteTypeA, true}, {BeatmapNote.NoteTypeB, true}, {BeatmapNote.NoteTypeBomb, false}
    };

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioSource source;
    [SerializeField] private SoundList[] soundLists;
    [FormerlySerializedAs("DensityCheckOffset")] [SerializeField] private int densityCheckOffset = 2;
    [FormerlySerializedAs("ThresholdInNoteTime")] [SerializeField] private float thresholdInNoteTime = 0.25f;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private NotesContainer container;
    [SerializeField] private BeatmapObjectCallbackController defaultCallbackController;
    [SerializeField] private BeatmapObjectCallbackController beatSaberCutCallbackController;
    [SerializeField] private BongoCat bongocat;
    [SerializeField] private GameObject discordPingPrefab;

    //debug
    [SerializeField] private float difference;

    private float lastCheckedTime;

    private float offset;
    private float songSpeed = 1;

    private void Start()
    {
        NoteTypeToDing[BeatmapNote.NoteTypeA] = Settings.Instance.Ding_Red_Notes;
        NoteTypeToDing[BeatmapNote.NoteTypeB] = Settings.Instance.Ding_Blue_Notes;
        NoteTypeToDing[BeatmapNote.NoteTypeBomb] = Settings.Instance.Ding_Bombs;

        beatSaberCutCallbackController.Offset = container.AudioTimeSyncController.GetBeatFromSeconds(0.5f);
        beatSaberCutCallbackController.UseAudioTime = true;

        UpdateHitSoundType(Settings.Instance.NoteHitSound);

        atsc.PlayToggle += OnPlayToggle;
    }

    private void OnEnable()
    {
        Settings.NotifyBySettingName("Ding_Red_Notes", UpdateRedNoteDing);
        Settings.NotifyBySettingName("Ding_Blue_Notes", UpdateBlueNoteDing);
        Settings.NotifyBySettingName("Ding_Bombs", UpdateBombDing);
        Settings.NotifyBySettingName("NoteHitSound", UpdateHitSoundType);
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        beatSaberCutCallbackController.NotePassedThreshold += PlaySound;
        defaultCallbackController.NotePassedThreshold += TriggerBongoCat;
        if (Settings.Instance.Load_MapV3)
        {
            beatSaberCutCallbackController.ChainPassedThreshold += PlaySound;
        }
    }

    private void OnDisable()
    {
        beatSaberCutCallbackController.NotePassedThreshold -= PlaySound;
        defaultCallbackController.NotePassedThreshold -= TriggerBongoCat;
        if (Settings.Instance.Load_MapV3)
        {
            beatSaberCutCallbackController.ChainPassedThreshold -= PlaySound;
        }

        Settings.ClearSettingNotifications("Ding_Red_Notes");
        Settings.ClearSettingNotifications("Ding_Blue_Notes");
        Settings.ClearSettingNotifications("Ding_Bombs");
        Settings.ClearSettingNotifications("NoteHitSound");
        Settings.ClearSettingNotifications("SongSpeed");
    }

    private void OnDestroy() => atsc.PlayToggle -= OnPlayToggle;

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10f;
    }

    private void OnPlayToggle(bool playing)
    {
        lastCheckedTime = -1;
        audioUtil.StopOneShot();
        if (playing)
        {
            var now = atsc.CurrentSongBeats;
            var notes = container.GetBetween(now, now + beatSaberCutCallbackController.Offset);

            // Schedule notes between now and threshold
            foreach (var n in notes) PlaySound(false, 0, n);
        }
    }

    private void UpdateRedNoteDing(object obj) => NoteTypeToDing[BeatmapNote.NoteTypeA] = (bool)obj;

    private void UpdateBlueNoteDing(object obj) => NoteTypeToDing[BeatmapNote.NoteTypeB] = (bool)obj;

    private void UpdateBombDing(object obj) => NoteTypeToDing[BeatmapNote.NoteTypeBomb] = (bool)obj;

    private void UpdateHitSoundType(object obj)
    {
        var soundID = (int)obj;
        var isBeatSaberCutSound = soundID == (int)HitSounds.Slice;

        if (isBeatSaberCutSound)
            offset = 0.18f;
        else
            offset = 0;
    }

    private void TriggerBongoCat(bool initial, int index, BeatmapObject objectData)
    {
        // Filter notes that are too far behind the current beat
        // (Commonly occurs when Unity freezes for some unrelated fucking reason)
        if (objectData.Time - container.AudioTimeSyncController.CurrentBeat <= -0.5f) return;

        var soundListId = Settings.Instance.NoteHitSound;
        if (soundListId == (int)HitSounds.Discord) Instantiate(discordPingPrefab, gameObject.transform, true);

        // bongo cat
        bongocat.TriggerArm(objectData as BeatmapNote, container);
    }

    private void PlaySound(bool initial, int index, BeatmapObject objectData)
    {
        // Filter notes that are too far behind the current beat
        // (Commonly occurs when Unity freezes for some unrelated fucking reason)
        if (objectData.Time - container.AudioTimeSyncController.CurrentBeat <= -0.5f) return;

        bool shortCut;
        if (Settings.Instance.Load_MapV3 && objectData is BeatmapChain)
        {
            if (objectData.Time == lastCheckedTime) return;
            shortCut = false;
        }
        else
        {
            //actual ding stuff
            if (objectData.Time == lastCheckedTime || !NoteTypeToDing[((BeatmapNote)objectData).Type]) return;
            /*
             * As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
             * the same time that are supposed to ding from triggering the sound effects.
             */

            shortCut = objectData.Time - lastCheckedTime < thresholdInNoteTime;

        }
        lastCheckedTime = objectData.Time;

        var soundListId = Settings.Instance.NoteHitSound;
        var list = soundLists[soundListId];

        var timeUntilDing = objectData.Time - atsc.CurrentSongBeats;
        var hitTime = (atsc.GetSecondsFromBeat(timeUntilDing) / songSpeed) - offset;
        audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), Settings.Instance.NoteHitVolume, 1, hitTime);
    }
}
