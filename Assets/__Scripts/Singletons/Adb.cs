using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;


namespace QuestDumper
{
    public struct AdbOutput
    {
        public readonly string StdOut;
        public readonly string ErrorOut;

        public AdbOutput(string stdOut, string errorOut)
        {
            StdOut = stdOut;
            ErrorOut = errorOut;
        }

        public override string ToString()
        {
            return $"{nameof(StdOut)}: {StdOut}, {nameof(ErrorOut)}: {ErrorOut}";
        }
    }

    /// <summary>
    /// This code was generously given by Cyuubi#4701
    /// Much appreciated :)
    /// </summary>
    public static class Adb
    {

        private static Process _process;

        private static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        private static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");

            return values?.Split(Path.PathSeparator).Select(path => Path.Combine(path, fileName)).FirstOrDefault(File.Exists);
        }

        public static void Initialize(string adbPath = "adb.exe")
        {
            Assert.IsTrue(ExistsOnPath(adbPath),
                $"Could not find {adbPath} in PATH or location on ${Environment.OSVersion.Platform}");

            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = adbPath,
                    CreateNoWindow = true
                }
            };
        }

        private static void ValidateADB() => Assert.IsNotNull(_process, "ADB has not been instantiated. Start with Initialize(path)");

        /// <summary>
        /// Clears the ADB process
        ///
        /// <param name="milliseconds">If -1, wait indefinitely. If 0, don't wait.</param>
        /// </summary>
        public static async Task Dispose(int milliseconds = -1)
        {
            if (milliseconds != 0 && !_process.HasExited)
            {
                await Task.Run(() => { _process.WaitForExit(milliseconds); });
            }

            _process.Dispose();
            _process = null;
        }

        /// <summary>
        /// Checks if the device is a Quest device.
        /// </summary>
        /// <param name="device">The device to check</param>
        /// <returns>True if Oculus is the manufacturer, may be naive in the future</returns>
        public static async Task<(bool, AdbOutput)> IsQuest(string device)
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"-s {device} shell getprop ro.product.manufacturer";

            return await Task.Run(() =>
            {
                _process.Start();

                var value = _process.StandardOutput.ReadToEnd().Trim();
                var error = _process.StandardError.ReadToEnd().Trim();
                _process.WaitForExit();


                return (value.Contains("Oculus"), new AdbOutput(value, error));
            });
        }

        /// <summary>
        /// Gets the model of the device. In my testing, a Q2 returns "Quest 2"
        /// </summary>
        /// <param name="device"></param>
        /// <returns>The model</returns>
        public static async Task<(string, AdbOutput)> GetModel(string device)
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"-s {device} shell getprop ro.product.model";

            return await Task.Run(() =>
            {
                _process.Start();

                var value = _process.StandardOutput.ReadToEnd().Trim();
                var error = _process.StandardError.ReadToEnd().Trim();
                _process.WaitForExit();


                return (value, new AdbOutput(value, error));
            });
        }

        /// <summary>
        /// Gets the list of devices connected
        /// </summary>
        /// <returns>List of devices</returns>
        public static async Task<(List<string>, AdbOutput)> GetDevices()
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"devices";

            return await Task.Run(() =>
            {
                _process.Start();

                var value = _process.StandardOutput.ReadToEnd().Trim().Replace("\r\n","\n");
                var error = _process.StandardError.ReadToEnd().Trim();
                _process.WaitForExit();

                // Quick return
                const string requiredString = "List of devices attached\n";
                if (!string.IsNullOrEmpty(error))
                    return (null, new AdbOutput(value, error));

                if (!value.StartsWith(requiredString))
                    return (new List<string>(), new AdbOutput(value, error));

                var devicesConnectedStr = value.Substring(requiredString.Length);
                var connectedDevices = devicesConnectedStr
                    .Split('\n')
                    .Select(s => s.Substring(0, s.IndexOf("\t", StringComparison.Ordinal)).Replace("\n","").Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                return (connectedDevices, new AdbOutput(value, error));
            });
        }

        /// <summary>
        /// Creates a folder on the device
        /// </summary>
        /// <param name="devicePath">Path for folder</param>
        /// <param name="makeParents">Make parent folders if needed</param>
        /// <param name="permission">the permission for the folder</param>
        /// <param name="serial">The device</param>
        public static async Task<AdbOutput> Mkdir(string devicePath, string serial, bool makeParents = true, string permission = "770")
        {
            ValidateADB();

            string makeParentsFlag = makeParents ? "-p" : "";

            _process.StartInfo.Arguments = $"-s {serial} shell mkdir {devicePath} {makeParentsFlag} -m {permission}";

            return await Task.Run(() =>
            {
                _process.Start();

                var value = _process.StandardOutput.ReadToEnd().Trim();
                var error = _process.StandardError.ReadToEnd().Trim();
                _process.WaitForExit();

                return new AdbOutput(value, error);
            });
        }

        /// <summary>
        /// Copies files from localPath to devicePath
        /// </summary>
        /// <param name="devicePath">Files to copy from Android device</param>
        /// <param name="localPath">Files to copy to local machine</param>
        /// <param name="serial">The device</param>
        public static async Task<AdbOutput> Push(string localPath, string devicePath, string serial)
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"-s {serial} push \"{localPath}\" {devicePath}";

            return await Task.Run(() =>
            {
                _process.Start();

                var value = _process.StandardOutput.ReadToEnd().Trim();
                var error = _process.StandardError.ReadToEnd().Trim();
                _process.WaitForExit();

                return new AdbOutput(value, error);
            });
        }

        /// <summary>
        /// Copies files from devicePath to localPath
        /// </summary>
        /// <param name="devicePath">Files to copy from Android device</param>
        /// <param name="localPath">Files to copy to local machine</param>
        public static async Task<AdbOutput> Pull(string devicePath, string localPath, string serial)
        {
            ValidateADB();
            _process.StartInfo.Arguments = $"-s {serial} pull {devicePath} \"{localPath}\"";

            return await Task.Run(() =>
            {
                _process.Start();

                var value = _process.StandardOutput.ReadToEnd().Trim();
                var error = _process.StandardError.ReadToEnd().Trim();

                _process.WaitForExit();

                return new AdbOutput(value, error);
            });
        }
    }
}
