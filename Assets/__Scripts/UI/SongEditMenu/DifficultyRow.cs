using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyRow
{
    public DifficultyRow(Transform obj)
    {
        Obj = obj;
        Name = obj.name;
        Background = obj.GetComponent<Image>();
        Toggle = obj.Find("Button/Toggle").GetComponent<Toggle>();
        Button = obj.Find("Button").GetComponent<Button>();
        NameInput = obj.Find("Button/Name").GetComponent<TMP_InputField>();
        Copy = obj.Find("Copy").GetComponent<Button>();
        CopyImage = obj.Find("Copy").GetComponent<Image>();
        Save = obj.Find("Warning").GetComponent<Button>();
        Revert = obj.Find("Revert").GetComponent<Button>();
        Paste = obj.Find("Paste").GetComponent<Button>();
    }

    public Transform Obj { get; }
    public Image Background { get; }
    public string Name { get; }
    public Toggle Toggle { get; }
    public Button Button { get; }
    public TMP_InputField NameInput { get; }
    public Button Copy { get; }
    public Image CopyImage { get; }
    public Button Save { get; }
    public Button Revert { get; }
    public Button Paste { get; }

    /// <summary>
    ///     Helper to enable or disable a row for editing
    /// </summary>
    /// <param name="val">True if it should be enabled</param>
    public void SetInteractable(bool val) => NameInput.interactable = Button.interactable = Toggle.isOn = val;

    /// <summary>
    ///     Helper to show UI buttons from the current settings
    /// </summary>
    /// <param name="difficultySettings">The current difficulty state</param>
    public void ShowDirtyObjects(DifficultySettings difficultySettings) =>
        ShowDirtyObjects(difficultySettings.IsDirty(), !difficultySettings.IsDirty());

    /// <summary>
    ///     Helper to show UI buttons for performing actions on this difficulty
    ///     we can't show all the buttons at once, but that logic isn't here
    /// </summary>
    /// <param name="show">Should we show the copy/save button</param>
    /// <param name="copy">Should we show the copy button</param>
    public void ShowDirtyObjects(bool show, bool copy)
    {
        Copy.gameObject.SetActive(copy);
        Save.gameObject.SetActive(show);
        Revert.gameObject.SetActive(show);
    }
}
