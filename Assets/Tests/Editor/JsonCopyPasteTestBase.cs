using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Editor
{
    public abstract class JsonCopyPasteTestBase : InputTestBase
    {
        private CMInput clipboardInput;
        private TestTextClipboard textClipboard;
        private Vector2 insideWindowPosition;
        private bool originalClipboardSyncSetting;
        private int originalSongSamples;
        private int originalSongFrequency;

        protected string ExternalClipboardText
        {
            get => textClipboard.Text;
            set => textClipboard.Text = value;
        }

        protected int ExternalClipboardWriteCount => textClipboard.WriteCount;
        protected int ExternalClipboardReadCount => textClipboard.ReadCount;

        [SetUp]
        public void PrepareJsonClipboardWorkflow()
        {
            textClipboard = new TestTextClipboard();
            ClipboardJsonController.SetTextClipboardForTests(textClipboard);
            originalClipboardSyncSetting = Settings.Instance.SyncCopiedObjectsWithSystemClipboard;
            Settings.Instance.SyncCopiedObjectsWithSystemClipboard = true;
            originalSongSamples = BeatSaberSongContainer.Instance.LoadedSongSamples;
            originalSongFrequency = BeatSaberSongContainer.Instance.LoadedSongFrequency;
            SelectionController.CopiedObjects.Clear();

            InitializeVirtualInput(false);
            clipboardInput = new CMInput();
            clipboardInput.Utils.SetCallbacks(Object.FindAnyObjectByType<KeybindsController>());
            clipboardInput.Utils.Enable();

#if UNITY_EDITOR
            var windowSize = UnityEditor.Handles.GetMainGameViewSize();
#else
            var windowSize = new Vector2(Screen.width, Screen.height);
#endif
            Assert.That(windowSize.x, Is.GreaterThan(2f), "The editor window had no usable width.");
            Assert.That(windowSize.y, Is.GreaterThan(2f), "The editor window had no usable height.");
            insideWindowPosition = windowSize * 0.5f;

            // Exercise a real boundary transition so the test never relies on static pointer state from another fixture.
            SetVirtualMouseState(-Vector2.one, Vector2.zero);
            Assert.That(KeybindsController.IsMouseInWindow, Is.False);
            SetVirtualMouseState(insideWindowPosition, Vector2.zero);
            Assert.That(KeybindsController.IsMouseInWindow, Is.True);
        }

        protected void LeaveChroMapperWindow()
        {
            SetVirtualMouseState(-Vector2.one, Vector2.zero);
            Assert.That(KeybindsController.IsMouseInWindow, Is.False);
        }

        protected void ReturnToChroMapperWindow()
        {
            SetVirtualMouseState(insideWindowPosition, Vector2.zero);
            Assert.That(KeybindsController.IsMouseInWindow, Is.True);
        }

        protected void SetAudioMetadata(int sampleCount, int frequency)
        {
            BeatSaberSongContainer.Instance.LoadedSongSamples = sampleCount;
            BeatSaberSongContainer.Instance.LoadedSongFrequency = frequency;
        }

        protected override void AfterCleanup()
        {
            if (clipboardInput != null)
            {
                clipboardInput.Utils.Disable();
                clipboardInput.Dispose();
                clipboardInput = null;
            }

            TearDownVirtualInput();
            ClipboardJsonController.ResetTextClipboardAfterTests();
            Settings.Instance.SyncCopiedObjectsWithSystemClipboard = originalClipboardSyncSetting;
            BeatSaberSongContainer.Instance.LoadedSongSamples = originalSongSamples;
            BeatSaberSongContainer.Instance.LoadedSongFrequency = originalSongFrequency;
            SelectionController.CopiedObjects.Clear();
        }

        private sealed class TestTextClipboard : ITextClipboard
        {
            private string text;

            public string Text
            {
                get
                {
                    ReadCount++;
                    return text;
                }
                set
                {
                    text = value;
                    WriteCount++;
                }
            }

            public int WriteCount { get; private set; }
            public int ReadCount { get; private set; }
        }
    }
}
