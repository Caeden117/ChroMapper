using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapNoteContainer : BeatmapObjectContainer
{
    private static readonly Color UNASSIGNED_COLOR = new Color(0.1544118f, 0.1544118f, 0.1544118f);

    private static readonly int AlwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");

    public override BeatmapObject objectData { get => mapNoteData; set => mapNoteData = (BeatmapNote)value; }

    public BeatmapNote mapNoteData;

    [SerializeField] GameObject simpleBlock;
    [SerializeField] GameObject complexBlock;

    [SerializeField] List<MeshRenderer> noteRenderer;
    [SerializeField] MeshRenderer bombRenderer;
    [SerializeField] MeshRenderer dotRenderer;
    [SerializeField] MeshRenderer arrowRenderer;
    [SerializeField] SpriteRenderer swingArcRenderer;

    public override void Setup()
    {
        base.Setup();

        if (simpleBlock != null)
        {
            simpleBlock.SetActive(Settings.Instance.SimpleBlocks);
            complexBlock.SetActive(!Settings.Instance.SimpleBlocks);

            MaterialPropertyBlock.SetFloat("_Lit", Settings.Instance.SimpleBlocks ? 0 : 1);

            UpdateMaterials();
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

    public void SetDotVisible(bool b) {
        dotRenderer.enabled = b;
    }

    public void SetArrowVisible(bool b) {
        arrowRenderer.enabled = b;
    }

    public void SetBomb(bool b)
    {
        simpleBlock.SetActive(!b && Settings.Instance.SimpleBlocks);
        complexBlock.SetActive(!b && !Settings.Instance.SimpleBlocks);

        bombRenderer.gameObject.SetActive(b);
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

    public override void UpdateGridPosition()
    {
        transform.localPosition = (Vector3)mapNoteData.GetPosition() +
            new Vector3(0, 0.5f, mapNoteData._time * EditorScaleController.EditorScale);
        transform.localScale = mapNoteData.GetScale() + new Vector3(0.5f, 0.5f, 0.5f);
        UpdateCollisionGroups();
        SetRotation(AssignedTrack?.RotationValue.y ?? 0);
    }

    private bool CurrentState = false;
    public void CheckTranslucent()
    {
        bool newState = transform.parent != null && (transform.localPosition.z + transform.parent.localPosition.z) <= BeatmapObjectContainerCollection.TranslucentCull;
        if (newState != CurrentState)
        {
            MaterialPropertyBlock.SetFloat(AlwaysTranslucent, newState ? 1 : 0);
            UpdateMaterials();
            CurrentState = newState;
        }
    }

    public void SetColor(Color? color)
    {
        MaterialPropertyBlock.SetColor(Color, color ?? UNASSIGNED_COLOR);
        UpdateMaterials();
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

    internal override void UpdateMaterials()
    {
        foreach (var renderer in noteRenderer)
        {
            renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        foreach (var renderer in selectionRenderers)
        {
            renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        bombRenderer.SetPropertyBlock(MaterialPropertyBlock);
    }
}
