using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using System;

namespace Beatmap.Containers
{
    public class NoteContainer : ObjectContainer
    {
        private static readonly int colorMultiplier = Shader.PropertyToID("_ColorMult");
        private static readonly int objectTime = Shader.PropertyToID("_ObjectTime");
        private static readonly int lit = Shader.PropertyToID("_Lit");
        private static readonly int translucentAlpha = Shader.PropertyToID("_TranslucentAlpha");

        private static readonly Color unassignedColor = new Color(0.1544118f, 0.1544118f, 0.1544118f);

        [SerializeField] private GameObject simpleBlock;
        [SerializeField] private GameObject simpleChainHead;
        [SerializeField] private GameObject complexBlock;
        [SerializeField] private GameObject complexChainHead;
        [SerializeField] public Transform DirectionTarget;

        [SerializeField] private List<MeshRenderer> noteRenderer;
        [SerializeField] private List<MeshRenderer> chainRenderer;
        [SerializeField] private MeshRenderer bombRenderer;
        [SerializeField] private MeshRenderer dotRenderer;
        [SerializeField] private MeshRenderer arrowRenderer;
        [SerializeField] private SpriteRenderer swingArcRenderer;

        public BaseNote NoteData;
        public MaterialPropertyBlock ArrowMaterialPropertyBlock;

        [NonSerialized] public Vector3 DirectionTargetEuler = Vector3.zero;

        public override BaseObject ObjectData
        {
            get => NoteData;
            set => NoteData = (BaseNote)value;
        }

        public override void Setup()
        {
            base.Setup();

            SetModelInfer();
            MaterialPropertyBlock.SetFloat(lit, Settings.Instance.SimpleBlocks ? 0 : 1);
            MaterialPropertyBlock.SetFloat(translucentAlpha, Settings.Instance.PastNoteModelAlpha);
            UpdateMaterials();

            if (ArrowMaterialPropertyBlock == null)
            {
                ArrowMaterialPropertyBlock = new MaterialPropertyBlock();
            }

            SetArcVisible(NoteGridContainer.ShowArcVisualizer);
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

        public void SetDotVisible(bool b) => dotRenderer.enabled = b;

        public void SetArrowVisible(bool b) => arrowRenderer.enabled = b;

        // TODO: have proper model swapper instead of convoluting the container
        public void SetChainHeadModel()
        {
            if (NoteData.Type == (int)NoteType.Bomb) return;
            simpleBlock.SetActive(false);
            complexBlock.SetActive(false);

            simpleChainHead.SetActive(Settings.Instance.SimpleBlocks);
            complexChainHead.SetActive(!Settings.Instance.SimpleBlocks);

            bombRenderer.gameObject.SetActive(false);
            bombRenderer.enabled = false;
        }

        public void SetModelInfer()
        {
            if (NoteData == null) return;
            if (NoteData.Type == (int)NoteType.Bomb)
                SetBombModel();
            else
                SetNoteModel();
        }

        public void SetNoteModel()
        {
            simpleBlock.SetActive(Settings.Instance.SimpleBlocks);
            complexBlock.SetActive(!Settings.Instance.SimpleBlocks);

            simpleChainHead.SetActive(false);
            complexChainHead.SetActive(false);

            bombRenderer.gameObject.SetActive(false);
            bombRenderer.enabled = false;
        }

        public void SetBombModel()
        {
            simpleBlock.SetActive(false);
            complexBlock.SetActive(false);

            simpleChainHead.SetActive(false);
            complexChainHead.SetActive(false);

            bombRenderer.gameObject.SetActive(true);
            bombRenderer.enabled = true;
        }

        public void SetArcVisible(bool showArcVisualizer)
        {
            if (swingArcRenderer != null) swingArcRenderer.enabled = showArcVisualizer;
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
            if (!(Animator != null && Animator.AnimatedTrack))
            {
                transform.localPosition = (Vector3)NoteData.GetPosition()
                    + new Vector3(0, offsetY, NoteData.SongBpmTime * EditorScaleController.EditorScale);
            }

            transform.localScale = NoteData.GetScale();
            DirectionTarget.localScale = Vector3.one;
            DirectionTarget.localEulerAngles = DirectionTargetEuler;

            // default scale prior to this setting worked out to be 90%
            if (!Settings.Instance.AccurateNoteSize && NoteData.Type != (int)NoteType.Bomb)
                DirectionTarget.localScale *= 0.9f;

            if (NoteData.Type != (int)NoteType.Bomb)
            {
                // Only apply this to notes as bomb DirectionTarget affects hover placement as well
                // really need to think about prefab structure soon
                DirectionTarget.localPosition = Vector3.zero;
            }

            UpdateCollisionGroups();

            MaterialPropertyBlock.SetFloat(objectTime, NoteData.SongBpmTime);
            ArrowMaterialPropertyBlock.SetFloat(objectTime, NoteData.SongBpmTime);
            SetRotation(AssignedTrack != null ? AssignedTrack.RotationValue.y : 0);
            UpdateMaterials();
        }

        public void SetColor(Color? c)
        {
            MaterialPropertyBlock.SetColor(color, c ?? unassignedColor);

            var arrowColor = Color.Lerp(c ?? unassignedColor, Color.white, Settings.Instance.ArrowColorWhiteBlend);
            ArrowMaterialPropertyBlock.SetColor(color, arrowColor);

            MaterialPropertyBlock.SetFloat(colorMultiplier, Settings.Instance.NoteColorMultiplier);
            ArrowMaterialPropertyBlock.SetFloat(colorMultiplier, Settings.Instance.ArrowColorMultiplier);

            UpdateMaterials();
        }

        internal override void UpdateMaterials()
        {
            foreach (var renderer in noteRenderer) renderer.SetPropertyBlock(MaterialPropertyBlock);
            foreach (var renderer in chainRenderer) renderer.SetPropertyBlock(MaterialPropertyBlock);
            foreach (var renderer in SelectionRenderers) renderer.SetPropertyBlock(MaterialPropertyBlock);
            bombRenderer.SetPropertyBlock(MaterialPropertyBlock);
            if (dotRenderer != null)
            {
                dotRenderer.SetPropertyBlock(ArrowMaterialPropertyBlock);
                arrowRenderer.SetPropertyBlock(ArrowMaterialPropertyBlock);
            }
        }
    }
}
