using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegacyNotesConverter : MonoBehaviour {

    public NotesContainer notesContainer;

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
            foreach (BeatmapObjectContainer container in notesContainer.LoadedContainers)
            {
                BeatmapNoteContainer note = container as BeatmapNoteContainer;
                BeatmapNoteContainer chromaBomb = notesContainer.LoadedContainers.Where((BeatmapObjectContainer x) =>
                    x.objectData._time == note.objectData._time && (x as BeatmapNoteContainer).mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB &&
                    (x as BeatmapNoteContainer).mapNoteData._lineIndex == note.mapNoteData._lineIndex &&
                    (x as BeatmapNoteContainer).mapNoteData._lineLayer == note.mapNoteData._lineLayer
                ).FirstOrDefault() as BeatmapNoteContainer;
                if (chromaBomb != null && note.mapNoteData._type != BeatmapNote.NOTE_TYPE_BOMB) //Chroma note PogU
                {
                    BeatmapChromaNote chromaNote = new BeatmapChromaNote(note.mapNoteData);
                    chromaNote.BombRotation = chromaBomb.mapNoteData._cutDirection;
                    notesContainer.DeleteObject(container);
                    notesContainer.DeleteObject(chromaBomb);
                    notesContainer.SpawnObject(chromaNote);
                }
            }
        }
        notesContainer.SortObjects();
        SelectionController.RefreshMap();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    private IEnumerator ConvertToLegacy()
    {
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        if (BeatSaberSongContainer.Instance != null)
        {
            foreach (BeatmapObjectContainer container in notesContainer.LoadedContainers)
            {
                BeatmapNoteContainer note = container as BeatmapNoteContainer;
                if (note.mapNoteData is BeatmapChromaNote)
                {
                    notesContainer.SpawnObject((note.mapNoteData as BeatmapChromaNote).ConvertToNote());
                    BeatmapNote bombData = new BeatmapNote((note.mapNoteData as BeatmapChromaNote).ConvertToNote().ConvertToJSON());
                    bombData._type = BeatmapNote.NOTE_TYPE_BOMB;
                    bombData._cutDirection = (note.mapNoteData as BeatmapChromaNote).BombRotation;
                    notesContainer.SpawnObject(bombData);
                    notesContainer.DeleteObject(note);
                }
            }
        }
        notesContainer.SortObjects();
        SelectionController.RefreshMap();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
