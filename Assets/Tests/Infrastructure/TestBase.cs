using System.Collections;
using Beatmap.Enums;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Infrastructure
{
    public abstract class TestBase
    {
        protected virtual EditingMode InitialEditingMode => EditingMode.Gameplay;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMap(3);
            // Speeds up some tests by multiple seconds. Which does beg some questions that I don't want to find the answer to right now lmao
            PersistentUI.Instance.EnableTransitions = false;
            yield return OnMapLoaded();
        }

        [SetUp]
        public void SetUpEditorMode()
        {
            // Normalize callbacks, focus routing, and devices together before every case so input cannot cross fixture boundaries.
            TestUtils.ResetSharedInputState();

            // Restore the shared metadata and its map together so a prior test cannot leave mismatched BPM conversion state behind.
            TestUtils.ResetSharedMapState();

            // Establish a deterministic tab before each test so editor mode cannot leak from a preceding fixture.
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            if (editModeContext != null)
                editModeContext.EditingMode = InitialEditingMode;

            // Paste and action tests share one loaded map, so reset playback and its cursor to prevent a prior fixture's beat from becoming their paste anchor.
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                if (atsc.IsPlaying)
                {
                    atsc.CancelPlaying();
                }

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
            OnReturnSettings();
            TestUtils.ReturnSettings();
        }

        protected virtual void OnReturnSettings()
        {
        }

        [TearDown]
        public void CleanupAfterTest()
        {
            SelectionController.DeselectAll();
            BeforeCleanup();
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupObjects();
            AfterCleanup();

            // Leave the shared editor in the default tab so tests that do not override their mode start consistently.
            //  Without this, test execution order can change the results of tests who forget to properly set their editor tab (CompositeTest, looking at you :V)
            var editModeContext = Object.FindAnyObjectByType<EditModeContext>();
            if (editModeContext != null)
                editModeContext.EditingMode = EditingMode.Gameplay;
        }

        protected virtual void BeforeCleanup()
        {
        }

        protected virtual void AfterCleanup()
        {
        }
    }
}
