using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadInitialMap : MonoBehaviour {

    [SerializeField] Transform notesGrid;
    [SerializeField] Transform eventsGrid;
    [Space]
    [SerializeField] NoteAppearanceSO noteAppearanceSO;
    [Space]
    [SerializeField] GameObject notePrefab;
    [SerializeField] GameObject bombPrefab;
    [Space]
    [SerializeField] NotesContainer notesContainer;

    private BeatSaberMap map;
    private BeatSaberSong.DifficultyData data;

    void Awake()
    {
        //StartCoroutine(LoadMap());
        SceneTransitionManager.Instance.AddLoadRoutine(LoadMap());
    }

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        float offset = 0;
        try {

            map = BeatSaberSongContainer.Instance.map;
            data = BeatSaberSongContainer.Instance.difficultyData;
            offset = (map._beatsPerMinute / 60) * (data.offset / 1000) * (map._beatsPerBar / 4);
            
            if (map != null) {
                foreach (BeatmapNote noteData in map._notes) {
                    BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote(noteData, ref notePrefab, ref bombPrefab, ref noteAppearanceSO); ;
                    /*switch (noteData._type) {
                        case BeatmapNote.NOTE_TYPE_A:
                            beatmapObject = BeatmapNoteContainer.SpawnBeatmapNote(noteData, ref notePrefab, ref noteAppearanceSO, false);
                            break;
                        case BeatmapNote.NOTE_TYPE_B:
                            beatmapObject = BeatmapNoteContainer.SpawnBeatmapNote(noteData, ref notePrefab, ref noteAppearanceSO, false);
                            break;
                        case BeatmapNote.NOTE_TYPE_BOMB:
                            beatmapObject = null;// Instantiate(bombPrefab);
                            break;
                        default:
                            //We're gonna default to a note for this one chief
                            beatmapObject = BeatmapNoteContainer.SpawnBeatmapNote(noteData, ref notePrefab, ref noteAppearanceSO, false);
                            break;
                    }*/
                    beatmapNote.transform.SetParent(notesGrid);
                    beatmapNote.UpdateGridPosition(map);
                    notesContainer.loadedNotes.Add(beatmapNote);
                }
                notesContainer.SortNotes();
            }

        } catch (Exception e) {
            Debug.LogWarning("No mapping for you!");
            Debug.LogException(e);
        }
    }
}
