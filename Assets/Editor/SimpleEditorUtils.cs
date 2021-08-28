using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

[SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
    Justification = "These methods are used by Jenkins to automatically build ChroMapper.")]
[InitializeOnLoad]
public static class SimpleEditorUtils
{
    static SimpleEditorUtils()
    {

    }

    private static string[] GetEnabledScenes() =>
    (
        from scene in EditorBuildSettings.scenes
        where scene.enabled
        where !string.IsNullOrEmpty(scene.path)
        select scene.path
    ).ToArray();

    private static void SetBuildNumber()
    {
        var buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (string.IsNullOrEmpty(buildNumber))
            buildNumber = "1";

        PlayerSettings.bundleVersion = PlayerSettings.bundleVersion.Replace(".0", "." + buildNumber);
    }

    private static void BuildWindows()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/Win64/chromapper/ChroMapper.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.CompressWithLz4);
    }

    private static void BuildOsx()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/MacOS/ChroMapper",
            BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.CompressWithLz4);
    }

    private static void BuildLinux()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/Linux64/ChroMapper",
            BuildTarget.StandaloneLinux64, BuildOptions.Development | BuildOptions.CompressWithLz4);
    }

    [InitializeOnLoadMethod]
    private static void Initialize() => BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);

    private static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        AddressableAssetSettings.BuildPlayerContent();
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }
}
