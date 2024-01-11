using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using QuestDumper;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class FirstBootMenu : MonoBehaviour
{
    private static readonly string oculusStoreBeatSaberFolderName = "hyperbolic-magnetism-beat-saber";

    [SerializeField] private GameObject directoryCanvas;

    [SerializeField] private TMP_InputField directoryField;

    [SerializeField] private TMP_Dropdown graphicsDropdown;

    [SerializeField] private TMP_Dropdown lightingDropdown;

    [SerializeField] private GameObject helpPanel;

    [SerializeField] private InputBoxFileValidator validation;

    private readonly Regex appManifestRegex =
        new Regex(@"\s""installdir""\s+""(.+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Regex libraryRegex =
        new Regex(@"\s""\d""\s+""(.+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Use this for initialization
    private void Start()
    {
        //Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
        //This should be thread-wide, but I have this set throughout just in case it isnt.
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        // Disable VSync by default
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Settings.Instance.MaximumFPS;

        //Debug.Log(Environment.CurrentDirectory);

        if (Settings.ValidateDirectory())
        {
            Debug.Log("Auto loaded directory");
            FirstBootRequirementsMet();
            return;
        }

        if (SystemInfo.graphicsMemorySize <= 1024)
            graphicsDropdown.value = 2;
        else
            graphicsDropdown.value = 1;

        var posInstallationDirectory = GuessBeatSaberInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory))
        {
            directoryField.text = posInstallationDirectory;
            ValidateQuiet();
        }

        directoryCanvas.SetActive(true);
    }

    public void InstallAdb() => StartCoroutine(AdbUI.DoDownload());


    private void SetFromTextbox()
    {
        var installation = directoryField.text;
        if (installation == null) return;
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

        validation.SetValidationState(true);
    }

    public void SetDefaults()
    {
        switch (graphicsDropdown.value)
        {
            // Performance
            case 2:
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

        switch (lightingDropdown.value)
        {
            // default ChroMapper lighting
            case 0:
                Settings.Instance.NoteColorMultiplier = 1.0f;
                Settings.Instance.ArrowColorMultiplier = 1.72f;
                Settings.Instance.ArrowColorWhiteBlend = 0.75f;
                Settings.Instance.ObstacleOpacity = 0.25f;
                Settings.Instance.AlternateLighting = false;
                break;
            // MMA2-based lighting
            case 1:
                Settings.Instance.NoteColorMultiplier = 0.3f;
                Settings.Instance.ArrowColorMultiplier = 3f;
                Settings.Instance.ArrowColorWhiteBlend = 0.25f;
                Settings.Instance.ObstacleOpacity = 0.1f;
                Settings.Instance.AlternateLighting = true;
                break;
        }
    }

    public void ErrorFeedback(string s) => DoErrorFeedback(s, false);

    public void ErrorFeedbackWithContinue(string s) => DoErrorFeedback(s, true);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0004:Remove Unnecessary Cast",
        Justification = "Does not compile with Unity Mono (cringe)")]
    private void DoErrorFeedback(string s, bool continueAfter)
    {
        var arg = LocalizationSettings.StringDatabase.GetLocalizedString("FirstBoot", s);
        PersistentUI.Instance.ShowDialogBox("FirstBoot", "validate.dialog",
            continueAfter ? (Action<int>)HandleGenerateMissingFoldersWithContinue : HandleGenerateMissingFolders,
            PersistentUI.DialogBoxPresetType.YesNo, new object[] { arg });
    }

    internal void HandleGenerateMissingFolders(int res) => HandleGenerateMissingFolders(res, false);

    internal void HandleGenerateMissingFoldersWithContinue(int res) => HandleGenerateMissingFolders(res, true);

    internal void HandleGenerateMissingFolders(int res, bool continueAfter)
    {
        if (res == 0)
        {
            Debug.Log("Creating directories that do not exist...");
            if (!Directory.Exists(Settings.Instance.BeatSaberInstallation))
                Directory.CreateDirectory(Settings.Instance.BeatSaberInstallation);
            if (!Directory.Exists(Settings.Instance.CustomSongsFolder))
                Directory.CreateDirectory(Settings.Instance.CustomSongsFolder);
            if (!Directory.Exists(Settings.Instance.CustomWIPSongsFolder))
                Directory.CreateDirectory(Settings.Instance.CustomWIPSongsFolder);
            SetDefaults();
            FirstBootRequirementsMet();
        }
    }

    public void FirstBootRequirementsMet()
    {
        ColourHistory.Load(); //Load color history from file.
        CustomPlatformsLoader.Instance.Init();
        SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
    }

    public void ToggleHelp() => helpPanel.SetActive(!helpPanel.activeSelf);

    private string GuessBeatSaberInstallationDirectory()
    {
        var posInstallationDirectory = GuessSteamInstallationDirectory();
        if (!string.IsNullOrEmpty(posInstallationDirectory)) return posInstallationDirectory;
        posInstallationDirectory = GuessOculusInstallationDirectory();
        return posInstallationDirectory;
    }

    private string GuessSteamInstallationDirectory()
    {
        // The Steam App ID seems to be static e.g. https://store.steampowered.com/app/620980/Beat_Saber/
        var steamRegistryKey =
            "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 620980";
        var registryValue = "InstallLocation";
        try
        {
            var installDirectory = (string)Registry.GetValue(steamRegistryKey, registryValue, "");
            if (!string.IsNullOrEmpty(installDirectory)) return installDirectory;
            return GuessSteamInstallationDirectoryComplex();
        }
        catch (Exception e)
        {
            Debug.Log("Error reading Steam registry key" + e);
            return "";
        }
    }

    private string GuessSteamInstallationDirectoryComplex()
    {
        // The above registry key only exists if you've installed Beat Saber since last installing windows
        // if you copy the game files or reinstall windows then the registry key will be missing even though
        // the game launches just fine
        var steamRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam";
        var registryValue = "InstallPath";

        var libraryFolders = new List<string>();

        var mainSteamFolder = (string)Registry.GetValue(steamRegistryKey, registryValue, "");
        libraryFolders.Add(mainSteamFolder);

        var libraryFolderFilename = mainSteamFolder + "\\steamapps\\libraryfolders.vdf";
        if (File.Exists(libraryFolderFilename))
        {
            using (var reader = new StreamReader(libraryFolderFilename))
            {
                var text = reader.ReadToEnd();
                var matches = libraryRegex.Matches(text);

                foreach (Match match in matches)
                {
                    if (Directory.Exists(match.Groups[1].Value))
                        libraryFolders.Add(match.Groups[1].Value);
                }
            }
        }

        foreach (var libraryFolder in libraryFolders)
        {
            var fileName = libraryFolder + "\\steamapps\\appmanifest_620980.acf";
            if (File.Exists(fileName))
            {
                using (var reader = new StreamReader(fileName))
                {
                    var text = reader.ReadToEnd();

                    var installDirMatch = appManifestRegex.Matches(text)[0].Groups;
                    var installDir = libraryFolder + "\\steamapps\\common\\" + installDirMatch[1].Value;

                    if (Directory.Exists(installDir)) return installDir;
                }
            }
        }

        return "";
    }

    private string GuessOculusInstallationDirectory()
    {
        var oculusRegistryKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Oculus VR, LLC\\Oculus";
        try
        {
            var registryValue = "InitialAppLibrary";
            var software = "Software";

            // older Oculus installations seem to have created the InitialAppLibrary value
            var installPath = TryRegistryWithPath(oculusRegistryKey + "\\Config", registryValue, software,
                oculusStoreBeatSaberFolderName, "");

            if (!string.IsNullOrEmpty(installPath)) return installPath;

            // the default library for newer installations seem to be below the base directory in "Software\\Software" folder.
            registryValue = "Base";
            installPath = TryRegistryWithPath(oculusRegistryKey, registryValue, software, software,
                oculusStoreBeatSaberFolderName);

            if (Directory.Exists(installPath))
                return installPath;
            return TryOculusStoreLibraryLocations();
        }
        catch (Exception e)
        {
            Debug.Log("Error guessing Oculus Beat Saber Directory" + e);
            return "";
        }
    }

    private string TryRegistryWithPath(string registryKey, string registryValue, string path1, string path2,
        string path3)
    {
        var oculusBaseDirectory = (string)Registry.GetValue(registryKey, registryValue, "");
        if (string.IsNullOrEmpty(oculusBaseDirectory)) return "";
        string installPath;
        if (string.IsNullOrEmpty(path3))
            installPath = Path.Combine(oculusBaseDirectory, path1, path2);
        else
            installPath = Path.Combine(oculusBaseDirectory, path1, path2, path3);
        if (Directory.Exists(installPath))
            return installPath;
        return "";
    }

    private string TryOculusStoreLibraryLocations()
    {
        var libraryKey = Registry.CurrentUser.OpenSubKey("Software\\Oculus VR, LLC\\Oculus\\Libraries");
        if (libraryKey == null) return "";
        var subKeys = libraryKey.GetSubKeyNames();
        foreach (var subKeyName in subKeys)
        {
            var originalPath = libraryKey.OpenSubKey(subKeyName).GetValue("OriginalPath");
            if (originalPath != null && string.IsNullOrEmpty((string)originalPath)) continue;
            var installPath = Path.Combine((string)originalPath, "Software", oculusStoreBeatSaberFolderName);
            if (Directory.Exists(installPath)) return installPath;
        }

        return "";
    }

    public void OpenFolderBrowser() =>
        StandaloneFileBrowser.OpenFolderPanelAsync("Installation Directory", "", false, paths =>
        {
            if (paths.Length <= 0) return;

            var installation = paths[0];
            Settings.Instance.BeatSaberInstallation = directoryField.text = installation;
            validation.SetValidationState(true, Settings.ValidateDirectory(ErrorFeedback));
        });

    public void ValidateQuiet()
    {
        SetFromTextbox();
        validation.SetValidationState(true, Settings.ValidateDirectory());
    }
}
