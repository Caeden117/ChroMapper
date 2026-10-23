using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "VisualRepositorySO", menuName = "Graphics/Create Visual Repository")]
public class VisualRepositorySO : ScriptableObject
{
    [SerializeField] private List<NoteModelSO> defaultNoteModels;
    [SerializeField] private List<VisualModelSO> defaultModels;
    private readonly HashSet<string> customNoteModels = new();
    public Dictionary<string, VisualModelSO> ModelsByName;

    public Dictionary<string, NoteModelSO> NoteModelsByName;

    public IEnumerable<string> NoteModelNames => NoteModelsByName.Keys.Concat(customNoteModels).Distinct();

    public void OnEnable()
    {
        NoteModelsByName = defaultNoteModels.ToDictionary(x => x.name, x => x);
        ModelsByName = defaultModels.ToDictionary(x => x.name, x => x);
    }

    public void OnDisable()
    {
        NoteModelsByName.Clear();
        ModelsByName.Clear();
        customNoteModels.Clear();
    }

    public event Action NoteModelChanged;
    public event Action NoteModelListChanged;

    public void SetAvailableCustomNoteModels(IEnumerable<string> names)
    {
        customNoteModels.Clear();
        customNoteModels.UnionWith(names.Where(n => !string.IsNullOrEmpty(n)));
        NoteModelListChanged?.Invoke();
    }

    public bool TryGetNoteModel(string n, out NoteModelSO model) => NoteModelsByName.TryGetValue(n, out model);

    public NoteModelSO RemoveNoteModel(string n)
    {
        if (!NoteModelsByName.Remove(n, out var model)) return null;
        NoteModelChanged?.Invoke();
        return model;
    }

    public void Add(NoteModelSO model)
    {
        if (model == null) return;
        Add(model.name, model);
    }

    private void Add(string n, NoteModelSO model)
    {
        if (model == null) return;

        if (model.NoteLeft == null) model.NoteLeft = defaultNoteModels[0].NoteLeft;
        FillWithFallback(model.NoteLeft, defaultNoteModels[0].NoteLeft);
        if (model.NoteRight == null) model.NoteRight = defaultNoteModels[0].NoteRight;
        FillWithFallback(model.NoteRight, defaultNoteModels[0].NoteRight);
        if (model.NoteDotLeft == null) model.NoteDotLeft = defaultNoteModels[0].NoteDotLeft;
        FillWithFallback(model.NoteDotLeft, defaultNoteModels[0].NoteDotLeft);
        if (model.NoteDotRight == null) model.NoteDotRight = defaultNoteModels[0].NoteDotRight;
        FillWithFallback(model.NoteDotRight, defaultNoteModels[0].NoteDotRight);
        if (model.NoteBomb == null) model.NoteBomb = defaultNoteModels[0].NoteBomb;
        FillWithFallback(model.NoteBomb, defaultNoteModels[0].NoteBomb);
        if (model.BurstSliderLeft == null) model.BurstSliderLeft = defaultNoteModels[0].BurstSliderLeft;
        FillWithFallback(model.BurstSliderLeft, defaultNoteModels[0].BurstSliderLeft);
        if (model.BurstSliderRight == null) model.BurstSliderRight = defaultNoteModels[0].BurstSliderRight;
        FillWithFallback(model.BurstSliderRight, defaultNoteModels[0].BurstSliderRight);
        if (model.BurstSliderHeadLeft == null) model.BurstSliderHeadLeft = defaultNoteModels[0].BurstSliderHeadLeft;
        FillWithFallback(model.BurstSliderHeadLeft, defaultNoteModels[0].BurstSliderHeadLeft);
        if (model.BurstSliderHeadRight == null) model.BurstSliderHeadRight = defaultNoteModels[0].BurstSliderHeadRight;
        FillWithFallback(model.BurstSliderHeadRight, defaultNoteModels[0].BurstSliderHeadRight);
        if (model.BurstSliderHeadDotLeft == null)
            model.BurstSliderHeadDotLeft = defaultNoteModels[0].BurstSliderHeadDotLeft;
        FillWithFallback(model.BurstSliderHeadDotLeft, defaultNoteModels[0].BurstSliderHeadDotLeft);
        if (model.BurstSliderHeadDotRight == null)
            model.BurstSliderHeadDotRight = defaultNoteModels[0].BurstSliderHeadDotRight;
        FillWithFallback(model.BurstSliderHeadDotRight, defaultNoteModels[0].BurstSliderHeadDotRight);

        NoteModelsByName[n] = model;
        NoteModelChanged?.Invoke();
    }

    public void Add(GameObject go, string prefabName)
    {
        var model = VisualModelSO.Create(go);
        var n = prefabName;
        model.name = n;
        ModelsByName.Add(n, model);
    }

    public void AddWithFallback(GameObject go, string prefabName)
    {
        var model = VisualModelSO.Create(go);
        var n = prefabName;
        model.name = n;
        FillWithFallback(model, defaultModels[0]);
        ModelsByName.Add(n, model);
    }

    public void Add(VisualModelSO model) => ModelsByName.Add(model.name, model);

    private static void FillWithFallback(VisualModelSO vm, VisualModelSO fallback)
    {
        if (vm.Prefab == null) vm.Prefab = fallback.Prefab;
        if (vm.Collider == null) vm.Collider = fallback.Collider;
    }
}
