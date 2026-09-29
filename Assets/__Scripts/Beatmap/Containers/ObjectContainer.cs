using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using Beatmap.Animations;

namespace Beatmap.Containers
{
    public abstract class ObjectContainer : MonoBehaviour
    {
        protected static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int rotationId = Shader.PropertyToID("_Rotation");
        private static readonly int objectTimeId = Shader.PropertyToID("_ObjectTime");

        [SerializeField] public ObjectAnimator Animator;
        [SerializeField] protected List<IntersectionCollider> Colliders;

        [Header("Visual")] [SerializeField] public VisualSettingsSO VisualSettings;
        [SerializeField] public MaterialPropertyBlockController MpbController;
        [SerializeField] public MaterialPropertyBlockController SelectionMpbController;

        private Color currentOutlineColor;
        private bool selected;

        public virtual bool Selected
        {
            get => selected;
            set
            {
                if (selected == value) return;
                selected = value;
                RefreshOutlineColor();
                RefreshOutlineVisual();
            }
        }

        private bool highlighted;

        public virtual bool Highlighted
        {
            get => highlighted;
            set
            {
                if (highlighted == value) return;
                highlighted = value;
                RefreshOutlineColor();
                RefreshOutlineVisual();
            }
        }

        private bool dragged;

        public bool Dragged
        {
            get => dragged;
            set
            {
                if (dragged == value) return;
                dragged = value;
                RefreshOutlineColor();
                RefreshOutlineVisual();
            }
        }

        public Track AssignedTrack { get; private set; }

        public abstract BaseObject ObjectData { get; set; }

        public int ChunkID => (int)(ObjectData.JsonTime / Intersections.ChunkSize);

        public void Start() => RegisterCallback();
        public void OnDestroy() => UnregisterCallback();

        protected virtual void RegisterCallback() { }
        protected virtual void UnregisterCallback() { }

        public virtual void Setup() { }

        protected void DisablePassedObjectDither()
        {
            MpbController.Mpb.SetFloat(objectTimeId, 9999f);
            MpbController.ApplyChanges();
        }

        internal void SafeSetActive(bool active) => gameObject.SetActive(active);

        public abstract void UpdateGridPosition();

        public virtual void UpdateScalable(float scale) { }

        internal virtual void UpdateMaterials() => MpbController.ApplyChanges();

        public void SetRotation(float rot)
        {
            MpbController.Mpb.SetFloat(rotationId, rot);
            UpdateMaterials();
        }

        public void SetOutlineColor(Color color)
        {
            currentOutlineColor = color;
            RefreshOutlineColor();
        }

        public virtual void AssignTrack(Track track) => AssignedTrack = track;

        protected virtual void UpdateCollisionGroups()
        {
            var chunkId = ChunkID;

            foreach (var c in Colliders)
            {
                var unregistered = Intersections.UnregisterColliderFromGroups(c);
                c.CollisionGroups.Clear();
                c.CollisionGroups.Add(chunkId);
                if (unregistered) Intersections.RegisterColliderToGroups(c);
            }
        }

        public void RefreshOutlineVisual() => SelectionMpbController.ShowRenderer(selected | highlighted | dragged);

        public void RefreshOutlineColor()
        {
            // A selected/copy outline has priority over the transient hover or drag outline.
            SelectionMpbController.Mpb.SetColor(
                ColorId,
                selected 
                    ? currentOutlineColor 
                    : highlighted | dragged 
                        ? DeleteToolController.IsActive 
                            ? Color.red 
                            : Color.white 
                        : currentOutlineColor);
            SelectionMpbController.ApplyChanges();
        }
    }
}
