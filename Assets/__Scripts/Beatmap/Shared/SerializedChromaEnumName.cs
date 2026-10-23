using System;
using SimpleJSON;

namespace Beatmap.Shared
{
    // Chroma parses string enums, so runtime aliases must not turn raw JSON numbers into strings on export.
    internal struct SerializedChromaEnumName
    {
        private static readonly Func<string, string> EasingCanonicalizer = Easing.CanonicalHeckName;
        private static readonly Func<string, string> LerpCanonicalizer = BasicEventColorLerp.CanonicalName;
        private JSONNode serializedValue;

        internal string RuntimeName { get; private set; }

        internal void SetEasingName(string name) => SetName(name, EasingCanonicalizer);

        internal void SetLerpName(string name) => SetName(name, LerpCanonicalizer);

        internal void SetSerializedEasing(JSONNode value) => SetSerialized(value, EasingCanonicalizer, "easeLinear");

        internal void SetSerializedLerp(JSONNode value) => SetSerialized(value, LerpCanonicalizer, "RGB");

        private void SetName(string name, Func<string, string> canonicalize)
        {
            serializedValue = name == null
                ? null
                : new JSONString(name);
            RuntimeName = canonicalize(name);
        }

        private void SetSerialized(JSONNode value, Func<string, string> canonicalize, string unsupportedName)
        {
            serializedValue = value;
            RuntimeName = value == null
                ? null
                : value.IsString
                    ? canonicalize(value.Value)
                    : unsupportedName;
        }

        internal void WriteTo(JSONNode output, string key)
        {
            if (serializedValue != null)
                output[key] = serializedValue;

            else
                output.Remove(key);

        }

        internal SerializedChromaEnumName Clone() => new()
        {
            serializedValue = serializedValue?.Clone(),
            RuntimeName = RuntimeName
        };
    }
}
