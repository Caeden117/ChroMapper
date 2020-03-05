using System.Globalization;
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
        //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
        //This should be thread-wide, but I have this set throughout just in case it isnt.
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        //Debug.Log(Environment.CurrentDirectory);

        if (Settings.ValidateDirectory(null)) {
            Debug.Log("Auto loaded directory");
            FirstBootRequirementsMet();
            return;
        }

        directoryCanvas.SetActive(true);
	}

    public void SetDirectoryButtonPressed() {
        string installation = directoryField.text;
        if (installation == null) {
            directoryErrorText.text = "Invalid directory!";
            return;
        }
        Settings.Instance.BeatSaberInstallation = Settings.ConvertToDirectory(installation);
        if (Settings.ValidateDirectory(ErrorFeedback)) FirstBootRequirementsMet();
    }

    public void ErrorFeedback(string s) {
        directoryErrorText.text = s;
    }

    public void FirstBootRequirementsMet() {
        ColourHistory.Load(); //Load color history from file.
        CustomPlatformsLoader.Instance.Init();
        SceneTransitionManager.Instance.LoadScene(1);
    }

    public void ToggleHelp() {
        helpPanel.SetActive(!helpPanel.activeSelf);
    }

}
