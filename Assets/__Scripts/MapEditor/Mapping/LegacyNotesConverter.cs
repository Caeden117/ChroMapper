using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegacyNotesConverter : MonoBehaviour {

    public NotesContainer notesContainer;
    [SerializeField] NoteAppearanceSO noteAppearanceSO;
    [SerializeField] GameObject notePrefab;
    [SerializeField] GameObject bombPrefab;
    [SerializeField] Transform notesGrid;

    private BeatSaberMap map;
    private List<BeatmapObjectContainer> ToRemove = new List<BeatmapObjectContainer>();
    private List<BeatmapObjectContainer> ToAdd = new List<BeatmapObjectContainer>();

    public void ConvertFrom()
    {
       StartCoroutine(ConvertFromLegacy()); 
    }

    public void ConvertTo()
    {
        StartCoroutine(ConvertToLegacy());
    }

    private IEnumerator ConvertFromLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        if (BeatSaberSongContainer.Instance != null)
        {
            map = BeatSaberSongContainer.Instance.map;
            foreach (BeatmapObjectContainer container in notesContainer.loadedNotes)
            {
                BeatmapNoteContainer note = container as BeatmapNoteContainer;
                BeatmapNoteContainer chromaBomb = notesContainer.loadedNotes.Where((BeatmapObjectContainer x) =>
                    x.objectData._time == note.objectData._time && (x as BeatmapNoteContainer).mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB &&
                    (x as BeatmapNoteContainer).mapNoteData._lineIndex == note.mapNoteData._lineIndex &&
                    (x as BeatmapNoteContainer).mapNoteData._lineLayer == note.mapNoteData._lineLayer
                ).FirstOrDefault() as BeatmapNoteContainer;
                if (chromaBomb != null && note.mapNoteData._type != BeatmapNote.NOTE_TYPE_BOMB) //Chroma note PogU
                {
                    BeatmapChromaNote chromaNote = new BeatmapChromaNote(note.mapNoteData);
                    chromaNote.BombRotation = chromaBomb.mapNoteData._cutDirection;
                    Destroy(container.gameObject);
                    Destroy(chromaBomb.gameObject);
                    ToRemove.Add(note);
                    ToRemove.Add(chromaBomb);
                    BeatmapNoteContainer chromaNoteContainer = BeatmapNoteContainer.SpawnBeatmapNote(chromaNote, ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
                    chromaNoteContainer.transform.SetParent(notesGrid);
                    chromaNoteContainer.UpdateGridPosition();
                    ToAdd.Add(chromaNoteContainer);
                }
            }
        }
        foreach (BeatmapObjectContainer container in ToAdd) notesContainer.loadedNotes.Add(container);
        foreach (BeatmapObjectContainer container in ToRemove) notesContainer.loadedNotes.Remove(container);
        notesContainer.SortNotes();
        ToAdd.Clear();
        ToRemove.Clear();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    private IEnumerator ConvertToLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        if (BeatSaberSongContainer.Instance != null)
        {
            map = BeatSaberSongContainer.Instance.map;
            foreach (BeatmapObjectContainer container in notesContainer.loadedNotes)
            {
                BeatmapNoteContainer note = container as BeatmapNoteContainer;
                if (note.mapNoteData is BeatmapChromaNote)
                {
                    BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote((note.mapNoteData as BeatmapChromaNote).ConvertToNote(), ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
                    beatmapNote.transform.SetParent(notesGrid);
                    beatmapNote.UpdateGridPosition();
                    BeatmapNote bombData = new BeatmapNote((note.mapNoteData as BeatmapChromaNote).ConvertToNote().ConvertToJSON());
                    bombData._type = BeatmapNote.NOTE_TYPE_BOMB;
                    bombData._cutDirection = (note.mapNoteData as BeatmapChromaNote).BombRotation;
                    BeatmapNoteContainer beatmapBomb = BeatmapNoteContainer.SpawnBeatmapNote(bombData, ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
                    beatmapBomb.transform.SetParent(notesGrid);
                    beatmapBomb.UpdateGridPosition();
                    ToAdd.Add(beatmapNote);
                    ToAdd.Add(beatmapBomb);
                    Destroy(note.gameObject);
                    ToRemove.Add(note);
                }
            }
        }
        foreach (BeatmapObjectContainer container in ToAdd) notesContainer.loadedNotes.Add(container);
        foreach (BeatmapObjectContainer container in ToRemove) notesContainer.loadedNotes.Remove(container);
        notesContainer.SortNotes();
        ToAdd.Clear();
        ToRemove.Clear();
        List<BeatmapNote> newNotes = new List<BeatmapNote>();
        foreach (BeatmapNoteContainer con in notesContainer.loadedNotes) newNotes.Add(con.mapNoteData);
        BeatSaberSongContainer.Instance.map._notes = newNotes; //Wont be saved until user clicks Save button
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
