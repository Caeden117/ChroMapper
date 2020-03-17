using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PastNotesWorker : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private NotesContainer notesContainer;
    [SerializeField] private GameObject gridNotePrefab;
    [SerializeField] private BeatmapObjectCallbackController callbackController;
    [SerializeField] private NoteAppearanceSO noteAppearance;

    private Transform notes;
    private Canvas _canvas;

    private readonly float _tolerance = 0.25f;
    private readonly float _gridSize = 25f;
    private Dictionary<int, BeatmapNote> lastByType = new Dictionary<int, BeatmapNote>(); //Used to improve performance
    private float scale = 0;

    private Dictionary<int, Dictionary<GameObject, Image>> InstantiatedNotes = new Dictionary<int, Dictionary<GameObject, Image>>();

    private void Start()
    {
        _canvas = GetComponent<Canvas>();
        scale = Settings.Instance.PastNotesGridScale;
        _canvas.enabled = scale != 0f;
        transform.localScale = Vector3.one * (scale + 0.25f);
        if (scale == 0f) return;

        notes = transform.GetChild(0);
        callbackController.NotePassedThreshold += NotePassedThreshold;
        Settings.NotifyBySettingName("PastNotesGridScale", UpdatePastNotesGridScale);
    }

    private void UpdatePastNotesGridScale(object obj)
    {
        scale = (float)obj;
        _canvas.enabled = scale != 0f;
        transform.localScale = Vector3.one * (scale + 0.25f);
    }

    private void OnDestroy()
    {
        callbackController.NotePassedThreshold -= NotePassedThreshold;
        Settings.ClearSettingNotifications("PastNotesGridScale");
    }

    private bool _firstLoad = true;

    private void NotePassedThreshold(bool natural, int id, BeatmapObject obj)
    {
        BeatmapNote note = obj as BeatmapNote;

        if (!InstantiatedNotes.ContainsKey(note._type))
        {
            InstantiatedNotes.Add(note._type, new Dictionary<GameObject, Image>());
        }

        if (lastByType.TryGetValue(note._type, out BeatmapNote lastInTime) && lastInTime?._time != obj._time)
        {
            foreach (KeyValuePair<GameObject, Image> child in InstantiatedNotes[note._type]) child.Key.SetActive(false);
        }

        if (note._type == BeatmapNote.NOTE_TYPE_BOMB) return;
        float gridPosX = note._lineIndex, gridPosY = note._lineLayer;

        if (gridPosX >= 1000) gridPosX = gridPosX / 1000 - 1f;
        else if (gridPosX <= -1000f) gridPosX = gridPosX / 1000f + 1f;

        if (gridPosY >= 1000) gridPosY = gridPosY / 1000f - 1f;
        else if (gridPosY <= -1000f) gridPosY = gridPosY / 1000f + 1f;

        GameObject g; //Instead of instantiating new objects every frame (Bad on performance), we are instead using a pooled system to use
        Image img; //Already existing notes, and only create ones we need.
        if (InstantiatedNotes[note._type].Any(x => !x.Key.activeSelf))
        {
            g = InstantiatedNotes[note._type].First(x => !x.Key.activeSelf).Key;
            img = InstantiatedNotes[note._type][g];
            g.SetActive(true);
            g.transform.SetSiblingIndex(g.transform.parent.childCount);
            foreach (Transform child in g.transform) child.gameObject.SetActive(true);
        }
        else
        {
            g = Instantiate(gridNotePrefab, notes.transform, true);
            img = g.GetComponent<Image>();
            InstantiatedNotes[note._type].Add(g, img);
        }

        var transform1 = img.transform;
        transform1.localPosition = new Vector3(_gridSize * gridPosX, _gridSize * gridPosY, 1);
        float sc = scale / 10f + .06f;
        transform1.localScale = new Vector3(sc, sc); //I have to do this because the UI scaling is weird

        //transform1.rotation = o.transform.rotation; //This code breaks when using 360 maps; use local rotation instead.
        transform1.localEulerAngles = Vector3.forward * Directionalize(note._cutDirection).z; //Sets the rotation of the image to match the same rotation as the block
        img.color = note._type == BeatmapNote.NOTE_TYPE_A ? noteAppearance.RedInstance.color : noteAppearance.BlueInstance.color;

        bool dotEnabled = note._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY; //Checks to see if the Dot is visible on the block

        if (dotEnabled) g.transform.GetChild(0).gameObject.SetActive(false);
        else g.transform.GetChild(1).gameObject.SetActive(false);
        img.enabled = true;

        if (!lastByType.ContainsKey(note._type))
        {
            lastByType.Add(note._type, note);
        }
        else
        {
            lastByType[note._type] = note;
        }
    }

    private Vector3 Directionalize(int cutDirection) //TODO move this to a static function to share with BeatmapNoteContainer
    {
        Vector3 directionEuler = Vector3.zero;
        switch (cutDirection)
        {
            case BeatmapNote.NOTE_CUT_DIRECTION_UP: directionEuler += new Vector3(0, 0, 180); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_DOWN: directionEuler += new Vector3(0, 0, 0); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_LEFT: directionEuler += new Vector3(0, 0, -90); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_RIGHT: directionEuler += new Vector3(0, 0, 90); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT: directionEuler += new Vector3(0, 0, 135); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT: directionEuler += new Vector3(0, 0, -135); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT: directionEuler += new Vector3(0, 0, -45); break;
            case BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT: directionEuler += new Vector3(0, 0, 45); break;
        }
        if (cutDirection >= 1000) directionEuler += new Vector3(0, 0, 360 - (cutDirection - 1000));
        return directionEuler;
    }
}
