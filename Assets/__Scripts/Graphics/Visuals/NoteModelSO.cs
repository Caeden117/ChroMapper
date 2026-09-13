using System;
using System.Collections.Generic;
using System.Linq;
using CustomNotes;
using UnityEngine;
using ZLinq;

[CreateAssetMenu(fileName = "NoteModelSO", menuName = "Graphics/Create Note Model")]
public class NoteModelSO : ScriptableObject
{
    public AssetBundle AssetBundle;
    public NoteDescriptor Descriptor;

    [Header("Prefab")] public VisualModelSO NoteLeft;
    public VisualModelSO NoteRight;
    public VisualModelSO NoteDotLeft;
    public VisualModelSO NoteDotRight;
    public VisualModelSO NoteBomb;
    public VisualModelSO BurstSliderLeft;
    public VisualModelSO BurstSliderRight;
    public VisualModelSO BurstSliderHeadLeft;
    public VisualModelSO BurstSliderHeadRight;
    public VisualModelSO BurstSliderHeadDotLeft;
    public VisualModelSO BurstSliderHeadDotRight;
    private readonly List<GameObject> generatedGameObjects = new();
    private VisualModelSO[] ownedVisualModels;

    public static bool TryCreate(
        GameObject prefab,
        string bundleName,
        string modelName,
        out NoteModelSO model,
        out string failureReason)
    {
        model = null;
        if (prefab == null)
        {
            failureReason = "the bundle does not contain assets/_customnote.prefab";
            return false;
        }

        var descriptor = prefab.GetComponent<NoteDescriptor>();
        if (descriptor == null)
        {
            failureReason = "the custom note prefab is missing NoteDescriptor";
            return false;
        }

        var so = CreateInstance<NoteModelSO>();
        so.Descriptor = descriptor;

        so.name = modelName ?? so.Descriptor.NoteName;
        if (string.IsNullOrEmpty(so.name)) so.name = bundleName;

        var noteLeft = prefab.transform.Find("NoteLeft");
        var noteRight = prefab.transform.Find("NoteRight");
        if (noteLeft == null || noteRight == null)
        {
            Destroy(so);
            failureReason = "the custom note prefab must contain NoteLeft and NoteRight";
            return false;
        }

        foreach (var comp in prefab.GetComponentsInChildren<Renderer>())
        foreach (var mat in comp.sharedMaterials)
        {
            if (!Settings.Instance.ShaderCompatibility) continue;
            if (mat == null) continue;
            if (mat.shader != null && mat.shader.isSupported) continue;

            var sourceKeywords = mat.shaderKeywords ?? Array.Empty<string>();
            mat.shader = Shader.Find("ChroMapper/Object/Note");
            mat.shaderKeywords = sourceKeywords
                .Select(keyword => keyword switch
                {
                    "ENABLE_CUTOUT" => "CUTOUT",
                    "ENABLE_PLANE_CUT" => "PLANE_CUT",
                    "ENABLE_RIM_DIM" => "RIM_DIM",
                    "ACES_TONE_MAPPING" => "ACES_TONE_MAPPING",
                    "ENABLE_HEIGHT_FOG" => "HEIGHT_FOG",
                    _ => keyword,
                })
                .Where(keyword => keyword != null)
                .Concat(new[] { "RIM_DIM", "CUTOUT", "_FOGTYPE_LERP", "HEIGHT_FOG" })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(keyword => keyword, StringComparer.Ordinal)
                .ToArray();
        }

        so.NoteLeft = VisualModelSO.Create(noteLeft.gameObject, so.name);
        so.NoteRight = VisualModelSO.Create(noteRight.gameObject, so.name);
        var noteDotLeftTransform = prefab.transform.Find("NoteDotLeft");
        var noteDotRightTransform = prefab.transform.Find("NoteDotRight");
        so.NoteDotLeft = noteDotLeftTransform != null
            ? VisualModelSO.Create(noteDotLeftTransform.gameObject, so.name)
            : so.NoteLeft;
        so.NoteDotRight = noteDotRightTransform != null
            ? VisualModelSO.Create(noteDotRightTransform.gameObject, so.name)
            : so.NoteRight;
        var bomb = prefab.transform.Find("NoteBomb");
        if (bomb != null) so.NoteBomb = VisualModelSO.Create(bomb.gameObject, so.name);

        so.BurstSliderLeft = VisualModelSO.Create(
            GetBurstSlider(prefab, so.NoteDotLeft.Prefab, "BurstSliderLeft"),
            so.name);
        so.BurstSliderRight = VisualModelSO.Create(
            GetBurstSlider(prefab, so.NoteDotRight.Prefab, "BurstSliderRight"),
            so.name);

        var burstSliderHeadLeft = prefab.transform.Find("BurstSliderHeadLeft");
        var burstSliderHeadRight = prefab.transform.Find("BurstSliderHeadRight");
        so.BurstSliderHeadLeft = burstSliderHeadLeft != null
            ? VisualModelSO.Create(burstSliderHeadLeft.gameObject, so.name)
            : so.NoteLeft;
        so.BurstSliderHeadRight = burstSliderHeadRight != null
            ? VisualModelSO.Create(burstSliderHeadRight.gameObject, so.name)
            : so.NoteRight;

        var burstSliderHeadDotLeft = prefab.transform.Find("BurstSliderHeadDotLeft");
        var burstSliderHeadDotRight = prefab.transform.Find("BurstSliderHeadDotRight");
        so.BurstSliderHeadDotLeft =
            burstSliderHeadDotLeft != null ? VisualModelSO.Create(burstSliderHeadDotLeft.gameObject, so.name)
            : burstSliderHeadLeft != null  ? VisualModelSO.Create(burstSliderHeadLeft.gameObject, so.name)
                                             : so.NoteDotLeft;
        so.BurstSliderHeadDotRight =
            burstSliderHeadDotRight != null ? VisualModelSO.Create(burstSliderHeadDotRight.gameObject, so.name)
            : burstSliderHeadRight != null  ? VisualModelSO.Create(burstSliderHeadRight.gameObject, so.name)
                                              : so.NoteDotRight;

        ResetTransform(so.NoteLeft.Prefab);
        ResetTransform(so.NoteRight.Prefab);
        ResetTransform(so.NoteDotLeft.Prefab);
        ResetTransform(so.NoteDotRight.Prefab);
        if (so.NoteBomb != null) ResetTransform(so.NoteBomb.Prefab);
        ResetTransform(so.BurstSliderHeadLeft.Prefab);
        ResetTransform(so.BurstSliderHeadRight.Prefab);
        ResetTransform(so.BurstSliderHeadDotLeft.Prefab);
        ResetTransform(so.BurstSliderHeadDotRight.Prefab);

        ResetGameObject(so.NoteLeft);
        ResetGameObject(so.NoteRight);
        ResetGameObject(so.NoteDotLeft);
        ResetGameObject(so.NoteDotRight);
        if (so.NoteBomb != null) ResetGameObject(so.NoteBomb);
        ResetGameObject(so.BurstSliderLeft);
        ResetGameObject(so.BurstSliderRight);
        ResetGameObject(so.BurstSliderHeadLeft);
        ResetGameObject(so.BurstSliderHeadRight);
        ResetGameObject(so.BurstSliderHeadDotLeft);
        ResetGameObject(so.BurstSliderHeadDotRight);

        so.ownedVisualModels = new[]
            {
                so.NoteLeft,
                so.NoteRight,
                so.NoteDotLeft,
                so.NoteDotRight,
                so.NoteBomb,
                so.BurstSliderLeft,
                so.BurstSliderRight,
                so.BurstSliderHeadLeft,
                so.BurstSliderHeadRight,
                so.BurstSliderHeadDotLeft,
                so.BurstSliderHeadDotRight
            }
            .Where(visual => visual != null)
            .Distinct()
            .ToArray();
        model = so;
        failureReason = null;
        return true;

        void ResetGameObject(VisualModelSO vm)
        {
            if (vm == null || vm.Prefab == null) return;
            vm.DisableAux = so.Descriptor.DisableBaseNoteArrows;
            vm.Prefab.SetLayerRecursively(LayerMask.NameToLayer("Beatmap Object"));
        }

        void ResetTransform(GameObject go)
        {
            if (go == null) return;
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * 0.4f;
        }

        GameObject GetBurstSlider(GameObject p, GameObject dP, string prefabName)
        {
            var t = p.transform.Find(prefabName);
            if (t != null)
            {
                ResetTransform(t.gameObject);
                return t.gameObject;
            }

            var burstSlider = new GameObject(prefabName);
            DontDestroyOnLoad(burstSlider);
            so.generatedGameObjects.Add(burstSlider);

            var burstSliderDot = Instantiate(dP, burstSlider.transform, true);
            burstSliderDot.transform.localPosition = Vector3.zero;

            var sliderScale = burstSliderDot.transform.localScale;
            var scale = sliderScale;
            scale.y = sliderScale.y / 4f;
            burstSliderDot.transform.localScale = scale;

            burstSlider.SetActive(false);
            ResetTransform(burstSlider);
            return burstSlider;
        }
    }

    public void DisposeRuntimeModel()
    {
        foreach (var generatedGameObject in generatedGameObjects
            .AsValueEnumerable()
            .Where(generatedGameObject => generatedGameObject != null))
            Destroy(generatedGameObject);

        if (ownedVisualModels != null)
        {
            foreach (var visualModel in ownedVisualModels)
            {
                if (visualModel != null) Destroy(visualModel);
            }
        }

        Destroy(this);
    }
}
