using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using System;
using System.Linq;
using TMPro;

namespace Beatmap.Containers
{
    public class NoteContainer : ObjectContainer
    {
        private static readonly int glowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int colorMultiplierId = Shader.PropertyToID("_ColorMultiplier");
        private static readonly int objectTime = Shader.PropertyToID("_ObjectTime");
        private static readonly int translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

        private static readonly Color unassignedColor = new(0.25f, 0.25f, 0.25f);

        [SerializeField] public VisualModelController ModelController;

        [Header("Visual (Direction)")] [SerializeField]
        public MaterialPropertyBlockController ArrowMpbController;

        [SerializeField] public VisualModelController ArrowModelController;
        [SerializeField] public VisualModelController DotModelController;

        [Header("Others")] [SerializeField] public Transform DirectionTarget;
        [SerializeField] private SpriteRenderer swingArcRenderer;
        [SerializeField] private TextMeshPro text;

        public AssignObjectPrefabManager AssignObjectPrefabManager;
        public BaseNote NoteData;

        [NonSerialized] public Vector3 DirectionTargetEuler = Vector3.zero;

        public override BaseObject ObjectData
        {
            get => NoteData;
            set => NoteData = (BaseNote)value;
        }

        protected override void RegisterCallback()
        {
            VisualSettings.OnNoteModelChanged += HandleModelChanged;
            VisualSettings.OnBombModelChanged += HandleModelChanged;
            VisualSettings.OnChainHeadModelChanged += HandleModelChanged;
        }

        protected override void UnregisterCallback()
        {
            VisualSettings.OnNoteModelChanged -= HandleModelChanged;
            VisualSettings.OnBombModelChanged -= HandleModelChanged;
            VisualSettings.OnChainHeadModelChanged -= HandleModelChanged;
        }

        public override void Setup()
        {
            base.Setup();

            HandleModelChanged();
            ArrowModelController.MpbController.Mpb.SetFloat(translucentAlpha, Settings.Instance.PastNoteModelAlpha);
            DotModelController.MpbController.Mpb.SetFloat(translucentAlpha, Settings.Instance.PastNoteModelAlpha);
            UpdateMaterials();

            SetArcVisible(NoteGridContainer.ShowArcVisualizer);
        }

        public override void HandleModelChanged()
        {
            if (NoteData == null) return;
            if (NoteData.Type == (int)NoteType.Bomb)
                SetBombModel();
            else
                SetNoteModel();
        }

        public void SetNoteModel()
        {
            VisualModelSO vm;
            if (NoteData.CutDirection == (int)NoteCutDirection.Any)
            {
                vm =
                    NoteData.Chains.Count > 0
                        ? NoteData.Type == (int)NoteType.Blue
                            ? VisualSettings.GetBurstSliderHeadDotRightModel()
                            : VisualSettings.GetBurstSliderHeadDotLeftModel()
                        : NoteData.Type == (int)NoteType.Blue
                            ? VisualSettings.GetNoteDotRightModel()
                            : VisualSettings.GetNoteDotLeftModel();
            }
            else
            {
                vm = NoteData.Chains.Count > 0
                    ? NoteData.Type == (int)NoteType.Blue
                        ? VisualSettings.GetBurstSliderHeadRightModel()
                        : VisualSettings.GetBurstSliderHeadLeftModel()
                    : NoteData.Type == (int)NoteType.Blue
                        ? VisualSettings.GetNoteRightModel()
                        : VisualSettings.GetNoteLeftModel();
            }

            var defaultModel = vm;
            ArrowMpbController.gameObject.SetActive(!vm.DisableAux);

            var kind = NoteData.Chains.Count > 0
                ? TrackModelState.Kind.BurstSlider
                : NoteData.CutDirection == (int)NoteCutDirection.Any
                    ? TrackModelState.Kind.AnyNote
                    : TrackModelState.Kind.DirectionalNote;

            if (NoteData.CustomTrack != null && AssignObjectPrefabManager != null)
            {
                if (NoteData.CustomTrack.IsString)
                {
                    var result = AssignObjectPrefabManager.GetCurrentModels(kind, NoteData.CustomTrack);
                    if (result.HasOverride && result.OverrideModel != null)
                    {
                        vm = result.OverrideModel;
                        ArrowMpbController.gameObject.SetActive(false);
                        ArrowMpbController.ShowRenderer(false);
                    }
                    else
                    {
                        ArrowMpbController.gameObject.SetActive(!vm.DisableAux);
                        ArrowMpbController.ShowRenderer(true);
                    }

                    ModelController.Set(vm);
                    foreach (var model in result.AdditiveModels) ModelController.Add(model != null ? model : defaultModel);
                    if (result.AdditiveModels.Contains(null))
                    {
                        ArrowMpbController.gameObject.SetActive(!defaultModel.DisableAux);
                        ArrowMpbController.ShowRenderer(true);
                    }
                }
                else if (NoteData.CustomTrack.IsArray)
                {
                    var result = AssignObjectPrefabManager.GetCurrentModels(
                        kind,
                        NoteData.CustomTrack.Children.Select(x => (string)x).ToArray());
                    if (result.HasOverride && result.OverrideModel != null)
                    {
                        vm = result.OverrideModel;
                        ArrowMpbController.gameObject.SetActive(false);
                        ArrowMpbController.ShowRenderer(false);
                    }
                    else
                    {
                        ArrowMpbController.gameObject.SetActive(!vm.DisableAux);
                        ArrowMpbController.ShowRenderer(true);
                    }

                    ModelController.Set(vm);
                    foreach (var model in result.AdditiveModels) ModelController.Add(model != null ? model : defaultModel);
                    if (result.AdditiveModels.Contains(null))
                    {
                        ArrowMpbController.gameObject.SetActive(!defaultModel.DisableAux);
                        ArrowMpbController.ShowRenderer(true);
                    }
                }
            }
            else
            {
                ModelController.Set(vm);
                ArrowMpbController.ShowRenderer(true);
            }
        }

        public void SetBombModel()
        {
            ArrowMpbController.ShowRenderer(false);
            var vm = VisualSettings.GetBombModel();
            var defaultModel = vm;

            if (NoteData.CustomTrack != null && AssignObjectPrefabManager != null)
            {
                if (NoteData.CustomTrack.IsString)
                {
                    var result = AssignObjectPrefabManager.GetCurrentModels(
                        TrackModelState.Kind.Bomb,
                        NoteData.CustomTrack);
                    if (result.HasOverride && result.OverrideModel != null) vm = result.OverrideModel;
                    ModelController.Set(vm);
                    foreach (var model in result.AdditiveModels) ModelController.Add(model != null ? model : defaultModel);
                }
                else if (NoteData.CustomTrack.IsArray)
                {
                    var result = AssignObjectPrefabManager.GetCurrentModels(
                        TrackModelState.Kind.Bomb,
                        NoteData.CustomTrack.Children.Select(x => (string)x).ToArray());
                    if (result.HasOverride && result.OverrideModel != null) vm = result.OverrideModel;
                    ModelController.Set(vm);
                    foreach (var model in result.AdditiveModels) ModelController.Add(model != null ? model : defaultModel);
                }
            }
            else
                ModelController.Set(vm);
        }

        internal static Vector3 Directionalize(BaseNote noteData)
        {
            if (noteData is null) return Vector3.zero;
            var cutDirection = noteData.CutDirection;
            var directionEuler = Directionalize(cutDirection);
            if (noteData.CustomDirection != null)
            {
                directionEuler = new Vector3(0, 0, noteData.CustomDirection ?? 0);
            }
            else
            {
                var newNoteData = noteData;
                if (newNoteData != null && newNoteData.AngleOffset != 0)
                {
                    directionEuler += new Vector3(0, 0, newNoteData.AngleOffset);
                }
                else
                {
                    if (cutDirection >= 1000) directionEuler += new Vector3(0, 0, 360 - (cutDirection - 1000));
                }
            }

            return directionEuler;
        }

        internal static Vector3 Directionalize(int cutDirection)
        {
            var directionEuler = Vector3.zero;
            switch (cutDirection)
            {
                case (int)NoteCutDirection.Up:
                    directionEuler += new Vector3(0, 0, 180);
                    break;
                case (int)NoteCutDirection.Down:
                    directionEuler += new Vector3(0, 0, 0);
                    break;
                case (int)NoteCutDirection.Left:
                    directionEuler += new Vector3(0, 0, -90);
                    break;
                case (int)NoteCutDirection.Right:
                    directionEuler += new Vector3(0, 0, 90);
                    break;
                case (int)NoteCutDirection.UpRight:
                    directionEuler += new Vector3(0, 0, 135);
                    break;
                case (int)NoteCutDirection.UpLeft:
                    directionEuler += new Vector3(0, 0, -135);
                    break;
                case (int)NoteCutDirection.DownLeft:
                    directionEuler += new Vector3(0, 0, -45);
                    break;
                case (int)NoteCutDirection.DownRight:
                    directionEuler += new Vector3(0, 0, 45);
                    break;
            }

            return directionEuler;
        }

        public void SetDot()
        {
            ArrowModelController.gameObject.SetActive(false);
            DotModelController.gameObject.SetActive(true);
        }

        public void SetArrow()
        {
            ArrowModelController.gameObject.SetActive(true);
            DotModelController.gameObject.SetActive(false);
        }

        public void SetArcVisible(bool showArcVisualizer)
        {
            if (swingArcRenderer != null) swingArcRenderer.enabled = showArcVisualizer;
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

        public static NoteContainer SpawnBeatmapNote(BaseNote noteData, ref GameObject notePrefab)
        {
            var container = Instantiate(notePrefab).GetComponent<NoteContainer>();
            container.NoteData = noteData;
            container.DirectionTarget.localEulerAngles = Directionalize(noteData);
            return container;
        }

        public override void UpdateGridPosition()
        {
            var pos = NoteData.GetPosition();
            if (!(Animator != null && Animator.AnimatedTrack))
            {
                transform.localPosition = (Vector3)pos
                    + new Vector3(
                        0f,
                        BeatmapConstant.YOffset + BeatmapConstant.PlayerYOffset,
                        (NoteData.SongBpmTime * EditorScaleController.EditorScale * BeatmapConstant.LaneSize)
                        + BeatmapConstant.ZOffset);
            }

            transform.localScale = NoteData.GetScale();
            DirectionTarget.localEulerAngles = DirectionTargetEuler;

            // default scale prior to this setting worked out to be 90%
            if (!Settings.Instance.AccurateNoteSize && NoteData.Type != (int)NoteType.Bomb)
                ModelController.transform.localScale = Vector3.one * 0.9f;
            else
                ModelController.transform.localScale = Vector3.one;

            UpdateCollisionGroups();

            ModelController.MpbController.Mpb.SetFloat(objectTime, NoteData.SongBpmTime);
            ArrowMpbController.Mpb.SetFloat(objectTime, NoteData.SongBpmTime);
            SetRotation(AssignedTrack != null ? AssignedTrack.RotationValue.y : 0);
            UpdateMaterials();
        }

        public void SetColor(Color? c)
        {
            var color = c ?? unassignedColor;
            ModelController.MpbController.Mpb.SetColor(ColorId, color);
            ModelController.MpbController.Mpb.SetFloat(colorMultiplierId, Settings.Instance.NoteColorMultiplier);

            var arrowColor = Color.Lerp(color, Color.white, Settings.Instance.ArrowColorWhiteBlend);
            ArrowMpbController.Mpb.SetColor(ColorId, arrowColor);
            ArrowMpbController.Mpb.SetColor(glowColorId, color);
            ArrowMpbController.Mpb.SetFloat(colorMultiplierId, Settings.Instance.ArrowColorMultiplier);

            UpdateMaterials();
        }

        internal override void UpdateMaterials()
        {
            MpbController.ApplyChanges();
            ArrowMpbController.ApplyChanges();
        }

        public void SetIndicators(bool visible) =>
            text.gameObject.SetActive(visible && Settings.Instance.DisplayNoteText);
    }
}
