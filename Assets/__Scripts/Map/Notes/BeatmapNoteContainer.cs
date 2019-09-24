using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapNoteContainer : BeatmapObjectContainer {

    public override BeatmapObject objectData {
        get {
            return mapNoteData;
        }
    }

    [SerializeField]
    private float _timeForEditor = 0;

    public BeatmapNote mapNoteData;
    public bool isBomb = false;

    [SerializeField]
    MeshRenderer modelRenderer;

    [SerializeField]
    SpriteRenderer dotRenderer;

    [SerializeField]
    MeshRenderer arrowRenderer;

    [SerializeField]
    NoteAppearanceSO noteAppearance;

    public void Directionalize(int cutDirection)
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
            default: break;
        }
        if (cutDirection >= 1000) directionEuler += new Vector3(0, 0, 360 - (cutDirection - 1000));
        transform.rotation = Quaternion.Euler(directionEuler);
    }

    public void SetModelMaterial(Material m) {
        modelRenderer.sharedMaterial = m;
    }

    public void SetDotVisible(bool b) {
        dotRenderer.enabled = b;
    }

    public void SetArrowVisible(bool b) {
        arrowRenderer.enabled = b;
    }

    public void SetDotSprite(Sprite sprite) {
        dotRenderer.sprite = sprite;
    }

    public static BeatmapNoteContainer SpawnBeatmapNote(BeatmapNote noteData, ref GameObject notePrefab, ref GameObject bombPrefab, ref NoteAppearanceSO appearanceSO) {
        bool isBomb = noteData._type == BeatmapNote.NOTE_TYPE_BOMB;
        BeatmapNoteContainer container = Instantiate(isBomb ? bombPrefab : notePrefab).GetComponent<BeatmapNoteContainer>();
        container.isBomb = isBomb;
        container.mapNoteData = noteData;
        container.noteAppearance = appearanceSO;
        appearanceSO.SetNoteAppearance(container);
        container.Directionalize(noteData._cutDirection);
        container._timeForEditor = noteData._time;
        return container;
    }

    public override void UpdateGridPosition() {
        float position = mapNoteData._lineIndex - 1.5f;
        float layer = mapNoteData._lineLayer + 0.5f;
        if (mapNoteData._lineIndex >= 1000)
            position = ((float)mapNoteData._lineIndex / 1000f) - 2.5f;
        else if (mapNoteData._lineIndex <= -1000)
            position = ((float)mapNoteData._lineIndex / 1000f) - 0.5f;
        if (mapNoteData._lineLayer >= 1000 || mapNoteData._lineLayer <= -1000)
            layer = ((float)mapNoteData._lineLayer / 1000f) - 0.5f;
        transform.localPosition = new Vector3(
            position,
            layer,
            mapNoteData._time * EditorScaleController.EditorScale
            );
    }

    internal override void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(2) && !KeybindsController.ShiftHeld)
        {
            if (mapNoteData is BeatmapChromaNote chroma) mapNoteData = chroma.originalNote; //Revert Chroma status, then invert types
            mapNoteData._type = mapNoteData._type == BeatmapNote.NOTE_TYPE_A ? BeatmapNote.NOTE_TYPE_B : BeatmapNote.NOTE_TYPE_A;
            noteAppearance.SetNoteAppearance(this);
        }else if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            if (KeybindsController.AltHeld)
            {
                if (mapNoteData._cutDirection == BeatmapNote.NOTE_CUT_DIRECTION_ANY) return;
                mapNoteData._cutDirection += (Input.GetAxis("Mouse ScrollWheel") > 0 ? -1 : 1);
                if (mapNoteData._cutDirection == -1) mapNoteData._cutDirection = 7;
                else if (mapNoteData._cutDirection == 8) mapNoteData._cutDirection = 0;
                Directionalize(mapNoteData._cutDirection);
            }
        }
        else base.OnMouseOver();
    }
}
