using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Beatmap.Base;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Random = System.Random;

namespace ManualTests
{
    public class InstalledMapLoadingTest : TestBase
    {
        private const string FailedMapSeparator = "\n\n-----------------\n\n";
        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions must turn a wedged scene coroutine into a
        // recorded failure instead of occupying the Unity runner indefinitely after an exception aborts map loading.
        private const float SceneTransitionTimeoutSeconds = 120f;

        private MapLoadFailure currentFailure;
        private string currentOperation;
        private int loadedMapCount;
        private bool logHandlerSubscribed;
        private bool? originalIgnoreFailingMessages;
        private int? originalMapVersion;
        private EditorSettingsCombination originalSettings;
        private HashSet<int> preexistingDialogInstanceIds;
        private bool sharedMapperRestored;
        private bool transitionExceptionLogged;

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions limits each seeded renderer run to five maps.
        private const int MaximumRandomMapCount = 25;

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions can exceed Unity's three-minute default while
        // rendering five full maps, so grant it an hour while aggregating both configured default song locations.
        [UnityTest]
        [Timeout(3_600_000)]
        [Explicit("Loads up to N randomly selected locally installed maps and can take a long time.")]
        [Category("Manual")]
        public IEnumerator UpToNRandomMapsInDefaultSongLocationsWithRandomCmSettingsLoadWithoutExceptions()
        {
            originalSettings = EditorSettingsCombination.Capture();
            originalMapVersion = Settings.Instance.MapVersion;
            originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            var randomSeed = Guid.NewGuid().GetHashCode();
            var random = new Random(randomSeed);
            loadedMapCount = 0;
            sharedMapperRestored = false;
            var failures = new List<MapLoadFailure>();
            string[] songLocations = null;
            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions distinguishes an empty discovered corpus
            // from discovered Info-only WIP shells.
            var discoveredMapCount = 0;

            try
            {
                // Production file helpers log some exceptions and return null, so capture those logs in the same
                // result array while suppressing Unity's premature log assertion until the complete report is built.
                LogAssert.ignoreFailingMessages = true;
                Application.logMessageReceived += HandleLogMessage;
                logHandlerSubscribed = true;

                // The full renderer path can legitimately open conversion dialogs, so remember any dialog that
                // predates the test and close only corpus-generated clones as each difficulty finishes loading.
                preexistingDialogInstanceIds = FindActiveDialogInstanceIds();

                try
                {
                    songLocations = ResolveDefaultSongLocations();
                    TestContext.Progress.WriteLine(
                        $"Resolved default song locations:\n{string.Join(Environment.NewLine, songLocations)}");
                }
                catch (Exception exception)
                {
                    var settings = EditorSettingsCombination.CreateRandom(random);
                    var failure = new MapLoadFailure("<default song locations>", settings);
                    failure.Add("Song location resolution", exception);
                    failures.Add(failure);
                }

                if (songLocations != null)
                {
                    // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions shuffles the combined GUI-location
                    // corpus so empty WIP shells can be skipped while still finding up to five renderable maps.
                    var discoveredMapDirectories = new List<DirectoryInfo>();
                    foreach (var songLocation in songLocations)
                    {
                        discoveredMapDirectories.AddRange(GetMapDirectories(songLocation, random, failures));
                    }

                    discoveredMapCount = discoveredMapDirectories.Count;
                    var mapDirectories = ShuffleMapDirectories(discoveredMapDirectories, random);
                    TestContext.Progress.WriteLine(
                        $"Random seed: {randomSeed}. Shuffled {mapDirectories.Count} discovered maps; "
                        + $"loading the first {MaximumRandomMapCount} non-empty candidates.");

                    for (var mapIndex = 0;
                         mapIndex < mapDirectories.Count && loadedMapCount < MaximumRandomMapCount;
                         mapIndex++)
                    {
                        var settings = EditorSettingsCombination.CreateRandom(random);
                        yield return LoadMap(
                            mapDirectories[mapIndex].FullName,
                            settings,
                            failures,
                            mapIndex + 1,
                            mapDirectories.Count);
                    }
                }

                // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions allows an all-empty WIP corpus,
                // but still reports when neither configured GUI location contains even an Info.dat song folder.
                if (songLocations != null && discoveredMapCount == 0 && failures.Count == 0)
                {
                    var settings = EditorSettingsCombination.CreateRandom(random);
                    var failure = new MapLoadFailure("<default song locations>", settings);
                    failure.Add(
                        "Map discovery",
                        new InvalidOperationException(
                            "No maps were found in either default song location. Resolved locations:\n"
                            + string.Join(Environment.NewLine, songLocations)));
                    failures.Add(failure);
                }
            }
            finally
            {
                RestoreManualTestState();
            }

            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions must return the shared PlayMode harness to
            // its synthetic mapper scene before NUnit advances to fixtures such as NotesContainerTest.
            yield return RestoreSharedTestMapperScene();
            sharedMapperRestored = true;

            var errors = failures.Where(failure => failure.Errors.Count > 0).ToArray();
            var failureReport = string.Join(
                FailedMapSeparator,
                errors.Select(failure => failure.Format()).ToArray());
            var failureReportPath = errors.Length > 0
                ? WriteFailureReport(randomSeed, failureReport)
                : null;

            Assert.That(
                errors,
                Is.Empty,
                $"Random seed: {randomSeed}\nFull failure report: {failureReportPath}\n\n{failureReport}");
        }

        // Unity can abort a timed-out coroutine without running its iterator finally, so independently restore every
        // global setting and log hook during NUnit teardown; this method is idempotent for ordinary completed runs.
        [UnityTearDown]
        public IEnumerator RestoreStateAfterManualCorpusTest()
        {
            RestoreManualTestState();
            if (!sharedMapperRestored)
            {
                // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions can be aborted by Unity before its
                // iterator reaches normal restoration, so the coroutine teardown repairs the scene as a fallback.
                yield return RestoreSharedTestMapperScene();
            }

            sharedMapperRestored = false;
        }

        private void RestoreManualTestState()
        {
            currentFailure = null;
            currentOperation = null;

            // A timeout can bypass the coroutine finally while a conversion dialog is still open, so teardown also
            // removes only dialogs created after this test began and restores the shared loading indicator.
            CloseTestCreatedDialogs();
            RestoreProgressIndicator();

            try
            {
                if (originalSettings != null)
                {
                    originalSettings.Apply(true);
                }

                if (originalMapVersion.HasValue)
                {
                    Settings.Instance.MapVersion = originalMapVersion.Value;
                }
            }
            finally
            {
                if (logHandlerSubscribed)
                {
                    Application.logMessageReceived -= HandleLogMessage;
                    logHandlerSubscribed = false;
                }

                if (originalIgnoreFailingMessages.HasValue)
                {
                    LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages.Value;
                }

                originalSettings = null;
                originalMapVersion = null;
                originalIgnoreFailingMessages = null;
                preexistingDialogInstanceIds = null;
                transitionExceptionLogged = false;
            }
        }

        // NUnit and the Codex attachment view truncate long assertion output, so persist the same complete report under
        // Unity's ignored Temp folder before asserting; every failed map, settings profile, and exception remains intact.
        private static string WriteFailureReport(int randomSeed, string failureReport)
        {
            var reportPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                "InstalledMapLoadingFailures.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, $"Random seed: {randomSeed}\n\n{failureReport}");
            return reportPath;
        }

        // Unity tests can permanently cache Settings.TestRunnerSettings at /root/bs, so this manual local-corpus test
        // must recover the persisted GUI installation instead of accepting the harness's valid-but-empty directories.
        private static string[] ResolveDefaultSongLocations()
        {
            var beatSaberInstallation = Settings.Instance.BeatSaberInstallation;
            if (ReferenceEquals(Settings.Instance, Settings.TestRunnerSettings))
            {
                var settingsFile = Path.Combine(Application.persistentDataPath, "ChroMapperSettings.json");
                if (!File.Exists(settingsFile))
                {
                    throw new FileNotFoundException(
                        "The persisted ChroMapper settings file was not found while resolving local map locations.",
                        settingsFile);
                }

                var settingsNode = JSON.Parse(File.ReadAllText(settingsFile));
                beatSaberInstallation = settingsNode[nameof(Settings.BeatSaberInstallation)].Value;
                if (string.IsNullOrWhiteSpace(beatSaberInstallation))
                {
                    throw new InvalidDataException(
                        $"'{nameof(Settings.BeatSaberInstallation)}' is missing from '{settingsFile}'.");
                }
            }

            return new[]
            {
                PathUtils.Combine(beatSaberInstallation, "Beat Saber_Data", "CustomWIPLevels"),
                PathUtils.Combine(beatSaberInstallation, "Beat Saber_Data", "CustomLevels"),
            };
        }

        // Directory discovery errors belong in the final aggregate instead of preventing the other GUI location
        // from being tested, and materializing here makes that directory snapshot stable for the full manual run.
        private static DirectoryInfo[] GetMapDirectories(
            string songLocation,
            Random random,
            List<MapLoadFailure> failures)
        {
            try
            {
                return new DirectoryInfo(songLocation)
                    .EnumerateDirectories()
                    .Where(directory => !directory.Attributes.HasFlag(FileAttributes.Hidden))
                    .Where(directory => File.Exists(Path.Combine(directory.FullName, "Info.dat"))
                        || File.Exists(Path.Combine(directory.FullName, "info.dat")))
                    .OrderBy(directory => directory.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception exception)
            {
                var settings = EditorSettingsCombination.CreateRandom(random);
                var failure = new MapLoadFailure(songLocation, settings);
                failure.Add("Song location discovery", exception);
                failures.Add(failure);
                return Array.Empty<DirectoryInfo>();
            }
        }

        // A failure in one installed map must not stop the PlayMode corpus run from rendering later maps and settings.
        private IEnumerator LoadMap(
            string mapDirectory,
            EditorSettingsCombination settings,
            List<MapLoadFailure> failures,
            int mapIndex,
            int mapCount)
        {
            var failure = new MapLoadFailure(mapDirectory, settings);
            currentFailure = failure;
            BaseInfo info = null;

            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions unloads the mapper through the same scene
            // transition as Exit to Menu, so the next settings profile cannot refresh the previous installed map.
            currentOperation = "Unloading previous mapper scene";
            yield return ExitMapperScene();

            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions applies settings only after Unity destroys
            // the old mapper scene, retaining renderer notifications without rebuilding stale visual containers.
            currentOperation = "Applying randomized editor settings";
            settings.Apply(true);

            try
            {
                currentOperation = "Map info";
                var errorCountBeforeLoad = failure.Errors.Count;
                info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
                if (info == null && failure.Errors.Count == errorCountBeforeLoad)
                {
                    failure.Add(
                        currentOperation,
                        new InvalidOperationException("The map info loader returned null without logging an error."));
                }
            }
            catch (Exception exception)
            {
                failure.Add(currentOperation, exception);
            }

            if (info != null)
            {
                failure.SongName = info.SongName;

                // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions renders only the highest non-empty
                // difficulty from the preferred characteristic instead of repeatedly reloading every difficulty.
                var selection = SelectHighestPriorityNonEmptyDifficulty(info, failure);
                if (selection != null)
                {
                    // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions counts only renderable songs toward
                    // its five-map cap, so empty WIP shells never consume a randomized renderer slot.
                    loadedMapCount++;
                    ReportProgress(
                        info,
                        selection.Difficulty,
                        loadedMapCount,
                        Math.Min(MaximumRandomMapCount, mapCount));
                    yield return LoadDifficulty(info, selection.Difficulty, selection.Map, failure);
                }
                else
                {
                    // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions treats an Info-only WIP project as
                    // an unrenderable candidate, then continues through the shuffled corpus without recording an error.
                    TestContext.Progress.WriteLine(
                        $"[candidate {mapIndex}/{mapCount}] {info.SongName} - "
                        + "no non-empty difficulty found; trying another song.");
                }

                // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions leaves through the production scene
                // lifecycle after the selected difficulty, keeping TestBase teardown away from full installed-map data.
                currentOperation = "Unloading completed map";
                yield return ExitMapperScene();
            }

            currentFailure = null;
            currentOperation = null;
            if (failure.Errors.Count > 0)
            {
                failures.Add(failure);
            }
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions orders characteristics before difficulties so
        // Lawless wins over 360, other special modes, and Standard even when a lower-priority mode is more difficult.
        private DifficultySelection SelectHighestPriorityNonEmptyDifficulty(BaseInfo info, MapLoadFailure failure)
        {
            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions flattens duplicate characteristic sets and
            // all other special modes into their priority tier before rank ordering, so the tier's ExpertPlus wins.
            var orderedDifficulties = info.DifficultySets
                .SelectMany(difficultySet => difficultySet.Difficulties)
                .OrderBy(difficulty => GetCharacteristicPriority(difficulty.Characteristic))
                .ThenByDescending(difficulty => difficulty.DifficultyRank);
            foreach (var difficulty in orderedDifficulties)
            {
                var difficultyDescription = $"Difficulty {difficulty.Characteristic}/{difficulty.Difficulty}";
                currentOperation = $"{difficultyDescription} candidate parsing";
                var errorCountBeforeLoad = failure.Errors.Count;
                BaseDifficulty map = null;

                try
                {
                    map = BeatSaberSongUtils.GetMapFromInfoFiles(info, difficulty);
                    if (map == null && failure.Errors.Count == errorCountBeforeLoad)
                    {
                        failure.Add(
                            currentOperation,
                            new InvalidOperationException(
                                "The difficulty loader returned null without logging an error."));
                    }
                }
                catch (Exception exception)
                {
                    failure.Add(currentOperation, exception);
                }

                if (map != null && !map.IsEmpty())
                {
                    return new DifficultySelection(difficulty, map);
                }
            }

            return null;
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions treats every named or custom characteristic
        // except Standard as a special mode after explicitly prioritizing Lawless and 360Degree.
        private static int GetCharacteristicPriority(string characteristic)
        {
            if (string.Equals(characteristic, "Lawless", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(characteristic, "360Degree", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return string.Equals(characteristic, "Standard", StringComparison.OrdinalIgnoreCase)
                ? 3
                : 2;
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions passes the already parsed winning difficulty
        // into the production mapper scene so selection does not double the map parsing cost before rendering.
        private IEnumerator LoadDifficulty(
            BaseInfo info,
            InfoDifficulty difficulty,
            BaseDifficulty map,
            MapLoadFailure failure)
        {
            var difficultyDescription = $"Difficulty {difficulty.Characteristic}/{difficulty.Difficulty}";
            currentOperation = $"{difficultyDescription} renderer load";
            try
            {
                var songContainer = BeatSaberSongContainer.Instance;
                songContainer.Info = info;
                songContainer.MapDifficultyInfo = difficulty;
                songContainer.Map = map;
                Settings.Instance.MapVersion = map.MajorVersion;

                // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions enters through LoadInitialMap so the
                // installed difficulty exercises the production mapper renderer instead of a test-only HardRefresh.
                transitionExceptionLogged = false;
                SceneTransitionManager.Instance.LoadScene("03_Mapper");
            }
            catch (Exception exception)
            {
                failure.Add(currentOperation, exception);
                yield break;
            }

            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions bounds a failed external load coroutine so
            // its captured exception cannot leave SceneTransitionManager.IsLoading wedged and hang the explicit test.
            yield return WaitForSceneTransition();
            yield return null;
            yield return null;

            // Legacy custom-BPM conversion opens an informational dialog on every affected difficulty; closing the
            // test-created clones after their renderer work prevents hundreds of canvases from accumulating.
            CloseTestCreatedDialogs();

            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions unloads the selected difficulty just as a
            // user exits a loaded map before opening another, avoiding per-object mutation cleanup entirely.
            currentOperation = $"{difficultyDescription} renderer unload";
            yield return ExitMapperScene();
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions uses PauseManager's authoritative fast unload
        // path while retaining the same bounded exception recovery as renderer entry.
        private IEnumerator ExitMapperScene()
        {
            if (!SceneManager.GetActiveScene().name.StartsWith("03", StringComparison.Ordinal))
            {
                yield break;
            }

            transitionExceptionLogged = false;
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
            yield return WaitForSceneTransition();
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions records and cancels a scene transition that
        // cannot complete after a logged exception, preserving later maps and PlayMode fixtures in the same run.
        private IEnumerator WaitForSceneTransition()
        {
            var deadline = Time.realtimeSinceStartup + SceneTransitionTimeoutSeconds;
            var framesAfterException = 0;
            while (SceneTransitionManager.IsLoading)
            {
                if (transitionExceptionLogged)
                {
                    framesAfterException++;
                    if (framesAfterException >= 2)
                    {
                        var manager = SceneTransitionManager.Instance;
                        if (manager != null)
                        {
                            manager.CancelLoading(string.Empty);
                        }

                        yield return null;
                        break;
                    }
                }

                if (Time.realtimeSinceStartup >= deadline)
                {
                    currentFailure?.Add(
                        currentOperation ?? "Scene transition",
                        new TimeoutException(
                            $"Scene transition exceeded {SceneTransitionTimeoutSeconds:R} seconds."));
                    var manager = SceneTransitionManager.Instance;
                    if (manager != null)
                    {
                        manager.CancelLoading(string.Empty);
                    }

                    yield return null;
                    break;
                }

                yield return null;
            }

            transitionExceptionLogged = false;
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions owns full scene transitions during its run, so
        // reload TestBase's synthetic mapper before handing the shared PlayMode process to the next fixture.
        private IEnumerator RestoreSharedTestMapperScene()
        {
            var manager = SceneTransitionManager.Instance;
            if (SceneTransitionManager.IsLoading && manager != null)
            {
                manager.CancelLoading(string.Empty);
                yield return null;
            }

            yield return ExitMapperScene();
            yield return base.LoadMap();
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions reports one selected difficulty per sampled map,
        // keeping progress accurate now that empty and lower-priority difficulties are not rendered.
        private static void ReportProgress(
            BaseInfo info,
            InfoDifficulty difficulty,
            int mapIndex,
            int mapCount)
        {
            var progress = $"[{mapIndex}/{mapCount}] {info.SongName} - "
                + $"{difficulty.Characteristic}/{difficulty.Difficulty}";
            TestContext.Progress.WriteLine(progress);

            if (PersistentUI.Instance != null)
            {
                PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
                PersistentUI.Instance.LevelLoadSlider.value = mapCount > 0
                    ? mapIndex / (float)mapCount
                    : 0;
                PersistentUI.Instance.LevelLoadSliderLabel.text = progress;
            }
        }

        // Instance IDs let cleanup preserve any dialog the user already had open before starting the explicit test.
        private static HashSet<int> FindActiveDialogInstanceIds() =>
            UnityEngine.Object.FindObjectsByType<DialogBox>(FindObjectsSortMode.None)
                .Select(dialog => dialog.GetInstanceID())
                .ToHashSet();

        // DialogBox.Close follows its normal callback/input cleanup and destroys ordinary preset dialogs on close.
        private void CloseTestCreatedDialogs()
        {
            if (preexistingDialogInstanceIds == null)
            {
                return;
            }

            foreach (var dialog in UnityEngine.Object.FindObjectsByType<DialogBox>(FindObjectsSortMode.None))
            {
                if (!preexistingDialogInstanceIds.Contains(dialog.GetInstanceID()))
                {
                    dialog.Close();
                }
            }
        }

        // The corpus borrows the standard loading indicator only for observability and must leave no visible test UI.
        private static void RestoreProgressIndicator()
        {
            if (PersistentUI.Instance != null)
            {
                PersistentUI.Instance.LevelLoadSlider.value = 0;
                PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false);
                PersistentUI.Instance.LevelLoadSliderLabel.text = string.Empty;
            }
        }

        // A seeded Fisher-Yates shuffle gives every discovered map equal selection probability while avoiding the
        // allocations and unclear ordering guarantees of sorting the corpus by random keys.
        private static List<DirectoryInfo> ShuffleMapDirectories(
            List<DirectoryInfo> discoveredMapDirectories,
            Random random)
        {
            var shuffledMapDirectories = new List<DirectoryInfo>(discoveredMapDirectories);
            for (var currentIndex = shuffledMapDirectories.Count - 1; currentIndex > 0; currentIndex--)
            {
                var swapIndex = random.Next(currentIndex + 1);
                (shuffledMapDirectories[currentIndex], shuffledMapDirectories[swapIndex]) =
                    (shuffledMapDirectories[swapIndex], shuffledMapDirectories[currentIndex]);
            }

            return shuffledMapDirectories;
        }

        // Unity error logs emitted by loaders that swallow exceptions must retain the active map, operation, and
        // randomized settings in the same final failure record as directly thrown exceptions.
        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (currentFailure == null
                || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert))
            {
                return;
            }

            currentFailure.Add(
                currentOperation ?? "Map loading",
                new Exception($"{condition}\n{stackTrace}"));

            // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions must distinguish exceptions that can abort
            // a load coroutine from ordinary error logs that should be observed while the transition continues.
            if (type == LogType.Exception && SceneTransitionManager.IsLoading)
            {
                transitionExceptionLogged = true;
            }
        }

        // UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions retains the parsed winning map so it can select
        // by authoritative content without parsing the same difficulty again immediately before the renderer load.
        private sealed class DifficultySelection
        {
            public DifficultySelection(InfoDifficulty difficulty, BaseDifficulty map)
            {
                Difficulty = difficulty;
                Map = map;
            }

            public InfoDifficulty Difficulty { get; }
            public BaseDifficulty Map { get; }
        }

        // Restrict random values to settings combinations that can be selected in ChroMapper's editor options GUI.
        private sealed class EditorSettingsCombination
        {
            private static readonly string[] ObsoleteBooleanSettingNames =
            {
                nameof(Settings.BongoBoye),
                nameof(Settings.PrecisionPlacementGrid),
                nameof(Settings.PyramidEventModels),
                nameof(Settings.SimpleBlocks),
                nameof(Settings.SolidChainLink),
            };

            private static readonly FieldInfo[] BooleanSettingFields = typeof(Settings)
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(field => field.FieldType == typeof(bool))
                .Where(field => !ObsoleteBooleanSettingNames.Contains(field.Name))
                .OrderBy(field => field.Name, StringComparer.Ordinal)
                .ToArray();

            private static readonly int[] CameraAaOptions = { 0, 2, 4, 8 };
            private static readonly string[] EventModelOptions = { "Block", "Pyramid", "Flat Pyramid", "Node" };
            private static readonly int[] MirrorQualityOptions =
            {
                (int)MirrorRendererSO.MirrorQuality.None,
                (int)MirrorRendererSO.MirrorQuality.Low,
                (int)MirrorRendererSO.MirrorQuality.High,
            };
            private static readonly string[] NoteModelOptions =
            {
                "Standard",
                "Standard Solid",
                "Simple",
                "Simple Solid",
            };
            private static readonly PrecisionPlacementMode[] PrecisionPlacementModeOptions =
            {
                PrecisionPlacementMode.Off,
                PrecisionPlacementMode.Hold,
                PrecisionPlacementMode.Toggle,
            };
            private static readonly int[] RenderScaleOptions = { 50, 75, 100, 125, 150, 200 };

            public Dictionary<string, bool> BooleanSettings { get; private set; }
            public int CameraAa { get; private set; }
            public string EventModels { get; private set; }
            public float GridTransparency { get; private set; }
            public float InterfaceOpacity { get; private set; }
            public int MirrorQuality { get; private set; }
            public string NoteModels { get; private set; }
            public float ObstacleOpacity { get; private set; }
            public float PastNoteModelAlpha { get; private set; }
            public PrecisionPlacementMode PrecisionPlacementMode { get; private set; }
            public int RenderScale { get; private set; }

            // Capturing every randomized field prevents this manual corpus test from changing the user's editor setup.
            public static EditorSettingsCombination Capture()
            {
                var settings = Settings.Instance;
                return new EditorSettingsCombination
                {
                    BooleanSettings = BooleanSettingFields.ToDictionary(
                        field => field.Name,
                        field => (bool)field.GetValue(settings)),
                    CameraAa = settings.CameraAA,
                    EventModels = settings.EventModels,
                    GridTransparency = settings.GridTransparency,
                    InterfaceOpacity = settings.InterfaceOpacity,
                    MirrorQuality = settings.MirrorQuality,
                    NoteModels = settings.NoteModels,
                    ObstacleOpacity = settings.ObstacleOpacity,
                    PastNoteModelAlpha = settings.PastNoteModelAlpha,
                    PrecisionPlacementMode = settings.PrecisionPlacementMode,
                    RenderScale = settings.RenderScale,
                };
            }

            // Each map receives a valid combination drawn from the serialized GUI dropdown values and boolean options.
            public static EditorSettingsCombination CreateRandom(Random random)
            {
                return new EditorSettingsCombination
                {
                    BooleanSettings = BooleanSettingFields.ToDictionary(
                        field => field.Name,
                        _ => NextBoolean(random)),
                    CameraAa = CameraAaOptions[random.Next(CameraAaOptions.Length)],
                    EventModels = EventModelOptions[random.Next(EventModelOptions.Length)],
                    GridTransparency = NextOpacity(random),
                    InterfaceOpacity = NextOpacity(random),
                    MirrorQuality = MirrorQualityOptions[random.Next(MirrorQualityOptions.Length)],
                    NoteModels = NoteModelOptions[random.Next(NoteModelOptions.Length)],
                    ObstacleOpacity = NextOpacity(random),
                    PastNoteModelAlpha = NextOpacity(random),
                    PrecisionPlacementMode =
                        PrecisionPlacementModeOptions[random.Next(PrecisionPlacementModeOptions.Length)],
                    RenderScale = RenderScaleOptions[random.Next(RenderScaleOptions.Length)],
                };
            }

            // Applying the snapshot immediately before loading a map makes the reported combination authoritative.
            public void Apply(bool notify)
            {
                var settings = Settings.Instance;
                foreach (var field in BooleanSettingFields)
                {
                    var value = BooleanSettings[field.Name];
                    field.SetValue(settings, value);
                    if (notify)
                    {
                        Settings.ManuallyNotifySettingUpdatedEvent(field.Name, value);
                    }
                }

                settings.CameraAA = CameraAa;
                settings.EventModels = EventModels;
                settings.GridTransparency = GridTransparency;
                settings.InterfaceOpacity = InterfaceOpacity;
                settings.MirrorQuality = MirrorQuality;
                settings.NoteModels = NoteModels;
                settings.ObstacleOpacity = ObstacleOpacity;
                settings.PastNoteModelAlpha = PastNoteModelAlpha;
                settings.PrecisionPlacementMode = PrecisionPlacementMode;
                settings.RenderScale = RenderScale;

                if (notify)
                {
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.CameraAA), CameraAa);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.EventModels), EventModels);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.GridTransparency), GridTransparency);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.InterfaceOpacity), InterfaceOpacity);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.MirrorQuality), MirrorQuality);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.NoteModels), NoteModels);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.ObstacleOpacity), ObstacleOpacity);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.PastNoteModelAlpha), PastNoteModelAlpha);
                    Settings.ManuallyNotifySettingUpdatedEvent(
                        nameof(Settings.PrecisionPlacementMode),
                        PrecisionPlacementMode);
                    Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.RenderScale), RenderScale);
                }
            }

            // A complete one-line profile keeps every failure reproducible without obscuring the associated exception.
            public override string ToString()
            {
                var booleanSettings = string.Join(
                    ", ",
                    BooleanSettingFields.Select(field => $"{field.Name}={BooleanSettings[field.Name]}").ToArray());
                return $"EventModels={EventModels}, NoteModels={NoteModels}, "
                    + $"PrecisionPlacementMode={PrecisionPlacementMode}, MirrorQuality={MirrorQuality}, "
                    + $"CameraAA={CameraAa}, RenderScale={RenderScale}, "
                    + $"GridTransparency={GridTransparency}, InterfaceOpacity={InterfaceOpacity}, "
                    + $"ObstacleOpacity={ObstacleOpacity}, PastNoteModelAlpha={PastNoteModelAlpha}, "
                    + booleanSettings;
            }

            private static bool NextBoolean(Random random) => random.Next(2) == 1;

            private static float NextOpacity(Random random) => random.Next(101) / 100f;
        }

        // Grouping errors by map ensures the requested separator appears between maps rather than between difficulties.
        private sealed class MapLoadFailure
        {
            public MapLoadFailure(string mapDirectory, EditorSettingsCombination settings)
            {
                MapDirectory = mapDirectory;
                Settings = settings;
            }

            public string MapDirectory { get; }
            public EditorSettingsCombination Settings { get; }
            public string SongName { get; set; }
            public List<MapLoadError> Errors { get; } = new();

            // Keeping operation context alongside each exception distinguishes multiple failures from one map.
            public void Add(string operation, Exception exception) =>
                Errors.Add(new MapLoadError(operation, exception));

            // One formatted block per failed map allows the caller to apply the requested separator exactly once.
            public string Format()
            {
                var builder = new StringBuilder();
                builder.Append("Map: ");
                if (!string.IsNullOrEmpty(SongName))
                {
                    builder.Append(SongName);
                    builder.Append(" (");
                    builder.Append(MapDirectory);
                    builder.Append(')');
                }
                else
                {
                    builder.Append(MapDirectory);
                }

                builder.AppendLine();
                builder.Append("Settings: ");
                builder.AppendLine(Settings.ToString());
                builder.AppendLine("Errors:");
                for (var errorIndex = 0; errorIndex < Errors.Count; errorIndex++)
                {
                    if (errorIndex > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append('[');
                    builder.Append(Errors[errorIndex].Operation);
                    builder.AppendLine("]");
                    builder.Append(Errors[errorIndex].Exception);
                }

                return builder.ToString();
            }
        }

        // Preserve the operation separately from the exception so repeated failures remain attributable in one map block.
        private sealed class MapLoadError
        {
            public MapLoadError(string operation, Exception exception)
            {
                Operation = operation;
                Exception = exception;
            }

            public string Operation { get; }
            public Exception Exception { get; }
        }
    }
}
