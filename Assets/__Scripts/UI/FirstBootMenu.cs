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

    private static string oculusStoreBeatSaberFolderName = "hyperbolic-magnetism-beat-saber";

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
        if (Settings.ValidateDirectory(ErrorFeedback))
        {
            FirstBootRequirementsMet();
        }
    }

    public void ErrorFeedback(string s)
    {
        PersistentUI.Instance.ShowDialogBox($"{s}\n\nWould you like ChroMapper to generate the missing folders?",
            HandleGenerateMissingFolders, PersistentUI.DialogBoxPresetType.YesNo);
    }

    private void HandleGenerateMissingFolders(int res)
    {
        if (res == 0)
        {
            Debug.Log("Creating directories that do not exist...");
            if (!Directory.Exists(Settings.Instance.BeatSaberInstallation))
            {
                Directory.CreateDirectory(Settings.Instance.BeatSaberInstallation);
            }
            if (!Directory.Exists(Settings.Instance.CustomSongsFolder))
            {
                Directory.CreateDirectory(Settings.Instance.CustomSongsFolder);
            }
            if (!Directory.Exists(Settings.Instance.CustomWIPSongsFolder))
            {
                Directory.CreateDirectory(Settings.Instance.CustomWIPSongsFolder);
            }
            FirstBootRequirementsMet();
        }
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
        string oculusRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Oculus VR, LLC\\Oculus";
        try
        {
            string registryValue = "InitialAppLibrary";
            string software = "Software";
            
            // older Oculus installations seem to have created the InitialAppLibrary value
            string installPath = tryRegistryWithPath(oculusRegistryKey + "\\Config",  registryValue, software, oculusStoreBeatSaberFolderName, "");

            if (!string.IsNullOrEmpty(installPath))
            {
                return installPath;
            }

            // the default library for newer installations seem to be below the base directory in "Software\\Software" folder.
            registryValue = "Base";
            installPath = tryRegistryWithPath(oculusRegistryKey, registryValue, software, software, oculusStoreBeatSaberFolderName);

            if (Directory.Exists(installPath))
            {
                return installPath;
            } else
            {
                return tryOculusStoreLibraryLocations();
            }
        } catch (System.Exception e)
        {
            Debug.Log("Error guessing Oculus Beat Saber Directory" + e);
            return "";
        }
    }

    private string tryRegistryWithPath(string registryKey, string registryValue, string path1, string path2, string path3)
    {
        string oculusBaseDirectory = (string) Registry.GetValue(registryKey, registryValue, "");
        if (string.IsNullOrEmpty(oculusBaseDirectory))
        {
            return "";
        }
        string installPath = "";
        if (string.IsNullOrEmpty(path3))
        {
            installPath = Path.Combine(oculusBaseDirectory, path1, path2);
        } else
        {
            installPath = Path.Combine(oculusBaseDirectory, path1, path2, path3);
        }
        if (Directory.Exists(installPath))
        {
            return installPath;
        }
        else
        {
            return "";
        }
    }

    private string tryOculusStoreLibraryLocations()
    {
        RegistryKey libraryKey = Registry.CurrentUser.OpenSubKey("Software\\Oculus VR, LLC\\Oculus\\Libraries");
        if (libraryKey == null)
        {
            return "";
        }
        string[] subKeys = libraryKey.GetSubKeyNames();
        foreach(string subKeyName in subKeys)
        {
            object originalPath = libraryKey.OpenSubKey(subKeyName).GetValue("OriginalPath");
            if (originalPath != null && string.IsNullOrEmpty((string)originalPath))
            {
                continue;
            }
            string installPath = Path.Combine((string)originalPath, "Software", oculusStoreBeatSaberFolderName);
            if (Directory.Exists(installPath))
            {
                return installPath;
            }
        }
        return "";
    }

    
}
