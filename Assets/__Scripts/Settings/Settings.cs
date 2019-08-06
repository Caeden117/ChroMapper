using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Settings {

    private static string beatSaberInstallation = "";
    public static string BeatSaberInstallation {
        get { return ConvertToDirectory(beatSaberInstallation); }
        set { beatSaberInstallation = value; }
    }

    public static string CustomSongsFolder {
        get {
            return ConvertToDirectory(BeatSaberInstallation + "/Beat Saber_Data/CustomLevels");
        }
    }

    public static string CustomWIPSongsFolder
    {
        get
        {
            return ConvertToDirectory(BeatSaberInstallation + "/Beat Saber_Data/CustomWIPLevels");
        }
    }

    public static bool LoadCustomSongsFolderDirectoryFromPrefs(Action<string> errorFeedback = null) {

        Settings.BeatSaberInstallation = PlayerPrefs.GetString("install");
        if (ValidateDirectory(errorFeedback)) {
            return true;
        }

        return false;
    }

    public static bool ValidateDirectory(Action<string> errorFeedback = null) {
        if (!Directory.Exists(BeatSaberInstallation)) {
            if (errorFeedback != null) errorFeedback("That folder does not exist!");
            return false;
        }
        if (!Directory.Exists(CustomSongsFolder)) {
            if (errorFeedback != null) errorFeedback("No \"Beat Saber_Data\" folder was found at chosen location!");
            return false;
        }
        return true;
    }

    public static string ConvertToDirectory(string s) {
        return s.Replace('\\', '/');
    }

}
