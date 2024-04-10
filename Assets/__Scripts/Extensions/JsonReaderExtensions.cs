using Newtonsoft.Json;
using System;
using UnityEngine;

public static class JsonReaderExtensions
{
    public static float ReadAsFloat(this JsonReader reader, bool throwOnNull = true, float defaultFloat = default)
    {
        var value = reader.ReadAsDouble();

        return value.HasValue
            ? (float)value.Value
            : throwOnNull
                ? throw new ArgumentNullException(nameof(reader))
                : defaultFloat;
    }
}
