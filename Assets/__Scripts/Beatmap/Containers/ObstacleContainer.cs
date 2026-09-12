using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class ObstacleContainer : ObjectContainer
    {
        // Match the shipped obstacle prefab override, not the native constructor default.
        private const float addColorMultiplier = 0.2f;
        private const float coreLerpToWhiteFactor = 0.75f;
        private const float obstacleEdgeSize = 0.05f;

        private static readonly int worldScaleId = Shader.PropertyToID("_WorldScale");
        private static readonly int uvScaleId = Shader.PropertyToID("_UVScale");
        private static readonly int addColorId = Shader.PropertyToID("_AddColor");
        private static readonly int tintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int sizeParamsId = Shader.PropertyToID("_SizeParams");

        [SerializeField] public MeshRenderer CoreRenderer;
        [SerializeField] private Material simpleObstacle;
        [SerializeField] private Material distortObstacle;

        [Header("Transform")] [SerializeField] public Transform CoreTransform;
        [SerializeField] public Transform OutlineTransform;
        [SerializeField] private TextMeshPro text;

        [Header("State")] [SerializeField] private TracksManager manager;
        public Vector3 ObstacleScale;

        public BaseObstacle ObstacleData;

        public override BaseObject ObjectData
        {
            get => ObstacleData;
            set => ObstacleData = (BaseObstacle)value;
        }

        public bool IsRotatedByNoodleExtensions => ObstacleData.CustomWorldRotation != null;

        public static ObstacleContainer SpawnObstacle(
            BaseObstacle data,
            TracksManager manager,
            ref GameObject prefab)
        {
            var container = Instantiate(prefab).GetComponent<ObstacleContainer>();
            container.ObstacleData = data;
            container.manager = manager;
            return container;
        }

        public void SwitchMaterial()
        {
            CoreRenderer.sharedMaterial = UIMode.PreviewMode ? distortObstacle : simpleObstacle;
            MpbController.ApplyChanges();
        }

        public void SetColor(Color c)
        {
            MpbController.Mpb.SetColor(ColorId, c);
            MpbController.Mpb.SetColor(tintColorId, Color.Lerp(c, Color.white, coreLerpToWhiteFactor));

            var addColor = c * addColorMultiplier;
            addColor.a = 0f;
            MpbController.Mpb.SetColor(addColorId, addColor);
            UpdateMaterials();
        }

        public void SetScale(Vector3 scale)
        {
            ObstacleScale = scale;

            scale.x *= 0.98f;
            var cubeOffset = scale / 2f;
            cubeOffset.x = 0f;

            CoreTransform.localScale = scale - (Vector3.one * 0.01f);
            CoreTransform.localPosition = cubeOffset;

            OutlineTransform.localScale = scale;
            OutlineTransform.localPosition = cubeOffset;

            MpbController.Mpb.SetVector(worldScaleId, OutlineTransform.localScale);
            MpbController.Mpb.SetVector(uvScaleId, CoreTransform.localScale);
            var absoluteScale = new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));
            MpbController.Mpb.SetVector(
                sizeParamsId,
                new Vector4(
                    absoluteScale.x * 0.5f,
                    absoluteScale.y * 0.5f,
                    absoluteScale.z * 0.5f,
                    obstacleEdgeSize * 0.5f));

            UpdateMaterials();
        }

        public float GetLength(float scale)
        {
            if (ObstacleData.CustomSize != null
                && ObstacleData.CustomSize.IsArray
                && ObstacleData.CustomSize[2].IsNumber)
                return ObstacleData.CustomSize[2];

            var length = ObstacleData.DurationSongBpmTime;

            //Take half jump duration into account if the setting is enabled.
            if (ObstacleData.Duration < 0 && Settings.Instance.ShowMoreAccurateFastWalls && !UIMode.AnimationMode)
                length -= length * Mathf.Abs(length / ObstacleData.HalfJumpDuration);

            length *= scale;

            return float.IsFinite(length) ? length : float.Epsilon;
        }

        public Vector3 ReadSize()
        {
            var bounds = ObstacleData.GetShape();

            return new Vector3(
                    Mathf.Abs(bounds.Width),
                    Mathf.Abs(bounds.Height),
                    0f
                )
                * BeatmapConstant.LaneSize;
        }

        public Vector3 ReadPosition()
        {
            var bounds = ObstacleData.GetShape();

            return new Vector3(
                    bounds.Position + (bounds.Width / 2.0f),
                    bounds.StartHeight + (bounds.Height < 0f ? bounds.Height : 0f),
                    0f
                )
                * BeatmapConstant.LaneSize;
        }

        public void UpdateScaleWithLength(float length)
        {
            var size = ReadSize();
            size.z = length;
            SetScale(size);
        }

        public override void UpdateScalable(float scale)
        {
            var length = Mathf.Abs(GetLength(scale));
            var size = ObstacleScale;
            size.z = length;
            SetScale(size);
        }

        public override void UpdateGridPosition()
        {
            var localRotation = Vector3.zero;
            var length = GetLength(
                UIMode.AnimationMode
                    ? ObstacleData.HalfJumpDistance
                    : EditorScaleController.EditorScale * BeatmapConstant.LaneSize);

            if (ObstacleData.CustomLocalRotation != null)
                localRotation = ObstacleData.CustomLocalRotation.ReadVector3();
            if (ObstacleData.CustomWorldRotation != null && !Animator.AnimatedTrack)
            {
                if (ObstacleData.CustomWorldRotation.IsNumber)
                    manager.CreateTrack(new Vector3(0, ObstacleData.CustomWorldRotation, 0)).AttachContainer(this);
                else
                    manager.CreateTrack(ObstacleData.CustomWorldRotation.ReadVector3()).AttachContainer(this);
            }

            // Enforce positive scale, offset our obstacles to match.
            transform.localPosition = new Vector3(
                0f,
                BeatmapConstant.YOffset + BeatmapConstant.ObstacleYOffset,
                (ObstacleData.SongBpmTime * EditorScaleController.EditorScale * BeatmapConstant.LaneSize)
                + (length < 0f ? length : 0f)
                + BeatmapConstant.ZOffset);
            Animator.LocalTarget.localPosition = ReadPosition();

            UpdateScaleWithLength(length);

            if (localRotation != Vector3.zero) Animator.LocalTarget.localEulerAngles = localRotation;

            UpdateCollisionGroups();
        }
        
        public void SetText(string str)
        {
            if (string.IsNullOrEmpty(str))
                text.enabled = false;
            else
            {
                text.enabled = true;
                text.SetText(str);
            }
        }

        public void SetIndicators(bool visible) =>
            text.gameObject.SetActive(visible && Settings.Instance.DisplayNoteText);
    }

}
