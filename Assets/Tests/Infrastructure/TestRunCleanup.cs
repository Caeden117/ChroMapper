using System;
using System.IO;
using NUnit.Framework;

// A fixture without a namespace wraps the entire test assembly.
[SetUpFixture]
public class TestRunCleanup
{
    [OneTimeTearDown]
    public void DeleteGeneratedTestInstallation()
    {
        var temporaryRoot = PathUtils.Combine(Path.GetFullPath(Path.GetTempPath()), "ChroMapper Tests") + "/";
        var installation = PathUtils.Combine(Path.GetFullPath(Settings.TestRunnerInstallationDirectory));
        if (!installation.StartsWith(temporaryRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("The generated test installation must remain inside its temporary directory.");

        if (Directory.Exists(installation))
            Directory.Delete(installation, true);
    }
}
