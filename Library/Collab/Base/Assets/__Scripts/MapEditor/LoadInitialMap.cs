using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadInitialMap : MonoBehaviour {

    [SerializeField] GameObject redNotePrefab;
    [SerializeField] GameObject blueNotePrefab;
    [SerializeField] GameObject bombPrefab;
    [Space]
    [SerializeField] GameObject redAltNotePrefab;
    [SerializeField] GameObject blueAltNotePrefab;
    [Space]
    [SerializeField] GameObject monochromeNotePrefab;
    [SerializeField] GameObject duochromeNotPrefab;
    [Space]
    [SerializeField] GameObject unassignedNote;
    [Space]
    [SerializeField] NotesContainer notesContainer;

    private BeatSaberMap map;
    private BeatSaberSong.DifficultyData data;

    void Start()
    {
        StartCoroutine(LoadMap());
    }

    public IEnumerator LoadMap()
    {
        if (BeatSaberSongContainer.Instance == null) yield break;
        float offset = 0;
        yield return PersistentUI.Instance.FadeInLoadingScreen();
        try
        {
            map = BeatSaberSongContainer.Instance.map;
            data = BeatSaberSongContainer.Instance.difficultyData;
            offset = (map._beatsPerMinute / 60) * (data.offset / 1000) * (map._beatsPerBar / 4);
        }
        catch
        {
            Debug.Log("No mapping for you!");
        }
        finally
        {
            if (map != null)
            {
                foreach (MapNote noteData in map._notes)
                {
                    GameObject note;
                    bool isBomb = false;
                    switch (noteData._type)
                    {
                        case MapNote.NOTE_TYPE_A:
                            note = Instantiate(redNotePrefab);
                            break;
                        case MapNote.NOTE_TYPE_B:
                            note = Instantiate(blueNotePrefab);
                            break;
                        case MapNote.NOTE_TYPE_BOMB: //KIWI THIS ISNT BOMB
                            note = Instantiate(bombPrefab);
                            isBomb = true;
                            break;
                        case 3: //THIS IS
                            note = Instantiate(bombPrefab);
                            isBomb = true;
                            break;
                        default:
                            note = Instantiate(unassignedNote);
                            break;
                    }
                    Vector3 direction = new Vector3(0, 0, 0);
                    bool useDot = noteData._cutDirection == MapNote.NOTE_CUT_DIRECTION_ANY;
                    switch (noteData._cutDirection)
                    {
                        case MapNote.NOTE_CUT_DIRECTION_UP: direction += new Vector3(0, 0, 180); break;
                        case MapNote.NOTE_CUT_DIRECTION_DOWN: direction += new Vector3(0, 0, 0); break;
                        case MapNote.NOTE_CUT_DIRECTION_LEFT: direction += new Vector3(0, 0, -90); break;
                        case MapNote.NOTE_CUT_DIRECTION_RIGHT: direction += new Vector3(0, 0, 90); break;
                        case MapNote.NOTE_CUT_DIRECTION_UP_RIGHT: direction += new Vector3(0, 0, 135); break;
                        case MapNote.NOTE_CUT_DIRECTION_UP_LEFT: direction += new Vector3(0, 0, -135); break;
                        case MapNote.NOTE_CUT_DIRECTION_DOWN_LEFT: direction += new Vector3(0, 0, -45); break;
                        case MapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT: direction += new Vector3(0, 0, 45); break;
                        default: direction += new Vector3(0, 0, 0); break;
                    }
                    if (!isBomb)
                    {
                        if (useDot)
                            note.transform.Find("Direction").gameObject.SetActive(false);
                        else
                            note.transform.Find("NoteDot").gameObject.SetActive(false);
                    }
                    note.transform.rotation = Quaternion.Euler(direction);
                    note.transform.SetParent(transform);
                    note.transform.position = new Vector3(
                        noteData._lineIndex - 1.5f,
                        noteData._lineLayer + 0.5f,
                        noteData._time * (map._beatsPerBar / 4)
                        );
                    foreach (Renderer renderer in note.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                    BeatmapDescriptor descriptor = note.AddComponent<BeatmapDescriptor>();
                    descriptor.beatmapObject = noteData;
                    notesContainer.loadedNotes.Add(descriptor);
                }
            }
            PersistentUI.Instance.FadeOutLoadingScreen();
        }
    }
}
