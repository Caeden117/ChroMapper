using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Win32;
using System.IO;

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

        string posInstallationDirectory = guessBeatSaberInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory))
        {
            directoryField.text = posInstallationDirectory;
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

    private string guessBeatSaberInstallationDirectory()
    {
        string posInstallationDirectory = guessSteamInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory))
        {
            return posInstallationDirectory;
        }
        posInstallationDirectory = guessOculusInstallationDirectory();
        return posInstallationDirectory;
    }

    private string guessSteamInstallationDirectory()
    {
        // The Steam App ID seems to be static e.g. https://store.steampowered.com/app/620980/Beat_Saber/
        string steamRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 620980";
        string registryValue = "InstallLocation";
        try
        {
            return (string) Registry.GetValue(steamRegistryKey, registryValue, "");
        } catch(System.Exception e)
        {
            Debug.Log("Error reading Steam registry key" + e);
            return "";
        }
    }

    private string guessOculusInstallationDirectory()
    {
        string oculusRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Oculus VR, LLC\\Oculus\\Config";
        string registryValue = "InitialAppLibrary";
        try
        {
            string oculusBaseDirectory = (string)Registry.GetValue(oculusRegistryKey, registryValue, "");
            if (string.IsNullOrEmpty(oculusBaseDirectory))
            {
                return "";
            }

            string installPath = Path.Combine(oculusBaseDirectory, "Software", "hyperbolic-magnetism-beat-saber");
            if (Directory.Exists(installPath))
            {
                return installPath;
            } else
            {
                return "";
            }
        } catch (System.Exception e)
        {
            Debug.Log("Error guessing Oculus Beat Saber Directory" + e);
            return "";
        }
    }
}
