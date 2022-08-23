using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Linq;
using UnityEngine.UI;

public abstract class MetaLightV3Binder<T> : MonoBehaviour
{
    [SerializeField] protected TMP_InputField[] InputFields;
    protected List<Func<T, string>> InputDumpFn = new List<Func<T, string>>();
    protected List<Action<T, string>> InputLoadFn = new List<Action<T, string>>();

    [SerializeField] protected TMP_Dropdown[] Dropdowns;
    protected List<Func<T, int>> DropdownDumpFn = new List<Func<T, int>>();
    protected List<Action<T, int>> DropdownLoadFn = new List<Action<T, int>>();

    [SerializeField] protected TMP_Text[] Texts;
    protected List<Func<T, string>> TextsDumpFn = new List<Func<T, string>>();

    [SerializeField] protected Toggle[] Toggles;
    protected List<Func<T, bool>> ToggleDumpFn = new List<Func<T, bool>>();
    protected List<Action<T, bool>> ToggleLoadFn = new List<Action<T, bool>>();

    public T ObjectData;
    protected void Awake()
    {
        InitBindings();
    }
    // Start is called before the first frame update
    protected void Start()
    {
        SelectionController.SelectionChangedEvent += OnSelectionChanged;
    }

    protected void OnDestroy()
    {
        SelectionController.SelectionChangedEvent -= OnSelectionChanged;
    }

    protected abstract void InitBindings();

    private void OnSelectionChanged()
    {
        if (SelectionController.SelectedObjects.Count == 1)
        {
            var obj = SelectionController.SelectedObjects.First();
            if (obj is T o)
            {
                Dump(o);
            }
        }
    }

    protected void Dump(T obj)
    {
        for (int i = 0; i < InputFields.Length; ++i)
        {
            InputFields[i].SetTextWithoutNotify(InputDumpFn[i](obj));
        }

        for (int i = 0; i < Dropdowns.Length; ++i)
        {
            Dropdowns[i].SetValueWithoutNotify(DropdownDumpFn[i](obj));
        }

        for (int i = 0; i < Texts.Length; ++i)
        {
            Texts[i].text = TextsDumpFn[i](obj);
        }

        for (int i = 0; i < Toggles.Length; ++i)
        {
            Toggles[i].SetIsOnWithoutNotify(ToggleDumpFn[i](obj));
        }
    }
}
