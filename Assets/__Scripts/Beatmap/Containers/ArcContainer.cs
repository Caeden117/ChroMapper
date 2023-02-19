using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using SplineMesh;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ArcContainer : ObjectContainer
    {
        private const float splineControlPointScaleFactor = 2.5f / 0.6f; // 2.5 multiplier used by game, divide by 0.6 to scale to cm units

        private const float
            directionZPerturbation = 1e-3f; // a small value to avoid 'look rotation viewing vector is zero'

        internal const float arcEmissionIntensity = 6;

        private const float partition = 0.00f;
        private static readonly Color unassignedColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);
        internal static readonly int emissionColor = Shader.PropertyToID("_ColorTint");

        [SerializeField] private TracksManager manager;
        [SerializeField] private GameObject indicatorMu;
        [SerializeField] private GameObject indicatorTmu;
        [SerializeField] private List<GameObject> indicators;
        [SerializeField] public BaseArc ArcData;

        private MaterialPropertyBlock indicatorMaterialPropertyBlock;

        private MeshRenderer splineRenderer;
        private bool splineRendererEnabled;

        internal MeshRenderer SplineRenderer
        {
            get => splineRenderer;
            set
            {
                splineRenderer = value;
                splineRenderer.enabled = splineRendererEnabled;
                splineRenderer.SetPropertyBlock(MaterialPropertyBlock);
            }
        }

        public override BaseObject ObjectData
        {
            get => ArcData;
            set => ArcData = (BaseArc)value;
        }

        public static ArcContainer SpawnArc(BaseArc data, ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<ArcContainer>();
            container.ArcData = data;
            return container;
        }

        public override void Setup()
        {
            base.Setup();
            MaterialPropertyBlock.SetFloat("_Lit", 1);
            MaterialPropertyBlock.SetFloat("_TranslucentAlpha", 1f);
            foreach (var gameObj in indicators) gameObj.GetComponent<ArcIndicatorContainer>().Setup();

            UpdateMaterials();
        }

        public override void UpdateGridPosition()
        {
            RecomputePosition();
            foreach (var gameObj in indicators) gameObj.GetComponent<ArcIndicatorContainer>().UpdateGridPosition();
            UpdateCollisionGroups();
        }

        public void SetScale(Vector3 scale) => transform.localScale = scale;

        //MaterialPropertyBlock.SetVector(shaderScale, scale);
        //UpdateMaterials();
        public void NotifySplineChanged(BaseArc arcData = null)
        {
            if (arcData != null) ArcData = arcData;
            if (splineRenderer != null) // since curve has been changed, firstly disable it until it is computed.
                splineRenderer.enabled = false;
            splineRendererEnabled = false;
            var arcCollection =
                BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc) as ArcGridContainer;
            arcCollection.RequestForSplineRecompute(this);
        }

        /// <summary>
        ///     Defer Bezier Curve based on arc data. Not the official algorithm I guess?
        /// </summary>
        public void RecomputePosition()
        {
            if (ArcData == null) return; // in case that this container has already been recycled when about to compute
            var spline = GetComponent<Spline>();
            var headNode = spline.nodes[0];
            var tailNode = spline.nodes[1];
            var headPos = ArcData.GetPosition();
            var tailPos = ArcData.GetTailPosition();

            headNode.SetPositionWithoutNotify(new Vector3(headPos.x, headPos.y + 0.5f, ArcData.Time * EditorScaleController.EditorScale));
            tailNode.SetPositionWithoutNotify(new Vector3(tailPos.x, tailPos.y + 0.5f, ArcData.TailTime * EditorScaleController.EditorScale));

            var headPointMultiplier = ArcData.CutDirection == (int)NoteCutDirection.Any
                ? 0f
                : ArcData.HeadControlPointLengthMultiplier;
            var d1 = new Vector3(0, splineControlPointScaleFactor * headPointMultiplier, directionZPerturbation);
            var rot1 = Quaternion.Euler(0, 0, NoteContainer.Directionalize(ArcData.CutDirection).z - 180);
            d1 = rot1 * d1;
            headNode.SetDirectionWithoutNotify(headNode.Position + d1);

            var tailPointMultiplier = ArcData.TailCutDirection == (int)NoteCutDirection.Any
                ? 0f
                : ArcData.TailControlPointLengthMultiplier;
            var d2 = new Vector3(0, splineControlPointScaleFactor * tailPointMultiplier, directionZPerturbation);
            var rot2 = Quaternion.Euler(0, 0, NoteContainer.Directionalize(ArcData.TailCutDirection).z - 180);
            d2 = rot2 * d2;
            tailNode.SetDirectionWithoutNotify(tailNode.Position + d2);

            spline.RefreshCurves();
            splineRendererEnabled = true;
            if (splineRenderer != null) splineRenderer.enabled = true;
            ResetIndicatorsPosition();
        }

        private void ResetIndicatorsPosition()
        {
            foreach (var gameObj in indicators) gameObj.GetComponent<ArcIndicatorContainer>().UpdateGridPosition();
        }

        public void SetColor(Color c)
        {
            MaterialPropertyBlock.SetColor(color, c);
            MaterialPropertyBlock.SetColor(emissionColor, c * arcEmissionIntensity);
            UpdateMaterials();
        }

        internal override void UpdateMaterials()
        {
            foreach (var gameObj in indicators)
                gameObj.GetComponent<ArcIndicatorContainer>().UpdateMaterials(MaterialPropertyBlock);
            foreach (var gameObj in indicators)
                gameObj.GetComponent<ArcIndicatorContainer>().OutlineVisible = OutlineVisible;
            if (SplineRenderer != null)
                SplineRenderer.SetPropertyBlock(MaterialPropertyBlock);
            foreach (var r in SelectionRenderers) r.SetPropertyBlock(MaterialPropertyBlock);
        }

        public void SetIndicatorBlocksActive(bool visible)
        {
            foreach (var gameObj in indicators) gameObj.SetActive(visible);
        }

        public void ChangeHeadMultiplier(float modifier) => ArcData.HeadControlPointLengthMultiplier += modifier;

        public void ChangeTailMultiplier(float modifier) => ArcData.TailControlPointLengthMultiplier += modifier;
    }
}
