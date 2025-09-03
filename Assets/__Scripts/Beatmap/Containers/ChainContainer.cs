using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beatmap.Containers
{
    public class ChainContainer : ObjectContainer
    {
        private static readonly int colorMultiplier = Shader.PropertyToID("_ColorMult");
        private static readonly int objectTime = Shader.PropertyToID("_ObjectTime");
        private static readonly int lit = Shader.PropertyToID("_Lit");
        private static readonly int translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

        [SerializeField] private GameObject mainObject;
        [SerializeField] private GameObject simpleLink;
        [SerializeField] private GameObject simpleLinkSolid;
        [SerializeField] private GameObject complexLink;
        [SerializeField] private GameObject complexLinkSolid;
        public NoteContainer AttachedHead;
        private readonly List<GameObject> nodes = new List<GameObject>();
        [SerializeField] public BaseChain ChainData;
        [SerializeField] private List<GameObject> indicators;
        [SerializeField] private GameObject tailLinkIndicator;
        [SerializeField] private GameObject tailSphereIndicator;
        private Vector3 headDirection;
        private bool headPointsToTail;
        private Vector3 interPoint;
        private MaterialPropertyBlock arrowMaterialPropertyBlock;

        public static readonly float PosOffsetFactor = 0.5f / 0.6f * (1 - BaseChain.ChainHeadScale.y) / 2f;

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
            SetModel();

            MaterialPropertyBlock.SetFloat(lit, Settings.Instance.SimpleBlocks ? 0 : 1);
            MaterialPropertyBlock.SetFloat(translucentAlpha, Settings.Instance.PastNoteModelAlpha);

            arrowMaterialPropertyBlock ??= new MaterialPropertyBlock();

            foreach (var gameObj in indicators) gameObj.GetComponent<ChainIndicatorContainer>().Setup();

            UpdateMaterials();
        }

        private void SetModel()
        {
            simpleLink.SetActive(Settings.Instance.SimpleBlocks && !Settings.Instance.SolidChainLink);
            simpleLinkSolid.SetActive(Settings.Instance.SimpleBlocks && Settings.Instance.SolidChainLink);
            complexLink.SetActive(!Settings.Instance.SimpleBlocks && !Settings.Instance.SolidChainLink);
            complexLinkSolid.SetActive(!Settings.Instance.SimpleBlocks && Settings.Instance.SolidChainLink);
        }

        public void AdjustTimePlacement()
        {
            if (!(Animator != null && Animator.AnimatedTrack))
            {
                transform.localPosition =
                    new Vector3(-1.5f, offsetY, ChainData.SongBpmTime * EditorScaleController.EditorScale);
            }
        }

        public override void UpdateGridPosition()
        {
            AdjustTimePlacement();
            GenerateChain();
            UpdateCollisionGroups();

            if (AttachedHead == null || AttachedHead.Animator.AnimatedTrack || IsHeadNote(AttachedHead.NoteData))
                return;

            // usually this does not update often and is already checked
            // but if attached head note data is different, then we update the head note
            // also temporary (permanent) fix because this shit needs rewrite
            AttachedHead = null;
            DetectHeadNote();
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
            mainObject.transform.localPosition = chainTail
                + new Vector3(
                    0,
                    0,
                    (ChainData.TailSongBpmTime - ChainData.SongBpmTime) * EditorScaleController.EditorScale);

            var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ChainData.CutDirection).z;
            headDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);

            var interMult = (chainHead - chainTail).magnitude / 2;
            interPoint = chainHead + (interMult * headDirection);

            Colliders.Clear();
            SelectionRenderers.Clear();
            ComputeHeadPointsToTail();
            var i = 0;
            for (; i < ChainData.SliceCount - 2; ++i)
            {
                if (i >= nodes.Count) break;
                nodes[i].SetActive(true);
                Interpolate(ChainData.SliceCount - 1, i + 1, headTrans, headRot, mainObject, nodes[i]);
                Colliders.Add(nodes[i].GetComponent<IntersectionCollider>());
                nodes[i].GetComponent<ChainComponentsFetcher>().SelectionRenderer.ForEach(SelectionRenderers.Add);
            }

            for (; i < nodes.Count; ++i) nodes[i].SetActive(false);
            for (; i < ChainData.SliceCount - 2; ++i)
            {
                var newNode = Instantiate(mainObject, Animator.AnimationThis.transform);
                newNode.SetActive(true);

                var cpfMain = mainObject.GetComponent<ChainComponentsFetcher>();
                var cpfNode = newNode.GetComponent<ChainComponentsFetcher>();

                for (var i1 = 0; i1 < cpfMain.NoteRenderer.Count; i1++)
                {
                    cpfMain.NoteRenderer[i1].sharedMaterial = cpfNode.NoteRenderer[i1].sharedMaterial;
                }

                Interpolate(ChainData.SliceCount - 1, i + 1, headTrans, headRot, mainObject, newNode);
                nodes.Add(newNode);
                Colliders.Add(nodes[i].GetComponent<IntersectionCollider>());
                nodes[i].GetComponent<ChainComponentsFetcher>().SelectionRenderer.ForEach(SelectionRenderers.Add);
            }

            if (ChainData.SliceCount == 1)
                mainObject.SetActive(false);
            else
            {
                mainObject.SetActive(true);
                Interpolate(
                    ChainData.SliceCount - 1,
                    ChainData.SliceCount - 1,
                    headTrans,
                    headRot,
                    mainObject,
                    mainObject);
                Colliders.Add(mainObject.GetComponent<IntersectionCollider>());
                mainObject.GetComponent<ChainComponentsFetcher>().SelectionRenderer.ForEach(SelectionRenderers.Add);
            }

            var scale = Vector3.one;
            if (!Settings.Instance.AccurateNoteSize) scale *= 0.9f;
            foreach (var node in nodes) node.transform.localScale = scale;
            mainObject.transform.localScale = scale;
            tailLinkIndicator.transform.localScale = scale;

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
        private void Interpolate(
            int n,
            int i,
            in Vector3 head,
            in Quaternion headRot,
            in GameObject tail,
            in GameObject linkSegment)
        {
            // This is how the game displays squish
            var gameSquish = (ChainData.Squish < 0.001f) ? 1f : ChainData.Squish;

            var t = (float)i / n;
            var tSquish = t * gameSquish;

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
                var bezierLerp = (Mathf.Pow(1 - tSquish, 2) * p0)
                    + (2 * (1 - tSquish) * tSquish * p1)
                    + (Mathf.Pow(tSquish, 2) * p2);
                linkSegment.transform.localPosition = new Vector3(bezierLerp.x, bezierLerp.y, lerpZPos);

                // Bezier derivative gives tangent line
                // B(t) = 2(1-t)(P1-P0) + 2t(P2-P1), 0 < t < 1
                var bezierDervLerp = (2 * (1 - tSquish) * (p1 - p0)) + (2 * tSquish * (p2 - p1));
                linkSegment.transform.localRotation = Quaternion.Euler(
                    new Vector3(
                        0,
                        0,
                        90 + (Mathf.Rad2Deg * Mathf.Atan2(bezierDervLerp.y, bezierDervLerp.x))));
            }
        }

        public void SetColor(Color c)
        {
            MaterialPropertyBlock.SetColor(color, c);

            var arrowColor = Color.Lerp(c, Color.white, Settings.Instance.ArrowColorWhiteBlend);
            arrowMaterialPropertyBlock.SetColor(color, arrowColor);

            MaterialPropertyBlock.SetFloat(colorMultiplier, Settings.Instance.NoteColorMultiplier);
            arrowMaterialPropertyBlock.SetFloat(colorMultiplier, Settings.Instance.ArrowColorMultiplier);

            UpdateMaterials();
        }

        internal override void UpdateMaterials()
        {
            foreach (var c in Colliders)
            {
                var cpf = c.GetComponent<ChainComponentsFetcher>();
                var dot = cpf.DotRenderer;

                var time = ChainData.SongBpmTime + (c.transform.localPosition.z / EditorScaleController.EditorScale);
                MaterialPropertyBlock.SetFloat(objectTime, time);
                arrowMaterialPropertyBlock.SetFloat(objectTime, time);

                // This alpha set is a workaround as callbackController can only despawn the entire chain
                var alpha = UIMode.SelectedMode == UIModeType.Preview || UIMode.SelectedMode == UIModeType.Playing
                    ? 0
                    : Settings.Instance.PastNoteModelAlpha;

                MaterialPropertyBlock.SetFloat(translucentAlpha, alpha);
                arrowMaterialPropertyBlock.SetFloat(translucentAlpha, alpha);

                cpf.NoteRenderer.ForEach(r => r.SetPropertyBlock(MaterialPropertyBlock));
                dot.SetPropertyBlock(arrowMaterialPropertyBlock);
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
                var notes = collection.GetBetween(
                    ChainData.JsonTime - ChainGridContainer.ViewEpsilon,
                    ChainData.JsonTime + ChainGridContainer.ViewEpsilon);
                foreach (var note in notes)
                {
                    if (note.ObjectType != ObjectType.Note || !note.HasAttachedContainer) continue;
                    if (!IsHeadNote(note)) continue;
                    collection.LoadedContainers.TryGetValue(note, out var container);
                    AttachedHead = container as NoteContainer;
                    AttachedHead.SetChainHeadModel();
                    break;
                }
            }
            else if (AttachedHead != null)
            {
                if (!IsHeadNote(AttachedHead.NoteData))
                {
                    if (AttachedHead.NoteData != null) AttachedHead.SetModelInfer();
                    AttachedHead = null;
                    DetectHeadNote();
                }
                else
                    AttachedHead.SetChainHeadModel();
            }
        }

        public void DetachHeadNote()
        {
            if (AttachedHead == null || AttachedHead.NoteData == null) return;
            AttachedHead.SetModelInfer();
            AttachedHead = null;
        }

        public bool IsHeadNote(BaseNote baseNote)
        {
            if (baseNote is null) return false;
            var noteHead = baseNote.GetPosition();
            var chainHead = ChainData.GetPosition();
            return Mathf.Abs(baseNote.JsonTime - ChainData.JsonTime) < BeatmapObjectContainerCollection.Epsilon
                && Vector2.Distance(noteHead, chainHead) < 0.1
                && baseNote.Type == ChainData.Color;
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
                if (gameObj.activeSelf) gameObj.GetComponent<ChainIndicatorContainer>().UpdateGridPosition();
            }
        }

        public Quaternion GetTailNodeRotation() => mainObject.transform.rotation;
    }
}
