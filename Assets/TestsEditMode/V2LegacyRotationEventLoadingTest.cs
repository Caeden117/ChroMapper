using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Helper;
using Beatmap.Info;
using Beatmap.V2;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestsEditMode
{
    public class V2LegacyRotationEventLoadingTest
    {
        private const string ReportedDifficultyJson = @"
        {
            ""_version"": ""2.0.0"",
            ""_events"": [
                { ""_time"": 6.82600021362305, ""_type"": 14, ""_value"": 108000 }
            ]
        }";

        private const string ReportedRotationEventJson = @"
        {
            ""_time"": 6.82600021362305,
            ""_type"": 14,
            ""_value"": 108000
        }";

        // AllMapsInDefaultSongLocationsLoadWithoutExceptions found that Lost Painting's legacy value indexes one past
        // BaseRotationEvent's lookup array, so preserve the exact first offending event as the smallest parser fixture.
        [Test]
        public void LegacyHighValueRotationEventDoesNotThrowWhileParsing()
        {
            BaseRotationEvent rotationEvent = null;
            Assert.That(
                () => rotationEvent = V2RotationEvent.GetFromJson(JSON.Parse(ReportedRotationEventJson)),
                Throws.Nothing);
            Assert.That(rotationEvent.Rotation, Is.EqualTo(60f));
            Assert.That(rotationEvent.Value, Is.EqualTo(7));
        }

        // V2Difficulty currently swallows the bounds exception and returns null, so assert both that no non-terminating
        // exception log is emitted and that the reported map remains available after parsing.
        [Test]
        public void DifficultyWithLegacyHighValueRotationEventLoadsWithoutExceptionLog()
        {
            var result = CaptureLoad(() => V2Difficulty.GetFromJson(JSON.Parse(ReportedDifficultyJson), "Hard.dat"));

            Assert.That(result.ErrorLogs, Is.Empty, "Difficulty parsing emitted exception/error logs.");
            Assert.That(result.ThrownException, Is.Null, "Difficulty parsing threw an exception.");
            Assert.That(result.Difficulty, Is.Not.Null, "Difficulty parsing returned null.");
        }

        // BeatmapFactory dereferences the null returned by V2Difficulty after the swallowed exception, so cover the
        // secondary NullReferenceException seen for both Lost Painting difficulties through the production factory path.
        [Test]
        public void FactoryLoadsDifficultyWithLegacyHighValueRotationEventWithoutCascadingException()
        {
            var info = new BaseInfo { BeatsPerMinute = 116f };
            var difficultySet = new InfoDifficultySet { Characteristic = "Standard" };
            var infoDifficulty = new InfoDifficulty(difficultySet) { Difficulty = "Hard" };
            var result = CaptureLoad(() => BeatmapFactory.GetDifficultyFromJson(
                JSON.Parse(ReportedDifficultyJson),
                "Hard.dat",
                info,
                infoDifficulty));

            Assert.That(result.ThrownException, Is.Null, "Factory loading threw a cascading exception.");
            Assert.That(result.ErrorLogs, Is.Empty, "Factory loading emitted exception/error logs.");
            Assert.That(result.Difficulty, Is.Not.Null, "Factory loading returned null.");
        }

        // Capturing Unity errors converts V2Difficulty's swallowed Debug.LogException into a deterministic assertion
        // while still allowing each focused test to report the returned value and any cascading thrown exception.
        private static (BaseDifficulty Difficulty, Exception ThrownException, string[] ErrorLogs) CaptureLoad(
            Func<BaseDifficulty> load)
        {
            var originalIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            var errorLogs = new List<string>();
            BaseDifficulty difficulty = null;
            Exception thrownException = null;

            void HandleLogMessage(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errorLogs.Add($"{type}: {condition}\n{stackTrace}");
                }
            }

            try
            {
                LogAssert.ignoreFailingMessages = true;
                Application.logMessageReceived += HandleLogMessage;
                try
                {
                    difficulty = load();
                }
                catch (Exception exception)
                {
                    thrownException = exception;
                }
            }
            finally
            {
                Application.logMessageReceived -= HandleLogMessage;
                LogAssert.ignoreFailingMessages = originalIgnoreFailingMessages;
            }

            return (difficulty, thrownException, errorLogs.ToArray());
        }
    }
}
