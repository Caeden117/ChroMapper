using UnityEngine;

public class BeatmapNoteContainer : BeatmapObjectContainer {

    public override BeatmapObject objectData { get => mapNoteData; set => mapNoteData = (BeatmapNote)value; }

    public BeatmapNote mapNoteData;

    [SerializeField] MeshRenderer noteRenderer;
    [SerializeField] MeshRenderer bombRenderer;
    [SerializeField] SpriteRenderer dotRenderer;
    [SerializeField] MeshRenderer arrowRenderer;
    [SerializeField] SpriteRenderer swingArcRenderer;
    [SerializeField] Shader transparentShader;
    [SerializeField] Shader opaqueShader;

    private Color bombColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);

    public override void Setup()
    {
        base.Setup();
        SetArcVisible(NotesContainer.ShowArcVisualizer);
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
        noteRenderer.sharedMaterial = m;
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

    public void SetBomb(bool b)
    {
        noteRenderer.enabled = !b;
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
        float position = mapNoteData._lineIndex - 1.5f;
        float layer = mapNoteData._lineLayer + 0.5f;
        if (mapNoteData._customData?.HasKey("_position") ?? false)
        {
            Vector2 NEPosition = mapNoteData._customData["_position"].ReadVector2();
            position = NEPosition.x;
            layer = NEPosition.y + 0.5f;
        }
        else
        {
            if (mapNoteData._lineIndex >= 1000)
                position = (mapNoteData._lineIndex / 1000f) - 2.5f;
            else if (mapNoteData._lineIndex <= -1000)
                position = (mapNoteData._lineIndex / 1000f) - 0.5f;
            if (mapNoteData._lineLayer >= 1000 || mapNoteData._lineLayer <= -1000)
                layer = (mapNoteData._lineLayer / 1000f) - 0.5f;
        }
        transform.localPosition = new Vector3(
            position,
            layer,
            mapNoteData._time * EditorScaleController.EditorScale
            );

        if (noteRenderer.material.HasProperty("_Rotation"))
            noteRenderer.material.SetFloat("_Rotation", AssignedTrack?.RotationValue.y ?? 0);
    }

    public void SetColor(Color? color)
    {
        noteRenderer.material.SetColor("_Color", color ?? bombColor);
        bombRenderer.material.SetColor("_Color", color ?? bombColor);
    }

    public void SetIsPlaying(bool isPlaying)
    {
        /*
         * Unfortunately Unity tries REALLY hard to not let you change opaque VS transparent at runtime.
         * 
         * So hard, in fact, that I've given up trying, and instead moved to storing references shaders and swapping them out.
         */
        Material material = ModelMaterials[0];
        material.shader = isPlaying ? transparentShader : opaqueShader;
        material.SetFloat("_Editor_IsPlaying", isPlaying ? 1 : 0);
    }
}
