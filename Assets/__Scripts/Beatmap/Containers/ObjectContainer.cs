using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

using Beatmap.Animations;

namespace Beatmap.Containers
{
    public abstract class ObjectContainer : MonoBehaviour
    {
        public static Action<ObjectContainer, bool, string> FlaggedForDeletionEvent;

        internal static readonly int color = Shader.PropertyToID("_Color");
        internal static readonly int rotation = Shader.PropertyToID("_Rotation");
        internal static readonly int outline = Shader.PropertyToID("_Outline");
        internal static readonly int outlineColor = Shader.PropertyToID("_OutlineColor");

        public bool Dragging;

        [SerializeField] protected List<IntersectionCollider> Colliders;
        [SerializeField] protected List<Renderer> SelectionRenderers = new List<Renderer>();
        [SerializeField] protected BoxCollider BoxCollider;
        [SerializeField] public ObjectAnimator Animator;

        protected readonly List<Renderer> modelRenderers = new List<Renderer>();
        public MaterialPropertyBlock MaterialPropertyBlock;
        internal bool selectionStateChanged;

        public bool OutlineVisible
        {
            get => MaterialPropertyBlock.GetFloat(outline) != 0;
            set
            {
                SelectionRenderers.ForEach(r => r.enabled = value);
                MaterialPropertyBlock.SetFloat(outline, value ? 0.05f : 0);
                UpdateMaterials();
            }
        }

        public Track AssignedTrack { get; private set; }

        public abstract BaseObject ObjectData { get; set; }

        public int ChunkID => (int)(ObjectData.JsonTime / Intersections.ChunkSize);

        public abstract void UpdateGridPosition();

        public virtual void Setup()
        {
            if (MaterialPropertyBlock == null)
            {
                MaterialPropertyBlock = new MaterialPropertyBlock();
                modelRenderers.AddRange(GetComponentsInChildren<Renderer>(true).Where(x => !(x is SpriteRenderer)));
            }
        }

        internal virtual void SafeSetActive(bool active)
        {
            if (active != gameObject.activeSelf) gameObject.SetActive(active);
        }

        internal void SafeSetBoxCollider(bool con)
        {
            if (BoxCollider == null) return;
            if (con != BoxCollider.isTrigger) BoxCollider.isTrigger = con;
        }

        internal virtual void UpdateMaterials()
        {
            foreach (var r in modelRenderers) r.SetPropertyBlock(MaterialPropertyBlock);
        }

        public void SetRotation(float rot)
        {
            MaterialPropertyBlock.SetFloat(rotation, rot);
            UpdateMaterials();
        }

        public void SetOutlineColor(Color color, bool automaticallyShowOutline = true)
        {
            if (automaticallyShowOutline) OutlineVisible = true;
            MaterialPropertyBlock.SetColor(outlineColor, color);
            UpdateMaterials();
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
    }
}
