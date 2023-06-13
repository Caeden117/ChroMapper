using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ArcContainer : ObjectContainer
    {
        private const float splineControlPointScaleFactor = 2.5f / 0.6f; // 2.5 multiplier used by game, divide by 0.6 to scale to cm units
        internal const float arcEmissionIntensity = 6;
        private const int numSamples = 30;

        internal static readonly int emissionColor = Shader.PropertyToID("_ColorTint");

        [SerializeField] private TracksManager manager;
        [SerializeField] private GameObject indicatorMu;
        [SerializeField] private GameObject indicatorTmu;
        [SerializeField] private List<GameObject> indicators;
        [SerializeField] public BaseArc ArcData;

        private MaterialPropertyBlock indicatorMaterialPropertyBlock;

        [SerializeField] private LineRenderer splineRenderer;

        public Vector3 p0()
        {
            var position = ArcData.GetPosition();
            return new Vector3(position.x, position.y + 1.5f, 0f);
        }
        public Vector3 p1()
        {
            var headPosition = ArcData.GetPosition();
            if (ArcData.CutDirection == (int)NoteCutDirection.Any)
            {
                return new Vector3(headPosition.x, headPosition.y + 1.5f, 0f);
            }

            var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ArcData.CutDirection).z;
            var headDirection = new Vector2(Mathf.Sin(zRads), -Mathf.Cos(zRads));
            var nodePosition = headPosition + headDirection * ArcData.HeadControlPointLengthMultiplier * splineControlPointScaleFactor;
            return new Vector3(nodePosition.x, nodePosition.y + 1.5f, 0f);
        }

        public Vector3 p2()
        {
            var tailPosition = ArcData.GetTailPosition();
            if (ArcData.TailCutDirection == (int)NoteCutDirection.Any)
            {
                return new Vector3(tailPosition.x, tailPosition.y + 1.5f, (ArcData.TailSongBpmTime - ArcData.SongBpmTime) * EditorScaleController.EditorScale);
            }

            var zRads = Mathf.Deg2Rad * NoteContainer.Directionalize(ArcData.TailCutDirection).z;
            var tailDirection = new Vector2(Mathf.Sin(zRads), -Mathf.Cos(zRads));
            var tailNodePosition = tailPosition - tailDirection * ArcData.TailControlPointLengthMultiplier * splineControlPointScaleFactor;
            return new Vector3(tailNodePosition.x, tailNodePosition.y + 1.5f, (ArcData.TailSongBpmTime - ArcData.SongBpmTime) * EditorScaleController.EditorScale);
        }

        public Vector3 p3()
        {
            var tailPosition = ArcData.GetTailPosition();
            return new Vector3(tailPosition.x, tailPosition.y + 1.5f, (ArcData.TailSongBpmTime - ArcData.SongBpmTime) * EditorScaleController.EditorScale);
        }

        // B(t) = (1-t)^3 p0 + 3(1-t)^2 t p1 + 3(1-t)t^2 p2 + t^3 p3
        Vector3 SampleCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var tDiff = 1 - t;
            return (Mathf.Pow(tDiff, 3) * p0) + (3 * Mathf.Pow(tDiff, 2) * t * p1) + (3 * tDiff * Mathf.Pow(t, 2) * p2) + (Mathf.Pow(t, 3) * p3);
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

        public void NotifySplineChanged(BaseArc arcData = null)
        {
            if (arcData != null) ArcData = arcData;
            var arcCollection =
                BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc) as ArcGridContainer;
            arcCollection.RequestForSplineRecompute(this);
        }

        /// <summary>
        ///     Defer Bezier Curve based on arc data.
        /// </summary>
        public void RecomputePosition()
        {
            if (ArcData == null) return; // in case that this container has already been recycled when about to compute
            this.transform.localPosition = new Vector3(0, 0, ArcData.SongBpmTime * EditorScaleController.EditorScale);
            splineRenderer.positionCount = numSamples + 1;

            var p0 = this.p0();
            var p1 = this.p1();
            var p2 = this.p2();
            var p3 = this.p3();

            for (int i = 0; i <= numSamples; i++)
            {
                splineRenderer.SetPosition(i, SampleCubicBezierPoint(p0, p1, p2, p3, (float)i / numSamples));
            }

            splineRenderer.enabled = true;
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
            splineRenderer.SetPropertyBlock(MaterialPropertyBlock);
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
