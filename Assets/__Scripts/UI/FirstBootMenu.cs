using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FirstBootMenu : MonoBehaviour {

    [SerializeField]
    GameObject directoryCanvas;

    [SerializeField]
    InputField directoryField;

    [SerializeField]
    Button directoryButton;

    [SerializeField]
    TMPro.TMP_Text directoryErrorText;

    [SerializeField]
    GameObject helpPanel;

	// Use this for initialization
	void Start() {

        //Debug.Log(Environment.CurrentDirectory);

        if (Settings.LoadCustomSongsFolderDirectoryFromPrefs(ErrorFeedback)) {
            Debug.Log("Auto loaded directory");
            FirstBootRequirementsMet();
            return;
        }

        if (Settings.BeatSaberInstallation == "" && PlayerPrefs.HasKey("install")) {
        }

        directoryCanvas.SetActive(true);

	}

    public void SetDirectoryButtonPressed() {
        string installation = directoryField.text;
        if (installation == null) {
            directoryErrorText.text = "Invalid directory!";
            return;
        }
        Settings.BeatSaberInstallation = installation;
        if (Settings.ValidateDirectory(ErrorFeedback)) {
            PlayerPrefs.SetString("install", installation);
            FirstBootRequirementsMet();
        }
    }

    public void ErrorFeedback(string s) {
        directoryErrorText.text = s;
    }

    public void FirstBootRequirementsMet() {
        ColourHistory.Load(); //Load color history from file.
        SceneTransitionManager.Instance.LoadScene(1);
    }

    public void ToggleHelp() {
        helpPanel.SetActive(!helpPanel.activeSelf);
    }

}
