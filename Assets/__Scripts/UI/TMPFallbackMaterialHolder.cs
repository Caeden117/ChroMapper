using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class TMPFallbackMaterialHolder
{
    private static readonly HashSet<Material> pinned = new();

    public static void Pin(Material material)
    {
        if (material == null || !pinned.Add(material))
            return;

        TMP_MaterialManager.AddFallbackMaterialReference(material);
    }

    public static bool IsPinned(Material material)
        => pinned.Contains(material);
}
