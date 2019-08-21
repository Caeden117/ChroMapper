using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NotePreview : MonoBehaviour {

    [SerializeField] NoteAppearanceSO noteAppearance;
    [SerializeField] GameObject unassignedNote;
    [SerializeField] GameObject bomb;
    [SerializeField] AudioTimeSyncController atsc;
    [SerializeField] Transform notesGrid;
    [SerializeField] NotesContainer notesContainer;
    
    public static bool IsActive = false;

    private GameObject hoverNote;
    private GameObject hoverBomb;
    private static NotePreview instance;
    private static BeatmapNoteContainer container; //Easily edit the hover note properties here (using UI)
    private BeatSaberMap map;

    private static int QueuedType = -1; //For changing stuff before preview note spawns.
    private static int QueuedDirection = -1;
    private static int QueuedChromaType = -1;
    private static bool QueuedIsChromaNote = false; 
    private static bool IsChromaNote = false;

    void Start()
    {
        instance = this;
        map = BeatSaberSongContainer.Instance.map;
    }

    void OnMouseOver()
    {
        if (PauseManager.IsPaused) return;
        if (NotePreviewController.Instance.PlacingWall || NodeEditorController.IsActive ) {
            if (hoverNote == null || hoverBomb == null) return;
            if (hoverNote.activeInHierarchy) hoverNote.SetActive(false);
            if (hoverBomb.activeInHierarchy) hoverBomb.SetActive(false);
            IsActive = false;
            return;
        };
        if (hoverNote == null) RefreshHovers();
        if (atsc.IsPlaying && hoverNote.activeInHierarchy) hoverNote.SetActive(false);
        if (!Input.GetMouseButton(1) && !atsc.IsPlaying)
        {
            IsActive = true;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1 << 10))
            {
                hoverNote.transform.position = new Vector3(
                    Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f), 
                        Mathf.Ceil(GetComponent<MeshCollider>().bounds.min.x),
                        Mathf.Floor(GetComponent<MeshCollider>().bounds.max.x)
                    ) - 0.5f,
                    Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                        Mathf.Floor(GetComponent<MeshCollider>().bounds.max.y)) + 0.5f,
                    0);
            }
            hoverBomb.transform.position = hoverNote.transform.position;
            if (container.mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB)
            {
                hoverBomb.SetActive(true);
                hoverNote.SetActive(false);
            }
            else
            {
                hoverBomb.SetActive(false);
                hoverNote.SetActive(true);
            }
            container.mapNoteData._lineIndex = Mathf.RoundToInt(hoverNote.transform.position.x + 1.5f);
            container.mapNoteData._lineLayer = Mathf.RoundToInt(hoverNote.transform.position.y - 0.5f);
        }
        else if(hoverNote.activeInHierarchy) OnMouseExit();
        if (Input.GetMouseButtonDown(0) && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl)))
            ApplyNoteToMap();
        if (Input.GetKeyDown(KeyCode.Delete) ||
            (KeybindsController.ShiftHeld && Input.GetMouseButtonDown(2))) DeleteHoveringNote();
    }

    void OnMouseExit()
    {
        IsActive = false;
        if (hoverNote != null) hoverNote.SetActive(false);
    }

    public static void UpdateChromaNote(bool value)
    {
        IsChromaNote = value;
        try
        {
            if (!IsChromaNote && container.mapNoteData is BeatmapChromaNote)
                container.mapNoteData = (container.mapNoteData as BeatmapChromaNote).originalNote;
            else if (IsChromaNote && !(container.mapNoteData is BeatmapChromaNote))
            {
                BeatmapChromaNote chromaNote = new BeatmapChromaNote(container.mapNoteData);
                chromaNote.BombRotation = QueuedChromaType;
                container.mapNoteData = chromaNote;
            }
        }
        catch { QueuedIsChromaNote = value; }
        UpdateHoverNote();
    }

    public static void UpdateChromaType(int type)
    {
        try
        {
            QueuedChromaType = type;
            if (IsChromaNote) (container.mapNoteData as BeatmapChromaNote).BombRotation = type;
            UpdateHoverNote();
        }
        catch { QueuedChromaType = type; }
    }

    public static void UpdateHoverNoteType(int type)
    {
        try
        {
            QueuedType = type;
            container.mapNoteData._type = type;
            UpdateHoverNote();
        }
        catch { QueuedType = type; }
    }

    public static void UpdateHoverNoteDirection(int direction)
    {
        try
        {
            QueuedDirection = direction;
            container.mapNoteData._cutDirection = direction;
            container.Directionalize(direction);
            UpdateHoverNote();
        }
        catch { QueuedDirection = direction; }
    }

    private static void UpdateHoverNote() //Easily update visuals here.
    {
        if (instance.hoverNote != null) instance.noteAppearance.SetNoteAppearance(container);
    }

    void ApplyNoteToMap()
    {
        if (atsc.IsPlaying || NodeEditorController.IsActive) return; //woops forgot about this
        //Remove any note that's in the same spot.
        BeatmapObjectContainer conflicting = notesContainer.loadedNotes.Where(
            (BeatmapObjectContainer x) => x.objectData._time >= atsc.CurrentBeat - 1 / 64f && //Check time, within a small margin
                x.objectData._time <= atsc.CurrentBeat + 1 / 64f && //Check time, within a small margin
            (x.objectData as BeatmapNote)._lineIndex == container.mapNoteData._lineIndex && //Check index
            (x.objectData as BeatmapNote)._lineLayer == container.mapNoteData._lineLayer //Check layer
            ).FirstOrDefault(); //Grab first instance (Or null if there is none)
        if (conflicting != null)
        {
            notesContainer.loadedNotes.Remove(conflicting); //Then lets remove it if there is a conflicting note.
            Destroy(conflicting.gameObject);
        }

        container.mapNoteData._time = atsc.CurrentBeat;
        BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote(container.mapNoteData, ref unassignedNote, ref bomb, ref noteAppearance);
        beatmapNote.transform.SetParent(notesGrid);
        beatmapNote.UpdateGridPosition();
        notesContainer.loadedNotes.Add(beatmapNote);
        notesContainer.SortNotes();
        List<BeatmapNote> newNotes = new List<BeatmapNote>();
        foreach (BeatmapNoteContainer con in notesContainer.loadedNotes) newNotes.Add(con.mapNoteData);
        BeatSaberSongContainer.Instance.map._notes = newNotes; //Wont be saved until user clicks Save button
        QueuedType = container.mapNoteData._type;
        QueuedDirection = container.mapNoteData._cutDirection;
        QueuedIsChromaNote = IsChromaNote;
        if (IsChromaNote)
            QueuedChromaType = (container.mapNoteData as BeatmapChromaNote).BombRotation;
        RefreshHovers();
        SelectionController.RefreshMap();
    }

    void DeleteHoveringNote()
    {
        BeatmapObjectContainer conflicting = notesContainer.loadedNotes.Where(
            (BeatmapObjectContainer x) => x.objectData._time >= atsc.CurrentBeat - 1 / 64f && //Check time, within a small margin
                x.objectData._time <= atsc.CurrentBeat + 1 / 64f && //Check time, within a small margin
            (x.objectData as BeatmapNote)._lineIndex == container.mapNoteData._lineIndex &&
            (x.objectData as BeatmapNote)._lineLayer == container.mapNoteData._lineLayer
            ).FirstOrDefault();
        if (conflicting == null) return;
        notesContainer.loadedNotes.Remove(conflicting);
        Destroy(conflicting.gameObject);
        SelectionController.RefreshMap();
    }

    void RefreshHovers()
    {
        IsChromaNote = QueuedIsChromaNote;
        if (hoverNote != null)
        {
            Destroy(hoverNote);
            Destroy(hoverBomb);
        }
        hoverNote = Instantiate(unassignedNote);
        hoverNote.name = "Hover Note";
        container = hoverNote.GetComponent<BeatmapNoteContainer>();
        if (QueuedType != -1) container.mapNoteData._type = QueuedType;
        if (QueuedDirection != -1) container.mapNoteData._cutDirection = (QueuedType == BeatmapNote.NOTE_TYPE_BOMB) ? BeatmapNote.NOTE_CUT_DIRECTION_DOWN : QueuedDirection;
        else container.mapNoteData._cutDirection = BeatmapNote.NOTE_CUT_DIRECTION_DOWN;
        if (IsChromaNote)
        {
            BeatmapChromaNote chromaNote = new BeatmapChromaNote(container.mapNoteData);
            chromaNote.BombRotation = QueuedChromaType;
            container.mapNoteData = chromaNote;
        }
        hoverBomb = Instantiate(bomb);
        hoverBomb.SetActive(false);
        hoverBomb.name = "Hover Bomb";
        UpdateHoverNote();
        container.Directionalize(QueuedDirection);
        if (container.mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB) hoverNote.SetActive(false);
        else hoverBomb.SetActive(false);
        QueuedType = QueuedDirection = QueuedChromaType = -1;
    }
}
