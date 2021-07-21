// IN YOUR EDITOR FOLDER, have SimpleEditorUtils.cs.
// paste in this text.
// to play, HIT COMMAND-ZERO rather than command-P
// (the zero key, is near the P key, so it's easy to remember)
// simply insert the actual name of your opening scene
// "__preEverythingScene" on the second last line of code below.

using UnityEditor;
using System.Linq;
using UnityEditor.SceneManagement;
using System;
using UnityEditor.AddressableAssets.Settings;

[InitializeOnLoad]
public static class SimpleEditorUtils
{
    // click command-0 to go to the prelaunch scene and then play

    private static string lastScenePath;

    [MenuItem("Edit/Play From FirstBoot Scene %1")]
    public static void PlayFromPrelaunchScene() {
        if (EditorApplication.isPlaying == true) {
            EditorApplication.isPlaying = false;
            return;
        }
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        lastScenePath = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene("Assets/__Scenes/00_FirstBoot.unity");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Edit/Open Mapper Scene %2")]
    public static void DelaySceneChange() {
        if (EditorApplication.isPlaying == true) {
            return;
        }
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        lastScenePath = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene("Assets/__Scenes/03_Mapper.unity");
    }
    static string[] GetEnabledScenes()
    {
        return (
            from scene in EditorBuildSettings.scenes
            where scene.enabled
            where !string.IsNullOrEmpty(scene.path)
            select scene.path
        ).ToArray();
    }

    static void SetBuildNumber()
    {
        string _buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (string.IsNullOrEmpty(_buildNumber))
            _buildNumber = "1";

        PlayerSettings.bundleVersion = PlayerSettings.bundleVersion.Replace(".0", "." + _buildNumber);
    }

    static void BuildWindows()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/Win64/chromapper/ChroMapper.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.CompressWithLz4);
    }

    static void BuildOSX()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/MacOS/ChroMapper", BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.CompressWithLz4);
    }

    static void BuildLinux()
    {
        AddressableAssetSettings.BuildPlayerContent();
        SetBuildNumber();

        BuildPipeline.BuildPlayer(GetEnabledScenes(), "/root/project/checkout/build/Linux64/ChroMapper", BuildTarget.StandaloneLinux64, BuildOptions.Development | BuildOptions.CompressWithLz4);
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerHandler);
    }

    private static void BuildPlayerHandler(BuildPlayerOptions options)
    {
        AddressableAssetSettings.BuildPlayerContent();
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }

}