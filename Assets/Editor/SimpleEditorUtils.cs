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

    private const BuildOptions buildOptions = BuildOptions.CompressWithLz4 | BuildOptions.Development;

    private static void BuildWindows()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/Win64/chromapper/ChroMapper.exe", BuildTarget.StandaloneWindows64, buildOptions);
    }

    private static void BuildOSX()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        // Needs to be wrapped in pre-processors since this code only compiles on macOS
#if UNITY_EDITOR && UNITY_STANDALONE_OSX
        UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.OSXStandalone.MacOSArchitecture.x64ARM64;
#endif
        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/MacOS/ChroMapper", BuildTarget.StandaloneOSX, buildOptions);
    }

    private static void BuildLinux()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/Linux64/ChroMapper", BuildTarget.StandaloneLinux64, buildOptions);
    }

    [InitializeOnLoadMethod]
    private static void Initialize() => BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);

    private static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        AddressableAssetSettings.BuildPlayerContent();
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }
}
