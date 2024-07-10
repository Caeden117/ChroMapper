

public class OSTools
{
    public static void OpenFileBrowser(string path)
    {
#if UNITY_STANDALONE_WIN
        path = path.Replace("/", "\\").Replace("\\\\", "\\");
#else
        path = path.Replace("\\", "/").Replace("//", "/");
#endif

        if (!path.StartsWith("\"")) path = "\"" + path;
        if (!path.EndsWith("\"")) path += "\"";

#if UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", $"{path}");
#elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", path);
#else
        Debug.LogError(
            "Unrecognized OS! If you happen to know this OS and would like to contribute," +
            " please contact me on Discord: Caeden117#0117");
#endif
    }
}
