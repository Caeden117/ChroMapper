using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapChainContainer : BeatmapObjectContainer
{
    [FormerlySerializedAs("chainData")] public BeatmapChain ChainData;
    public override BeatmapObject ObjectData { get => ChainData; set => ChainData = (BeatmapChain)value; }

    [SerializeField] private GameObject tailNode;
    private List<GameObject> nodes = new List<GameObject>();
    public BeatmapNoteContainer AttachedHead = null;
    private bool headPointsToTail;
    private const float epsilon = 1e-2f;
    private Vector3 interPoint;
    private const float interMult = 1.5f;

    public static BeatmapChainContainer SpawnChain(BeatmapChain data, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapChainContainer>();
        container.ChainData = data;
        return container;
    }

    public override void Setup()
    {
        base.Setup();
        MaterialPropertyBlock.SetFloat("_Lit", 1);
        MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 1);
        UpdateMaterials();
    }
    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(-1.5f, 0.5f, ChainData.Time * EditorScaleController.EditorScale);
        GenerateChain();
        UpdateCollisionGroups();
    }

    /// <summary>
    /// Generate chain's all notes based on <see cref="ChainData"/> 
    /// </summary>
    /// <param name="chainData"></param>
    public void GenerateChain(BeatmapChain chainData = null)
    {
        if (chainData != null) ChainData = chainData;
        var headTrans = new Vector3(ChainData.X, ChainData.Y, 0); 
        var headRot = Quaternion.Euler(BeatmapNoteContainer.Directionalize(ChainData.Direction));
        tailNode.transform.localPosition = new Vector3(ChainData.TailX, ChainData.TailY, (ChainData.TailTime - ChainData.Time) * EditorScaleController.EditorScale);

        interPoint = new Vector3(ChainData.X, ChainData.Y, ChainData.Time); 
        var zRads = Mathf.Deg2Rad * BeatmapNoteContainer.Directionalize(ChainData.Direction).z;
        interPoint += new Vector3(interMult * Mathf.Sin(zRads), -interMult * Mathf.Cos(zRads), 0f);

        Colliders.Clear();
        SelectionRenderers.Clear();
        var cutDirection = NotesContainer.Direction(new BeatmapColorNote(ChainData));
        ComputeHeadPointsToTail(ChainData);
        int i = 0;
        for (; i < ChainData.SliceCount - 2; ++i)
        {
            if (i >= nodes.Count) break;
            nodes[i].SetActive(true);
            Interpolate(ChainData.SliceCount - 1, i + 1, headTrans, headRot, tailNode, nodes[i]);
            Colliders.Add(nodes[i].GetComponent<IntersectionCollider>());
            SelectionRenderers.Add(nodes[i].GetComponent<ChainComponentsFetcher>().SelectionRenderer);
        }
        for (; i < nodes.Count; ++i)
        {
            nodes[i].SetActive(false);
        }
        for (; i < ChainData.SliceCount - 2; ++i)
        {
            var newNode = Instantiate(tailNode, transform);
            newNode.SetActive(true);
            newNode.GetComponent<MeshRenderer>().material.CopyPropertiesFromMaterial(tailNode.GetComponent<MeshRenderer>().material);
            Interpolate(ChainData.SliceCount - 1, i + 1, headTrans, headRot, tailNode, newNode);
            nodes.Add(newNode);
            Colliders.Add(nodes[i].GetComponent<IntersectionCollider>());
            SelectionRenderers.Add(nodes[i].GetComponent<ChainComponentsFetcher>().SelectionRenderer);
        }
        Interpolate(ChainData.SliceCount - 1, ChainData.SliceCount - 1, headTrans, headRot, tailNode, tailNode);
        Colliders.Add(tailNode.GetComponent<IntersectionCollider>());
        SelectionRenderers.Add(tailNode.GetComponent<ChainComponentsFetcher>().SelectionRenderer);
        UpdateMaterials();
    }

    private void ComputeHeadPointsToTail(BeatmapChain chainData) {
        Vector2 path = new Vector2(ChainData.TailX - ChainData.X, ChainData.TailY - ChainData.Y);
        var pathAngle = Vector2.SignedAngle(Vector2.down, path);
        var cutAngle = BeatmapNoteContainer.Directionalize(ChainData.Direction).z;

        headPointsToTail = (Mathf.Abs(pathAngle - cutAngle) < 0.01f);
    }

    /// <summary>
    /// Interpolate between head and tail.
    /// </summary>
    /// <param name="n">Number of segments (excluding head)</param>
    /// <param name="i">Segment index</param>
    /// <param name="head">Head</param>
    /// <param name="headRot"></param>
    /// <param name="tail"></param>
    /// <param name="linkSegment"></param>
    private void Interpolate(int n, int i, in Vector3 head, in Quaternion headRot, in GameObject tail, in GameObject linkSegment)
    {
        float t = (float)i / n;
        float tSquish = t * ChainData.SquishAmount;

        var P0 = head;
        var P1 = interPoint;
        var P2 = tail.transform.localPosition;

        var lerpZPos = Mathf.Lerp(head.z, tail.transform.localPosition.z, t);

        if (headPointsToTail)
        {
            var lerpPos = Vector3.LerpUnclamped(head, tail.transform.localPosition, tSquish);
            linkSegment.transform.localPosition = new Vector3(lerpPos.x, lerpPos.y, lerpZPos);
            linkSegment.transform.localRotation = headRot;
        }
        else
        {
            // Quadratic bezier curve
            // B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2, 0 < t < 1
            var bezierLerp = (Mathf.Pow((1 - tSquish), 2) * P0) + (2 * (1 - tSquish) * tSquish * P1) + (Mathf.Pow(tSquish, 2) * P2);   
            linkSegment.transform.localPosition = new Vector3(bezierLerp.x, bezierLerp.y, lerpZPos);

            // Bezier derivative gives tangent line
            // B(t) = 2(1-t)(P1-P0) + 2t(P2-P1), 0 < t < 1
            var bezierDervLerp = 2 * (1 - tSquish) * (P1 - P0) + 2 * tSquish * (P2 - P1);
            linkSegment.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 90 + Mathf.Rad2Deg * Mathf.Atan2(bezierDervLerp.y, bezierDervLerp.x)));
        }
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(BeatmapObjectContainer.color, color);
        UpdateMaterials();
    }

    internal override void UpdateMaterials()
    {
        foreach (var collider in Colliders)
        {
            var renderer = collider.GetComponent<MeshRenderer>();
            renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
        foreach (var renderer in SelectionRenderers) renderer.SetPropertyBlock(MaterialPropertyBlock);
    }

    public void DetectHeadNote(bool detect = true)
    {
        if (ChainData == null) return;
        if (detect && AttachedHead == null)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType<NotesContainer>(BeatmapObject.ObjectType.Note);
            var notes = collection.GetBetween(ChainData.Time - ChainsContainer.ViewEpsilon, ChainData.Time + ChainsContainer.ViewEpsilon);
            foreach (BeatmapNote note in notes)
            {
                if (note.Type == BeatmapNote.NoteTypeBomb || !note.HasAttachedContainer) continue;
                if (IsHeadNote(note))
                {
                    collection.LoadedContainers.TryGetValue(note, out var container);
                    AttachedHead = container as BeatmapNoteContainer;
                    AttachedHead.transform.localScale = BeatmapChain.ChainScale;
                    break;
                }
            }
        }
        else if (AttachedHead != null)
        {
            if (!IsHeadNote(AttachedHead.MapNoteData))
            {
                if (AttachedHead.MapNoteData != null)
                {
                    AttachedHead.UpdateGridPosition();
                }
                AttachedHead = null;
                DetectHeadNote();
            }
            else
            {
                AttachedHead.transform.localScale = BeatmapChain.ChainScale;
            }
        }
    }

    public void ResetHeadNoteScale()
    {
        if (AttachedHead == null || AttachedHead.MapNoteData == null) return;
        AttachedHead.UpdateGridPosition();
    }

    public bool IsHeadNote(BeatmapNote note)
    {
        if (note is null) return false;
        return Mathf.Approximately(note.Time, ChainData.Time) && note.LineIndex == ChainData.X && note.LineLayer == ChainData.Y
            && note.CutDirection == ChainData.Direction && note.Type == ChainData.Color;
    }
}
