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
    private Vector3 circleCenter;
    private bool headTailInALine;
    private Vector3 tailTangent;
    private const float epsilon = 1e-2f;

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
        Colliders.Clear();
        SelectionRenderers.Clear();
        var cutDirection = NotesContainer.Direction(new BeatmapColorNote(ChainData));
        ComputeCircleCenter(headTrans, headRot, new Vector3(cutDirection.x, cutDirection.y, 
            (ChainData.TailTime - ChainData.Time) * EditorScaleController.EditorScale), tailNode.transform);
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

    /// <summary>
    /// Compute Circle center for interpolation(and by the way compute tail rotation & tail tangent)
    /// The basic idea is giving a head point and its tangent vector and a tail point, compute the circle.
    /// </summary>
    /// <param name="headPos"></param>
    /// <param name="headRot"></param>
    /// <param name="headTangent"></param>
    /// <param name="tailTrans"></param>
    private void ComputeCircleCenter(in Vector3 headPos, in Quaternion headRot, in Vector3 headTangent, in Transform tailTrans)
    {
        //Debug.Log("headpos" + headPos);
        //Debug.Log("headTangent" + headTangent);
        var tailPos = tailTrans.localPosition;
        //Debug.Log("tailPos" + tailPos);
        var headToTail = tailPos - headPos;
        if (Mathf.Abs((headTangent.x * headToTail.y - headTangent.y * headToTail.x) / headTangent.magnitude / headToTail.magnitude) < epsilon)
        {
            // cross product = 0, indicate in a line
            headTailInALine = true;
            tailTrans.localRotation = headRot;
            return;
        }
        /// compute circle center
        headTailInALine = false;
        Vector3 circleNormal = Vector3.Cross(headTangent, headToTail); // normal vector perpendicular to circle plane
        Vector3 headToCenter = Vector3.Cross(circleNormal, headTangent);
        //Debug.Log("headTOcenter" + headToCenter);
        Vector3 midPoint = (headPos + tailPos) / 2.0f;
        Vector3 midToCenter = Vector3.Cross(circleNormal, headToTail);
        //Debug.Log("midtocenter" + midToCenter);

        var headToMid = midPoint - headPos;
        Vector3 crossVec1and2 = Vector3.Cross(headToCenter, midToCenter);
        Vector3 crossVec3and2 = Vector3.Cross(headToMid, midToCenter);

        float planarFactor = Vector3.Dot(headToMid, crossVec1and2);

        float s = Vector3.Dot(crossVec3and2, crossVec1and2)
                / crossVec1and2.sqrMagnitude;
        circleCenter = headPos + (headToCenter * s);
        //Debug.Log("circleCenter" + circleCenter);

        /// compute tail angle and tail tangent;
        var centerToTail = tailTrans.localPosition - circleCenter;
        tailTangent = Vector3.Cross(circleNormal, centerToTail).normalized * centerToTail.magnitude;
        var tailRot = Quaternion.FromToRotation(headTangent, tailTangent); // do not take u-turn, it will not be correct
        tailTrans.localRotation = tailRot * headRot;

        if (Mathf.Abs(Vector3.Dot(headToTail.normalized, tailTangent.normalized)) < epsilon)
        {
            tailTrans.localPosition -= tailTangent * epsilon; // move tail back a little bit to avoid head-tail-center in a line
        }
        //Debug.Log("tail tangent" + tailTangent);
    }

    /// <summary>
    /// Interpolate between head and tail. The algorithm may not be correct.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="i"></param>
    /// <param name="t0"></param>
    /// <param name="t1"></param>
    /// <param name="t"></param>
    private void Interpolate(int n, int i, in Vector3 trans0, in Quaternion rot0, in GameObject t1, in GameObject t)
    {
        float p0 = (float)i / n;
        if (headTailInALine)
        {
            t.transform.localPosition = Vector3.LerpUnclamped(trans0, t1.transform.localPosition, p0 * ChainData.SquishAmount);

        }
        else
        {
            t.transform.localPosition = Vector3.Slerp(trans0 - circleCenter, t1.transform.localPosition - circleCenter, p0 * ChainData.SquishAmount)
                + circleCenter;
            if (p0 * ChainData.SquishAmount > 1.0f)
                t.transform.localPosition += tailTangent * (p0 * ChainData.SquishAmount - 1.0f);
        }
        t.transform.localRotation = Quaternion.Slerp(rot0, t1.transform.localRotation, p0 * ChainData.SquishAmount);
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
