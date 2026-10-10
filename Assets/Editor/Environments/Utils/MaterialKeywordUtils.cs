using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class MaterialKeywordUtils
{
    public static void SetEnumKeywords(Material material, string propertyName, IReadOnlyList<string> options, int index)
    {
        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i].Replace(' ', '_').ToUpperInvariant();
            if (option is "NONE" or "OFF")
                continue;

            var keyword = $"{propertyName.ToUpperInvariant()}_{option}";
            var localKeyword = material.shader.keywordSpace.FindKeyword(keyword);
            if (!localKeyword.isValid)
                continue;

            if (i == index)
                material.EnableKeyword(localKeyword);
            else
                material.DisableKeyword(localKeyword);
        }
    }

    public static void ApplyCustomEnumPropertyKeywords(Material material)
    {
        if (material == null || material.shader == null)
            return;

        var shader = material.shader;
        var propertyCount = shader.GetPropertyCount();
        for (var i = 0; i < propertyCount; i++)
        {
            foreach (var attribute in shader.GetPropertyAttributes(i))
            {
                if (!attribute.StartsWith("EnumShowIfAny(", StringComparison.Ordinal))
                    continue;

                var arguments = attribute["EnumShowIfAny(".Length..^1]
                    .Split(',')
                    .Select(argument => argument.Trim())
                    .ToArray();
                var optionCount = int.Parse(arguments[0], CultureInfo.InvariantCulture);
                var options = arguments.Skip(1).Take(optionCount).ToArray();
                var propertyName = shader.GetPropertyName(i);

                SetEnumKeywords(material, propertyName, options, (int)material.GetFloat(propertyName));
            }
        }
    }
}
