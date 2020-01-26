using System;
using System.Collections.Generic;
using __Scripts.MapEditor.Hit_Sounds;
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
        callbackController.NotePassedThreshold += PlaySound;
    }

    private void OnDisable() {
        callbackController.NotePassedThreshold -= PlaySound;
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
            default:
                callbackController.offset = 0;
                break;
        }
        
        bool shortCut = false;
        if (index - DensityCheckOffset > 0 && index + DensityCheckOffset < container.LoadedContainers.Count)
        {
            BeatmapObject first = container.LoadedContainers[index + DensityCheckOffset]?.objectData;
            BeatmapObject second = container.LoadedContainers[index - DensityCheckOffset]?.objectData;
            if (first != null && second != null)
            {
                if (first._time - objectData._time <= ThresholdInNoteTime && objectData._time - second._time <= ThresholdInNoteTime)
                    shortCut = true;
            }
        }
        audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), Settings.Instance.NoteHitVolume);
    }

}
