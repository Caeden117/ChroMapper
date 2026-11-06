using System;
using UnityEngine;

namespace Beatmap.Containers
{
    [CreateAssetMenu(menuName = "Beatmap/GLS Node Label Layout")]
    public sealed class GLSNodeLabelLayout : ScriptableObject
    {
        [Serializable]
        public struct Field
        {
            [Tooltip("Baseline position in the text face's local coordinates.")]
            public Vector2 Anchor;
            [Tooltip("Size relative to the TMP component's font size.")]
            public float FontScale;
        }

        public Field Brightness;
        public Field StrobeBrightness;
        public Field StrobeRate;
        public Field Loop;
        public Field RotationEasing;
        public Field TransformEasing;
        public Field Value;
        public Field HoverTitle;
        public Field HoverEasing;

        internal Field GetField(GLSNodeLabelKind kind, int row)
        {
            return (kind, row) switch
            {
                (GLSNodeLabelKind.Color, 0) => Brightness,
                (GLSNodeLabelKind.Color, 1) => StrobeBrightness,
                (GLSNodeLabelKind.Color, _) => StrobeRate,
                (GLSNodeLabelKind.Rotation or GLSNodeLabelKind.Transform, 0) => Loop,
                (GLSNodeLabelKind.Rotation, 1) => RotationEasing,
                (GLSNodeLabelKind.Transform, 1) => TransformEasing,
                (GLSNodeLabelKind.Rotation or GLSNodeLabelKind.Transform, _) => Value,
                (GLSNodeLabelKind.Hover, 0) => HoverTitle,
                _ => HoverEasing
            };
        }
    }

    internal enum GLSNodeLabelKind
    {
        Color,
        Rotation,
        Transform,
        Hover
    }
}
