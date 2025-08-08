using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Info;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class CreateNewSong : MonoBehaviour
{
    [SerializeField] private SongList list;

    private DialogBox createNewSongDialogueBox;
    private TextComponent textComponent;
    private TextBoxComponent folderTextBoxComponent;
    private DropdownComponent versionDropdownComponent;

    private void InitialiseDialogueBox()
    {
        createNewSongDialogueBox = PersistentUI.Instance.CreateNewDialogBox()
            .WithNoTitle()
            .DontDestroyOnClose();

        textComponent = createNewSongDialogueBox
            .AddComponent<TextComponent>()
            .WithInitialValue("SongSelectMenu", "newmap.dialog");

        folderTextBoxComponent = createNewSongDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("SongSelectMenu", "foldername")
            .WithInitialValue("");
        
        var dropdownOptions = new List<string>
        {
            LocalizationSettings.StringDatabase.GetLocalizedString("SongSelectMenu", "newmap.v4format"),
            LocalizationSettings.StringDatabase.GetLocalizedString("SongSelectMenu", "newmap.v2format")
        };
        
        versionDropdownComponent = createNewSongDialogueBox
            .AddComponent<DropdownComponent>()
            .WithLabel("SongSelectMenu", "format.version")
            .WithOptions(dropdownOptions)
            .WithInitialValue(0);
        
        // This dialogue box remains open otherwise on error. We'll only close on success or cancel.
        createNewSongDialogueBox.OnQuickSubmit(HandleNewSong, closeOnQuickSubmit: false);

        // Cancel button
        createNewSongDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");

        // Submit/OK button
        createNewSongDialogueBox.AddFooterButton(null, "PersistentUI", "ok")
            .OnClick(HandleNewSong);
    }

    public void CreateSong()
    {
        // The user will be selecting and editing a way more often than creating a map
        // So only create the dialogue box once we need it
        if (createNewSongDialogueBox == null) InitialiseDialogueBox();

        textComponent.Value = LocalizationSettings.StringDatabase.GetLocalizedString("SongSelectMenu", "newmap.dialog");
        folderTextBoxComponent.Value = "";
        createNewSongDialogueBox.Open();
    }

    private void HandleNewSong()
    {
        var folderName = folderTextBoxComponent.Value;

        if (string.IsNullOrWhiteSpace(folderName)) return;

        var isV4 = versionDropdownComponent.Value == 0;
        var song = new BaseInfo { SongName = folderName, Version = isV4 ? V4Info.Version : V2Info.Version };

        if (string.IsNullOrWhiteSpace(song.CleanSongName))
        {
            textComponent.Value = LocalizationSettings.StringDatabase.GetLocalizedString("SongSelectMenu", "newmap.dialog.invalid");
            return;
        }

        var songDirectory = Path.Combine(list.SelectedFolderPath, song.CleanSongName);
        if (list.SongInfos.Any(x => Path.GetFullPath(x.Directory).Equals(
                Path.GetFullPath(Path.Combine(songDirectory)),
                StringComparison.CurrentCultureIgnoreCase)))
        {
            textComponent.Value = LocalizationSettings.StringDatabase.GetLocalizedString("SongSelectMenu", "newmap.dialog.duplicate");
            return;
        }

        song.Directory = songDirectory;

        createNewSongDialogueBox.Close();

        var standardSet = new InfoDifficultySet { Characteristic = "Standard" };
        song.DifficultySets.Add(standardSet);
        BeatSaberSongContainer.Instance.SelectSongForEditing(song);
        PersistentUI.Instance.ShowDialogBox("SongSelectMenu", "newmap.message", null,
            PersistentUI.DialogBoxPresetType.Ok);
    }
}
