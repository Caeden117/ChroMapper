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


    private T selectingData;
    public T DisplayingData => DisplayingSelectedObject ? selectingData : ObjectData;
    protected bool DisplayingSelectedObject;

    protected const string MixedMark = "-";
    [SerializeField] protected Sprite CheckMark;
    [SerializeField] protected Sprite MixedIcon;

    protected void Awake()
    {
        InitBindings();
        foreach (var toggle in Toggles)
        {
            toggle.onValueChanged.AddListener(_ => toggle.GetComponent<LightV3ToggleCheckmark>().SetSprite(CheckMark));
        }
    }
    // Start is called before the first frame update
    protected void Start()
    {
        Dump(ObjectData);
        SelectionController.SelectionChangedEvent += OnSelectionChanged;
    }

    protected void OnDestroy()
    {
        SelectionController.SelectionChangedEvent -= OnSelectionChanged;
    }

    protected abstract void InitBindings();

    private void OnSelectionChanged()
    {
        DisplayingSelectedObject = false;
        foreach (var toggle in Toggles) toggle.GetComponent<LightV3ToggleCheckmark>().SetSprite(CheckMark);
        if (SelectionController.SelectedObjects.Count > 0)
        {
            DumpGroup(SelectionController.SelectedObjects.OfType<T>());
            /*
            var obj = SelectionController.SelectedObjects.First();
            if (obj is T o)
            {
                selectingData = o;
                Dump(o);
            }
            */
        }
        else if (SelectionController.SelectedObjects.Count == 0)
        {
            Dump(ObjectData);
        }
    }

    public virtual void DumpGroup(IEnumerable<T> objects)
    {
        if (!objects.Any()) return;
        Dump(objects.Last()); // dump the last to get the correct idx

        selectingData = objects.First();
        DisplayingSelectedObject = true;
        for (int i = 0; i < InputFields.Length; ++i)
        {
            string prev = null;
            foreach (var obj in objects)
            {
                InputFields[i].SetTextWithoutNotify(InputDumpFn[i](obj));
                if (prev == null)
                    prev = InputFields[i].text;
                else if (prev != InputFields[i].text)
                {
                    InputFields[i].SetTextWithoutNotify(MixedMark);
                    break;
                }
            }
        }

        for (int i = 0; i < Dropdowns.Length; ++i)
        {
            int? prev = null;
            foreach (var obj in objects)
            {
                Dropdowns[i].SetValueWithoutNotify(DropdownDumpFn[i](obj));
                if (prev == null)
                    prev = Dropdowns[i].value;
                else if (prev != Dropdowns[i].value)
                {
                    Dropdowns[i].SetValueWithoutNotify(Dropdowns[i].options.Count - 1);
                    break;
                }
            }
        }

        for (int i = 0; i < Texts.Length; ++i)
        {
            foreach (var obj in objects)
                Texts[i].text = TextsDumpFn[i](obj);
        }

        for (int i = 0; i < Toggles.Length; ++i)
        {
            bool? prev = null;
            // maybe we need a mixed state for toggle?
            foreach (var obj in objects)
            {
                Toggles[i].SetIsOnWithoutNotify(ToggleDumpFn[i](obj));
                if (prev == null)
                    prev = Toggles[i].isOn;
                else if (prev != Toggles[i].isOn)
                {
                    Toggles[i].SetIsOnWithoutNotify(true);
                    Toggles[i].GetComponent<LightV3ToggleCheckmark>().SetSprite(MixedIcon);
                    break;
                }
            }
        }
    }

    public virtual void Dump(T obj)
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

    public void Load(T obj)
    {
        if (obj == null) obj = ObjectData;
        // dropdown needs to be loaded first
        for (int i = 0; i < Dropdowns.Length; ++i)
        {
            if (Dropdowns[i].value != Dropdowns[i].options.Count - 1)
                DropdownLoadFn[i](obj, Dropdowns[i].value);
        }

        for (int i = 0; i < InputFields.Length; ++i)
        {
            if (InputFields[i].text != MixedMark)
                InputLoadFn[i](obj, InputFields[i].text);
        }


        for (int i = 0; i < Toggles.Length; ++i)
        {
            if (Toggles[i].GetComponent<LightV3ToggleCheckmark>().GetSprite() != MixedIcon)
                ToggleLoadFn[i](obj, Toggles[i].isOn);
        }
    }

}
