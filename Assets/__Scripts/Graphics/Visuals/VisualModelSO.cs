using UnityEngine;

[CreateAssetMenu(fileName = "VisualModelSO", menuName = "Graphics/Create Visual Model")]
public class VisualModelSO : ScriptableObject
{
    public string Name;
    public GameObject Prefab;
    public Mesh Collider;
    public bool DisableAux; // this refer to arrow/dot, can be for other entity
    public bool AlternateShader; // for use object or shader have different interaction

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!string.IsNullOrEmpty(name)) Name = name;
        if (Collider == null && Prefab != null) Collider = Prefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
    }

    public static VisualModelSO Create(GameObject prefab)
    {
        var so = CreateInstance<VisualModelSO>();
        so.Prefab = prefab;
        so.name = prefab.name;
        so.Name = so.name;
        return so;
    }

    public static VisualModelSO Create(GameObject prefab, string prefix)
    {
        var so = CreateInstance<VisualModelSO>();
        so.Prefab = prefab;
        so.name = $"{prefix}_{prefab.name}";
        so.Name = so.name;
        return so;
    }
}
