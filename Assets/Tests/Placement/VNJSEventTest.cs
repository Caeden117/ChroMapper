using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    // V3 VNJS placement must expose its lane and obtain explicit BeatToTheFuture consent before creating the first event.
    public class VNJSEventTest : TestBase
    {
        private const string BeatToTheFutureRequirement = "BeatToTheFuture";
        private HashSet<int> dialogIdsAtTestStart;

        // Each confirmation case starts without the requirement or an event so it exercises first-node behavior.
        [SetUp]
        public void ResetBeatToTheFutureRequirement()
        {
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements.RemoveAll(
                requirement => requirement == BeatToTheFutureRequirement);
            BeatSaberSongContainer.Instance.Map.SaveVNJSEventsInV3 = true;
            // Batch fixtures can leak unrelated modals, so record the preexisting set before this test opens anything.
            dialogIdsAtTestStart = GetActiveDialogs().Select(dialog => dialog.GetInstanceID()).ToHashSet();
        }

        // V3 maps use V2 Info.dat metadata, so lane visibility must follow the difficulty version instead of Info.dat.
        [Test]
        public void V3MapDisplaysVNJSEventLane()
        {
            var song = BeatSaberSongContainer.Instance;
            var placement = GetNJSEventPlacement();
            var gridLane = GetPrivateField<GridLane>(placement, "gridLane");

            Assert.That(song.Map.MajorVersion, Is.EqualTo(3), "The test fixture must load a V3 difficulty.");
            Assert.That(song.Info.MajorVersion, Is.EqualTo(2), "The fixture must reproduce V3 maps with V2 Info.dat.");
            // Sequential assertions support ChroMapper's NUnit version while preserving both lane-state checks.
            Assert.That(
                placement.gameObject.activeInHierarchy,
                Is.True,
                "The VNJS placement lane was disabled for a V3 difficulty.");
            Assert.That(gridLane.Hide, Is.False, "The VNJS grid lane was hidden for a V3 difficulty.");
        }

        // The right-hand No choice must leave both metadata and beatmap data untouched.
        [UnityTest]
        public IEnumerator FirstV3VNJSEventBeatToTheFuturePromptNoOnRightDoesNotPlaceEvent()
        {
            InvokeFinalPlacement();

            // TextComponent applies its configured prompt body during Start, so inspect it after Unity initializes the clone.
            yield return null;
            var prompt = GetBeatToTheFutureRequirementPrompt();
            ClickFooterButton(prompt, 1);

            // Sequential assertions keep the cancellation contract compatible with the project's NUnit API.
            Assert.That(
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements,
                Does.Not.Contain(BeatToTheFutureRequirement));
            Assert.That(
                GetNJSEventPlacement().ObjectContainerCollection.MapObjects,
                Is.Empty,
                "Declining the BeatToTheFuture requirement still placed the first V3 VNJS event.");
        }

        // The left-hand Yes choice must add BeatToTheFuture and then finish the deferred placement exactly once.
        [UnityTest]
        public IEnumerator FirstV3VNJSEventBeatToTheFuturePromptYesOnLeftAddsRequirementAndPlacesEvent()
        {
            InvokeFinalPlacement();

            // TextComponent applies its configured prompt body during Start, so inspect it after Unity initializes the clone.
            yield return null;
            var prompt = GetBeatToTheFutureRequirementPrompt();
            ClickFooterButton(prompt, 0);

            // Sequential assertions keep the acceptance contract compatible with the project's NUnit API.
            Assert.That(
                BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements,
                Does.Contain(BeatToTheFutureRequirement));
            Assert.That(
                GetNJSEventPlacement().ObjectContainerCollection.MapObjects,
                Has.Count.EqualTo(1),
                "Accepting the BeatToTheFuture requirement did not finish the first V3 VNJS placement.");
        }

        // Existing BeatToTheFuture metadata already provides consent, so later placement must not open another prompt.
        [Test]
        public void V3VNJSEventWithBeatToTheFutureRequirementPlacesWithoutPrompt()
        {
            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements.Add(BeatToTheFutureRequirement);

            InvokeFinalPlacement();

            // Sequential assertions keep the existing-requirement path compatible with the project's NUnit API.
            Assert.That(GetDialogsOpenedByTest(), Is.Empty, "VNJS placement prompted despite the existing requirement.");
            Assert.That(GetNJSEventPlacement().ObjectContainerCollection.MapObjects, Has.Count.EqualTo(1));
        }

        // Failed prompt assertions must not leak modal input state or test-owned metadata into the shared mapper scene.
        protected override void BeforeCleanup()
        {
            // Close only dialogs created by this test so cleanup cannot mutate another batch fixture's modal state.
            foreach (var dialog in GetDialogsOpenedByTest())
            {
                dialog.Close();
            }

            BeatSaberSongContainer.Instance.MapDifficultyInfo.CustomRequirements.RemoveAll(
                requirement => requirement == BeatToTheFutureRequirement);
        }

        // Invoke the production callback reached after the NJS-value dialog accepts valid input.
        private static void InvokeFinalPlacement()
        {
            var placement = GetNJSEventPlacement();
            placement.QueuedData = new BaseNJSEvent { JsonTime = 1 };

            var attemptPlacement = typeof(NJSEventPlacement).GetMethod(
                "AttemptPlaceNJSChange",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(attemptPlacement, Is.Not.Null);
            attemptPlacement.Invoke(placement, new object[] { "10", 1, false });
        }

        // A deferred first placement must leave one BeatToTheFuture-labelled Yes/No dialog and no map event.
        private DialogBox GetBeatToTheFutureRequirementPrompt()
        {
            var dialogs = GetDialogsOpenedByTest()
                .Where(dialog => GetDialogText(dialog).Contains(BeatToTheFutureRequirement))
                .ToArray();
            Assert.That(dialogs, Has.Length.EqualTo(1), "Placing the first V3 VNJS event did not open one prompt.");

            var prompt = dialogs[0];
            var promptText = GetDialogText(prompt);
            // Sequential assertions keep prompt shape and deferred-placement checks compatible with the project's NUnit API.
            Assert.That(promptText, Does.Contain(BeatToTheFutureRequirement));
            Assert.That(prompt.GetComponentsInChildren<ButtonComponent>(), Has.Length.EqualTo(2));
            Assert.That(
                GetNJSEventPlacement().ObjectContainerCollection.MapObjects,
                Is.Empty,
                "The first V3 VNJS event was placed before the user accepted BeatToTheFuture.");
            return prompt;
        }

        // Footer child order is the dialog's visual left-to-right order, allowing the tests to prove Yes-left/No-right behavior.
        private static void ClickFooterButton(DialogBox dialog, int buttonIndex)
        {
            var buttons = dialog.GetComponentsInChildren<ButtonComponent>();
            Assert.That(buttonIndex, Is.LessThan(buttons.Length));

            var callbackField = typeof(ButtonComponent).GetField(
                "onClick",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(callbackField, Is.Not.Null);

            var callback = callbackField.GetValue(buttons[buttonIndex]) as Action;
            Assert.That(callback, Is.Not.Null);
            callback.Invoke();
        }

        // Include inactive scene objects because the unfixed V3 regression disables this placement during Start.
        private static NJSEventPlacement GetNJSEventPlacement() =>
            UnityEngine.Object.FindObjectsByType<NJSEventPlacement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single();

        // Reflection is limited to serialized UI state that NJSEventPlacement does not expose publicly.
        private static T GetPrivateField<T>(NJSEventPlacement placement, string fieldName)
        {
            var field = typeof(NJSEventPlacement).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(placement);
        }

        // Only active scene dialogs represent prompts; closed instances can remain alive until Unity's frame-end destroy pass.
        private static DialogBox[] GetActiveDialogs() =>
            UnityEngine.Object.FindObjectsByType<DialogBox>(FindObjectsSortMode.None);

        // Batch isolation treats only active dialogs absent at SetUp as products of the current test.
        private DialogBox[] GetDialogsOpenedByTest() =>
            GetActiveDialogs()
                .Where(dialog => !dialogIdsAtTestStart.Contains(dialog.GetInstanceID()))
                .ToArray();

        // Prompt identity comes from rendered text after the UnityTest frame initializes TextComponent.
        private static string GetDialogText(DialogBox dialog) =>
            string.Join("\n", dialog.GetComponentsInChildren<TMP_Text>().Select(text => text.text));
    }
}
