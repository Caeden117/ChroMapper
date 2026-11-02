using System;
using Beatmap.Base;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    [DisallowMultipleComponent]
    public sealed class GLSNodeLabel : MonoBehaviour
    {
        private const int InitialCharacterCapacity = 32;
        private const string BufferWarmupText = "0000000000000000000000000000\n\n\n";

        [SerializeField] private TextMeshPro display;
        [SerializeField] private GLSNodeLabelLayout layout;
        private readonly GLSNodeLabelBuffer buffer = new();
        private char[] submitted = new char[64];
        private int submittedLength = -1;
        private GLSNodeLabelKind submittedKind;

        private void Awake()
        {
            WarmTextBuffers();
            display.OnPreRenderText += ApplyLayout;
        }

        private void OnDestroy() => display.OnPreRenderText -= ApplyLayout;

        // TMP's internal text buffers are runtime-only, so reserve them before this pooled face is used.
        private void WarmTextBuffers()
        {
            display.SetText(BufferWarmupText);
            display.ForceMeshUpdate(true);
            TMP_TextInfo.Resize(ref display.textInfo.characterInfo, InitialCharacterCapacity);
            display.textInfo.meshInfo[0].ResizeMeshInfo(InitialCharacterCapacity);
            display.SetText(string.Empty);
            // A disabled TMP must keep its detached mesh, so clear the one the warmup forced.
            if (!display.enabled)
                display.meshFilter.sharedMesh = null;
        }

        public void SetEvent(BaseGLSEvent evt)
        {
            buffer.WriteEvent(evt);
            Submit();
        }

        internal void SetHover(string title, int easing, bool hsv, float? pulseWidth)
        {
            buffer.WriteHover(title, easing, hsv, pulseWidth);
            Submit();
        }

        private void Submit()
        {
            var length = buffer.Length;
            if (submittedLength == length && submittedKind == buffer.Kind &&
                buffer.Characters.AsSpan(0, length).SequenceEqual(submitted.AsSpan(0, length)))
            {
                return;
            }

            if (submitted.Length < length)
                Array.Resize(ref submitted, Mathf.NextPowerOfTwo(length));

            Array.Copy(buffer.Characters, submitted, length);
            submittedLength = length;
            submittedKind = buffer.Kind;
            var textInfo = display.textInfo;
            if (textInfo.characterInfo.Length < length + 1)
                TMP_TextInfo.Resize(ref textInfo.characterInfo, Mathf.NextPowerOfTwo(length + 1));

            display.SetCharArray(buffer.Characters, 0, length);
        }

        private void ApplyLayout(TMP_TextInfo text)
        {
            if (submittedLength < 0)
                return;

            var font = display.font;
            var spacing = font.normalSpacingOffset + display.characterSpacing;
            if ((display.fontStyle & FontStyles.Bold) != 0)
                spacing += font.boldSpacing;

            var emScale = display.isOrthographic
                ? 0.01f
                : 0.001f;
            var emSpacing = spacing * display.fontSize * emScale;
            var isHover = submittedKind == GLSNodeLabelKind.Hover;
            var alignment = isHover
                ? display.alignment
                : TextAlignmentOptions.Center;
            var edge = alignment == TextAlignmentOptions.Right
                ? 0.5f
                : -0.5f;
            var hoverEdge = isHover
                ? display.rectTransform.rect.width * edge
                : 0f;
            for (var index = 0; index < text.characterCount; index++)
            {
                ref var character = ref text.characterInfo[index];
                var field = layout.GetField(submittedKind, character.lineNumber);
                var scale = field.FontScale;
                var anchor = field.Anchor;
                var line = text.lineInfo[character.lineNumber];
                var last = line.lastCharacterIndex;
                if (text.characterInfo[last].character == '\n')
                    last--;

                var ordinal = index - line.firstCharacterIndex;
                // TMP's character spacing uses the component font size even inside a <size> span.
                var spacingDelta = emSpacing * (1f - scale);
                var alignmentOffset = alignment == TextAlignmentOptions.Right
                    ? last - line.firstCharacterIndex
                    : alignment == TextAlignmentOptions.Left
                        ? 0f
                        : (last - line.firstCharacterIndex) * 0.5f;
                anchor.x += (ordinal - alignmentOffset) * spacingDelta;
                anchor.x += hoverEdge * (1f - scale);

                var oldBaseline = character.baseLine;
                var offset = new Vector3(anchor.x, anchor.y - (oldBaseline * scale));
                character.origin = (character.origin * scale) + offset.x;
                character.xAdvance = (character.xAdvance * scale) + offset.x + spacingDelta;
                character.baseLine = anchor.y;
                character.ascender = (character.ascender * scale) + offset.y;
                character.descender = (character.descender * scale) + offset.y;
                character.pointSize *= scale;
                character.bottomLeft = (character.bottomLeft * scale) + offset;
                character.topLeft = (character.topLeft * scale) + offset;
                character.topRight = (character.topRight * scale) + offset;
                character.bottomRight = (character.bottomRight * scale) + offset;
                if (!character.isVisible)
                    continue;

                var mesh = text.meshInfo[character.materialReferenceIndex];
                // TMP's shader reads the signed SDF scale from UV0.w. Vertex scaling alone changes the outline and AA.
                for (var vertex = character.vertexIndex; vertex < character.vertexIndex + 4; vertex++)
                {
                    mesh.vertices[vertex] = (mesh.vertices[vertex] * scale) + offset;
                    mesh.uvs0[vertex].w *= scale;
                }
            }
        }
    }
}
