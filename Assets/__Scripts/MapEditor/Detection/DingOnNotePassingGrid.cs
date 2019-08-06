using System;
using System.Collections;
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
    
    private void OnEnable() {
        callbackController.NotePassedThreshold += PlaySound;
    }

    private void OnDisable() {
        callbackController.NotePassedThreshold -= PlaySound;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) soundListToUse++;
        if (Input.GetKeyDown(KeyCode.DownArrow)) soundListToUse--;
        if (soundListToUse < 0) soundListToUse = 0;
        if (soundListToUse >= soundLists.Length) soundListToUse = soundLists.Length - 1;
    }

    void PlaySound(bool initial, int index, BeatmapObject objectData) {
        if (initial && objectData.beatmapType != BeatmapObject.Type.BOMB)
        {
            SoundList list = soundLists[soundListToUse];
            bool shortCut = false;
            BeatmapObject first = null;
            BeatmapObject second = null;
            try
            {
                first = container.loadedNotes[index + DensityCheckOffset].objectData;
                second = container.loadedNotes[index - DensityCheckOffset].objectData;
            }
            catch { }
            if (first != null && second != null)
            {
                if (first._time - objectData._time <= ThresholdInNoteTime &&
                    objectData._time - second._time <= ThresholdInNoteTime)
                    shortCut = true;
            } 
            audioUtil.PlayOneShotSound(list.GetRandomClip(shortCut), 0.5f);
        }
    }

}
