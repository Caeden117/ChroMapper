using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beatmap.Containers
{
    [DefaultExecutionOrder(30000)]
    internal sealed class GLSColorHoverPool : MonoBehaviour
    {
        internal sealed class Labels
        {
            internal readonly GLSNodeLabel[] Fade = new GLSNodeLabel[2];
            internal readonly GLSNodeLabel[] Strobe = new GLSNodeLabel[2];
            internal readonly GLSNodeLabel[] StrobeColor = new GLSNodeLabel[2];
            internal GLSColorHoverPool Pool;
            internal Transform Root;
            internal Transform Owner;
            internal int ActiveIndex;

            internal void SyncTransform()
            {
                // Preview visuals can be hidden independently of the container that owns the hover state.
                Root.gameObject.SetActive(Owner.gameObject.activeInHierarchy);
                Root.SetPositionAndRotation(Owner.position, Owner.rotation);
                Root.localScale = Owner.lossyScale;
            }

            private static void Hide(GLSNodeLabel[] pair)
            {
                foreach (var label in pair)
                    label.gameObject.SetActive(false);
            }

            internal void Release()
            {
                // During scene unload the pool may be destroyed before the node that borrowed these displays.
                if (Pool == null)
                    return;

                Hide(Fade);
                Hide(Strobe);
                Hide(StrobeColor);
                Root.gameObject.SetActive(false);
                Owner = null;
                var last = Pool.active.Count - 1;
                var moved = Pool.active[last];
                Pool.active[ActiveIndex] = moved;
                moved.ActiveIndex = ActiveIndex;
                Pool.active.RemoveAt(last);
                Pool.available.Push(this);
            }
        }

        private static GLSColorHoverPool instance;
        private readonly Stack<Labels> available = new();
        private readonly List<Labels> active = new();

        internal static Labels Rent(TextMeshPro[] sources, Transform parent)
        {
            if (instance == null)
            {
                var root = new GameObject("GLS Hover Label Pool");
                SceneManager.MoveGameObjectToScene(root, parent.gameObject.scene);
                instance = root.AddComponent<GLSColorHoverPool>();
            }

            var labels = instance.available.Count > 0
                ? instance.available.Pop()
                : instance.Create(sources);
            labels.Owner = parent;
            labels.ActiveIndex = instance.active.Count;
            instance.active.Add(labels);
            labels.SyncTransform();
            return labels;
        }

        private Labels Create(TextMeshPro[] sources)
        {
            var labels = new Labels { Pool = this };
            labels.Root = new GameObject("GLS Color Hover Labels").transform;
            labels.Root.SetParent(transform, false);
            CreatePair(
                labels.Root,
                labels.Fade,
                sources,
                "Fade Ease",
                -GLSEventIconView.HoverLabelHorizontalPosition,
                GLSEventCommon.ColorEasingIconHeight + (1f / 20f));
            CreatePair(
                labels.Root,
                labels.Strobe,
                sources,
                "Strobe Ease",
                GLSEventIconView.HoverLabelHorizontalPosition,
                GLSEventCommon.ColorStrobeIconHeight);
            CreatePair(
                labels.Root,
                labels.StrobeColor,
                sources,
                "Strobe Color Ease",
                -GLSEventIconView.HoverLabelHorizontalPosition,
                GLSEventCommon.ColorTertiaryIconHeight - (1f / 15f));
            return labels;
        }

        private void CreatePair(
            Transform parent,
            GLSNodeLabel[] pair,
            TextMeshPro[] sources,
            string title,
            float x,
            float height)
        {
            for (var i = 0; i < pair.Length; i++)
            {
                var display = Instantiate(sources[i], parent, false);
                display.name = $"{title} Hover {(i == 0 ? "Top" : "Side")}";
                display.enabled = true;
                display.rectTransform.sizeDelta = new Vector2(3f, 1.5f);
                display.alignment = x < 0f
                    ? TextAlignmentOptions.Right
                    : TextAlignmentOptions.Left;
                display.rectTransform.localPosition = i == 0
                    ? new Vector3(x, 0.506f, height)
                    : new Vector3(x, height, -0.506f);
                display.gameObject.SetActive(false);
                pair[i] = display.GetComponent<GLSNodeLabel>();
            }
        }

        private void LateUpdate()
        {
            foreach (var labels in active)
                labels.SyncTransform();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            available.Clear();
            active.Clear();
        }
    }
}
