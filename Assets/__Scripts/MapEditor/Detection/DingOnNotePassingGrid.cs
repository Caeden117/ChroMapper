using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DingOnNotePassingGrid : MonoBehaviour {

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] AudioSource source;
    [SerializeField] SoundList[] soundLists;
    [SerializeField] int DensityCheckOffset = 2;
    [SerializeField] float ThresholdInNoteTime = 0.25f;
    [SerializeField] AudioUtil audioUtil;
    [SerializeField] NotesContainer container;
    [SerializeField] BeatmapObjectCallbackController defaultCallbackController;
    [SerializeField] BeatmapObjectCallbackController beatSaberCutCallbackController;
    [SerializeField] BongoCat bongocat;
    [SerializeField] private GameObject discordPingPrefab;

    //debug
    [SerializeField] float difference;

    private float offset;

    public static Dictionary<int, bool> NoteTypeToDing = new Dictionary<int, bool>()
    {
        { BeatmapNote.NOTE_TYPE_A, true },
        { BeatmapNote.NOTE_TYPE_B, true },
        { BeatmapNote.NOTE_TYPE_BOMB, false },
    };

    private float lastCheckedTime;
    private float songSpeed = 1;

    private void Start()
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_A] = Settings.Instance.Ding_Red_Notes;
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_B] = Settings.Instance.Ding_Blue_Notes;
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB] = Settings.Instance.Ding_Bombs;

        beatSaberCutCallbackController.offset = container.AudioTimeSyncController.GetBeatFromSeconds(0.5f);

        UpdateHitSoundType(Settings.Instance.NoteHitSound);

        atsc.OnPlayToggle += OnPlayToggle;
    }

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
            var notes = container.GetBetween(atsc.CurrentBeat, atsc.CurrentBeat + beatSaberCutCallbackController.offset);

            // Schedule notes between now and threshold
            foreach (var n in notes)
            {
                PlaySound(false, 0, n);
            }
        }
    }

    private void OnDestroy()
    {
        atsc.OnPlayToggle -= OnPlayToggle;
    }

    private void UpdateRedNoteDing(object obj)
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_A] = (bool)obj;
    }

    private void UpdateBlueNoteDing(object obj)
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_B] = (bool)obj;
    }

    private void UpdateBombDing(object obj)
    {
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB] = (bool)obj;
    }

    private void UpdateHitSoundType(object obj)
    {
        int soundID = (int)obj;
        var isBeatSaberCutSound = soundID == (int)HitSounds.SLICE;

        if (isBeatSaberCutSound)
        {
            offset = 0.18f;
        }
        else
        {
            offset = 0;
        }
    }

    private void OnDisable()
    {
        beatSaberCutCallbackController.NotePassedThreshold -= PlaySound;
        defaultCallbackController.NotePassedThreshold -= TriggerBongoCat;

        Settings.ClearSettingNotifications("Ding_Red_Notes");
        Settings.ClearSettingNotifications("Ding_Blue_Notes");
        Settings.ClearSettingNotifications("Ding_Bombs");
        Settings.ClearSettingNotifications("NoteHitSound");
        Settings.ClearSettingNotifications("SongSpeed");
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
    }

    void TriggerBongoCat(bool initial, int index, BeatmapObject objectData)
    {
        // Filter notes that are too far behind the current beat
        // (Commonly occurs when Unity freezes for some unrelated fucking reason)
        if (objectData._time - container.AudioTimeSyncController.CurrentBeat <= -0.5f) return;

        var soundListId = Settings.Instance.NoteHitSound;
        if (soundListId == (int)HitSounds.DISCORD)
        {
            Instantiate(discordPingPrefab, gameObject.transform, true);
        }

        // bongo cat
        bongocat.triggerArm(objectData as BeatmapNote, container);
    }

    void PlaySound(bool initial, int index, BeatmapObject objectData) {
        // Filter notes that are too far behind the current beat
        // (Commonly occurs when Unity freezes for some unrelated fucking reason)
        if (objectData._time - container.AudioTimeSyncController.CurrentBeat <= -0.5f) return;

        //actual ding stuff
        if (objectData._time == lastCheckedTime || !NoteTypeToDing[((BeatmapNote) objectData)._type]) return;
        /*
         * As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
         * the same time that are supposed to ding from triggering the sound effects.
         */
        lastCheckedTime = objectData._time;
        var soundListId = Settings.Instance.NoteHitSound;
        var list = soundLists[soundListId];

        var shortCut = false;
        if (index - DensityCheckOffset > 0 && index + DensityCheckOffset < container.LoadedObjects.Count)
        {
            BeatmapObject first = container.LoadedObjects.ElementAt(index + DensityCheckOffset);
            BeatmapObject second = container.LoadedObjects.ElementAt(index - DensityCheckOffset);
            if (first != null && second != null)
            {
                if (first._time - objectData._time <= ThresholdInNoteTime && objectData._time - second._time <= ThresholdInNoteTime)
                    shortCut = true;
            }
        }

        var timeUntilDing = objectData._time - atsc.GetBeatFromSeconds(atsc.songAudioSource.time);
        var hitTime = (atsc.GetSecondsFromBeat(timeUntilDing) / songSpeed) - offset;
        audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), Settings.Instance.NoteHitVolume, 1, hitTime);
    }

}
