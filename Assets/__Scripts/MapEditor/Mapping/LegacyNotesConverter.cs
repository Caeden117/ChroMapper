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
        List<BeatmapObject> ToSpawn = new List<BeatmapObject>();
        List<BeatmapObjectContainer> ToDestroy = new List<BeatmapObjectContainer>();
        if (BeatSaberSongContainer.Instance != null)
        {
            foreach (BeatmapObjectContainer container in notesContainer.LoadedContainers)
            {
                BeatmapNoteContainer note = container as BeatmapNoteContainer;
                BeatmapNoteContainer chromaBomb = notesContainer.LoadedContainers.FirstOrDefault(x =>
                    x.objectData._time == note.objectData._time && ((BeatmapNoteContainer) x).mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB &&
                    ((BeatmapNoteContainer) x).mapNoteData._lineIndex == note.mapNoteData._lineIndex &&
                    ((BeatmapNoteContainer) x).mapNoteData._lineLayer == note.mapNoteData._lineLayer) as BeatmapNoteContainer;
                if (chromaBomb != null && note.mapNoteData._type != BeatmapNote.NOTE_TYPE_BOMB) //Chroma note PogU
                {
                    BeatmapChromaNote chromaNote = new BeatmapChromaNote(note.mapNoteData);
                    chromaNote.BombRotation = chromaBomb.mapNoteData._cutDirection;
                    ToDestroy.Add(container);
                    ToDestroy.Add(chromaBomb);
                    ToSpawn.Add(chromaNote);
                }
            }
        }
        notesContainer.SortObjects();
        foreach (BeatmapObjectContainer con in ToDestroy) notesContainer.DeleteObject(con);
        foreach (BeatmapObject data in ToSpawn) notesContainer.SpawnObject(data, out _);
        SelectionController.RefreshMap();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }

    private IEnumerator ConvertToLegacy()
    {
        List<BeatmapObject> ToSpawn = new List<BeatmapObject>();
        List<BeatmapObjectContainer> ToDestroy = new List<BeatmapObjectContainer>();
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        if (BeatSaberSongContainer.Instance != null)
        {
            foreach (BeatmapObjectContainer container in notesContainer.LoadedContainers)
            {
                BeatmapNoteContainer note = container as BeatmapNoteContainer;
                if (note.mapNoteData is BeatmapChromaNote)
                {
                    ToSpawn.Add((note.mapNoteData as BeatmapChromaNote).ConvertToNote());
                    BeatmapNote bombData = new BeatmapNote((note.mapNoteData as BeatmapChromaNote).ConvertToNote().ConvertToJSON());
                    bombData._type = BeatmapNote.NOTE_TYPE_BOMB;
                    bombData._cutDirection = (note.mapNoteData as BeatmapChromaNote).BombRotation;
                    ToSpawn.Add(bombData);
                    ToDestroy.Add(note);
                }
            }
        }
        notesContainer.SortObjects();
        foreach (BeatmapObjectContainer con in ToDestroy) notesContainer.DeleteObject(con);
        foreach (BeatmapObject data in ToSpawn) notesContainer.SpawnObject(data, out _);
        SelectionController.RefreshMap();
        yield return PersistentUI.Instance.FadeOutLoadingScreen();
    }
}
