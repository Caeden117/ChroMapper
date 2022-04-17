using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapNoteContainer : BeatmapObjectContainer
{
    private static readonly Color unassignedColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);

    [FormerlySerializedAs("mapNoteData")] public BeatmapNote MapNoteData;

    [SerializeField] private GameObject simpleBlock;
    [SerializeField] private GameObject complexBlock;

    [SerializeField] private List<MeshRenderer> noteRenderer;
    [SerializeField] private MeshRenderer bombRenderer;
    [SerializeField] private MeshRenderer dotRenderer;
    [SerializeField] private MeshRenderer arrowRenderer;
    [SerializeField] private SpriteRenderer swingArcRenderer;

    public override BeatmapObject ObjectData { get => MapNoteData; set => MapNoteData = (BeatmapNote)value; }

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
    }

    internal static Vector3 Directionalize(BeatmapNote mapNoteData)
    {
        if (mapNoteData is null) return Vector3.zero;
        int cutDirection = mapNoteData.CutDirection;
        var directionEuler = Directionalize(cutDirection);
        if (mapNoteData.CustomData?.HasKey("_cutDirection") ?? false)
        {
            directionEuler = new Vector3(0, 0, mapNoteData.CustomData["_cutDirection"]?.AsFloat ?? 0);
        }
        else if ((mapNoteData is BeatmapColorNote newMapNoteData) && (newMapNoteData?.AngleOffset != 0)) directionEuler += new Vector3(0, 0, newMapNoteData.AngleOffset);
        else
        {
            if (cutDirection >= 1000) directionEuler += new Vector3(0, 0, 360 - (cutDirection - 1000));
        }

        return directionEuler;
    }

    internal static Vector3 Directionalize(int cutDirection)
    {
        var directionEuler = Vector3.zero;
        switch (cutDirection)
        {
            case BeatmapNote.NoteCutDirectionUp:
                directionEuler += new Vector3(0, 0, 180);
                break;
            case BeatmapNote.NoteCutDirectionDown:
                directionEuler += new Vector3(0, 0, 0);
                break;
            case BeatmapNote.NoteCutDirectionLeft:
                directionEuler += new Vector3(0, 0, -90);
                break;
            case BeatmapNote.NoteCutDirectionRight:
                directionEuler += new Vector3(0, 0, 90);
                break;
            case BeatmapNote.NoteCutDirectionUpRight:
                directionEuler += new Vector3(0, 0, 135);
                break;
            case BeatmapNote.NoteCutDirectionUpLeft:
                directionEuler += new Vector3(0, 0, -135);
                break;
            case BeatmapNote.NoteCutDirectionDownLeft:
                directionEuler += new Vector3(0, 0, -45);
                break;
            case BeatmapNote.NoteCutDirectionDownRight:
                directionEuler += new Vector3(0, 0, 45);
                break;
            default:
                break;
        }
        return directionEuler;
    }

    public void SetDotVisible(bool b) => dotRenderer.enabled = b;

    public void SetArrowVisible(bool b) => arrowRenderer.enabled = b;

    public void SetBomb(bool b)
    {
        simpleBlock.SetActive(!b && Settings.Instance.SimpleBlocks);
        complexBlock.SetActive(!b && !Settings.Instance.SimpleBlocks);

        bombRenderer.gameObject.SetActive(b);
        bombRenderer.enabled = b;
    }

    public void SetArcVisible(bool showArcVisualizer)
    {
        if (swingArcRenderer != null) swingArcRenderer.enabled = showArcVisualizer;
    }

    public static BeatmapNoteContainer SpawnBeatmapNote(BeatmapNote noteData, ref GameObject notePrefab)
    {
        var container = Instantiate(notePrefab).GetComponent<BeatmapNoteContainer>();
        container.MapNoteData = noteData;
        container.transform.localEulerAngles = Directionalize(noteData);
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = (Vector3)MapNoteData.GetPosition() +
                                  new Vector3(0, 0.5f, MapNoteData.Time * EditorScaleController.EditorScale);
        transform.localScale = MapNoteData.GetScale() + new Vector3(0.5f, 0.5f, 0.5f);

        UpdateCollisionGroups();

        MaterialPropertyBlock.SetFloat("_ObjectTime", MapNoteData.Time);
        SetRotation(AssignedTrack != null ? AssignedTrack.RotationValue.y : 0);
        UpdateMaterials();
    }

    public void SetColor(Color? color)
    {
        MaterialPropertyBlock.SetColor(BeatmapObjectContainer.color, color ?? unassignedColor);
        UpdateMaterials();
    }

    internal override void UpdateMaterials()
    {
        foreach (var renderer in noteRenderer) renderer.SetPropertyBlock(MaterialPropertyBlock);
        foreach (var renderer in SelectionRenderers) renderer.SetPropertyBlock(MaterialPropertyBlock);
        bombRenderer.SetPropertyBlock(MaterialPropertyBlock);
    }
}
