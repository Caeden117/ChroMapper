using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.Win32;
using System.IO;
using UnityEngine.Localization.Settings;
using SFB;
using TMPro;

public class FirstBootMenu : MonoBehaviour {

    [SerializeField]
    GameObject directoryCanvas;

    [SerializeField]
    TMP_InputField directoryField;

    [SerializeField]
    Button directoryButton;

    [SerializeField]
    TMP_Text directoryErrorText;

    [SerializeField]
    TMP_Dropdown graphicsDropdown;

    [SerializeField]
    GameObject helpPanel;

    [SerializeField] private InputBoxFileValidator validation;

    private static string oculusStoreBeatSaberFolderName = "hyperbolic-magnetism-beat-saber";

    private Regex appManifestRegex = new Regex(@"\s""installdir""\s+""(.+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private Regex libraryRegex = new Regex(@"\s""\d""\s+""(.+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

        if (SystemInfo.graphicsMemorySize <= 1024)
        {
            graphicsDropdown.value = 2;
        }
        else if (SystemInfo.graphicsMemorySize <= 2048)
        {
            graphicsDropdown.value = 1;
        }

        string posInstallationDirectory = guessBeatSaberInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory))
        {
            directoryField.text = posInstallationDirectory;
            ValidateQuiet();
        }

        directoryCanvas.SetActive(true);
    }

    private void SetFromTextbox()
    {
        string installation = directoryField.text;
        if (installation == null)
        {
            return;
        }
        Settings.Instance.BeatSaberInstallation = Path.GetFullPath(installation);
    }

    public void SetDirectoryButtonPressed()
    {
        SetFromTextbox();
        if (Settings.ValidateDirectory(ErrorFeedbackWithContinue))
        {
            SetDefaults();
            FirstBootRequirementsMet();
            return;
        }
        validation.SetValidationState(true, false);
    }

    public void SetDefaults()
    {
        switch (graphicsDropdown.value)
        {
            // Performance
            case 2:
                Settings.Instance.Waveform = 0;
                Settings.Instance.ObstacleOutlines = false;
                Settings.Instance.PostProcessingIntensity = 0;
                Settings.Instance.ChromaticAberration = false;
                Settings.Instance.SimpleBlocks = true;
                Settings.Instance.Reflections = false;
                Settings.Instance.HighQualityBloom = false;
                break;
            // Balanced
            case 1:
                Settings.Instance.ChromaticAberration = false;
                Settings.Instance.SimpleBlocks = true;
                Settings.Instance.Reflections = false;
                break;
            // Quality
            case 0:
                Settings.Instance.Offset_Spawning = 8;
                Settings.Instance.Offset_Despawning = 2;
                Settings.Instance.ChunkDistance = 10;
                break;
        }
    }

    public void ErrorFeedback(string s)
    {
        DoErrorFeedback(s, false);
    }

    public void ErrorFeedbackWithContinue(string s)
    {
        DoErrorFeedback(s, true);
    }

    private void DoErrorFeedback(string s, bool continueAfter)
    {
        var arg = LocalizationSettings.StringDatabase.GetLocalizedString("FirstBoot", s);
        PersistentUI.Instance.ShowDialogBox("FirstBoot", "validate.dialog",
            continueAfter ? (Action<int>)HandleGenerateMissingFoldersWithContinue : HandleGenerateMissingFolders, PersistentUI.DialogBoxPresetType.YesNo, new object[] { arg });
    }

    internal void HandleGenerateMissingFolders(int res)
    {
        HandleGenerateMissingFolders(res, false);
    }

    internal void HandleGenerateMissingFoldersWithContinue(int res)
    {
        HandleGenerateMissingFolders(res, true);
    }

    internal void HandleGenerateMissingFolders(int res, bool continueAfter)
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
            SetDefaults();
            FirstBootRequirementsMet();
        }
    }

    public void FirstBootRequirementsMet() {
        ColourHistory.Load(); //Load color history from file.
        CustomPlatformsLoader.Instance.Init();
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
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
            string installDirectory = (string) Registry.GetValue(steamRegistryKey, registryValue, "");
            if (!string.IsNullOrEmpty(installDirectory))
            {
                return installDirectory;
            }
            return guessSteamInstallationDirectoryComplex();
        } catch(System.Exception e)
        {
            Debug.Log("Error reading Steam registry key" + e);
            return "";
        }
    }

    private string guessSteamInstallationDirectoryComplex()
    {
        // The above registry key only exists if you've installed Beat Saber since last installing windows
        // if you copy the game files or reinstall windows then the registry key will be missing even though
        // the game launches just fine
        string steamRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam";
        string registryValue = "InstallPath";

        List<string> libraryFolders = new List<string>();

        string mainSteamFolder = (string) Registry.GetValue(steamRegistryKey, registryValue, "");
        libraryFolders.Add(mainSteamFolder);

        string libraryFolderFilename = mainSteamFolder + "\\steamapps\\libraryfolders.vdf";
        if (File.Exists(libraryFolderFilename))
        {
            using (StreamReader reader = new StreamReader(libraryFolderFilename))
            {
                string text = reader.ReadToEnd();
                MatchCollection matches = libraryRegex.Matches(text);

                foreach (Match match in matches)
                {
                    if (Directory.Exists(match.Groups[1].Value))
                    {
                        libraryFolders.Add(match.Groups[1].Value);
                    }
                }
            }
        }

        foreach (string libraryFolder in libraryFolders)
        {
            string fileName = libraryFolder + "\\steamapps\\appmanifest_620980.acf";
            if (File.Exists(fileName))
            {
                using (StreamReader reader = new StreamReader(fileName))
                {
                    string text = reader.ReadToEnd();

                    GroupCollection installDirMatch = appManifestRegex.Matches(text)[0].Groups;
                    string installDir = libraryFolder + "\\steamapps\\common\\" + installDirMatch[1].Value;

                    if (Directory.Exists(installDir))
                    {
                        return installDir;
                    }
                }
            }
        }
        return "";
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
        string installPath;
        if (string.IsNullOrEmpty(path3))
        {
            installPath = Path.Combine(oculusBaseDirectory, path1, path2);
        }
        else
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

    public void OpenFolderBrowser()
    {
        StandaloneFileBrowser.OpenFolderPanelAsync("Installation Directory", "", false, paths =>
        {
            if (paths.Length <= 0) return;

            var installation = paths[0];
            Settings.Instance.BeatSaberInstallation = directoryField.text = installation;
            validation.SetValidationState(true, Settings.ValidateDirectory(ErrorFeedback));
        });
    }

    public void ValidateQuiet()
    {
        SetFromTextbox();
        validation.SetValidationState(true, Settings.ValidateDirectory(null));
    }

}
