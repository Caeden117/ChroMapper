using System.Collections;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Infrastructure
{
    public abstract class TestBase
    {
        // Action diagnostics are static, so each fixture must preserve the editor's value before disabling them.
        private bool previousActionDiagnosticsEnabled;

        protected virtual EditingMode InitialEditingMode => EditingMode.Gameplay;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            // Map setup and action-heavy tests do not need undo/redo logs; keep their formatting and logging out of the run.
            previousActionDiagnosticsEnabled = BeatmapActionContainer.ActionDiagnosticsEnabled;
            BeatmapActionContainer.ActionDiagnosticsEnabled = false;
            yield return TestUtils.LoadMap(3);
            // Speeds up some tests by multiple seconds. Which does beg some questions that I don't want to find the answer to right now lmao
            PersistentUI.Instance.EnableTransitions = false;
            yield return OnMapLoaded();
        }

        [SetUp]
        public void SetUpEditorMode()
        {
            var atsc = ResetPlayback();

            // An explicit chromatic-aberration test must not leave color fringing in later captures.
            Settings.ApplyOptionByName(nameof(Settings.ChromaticAberration), false);

            // Debug.Log($"[GLSHang] Case setup {TestContext.CurrentContext.Test.FullName}");
            // Normalize callbacks, focus routing, and devices together before every case so input cannot cross fixture boundaries.
            TestUtils.ResetSharedInputState();

            // Restore the shared metadata and its map together so a prior test cannot leave mismatched BPM conversion state behind.
            ResetSharedMapState();

            // Establish a deterministic tab before each test so editor mode cannot leak from a preceding fixture.
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            if (editModeContext != null)
                editModeContext.EditingMode = InitialEditingMode;

            // Paste and action tests share one loaded map, so reset playback and its cursor to prevent a prior fixture's beat from becoming their paste anchor.
            if (atsc != null)
            {
                atsc.MoveToJsonTime(0);
            }
        }

        protected virtual IEnumerator OnMapLoaded()
        {
            yield break;
        }

        [OneTimeTearDown]
        public void ReturnSettings()
        {
            // Restore the static flag even if fixture cleanup fails so an editor test run cannot leave real CM diagnostics disabled.
            try
            {
                OnReturnSettings();
                TestUtils.ReturnSettings();
            }
            finally
            {
                BeatmapActionContainer.ActionDiagnosticsEnabled = previousActionDiagnosticsEnabled;
            }
        }

        protected virtual void OnReturnSettings()
        {
        }

        [TearDown]
        public void CleanupAfterTest()
        {
            try
            {
                // Debug.Log($"[GLSHang] Case cleanup {TestContext.CurrentContext.Test.FullName}");
                SelectionController.DeselectAll();
                BeforeCleanup();
                BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
                // SongBoundaryTest performance coverage records the exact collections it authors so hundreds of cases do not sweep every map collection.
                CleanupTestObjects();
                AfterCleanup();

                // Leave the shared editor in the default tab so tests that do not override their mode start consistently.
                //  Without this, test execution order can change the results of tests who forget to properly set their editor tab (CompositeTest, looking at you :V)
                var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
                if (editModeContext != null)
                    editModeContext.EditingMode = EditingMode.Gameplay;
            }
            finally
            {
                ResetPlayback();
            }
        }

        protected virtual void BeforeCleanup()
        {
        }

        protected virtual void AfterCleanup()
        {
        }

        // Reset the shared map between cases by default. Fixtures with many cases can reuse it if they clean
        // up every object they add.
        protected virtual void ResetSharedMapState() => TestUtils.ResetSharedMapState();

        // Most fixtures retain the conservative whole-map cleanup; high-cardinality fixtures override this with their authored collection set.
        protected virtual void CleanupTestObjects() => CleanupUtils.CleanupObjects();

        private static AudioTimeSyncController ResetPlayback()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null && atsc.IsPlaying)
            {
                atsc.CancelPlaying();
            }

            Settings.Instance.AudioLatencyCompensation = 0;
            return atsc;
        }
    }
}
