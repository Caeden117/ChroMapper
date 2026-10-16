using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Infrastructure
{
    public class InputTestPolicyTest
    {
        private const string MouseTypeName = "Mouse";
        private const string KeyboardTypeName = "Keyboard";

        // Avoid line comments, matches // Keyboard.current.
        private const string LineCommentPattern = @"//[^\r\n]*";

        // Avoid block comments, matches /* Mouse.current */.
        private const string BlockCommentPattern = @"/\*[\s\S]*?\*/";

        private static readonly Regex Comments = new($"{LineCommentPattern}|{BlockCommentPattern}");
        private static readonly Regex CurrentDevice = new($@"\b(?:{MouseTypeName}|{KeyboardTypeName})\s*\.\s*current\b");

        [Test]
        public void TestSourcesAvoidCurrentDeviceAccess()
        {
            var failures = new List<string>();
            foreach (var directory in new[] { "Tests", "TestsEditMode", "ManualTests" })
            {
                foreach (var path in Directory.GetFiles(
                    PathUtils.Combine(Application.dataPath, directory),
                    "*.cs",
                    SearchOption.AllDirectories))
                {
                    // This file contains forbidden examples used to test the policy.
                    if (Path.GetFileName(path) == $"{nameof(InputTestPolicyTest)}.cs")
                        continue;

                    var source = Comments.Replace(File.ReadAllText(path), " ");
                    foreach (var violation in FindViolations(source))
                        failures.Add($"{path}: {violation}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        #region self tests verifying we'd actually catch the bad examples
        [Test]
        public void PolicyRejectsCurrentDeviceAccess()
        {
            Assert.That(FindViolations("var key = UnityEngine.InputSystem.Keyboard.current;"), Is.Not.Empty);
            Assert.That(FindViolations("var mouse = Mouse . current;"), Is.Not.Empty);
            Assert.That(
                FindViolations(Comments.Replace("// Mouse.current\n/* Keyboard.current */", " ")),
                Is.Empty);
        }

        [Test]
        public void PolicyChecksStringsAsWellAsCode()
        {
            Assert.That(
                FindViolations(Comments.Replace("var name = \"Mouse.current\";", " ")),
                Is.Not.Empty);
            Assert.That(
                FindViolations(Comments.Replace("var name = @\"Keyboard.current\";", " ")),
                Is.Not.Empty);
        }

        [Test]
        public void PolicyAllowsSyntheticInputWithoutRequiringFixtureShape()
        {
            Assert.That(
                FindViolations("InputSystem.QueueStateEvent(virtualKeyboard, state); InputSystem.Update();"),
                Is.Empty,
                "Explicit device input must not require a particular base class or field declaration.");
        }
        #endregion self tests

        private static IEnumerable<string> FindViolations(string source)
        {
            if (CurrentDevice.IsMatch(source))
                yield return "Use a dedicated virtual Mouse/Keyboard reference instead of .current.";
        }
    }
}
