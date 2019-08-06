// IN YOUR EDITOR FOLDER, have SimpleEditorUtils.cs.
// paste in this text.
// to play, HIT COMMAND-ZERO rather than command-P
// (the zero key, is near the P key, so it's easy to remember)
// simply insert the actual name of your opening scene
// "__preEverythingScene" on the second last line of code below.

using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SimpleEditorUtils {
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

}