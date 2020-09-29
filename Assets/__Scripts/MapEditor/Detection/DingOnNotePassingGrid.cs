using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DingOnNotePassingGrid : MonoBehaviour {
    
    [SerializeField] AudioSource source;
    [SerializeField] SoundList[] soundLists;
    [SerializeField] int DensityCheckOffset = 2;
    [SerializeField] float ThresholdInNoteTime = 0.25f;
    [SerializeField] AudioUtil audioUtil;
    [SerializeField] NotesContainer container;
    [SerializeField] BeatmapObjectCallbackController callbackController;
    [SerializeField] BongoCat bongocat;
    [SerializeField] private GameObject discordPingPrefab;

    //debug
    [SerializeField] float difference;

    public static Dictionary<int, bool> NoteTypeToDing = new Dictionary<int, bool>()
    {
        { BeatmapNote.NOTE_TYPE_A, true },
        { BeatmapNote.NOTE_TYPE_B, true },
        { BeatmapNote.NOTE_TYPE_BOMB, false },
    };

    private float lastCheckedTime;

    private void Start() {
        Settings.NotifyBySettingName("Ding_Red_Notes", UpdateRedNoteDing);
        Settings.NotifyBySettingName("Ding_Blue_Notes", UpdateBlueNoteDing);
        Settings.NotifyBySettingName("Ding_Bombs", UpdateBombDing);
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_A] = Settings.Instance.Ding_Red_Notes;
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_B] = Settings.Instance.Ding_Blue_Notes;
        NoteTypeToDing[BeatmapNote.NOTE_TYPE_BOMB] = Settings.Instance.Ding_Bombs;
        callbackController.NotePassedThreshold += PlaySound;
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

    private void OnDisable() {
        callbackController.NotePassedThreshold -= PlaySound;
        Settings.ClearSettingNotifications("Ding_Red_Notes");
        Settings.ClearSettingNotifications("Ding_Blue_Notes");
        Settings.ClearSettingNotifications("Ding_Bombs");
    }

    void PlaySound(bool initial, int index, BeatmapObject objectData) {
        // Filter notes that are too far behind the current beat
        // (Commonly occurs when Unity freezes for some unrelated fucking reason)
        if (objectData._time - container.AudioTimeSyncController.CurrentBeat <= -0.5f) return;

        // bongo cat
        bongocat.triggerArm(objectData as BeatmapNote, container);

        //actual ding stuff
        if (objectData._time == lastCheckedTime || !NoteTypeToDing[((BeatmapNote) objectData)._type]) return;
        /*
         * As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
         * the same time that are supposed to ding from triggering the sound effects.
         */
        lastCheckedTime = objectData._time;
        int soundListId = Settings.Instance.NoteHitSound;
        SoundList list = soundLists[soundListId];
        
        switch (soundListId)
        {
            case (int) HitSounds.SLICE:
                callbackController.offset = container.AudioTimeSyncController.GetBeatFromSeconds(0.18f);
                break;
            case (int)HitSounds.DISCORD:
                Instantiate(discordPingPrefab, gameObject.transform, true);
                break;
            default:
                callbackController.offset = 0;
                break;
        }
        
        bool shortCut = false;
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
        audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), Settings.Instance.NoteHitVolume);
    }

}
