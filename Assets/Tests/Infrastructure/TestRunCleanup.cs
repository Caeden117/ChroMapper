using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// A fixture without a namespace wraps the entire test assembly.
[SetUpFixture]
public class TestRunCleanup
{
    // Batchmode provides no native mouse or keyboard for production Update loops to read.
    // Input-driven tests still isolate input through their own InputTestFixture runtimes.
    private Mouse mouse;
    private Keyboard keyboard;

    [OneTimeSetUp]
    public void AddBaselineInputDevices()
    {
        mouse = InputSystem.AddDevice<Mouse>();
        keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [UnityOneTimeTearDown]
    public IEnumerator DeleteGeneratedTestInstallation()
    {
        // Loaded scenes keep reading Mouse.current in Update, so they must unload before the devices are removed.
        var cleanupScene = SceneManager.CreateScene("TestRunCleanup");
        SceneManager.SetActiveScene(cleanupScene);
        for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene == cleanupScene)
                continue;

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        try
        {
            var temporaryRoot = PathUtils.Combine(Path.GetFullPath(Path.GetTempPath()), "ChroMapper Tests") + "/";
            var installation = PathUtils.Combine(Path.GetFullPath(Settings.TestRunnerInstallationDirectory));
            if (!installation.StartsWith(temporaryRoot, StringComparison.Ordinal))
                throw new InvalidOperationException("The generated test installation must remain inside its temporary directory.");

            if (Directory.Exists(installation))
                Directory.Delete(installation, true);
        }
        finally
        {
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);

            if (keyboard != null && keyboard.added)
                InputSystem.RemoveDevice(keyboard);
        }
    }
}
