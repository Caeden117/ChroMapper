using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ChainContainer : ObjectContainer
    {
        private static readonly int colorMultiplierId = Shader.PropertyToID("_ColorMultiplier");
        private static readonly int objectTimeId = Shader.PropertyToID("_ObjectTime");
        private static readonly int translucentAlphaId = Shader.PropertyToID("_TranslucentAlpha");

        [SerializeField] public TextMeshPro InfoText;
        [SerializeField] public ChainComponentsFetcher Prefab;

        [Header("Indicator")] [SerializeField] private List<ChainIndicatorContainer> indicators;
        public List<ChainComponentsFetcher> Nodes = new();

        public AssignObjectPrefabManager AssignObjectPrefabManager;
        public BaseChain ChainData;

        public GameObject TailObject;

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

        protected override void RegisterCallback() => VisualSettings.OnChainLinkModelChanged += HandleModelChanged;
        protected override void UnregisterCallback() => VisualSettings.OnChainLinkModelChanged -= HandleModelChanged;

        public override void HandleModelChanged()
        {
            if (ChainData == null) return;

            var vm = ChainData is { Color: (int)NoteColor.Blue }
                ? VisualSettings.GetBurstSliderRightModel()
                : VisualSettings.GetBurstSliderLeftModel();
            var defaultModel = vm;

            if (ChainData.CustomTrack != null && AssignObjectPrefabManager != null)
            {
                if (ChainData.CustomTrack.IsString)
                {
                    var result = AssignObjectPrefabManager.GetCurrentModels(
                        TrackModelState.Kind.BurstSliderElement,
                        ChainData.CustomTrack);
                    if (result.HasOverride && result.OverrideModel != null) vm = result.OverrideModel;
                    foreach (var cpf in Nodes)
                    {
                        cpf.ModelController.Set(vm);
                        cpf.DotMpbController.gameObject.SetActive(
                            !(result.AdditiveModels.Contains(null) ? defaultModel : vm).DisableAux);
                    }

                    foreach (var model in result.AdditiveModels)
                    foreach (var cpf in Nodes)
                        cpf.ModelController.Add(model != null ? model : defaultModel);
                }
                else if (ChainData.CustomTrack.IsArray)
                {
                    var result = AssignObjectPrefabManager.GetCurrentModels(
                        TrackModelState.Kind.BurstSliderElement,
                        ChainData.CustomTrack.Children.Select(x => (string)x).ToArray());
                    if (result.HasOverride && result.OverrideModel != null) vm = result.OverrideModel;
                    foreach (var cpf in Nodes)
                    {
                        cpf.ModelController.Set(vm);
                        cpf.DotMpbController.gameObject.SetActive(
                            !(result.AdditiveModels.Contains(null) ? defaultModel : vm).DisableAux);
                    }

                    foreach (var model in result.AdditiveModels)
                    foreach (var cpf in Nodes)
                        cpf.ModelController.Add(model != null ? model : defaultModel);
                }
            }
            else
            {
                foreach (var cpf in Nodes)
                {
                    cpf.ModelController.Set(vm);
                    cpf.DotMpbController.gameObject.SetActive(!vm.DisableAux);
                }
            }
        }

        public override void Setup()
        {
            base.Setup();
            Prefab.gameObject.SetActive(false);
            HandleModelChanged();

            MpbController.Mpb.SetFloat(translucentAlphaId, Settings.Instance.PastNoteModelAlpha);

            foreach (var gameObj in indicators) gameObj.GetComponent<ChainIndicatorContainer>().Setup();

            UpdateMaterials();
        }

        public void AdjustTimePlacement()
        {
            if (!(Animator != null && Animator.AnimatedTrack))
            {
                transform.localPosition = (Vector3)ChainData.GetPosition()
                    + new Vector3(
                        0f,
                        BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset,
                        (ChainData.SongBpmTime * EditorScaleController.EditorScale * BeatmapConstant.LaneSize)
                        + BeatmapConstant.ZOffset);
            }
        }

        public override void UpdateGridPosition()
        {
            AdjustTimePlacement();
            GenerateChain();
            UpdateCollisionGroups();
        }

        /// <summary>
        ///     Generate chain's all notes based on <see cref="ChainData" />
        /// </summary>
        /// <param name="chainData"></param>
        public void GenerateChain(BaseChain chainData = null)
        {
            if (chainData != null) ChainData = chainData;
            var tailRelPos = (Vector3)(ChainData.GetTailPosition() - ChainData.GetPosition());
            var headRot = Quaternion.Euler(NoteContainer.Directionalize(ChainData.CutDirection));
            var targetPos = tailRelPos
                + new Vector3(
                    0f,
                    0f,
                    (ChainData.TailSongBpmTime - ChainData.SongBpmTime)
                    * EditorScaleController.EditorScale
                    * BeatmapConstant.LaneSize);

            var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ChainData.CutDirection).z;
            var headDirection = new Vector3(Mathf.Sin(zRads), -Mathf.Cos(zRads), 0f);

            var interMult = (Vector3.zero - tailRelPos).magnitude / 2;
            var interPoint = Vector3.zero + (interMult * headDirection);

            Colliders.Clear();
            var headPointsToTail = ComputeHeadPointsToTail();
            var i = 0;
            for (; i < ChainData.SliceCount - 1; ++i)
            {
                ChainComponentsFetcher node;
                if (i >= Nodes.Count)
                {
                    node = Instantiate(Prefab, Animator.AnimationThis.transform);
                    Nodes.Add(node);
                }
                else
                    node = Nodes[i];

                node.gameObject.SetActive(true);
                Interpolate(
                    ChainData.SliceCount - 1,
                    i + 1,
                    headRot,
                    targetPos,
                    interPoint,
                    headPointsToTail,
                    node.gameObject);
                Colliders.Add(node.OutlineController.Collider);
                SelectionMpbController.Add(node.OutlineController.Renderer);
                TailObject = node.gameObject;
            }

            for (; i < Nodes.Count; ++i) Nodes[i].gameObject.SetActive(false);

            var scale = Vector3.one;
            if (!Settings.Instance.AccurateNoteSize) scale *= 0.9f;
            foreach (var node in Nodes) node.transform.localScale = scale;

            UpdateMaterials();
            ResetIndicatorsPosition();
        }

        private bool ComputeHeadPointsToTail()
        {
            var path = ChainData.GetTailPosition() - ChainData.GetPosition() + new Vector2(1.5f, 0);
            var pathAngle = Vector2.SignedAngle(Vector2.down, path);
            var cutAngle = NoteContainer.Directionalize(ChainData.CutDirection).z;

            return Mathf.Abs(pathAngle - cutAngle) < 0.01f;
        }

        /// <summary>
        ///     Interpolate between head and tail.
        /// </summary>
        /// <param name="n">Number of segments (excluding head)</param>
        /// <param name="i">Segment index</param>
        /// <param name="headRot"></param>
        /// <param name="targetPos"></param>
        /// <param name="headPointsToTail"></param>
        /// <param name="linkSegment"></param>
        /// <param name="interPoint"></param>
        private void Interpolate(
            int n,
            int i,
            in Quaternion headRot,
            in Vector3 targetPos,
            in Vector3 interPoint,
            bool headPointsToTail,
            in GameObject linkSegment)
        {
            // This is how the game displays squish
            var gameSquish = ChainData.Squish < 0.001f ? 1f : ChainData.Squish;

            var t = (float)i / n;
            var tSquish = t * gameSquish;

            var p0 = Vector3.zero;
            var p1 = interPoint;
            var p2 = targetPos;

            var lerpZPos = Mathf.Lerp(0f, targetPos.z, t);

            if (headPointsToTail)
            {
                var lerpPos = Vector3.LerpUnclamped(Vector3.zero, targetPos, tSquish);
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
            MpbController.Mpb.SetColor(ColorId, c);
            MpbController.Mpb.SetFloat(colorMultiplierId, Settings.Instance.NoteColorMultiplier);
            for (var i = 0; i < Nodes.Count; i++)
            {
                SetNodeColor(i, c);
            }
            UpdateMaterials();
        }

        internal void SetNodeColor(int index, Color color)
        {
            var node = Nodes[index];
            var arrowColor = Color.Lerp(color, Color.white, Settings.Instance.ArrowColorWhiteBlend);
            node.ModelController.MpbController.Mpb.SetColor(ColorId, color);
            node.ModelController.MpbController.Mpb.SetFloat(colorMultiplierId, Settings.Instance.NoteColorMultiplier);
            node.DotMpbController.Mpb.SetColor(ColorId, arrowColor);
            node.DotMpbController.Mpb.SetFloat(colorMultiplierId, Settings.Instance.ArrowColorMultiplier);
        }

        internal override void UpdateMaterials()
        {
            var alpha = UIMode.SelectedMode == UIModeType.Preview || UIMode.SelectedMode == UIModeType.Playing
                ? 0
                : Settings.Instance.PastNoteModelAlpha;

            MpbController.ApplyChanges();

            if (ChainData != null)
            {
                foreach (var cpf in Nodes)
                {
                    var time = ChainData.SongBpmTime
                        + (cpf.transform.localPosition.z / EditorScaleController.EditorScale);
                    cpf.ModelController.MpbController.Mpb.SetFloat(objectTimeId, time);
                    cpf.DotMpbController.Mpb.SetFloat(objectTimeId, time);

                    // This alpha set is a workaround as callbackController can only despawn the entire chain
                    cpf.ModelController.MpbController.Mpb.SetFloat(translucentAlphaId, alpha);
                    cpf.DotMpbController.Mpb.SetFloat(translucentAlphaId, alpha);

                    cpf.ModelController.MpbController.ApplyChanges();
                    cpf.DotMpbController.ApplyChanges();
                }
            }

            foreach (var container in indicators)
            {
                container.UpdateMaterials(MpbController.Mpb);
                container.Selected = Selected;
            }
        }

        public void SetIndicators(bool visible)
        {
            indicators[0].gameObject.SetActive(visible); // Head
            indicators[1].gameObject.SetActive(visible && ChainData.SliceCount != 1);
            indicators[2].gameObject.SetActive(visible && ChainData.SliceCount == 1);
            InfoText.gameObject.SetActive(visible && Settings.Instance.DisplayNoteText);
        }

        private void ResetIndicatorsPosition()
        {
            indicators[1].gameObject.SetActive(ChainData.SliceCount != 1);
            indicators[2].gameObject.SetActive(ChainData.SliceCount == 1);

            foreach (var container in indicators)
            {
                if (container.gameObject.activeSelf) container.UpdateGridPosition();
            }
        }

        public Quaternion GetTailNodeRotation() =>
            TailObject != null ? TailObject.transform.rotation : Quaternion.identity;
    }
}
