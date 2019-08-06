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
}
