using System.Collections.Generic;
using TMPro;
using UnityEngine;

// TMP creates fallback/multi-atlas materials at runtime with HideAndDontSave and tracks them only
// in managed dictionaries inside TMP_MaterialManager. Managed references do not count for Unity's
// asset garbage collection (Resources.UnloadUnusedAssets, which also runs automatically on scene
// loads), so once the last TMP_SubMeshUI drops its reference the next collection destroys the
// material while TMP's cache keeps returning the corpse to every later mesh rebuild -
// materialForRendering then returns null and the text draws nothing permanently (deployed log:
// mat=null with live vertex counts on song-list CJK rows). Holding each material in a serialized
// field on a persistent object keeps a Unity-side reference so it cannot be collected, and the
// permanent counted ref keeps TMP's own refcount cleanup from destroying it either.
public class TMPFallbackMaterialHolder : MonoBehaviour
{
    private static TMPFallbackMaterialHolder instance;

    [SerializeField] private List<Material> pinned = new();

    public static void Pin(Material material)
    {
        if (material == null) return;

        if (instance == null)
        {
            var go = new GameObject(nameof(TMPFallbackMaterialHolder)) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<TMPFallbackMaterialHolder>();
        }

        if (instance.pinned.Contains(material)) return;
        instance.pinned.Add(material);
        TMP_MaterialManager.AddFallbackMaterialReference(material);
    }

    public static bool IsPinned(Material material) =>
        instance != null && instance.pinned.Contains(material);
}
