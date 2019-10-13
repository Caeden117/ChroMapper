using System;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine;

public class DingOnNotePassingGrid : MonoBehaviour {
    
    [SerializeField] AudioSource source;
    [SerializeField] SoundList[] soundLists;
    [SerializeField] int soundListToUse;
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

    private float lastCheckedTime = 0;

    private void Start() {
        callbackController.NotePassedThreshold += PlaySound;
    }

    private void OnDisable() {
        callbackController.NotePassedThreshold -= PlaySound;
    }

    void Update()
    {
        if (KeybindsController.AltHeld && Input.GetKeyDown(KeyCode.UpArrow)) soundListToUse++;
        if (KeybindsController.AltHeld && Input.GetKeyDown(KeyCode.DownArrow)) soundListToUse--;
        if (soundListToUse < 0) soundListToUse = 0;
        if (soundListToUse >= soundLists.Length) soundListToUse = soundLists.Length - 1;
        if (soundListToUse == soundLists.Length - 1)
            callbackController.offset = container.AudioTimeSyncController.GetBeatFromSeconds(0.18f);
        else callbackController.offset = 0;
    }

    void PlaySound(bool initial, int index, BeatmapObject objectData) {

        // bongo cat
        bongocat.triggerArm(objectData as BeatmapNote, container);

        //actual ding stuff
        if (objectData._time == lastCheckedTime || !NoteTypeToDing[(objectData as BeatmapNote)._type]) return;
        /*
         * As for why we are not using "initial", it is so notes that are not supposed to ding do not prevent notes at
         * the same time that are supposed to ding from triggering the sound effects.
         */
        lastCheckedTime = objectData._time;
        SoundList list = soundLists[soundListToUse]; 
        bool shortCut = false;
        if (index - DensityCheckOffset > 0 && index + DensityCheckOffset < container.LoadedContainers.Count)
        {
            BeatmapObject first = container.LoadedContainers[index + DensityCheckOffset]?.objectData;
            BeatmapObject second = container.LoadedContainers[index - DensityCheckOffset]?.objectData;
            if (first != null && second != null)
            {
                if (first._time - objectData._time <= ThresholdInNoteTime &&
                    objectData._time - second._time <= ThresholdInNoteTime)
                    shortCut = true;
            }
        }
        audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), 0.5f);
    }

}
