using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using Beatmap.V3;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ChainContainer : ObjectContainer
    {
        [SerializeField] private GameObject tailNode;
        public NoteContainer AttachedHead;
        private readonly List<GameObject> nodes = new List<GameObject>();
        [SerializeField] public BaseChain ChainData;
        [SerializeField] private List<GameObject> indicators;
        [SerializeField] private GameObject tailLinkIndicator;
        [SerializeField] private GameObject tailSphereIndicator;
        private Vector3 headDirection;
        private bool headPointsToTail;
        private Vector3 interPoint;

        public const float
            posOffsetFactor = 0.17333f; // Hardcoded because haven't found exact relationship between ChainScale yet

        public override BaseObject ObjectData
        {
            get => ChainData;
            set => ChainData = (BaseChain)value;
        }

        public static ChainContainer SpawnChain(BaseChain data, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<ChainContainer>();
            container.ChainData = data;
            return container;
        }

        public override void Setup()
        {
            base.Setup();
            MaterialPropertyBlock.SetFloat("_Lit", Settings.Instance.SimpleBlocks ? 0 : 1);
            MaterialPropertyBlock.SetFloat("_TranslucentAlpha", Settings.Instance.PastNoteModelAlpha);
            foreach (var gameObj in indicators) gameObj.GetComponent<ChainIndicatorContainer>().Setup();
            UpdateMaterials();
        }

        public override void UpdateGridPosition()
        {
            transform.localPosition = new Vector3(-1.5f, offsetY, ChainData.SongBpmTime * EditorScaleController.EditorScale);
            GenerateChain();
            UpdateCollisionGroups();
            if (AttachedHead is NoteContainer)
            {
                if (!AttachedHead.Animator.AnimatedTrack)
                {
                    AttachedHead.UpdateGridPosition();
                    AttachedHead.transform.localPosition -= posOffsetFactor * headDirection;
                }
                AttachedHead.directionTarget.localScale = BaseChain.ChainScale;
            }
        }

        /// <summary>
        ///     Generate chain's all notes based on <see cref="ChainData" />
        /// </summary>
        /// <param name="chainData"></param>
        public void GenerateChain(BaseChain chainData = null)
        {
            if (chainData != null) ChainData = chainData;
            var chainHead = (Vector3)ChainData.GetPosition() + new Vector3(1.5f, 0, 0);
            var chainTail = (Vector3)ChainData.GetTailPosition() + new Vector3(1.5f, 0, 0);
            var headTrans = chainHead;
            var headRot = Quaternion.Euler(NoteContainer.Directionalize(ChainData.CutDirection));
            tailNode.transform.localPosition = chainTail + new Vector3(0, 0,
                (ChainData.TailSongBpmTime - ChainData.SongBpmTime) * EditorScaleController.EditorScale);

            var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ChainData.CutDirection).z;
            headDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);

            var interMult = (chainHead - chainTail).magnitude / 2;
            interPoint = chainHead + interMult * headDirection;

            Colliders.Clear();
            SelectionRenderers.Clear();
            ComputeHeadPointsToTail();
            var i = 0;
            for (; i < ChainData.SliceCount - 2; ++i)
            {
                if (i >= nodes.Count) break;
                nodes[i].SetActive(true);
                Interpolate(ChainData.SliceCount - 1, i + 1, headTrans, headRot, tailNode, nodes[i]);
                Colliders.Add(nodes[i].GetComponent<IntersectionCollider>());
                SelectionRenderers.Add(nodes[i].GetComponent<ChainComponentsFetcher>().SelectionRenderer);
            }

            for (; i < nodes.Count; ++i) nodes[i].SetActive(false);
            for (; i < ChainData.SliceCount - 2; ++i)
            {
                var newNode = Instantiate(tailNode, transform);
                newNode.SetActive(true);
                newNode.GetComponent<MeshRenderer>().sharedMaterial = tailNode.GetComponent<MeshRenderer>().sharedMaterial;
                Interpolate(ChainData.SliceCount - 1, i + 1, headTrans, headRot, tailNode, newNode);
                nodes.Add(newNode);
                Colliders.Add(nodes[i].GetComponent<IntersectionCollider>());
                SelectionRenderers.Add(nodes[i].GetComponent<ChainComponentsFetcher>().SelectionRenderer);
            }

            if (ChainData.SliceCount == 1)
            {
                tailNode.SetActive(false);
            }
            else
            {
                tailNode.SetActive(true);
                Interpolate(ChainData.SliceCount - 1, ChainData.SliceCount - 1, headTrans, headRot, tailNode, tailNode);
                Colliders.Add(tailNode.GetComponent<IntersectionCollider>());
                SelectionRenderers.Add(tailNode.GetComponent<ChainComponentsFetcher>().SelectionRenderer);
            }

            UpdateMaterials();

            ResetIndicatorsPosition();
        }

        private void ComputeHeadPointsToTail()
        {
            var path = ChainData.GetTailPosition() - ChainData.GetPosition() + new Vector2(1.5f, 0);
            var pathAngle = Vector2.SignedAngle(Vector2.down, path);
            var cutAngle = NoteContainer.Directionalize(ChainData.CutDirection).z;

            headPointsToTail = Mathf.Abs(pathAngle - cutAngle) < 0.01f;
        }

        /// <summary>
        ///     Interpolate between head and tail.
        /// </summary>
        /// <param name="n">Number of segments (excluding head)</param>
        /// <param name="i">Segment index</param>
        /// <param name="head">Head</param>
        /// <param name="headRot"></param>
        /// <param name="tail"></param>
        /// <param name="linkSegment"></param>
        private void Interpolate(int n, int i, in Vector3 head, in Quaternion headRot, in GameObject tail,
            in GameObject linkSegment)
        {
            var t = (float)i / n;
            var tSquish = t * ChainData.Squish;

            var p0 = head;
            var p1 = interPoint;
            var p2 = tail.transform.localPosition;

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
                var bezierLerp = (Mathf.Pow(1 - tSquish, 2) * p0) + (2 * (1 - tSquish) * tSquish * p1) +
                                 (Mathf.Pow(tSquish, 2) * p2);
                linkSegment.transform.localPosition = new Vector3(bezierLerp.x, bezierLerp.y, lerpZPos);

                // Bezier derivative gives tangent line
                // B(t) = 2(1-t)(P1-P0) + 2t(P2-P1), 0 < t < 1
                var bezierDervLerp = (2 * (1 - tSquish) * (p1 - p0)) + (2 * tSquish * (p2 - p1));
                linkSegment.transform.localRotation = Quaternion.Euler(new Vector3(0, 0,
                    90 + (Mathf.Rad2Deg * Mathf.Atan2(bezierDervLerp.y, bezierDervLerp.x))));
            }
        }

        public void SetColor(Color c)
        {
            MaterialPropertyBlock.SetColor(color, c);
            UpdateMaterials();
        }

        internal override void UpdateMaterials()
        {
            foreach (var c in Colliders)
            {
                var r = c.GetComponent<MeshRenderer>();
                MaterialPropertyBlock.SetFloat("_ObjectTime", ChainData.SongBpmTime + c.transform.localPosition.z / EditorScaleController.EditorScale);
                // This alpha set is a workaround as callbackController can only despawn the entire chain
                if (UIMode.SelectedMode == UIModeType.Preview || UIMode.SelectedMode == UIModeType.Playing)
                    MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 0f);
                else
                    MaterialPropertyBlock.SetFloat("_TranslucentAlpha", Settings.Instance.PastNoteModelAlpha);
                r.SetPropertyBlock(MaterialPropertyBlock);
            }

            foreach (var r in SelectionRenderers) r.SetPropertyBlock(MaterialPropertyBlock);

            foreach (var gameObj in indicators)
                gameObj.GetComponent<ChainIndicatorContainer>().UpdateMaterials(MaterialPropertyBlock);
            foreach (var gameObj in indicators)
                gameObj.GetComponent<ChainIndicatorContainer>().OutlineVisible = OutlineVisible;
        }

        public void DetectHeadNote(bool detect = true)
        {
            if (ChainData == null) return;
            if (detect && AttachedHead == null)
            {
                var collection =
                    BeatmapObjectContainerCollection.GetCollectionForType<NoteGridContainer>(ObjectType.Note);
                var notes = collection.GetBetween(ChainData.JsonTime - ChainGridContainer.ViewEpsilon,
                    ChainData.JsonTime + ChainGridContainer.ViewEpsilon);
                foreach (var note in notes)
                {
                    if (note.ObjectType != ObjectType.Note || !note.HasAttachedContainer) continue;
                    if (!IsHeadNote((BaseNote)note)) continue;
                    collection.LoadedContainers.TryGetValue(note, out var container);
                    AttachedHead = container as NoteContainer;
                    AttachedHead.directionTarget.localScale = BaseChain.ChainScale;
                    AttachedHead.transform.localPosition -= posOffsetFactor * headDirection;
                    break;
                }
            }
            else if (AttachedHead != null)
            {
                if (!IsHeadNote(AttachedHead.NoteData))
                {
                    if (AttachedHead.NoteData != null) AttachedHead.UpdateGridPosition();
                    AttachedHead = null;
                    DetectHeadNote();
                }
                else
                {
                    AttachedHead.directionTarget.localScale = BaseChain.ChainScale;
                    AttachedHead.transform.localPosition -= posOffsetFactor * headDirection;
                }
            }
        }

        public void ResetHeadNoteScale()
        {
            if (AttachedHead == null || AttachedHead.NoteData == null) return;
            AttachedHead.UpdateGridPosition();
        }

        public bool IsHeadNote(BaseNote baseNote)
        {
            if (baseNote is null) return false;
            var noteHead = baseNote.GetPosition();
            var chainHead = ChainData.GetPosition();
            return Mathf.Approximately(baseNote.JsonTime, ChainData.JsonTime) && Mathf.Approximately(noteHead.x, chainHead.x) && Mathf.Approximately(noteHead.y, chainHead.y)
                   && baseNote.CutDirection == ChainData.CutDirection && baseNote.Type == ChainData.Color;
        }

        public void SetIndicatorBlocksActive(bool visible)
        {
            indicators[0].SetActive(visible); // Head
            tailSphereIndicator.SetActive(visible && ChainData.SliceCount == 1);
            tailLinkIndicator.SetActive(visible && ChainData.SliceCount != 1);
        }

        private void ResetIndicatorsPosition()
        {
            tailSphereIndicator.SetActive(ChainData.SliceCount == 1);
            tailLinkIndicator.SetActive(ChainData.SliceCount != 1);

            foreach (var gameObj in indicators)
            {
                if (gameObj.activeSelf)
                    gameObj.GetComponent<ChainIndicatorContainer>().UpdateGridPosition();
            }
        }

        public Quaternion GetTailNodeRotation() => tailNode.transform.rotation;
    }
}
