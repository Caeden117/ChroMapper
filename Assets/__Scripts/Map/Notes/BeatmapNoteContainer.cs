using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapNoteContainer : BeatmapObjectContainer {

    public override BeatmapObject objectData { get => mapNoteData; set => mapNoteData = (BeatmapNote)value; }

    public BeatmapNote mapNoteData;

    [SerializeField] GameObject simpleBlock;
    [SerializeField] GameObject complexBlock;

    [SerializeField] List<MeshRenderer> noteRenderer;
    [SerializeField] MeshRenderer bombRenderer;
    [SerializeField] MeshRenderer dotRenderer;
    [SerializeField] MeshRenderer arrowRenderer;
    [SerializeField] SpriteRenderer swingArcRenderer;

    private Color bombColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);

    public override void Setup()
    {
        if (!ModelMaterials.Any())
        {
            base.Setup();
        }

        if (simpleBlock != null)
        {
            simpleBlock.SetActive(Settings.Instance.SimpleBlocks);
            complexBlock.SetActive(!Settings.Instance.SimpleBlocks);
            if (Settings.Instance.SimpleBlocks)
            {
                dotRenderer.material.EnableKeyword("_EMISSION");
                arrowRenderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                dotRenderer.material.DisableKeyword("_EMISSION");
                arrowRenderer.material.DisableKeyword("_EMISSION");
            }

            foreach (Renderer renderer in noteRenderer)
            {
                var material = renderer.materials.First();
                material.SetFloat("_Lit", Settings.Instance.SimpleBlocks ? 0 : 1);
            }
        }

        SetArcVisible(NotesContainer.ShowArcVisualizer);
        CheckTranslucent();
    }

    internal static Vector3 Directionalize(BeatmapNote mapNoteData)
    {
        if (mapNoteData is null) return Vector3.zero;
        Vector3 directionEuler = Vector3.zero;
        int cutDirection = mapNoteData._cutDirection;
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
        if (mapNoteData._customData?.HasKey("_cutDirection") ?? false)
        {
            directionEuler = new Vector3(0, 0, mapNoteData._customData["_cutDirection"]?.AsFloat ?? 0);
        }
        else
        {
            if (cutDirection >= 1000) directionEuler += new Vector3(0, 0, 360 - (cutDirection - 1000));
        }
        return directionEuler;
    }

    public void SetModelMaterial(Material m) {
        noteRenderer.ForEach(it => it.sharedMaterial = m);
    }

    public void SetDotVisible(bool b) {
        dotRenderer.enabled = b;
    }

    public void SetArrowVisible(bool b) {
        arrowRenderer.enabled = b;
    }

    public void SetBomb(bool b)
    {
        noteRenderer.ForEach(it => it.enabled = !b);
        bombRenderer.enabled = b;
    }

    public void SetArcVisible(bool ShowArcVisualizer)
    {
        if (swingArcRenderer != null) swingArcRenderer.enabled = ShowArcVisualizer;
    }

    public static BeatmapNoteContainer SpawnBeatmapNote(BeatmapNote noteData, ref GameObject notePrefab) {
        BeatmapNoteContainer container = Instantiate(notePrefab).GetComponent<BeatmapNoteContainer>();
        container.mapNoteData = noteData;
        container.transform.localEulerAngles = Directionalize(noteData);
        return container;
    }

    public override void UpdateGridPosition() {
        transform.localPosition = (Vector3)mapNoteData.GetPosition() +
            new Vector3(0, 0.5f, mapNoteData._time * EditorScaleController.EditorScale);
        transform.localScale = mapNoteData.GetScale() + new Vector3(0.5f, 0.5f, 0.5f);
        

        noteRenderer.ForEach(it =>
        {
            if (it.material.HasProperty("_Rotation"))
                it.material.SetFloat("_Rotation", AssignedTrack?.RotationValue.y ?? 0);
        });
    }

    private bool CurrentState = false;
    public void CheckTranslucent()
    {
        bool newState = transform.parent != null && (transform.localPosition.z + transform.parent.localPosition.z) <= BeatmapObjectContainerCollection.TranslucentCull;
        if (newState != CurrentState) {
            noteRenderer.ForEach(it =>
            {
                if (it.material.HasProperty("_AlwaysTranslucent"))
                    it.material.SetFloat("_AlwaysTranslucent", newState ? 1 : 0);
            });
            CurrentState = newState;
        }
    }

    public void SetColor(Color? color)
    {
        noteRenderer.ForEach(it => it.material.SetColor("_Color", color ?? bombColor));
        bombRenderer.material.SetColor("_Color", color ?? bombColor);
    }

    public override void AssignTrack(Track track)
    {
        if (AssignedTrack != null)
        {
            AssignedTrack.OnTimeChanged -= CheckTranslucent;
        }

        base.AssignTrack(track);
        track.OnTimeChanged += CheckTranslucent;
    }
}
