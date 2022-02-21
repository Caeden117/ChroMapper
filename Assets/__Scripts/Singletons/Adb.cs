using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;


namespace QuestDumper
{
    public readonly struct AdbOutput
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
        private const string PLATFORM_TOOLS_DOWNLOAD_GENERIC =
            "https://dl.google.com/android/repository/platform-tools-latest-";

        private static Process _process;

        private static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            fileName = Path.GetFileName(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");

            return values?.Split(Path.PathSeparator).Select(path => Path.Combine(path, fileName)).FirstOrDefault(File.Exists);
        }

        private static string GetADBUrl()
        {
#if UNITY_STANDALONE_WIN
            return PLATFORM_TOOLS_DOWNLOAD_GENERIC + "windows.zip";
#elif UNITY_STANDALONE_OSX
            return PLATFORM_TOOLS_DOWNLOAD_GENERIC + "darwin.zip";
#elif UNITY_STANDALONE_LINUX
            return PLATFORM_TOOLS_DOWNLOAD_GENERIC + "linux.zip";
            // Obviously Android is required
#elif PLATFORM_ANDROID
            return PLATFORM_TOOLS_DOWNLOAD_GENERIC + "linux.zip";
#endif
            throw new InvalidOperationException("How could this even happen?");
        }

        private static bool IsWindows => Application.platform == RuntimePlatform.WindowsPlayer ||
                                                 Application.platform == RuntimePlatform.WindowsEditor;
        // TODO: Lazy init?
        private static Lazy<string> ExtractAdbPath = new Lazy<string>(() => Settings.AndroidPlatformTools);
        private static Lazy<string> ChroMapperAdbPath = new Lazy<string>(() => Path.Combine(ExtractAdbPath.Value, "platform-tools", "adb" + (IsWindows ? ".exe" : "")));

        public static IEnumerator DownloadADB([CanBeNull] Action<UnityWebRequest> onSuccess, [CanBeNull] Action<UnityWebRequest, Exception> onError, Action<UnityWebRequest, bool> progressUpdate)
        {
            // We will extract the contents of the zip to the temp directory, so we will save the zip in memory.
            var downloadHandler = new DownloadHandlerBuffer();

            using var www = UnityWebRequest.Get(GetADBUrl());
            www.downloadHandler = downloadHandler;

            var request = www.SendWebRequest();

            while (!request.isDone)
            {
                progressUpdate?.Invoke(www, false);
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                onError?.Invoke(www, null);
                yield break;
            }

            // Wahoo! We are done. Let's grab our downloaded data.
            var downloaded = downloadHandler.data;

            if (downloaded == null) yield break;

            yield return new WaitForEndOfFrame();

            progressUpdate?.Invoke(www, true);

            // FOR GOD SAKES UNITY YOU CAN'T EVEN HAVE APPLICATION.DATAPATH ON A TASK CALLED? REALLY? 
            var extractPath = ExtractAdbPath.Value!;

            var task = Task.Run(() =>
            {

                // Slap our downloaded bytes into a memory stream and slap that into a ZipArchive.
                var stream = new MemoryStream(downloaded);
                var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                // Create the directory for our song to go to.
                // Path.GetTempPath() should be compatible with Windows and UNIX.
                // See Microsoft docs on it.

                if (!Directory.Exists(extractPath))
                    Assert.IsTrue(Directory.CreateDirectory(extractPath).Exists);

                // Extract our zipped file into this directory.
                archive.ExtractToDirectory(extractPath);

                // Dispose our downloaded bytes, we don't need them.
                downloadHandler.Dispose();
            });

            while (!task.IsCompleted)
                yield return null;

            onSuccess?.Invoke(www);
        }

        public static IEnumerator RemoveADB()
        {
            var adbPath = ChroMapperAdbPath.Value;
            var adbFolder = Path.GetDirectoryName(adbPath)!;
            if (!File.Exists(adbPath) && !Directory.Exists(adbFolder)) yield break;
            
            Initialize();

            // Don't block main thread
            yield return Task.Run(async () =>
            {
                await KillServer();
                await Dispose();
                

                Directory.Delete(adbFolder, true);
            }).AsCoroutine();
        }

        
        public static bool IsAdbInstalled([CanBeNull] out string adbPath)
        {
            adbPath = GetFullPath(ChroMapperAdbPath.Value);

            return adbPath != null;
        }

        public static void Initialize()
        {
            string adbPath = null; // stupid Unity
            Assert.IsTrue(IsAdbInstalled(out adbPath) && adbPath != null,
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
            if (_process == null)
                return;

            try
            {
                if (milliseconds != 0 && !_process.HasExited)
                {
                    await Task.Run(() => { _process.WaitForExit(milliseconds); });
                }
            } catch (InvalidOperationException) {}

            _process.Dispose();
            _process = null;
        }
        
        // surrounds the string as "\"{s}\""
        private static string EscapeStringFix(string s) => $"\"\\\"{s}\\\"";

        private static Task<AdbOutput> RunADBCommand() =>
            Task.Run( () =>
            {
                _process.Start();

                var standardOutputBuilder = new StringBuilder();
                var errorOutputBuilder = new StringBuilder();

                _process.OutputDataReceived += (_, args) =>
                {
                    if (!(args.Data is null)) { standardOutputBuilder.AppendLine(args.Data); }
                };

                _process.ErrorDataReceived += (_, args) =>
                {
                    if (!(args.Data is null)) { errorOutputBuilder.AppendLine(args.Data); }
                };

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                _process.WaitForExit();

                _process.CancelOutputRead();
                _process.CancelErrorRead();

                return new AdbOutput(standardOutputBuilder.Replace("\r\n", "\n").ToString().Trim(), errorOutputBuilder.Replace("\r\n","\n").ToString().Trim());
            });

        /// <summary>
        /// Checks if the device is a Quest device.
        /// </summary>
        /// <param name="device">The device to check</param>
        /// <returns>True if Oculus is the manufacturer, may be naive in the future</returns>
        public static async Task<(bool, AdbOutput)> IsQuest(string device)
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"-s {device} shell getprop ro.product.manufacturer";

            var ret = await RunADBCommand();

            return (ret.StdOut.Contains("Oculus"), ret);
        }
        
        /// <summary>
        /// Kills the ADB server
        /// </summary>
        /// <returns>ADB output</returns>
        public static async Task<AdbOutput> KillServer()
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"kill-server";

            var ret = await RunADBCommand();

            return ret;
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

            var ret = await RunADBCommand();

            return (ret.StdOut, ret);
        }

        /// <summary>
        /// Gets the list of devices connected
        /// </summary>
        /// <returns>List of devices</returns>
        public static async Task<(List<string>, AdbOutput)> GetDevices()
        {
            ValidateADB();

            _process.StartInfo.Arguments = $"devices";

            var ret = await RunADBCommand();

            // Quick return
            const string requiredString = "List of devices attached\n";
            if (!string.IsNullOrEmpty(ret.ErrorOut))
                return (null, ret);

            if (!ret.StdOut.StartsWith(requiredString))
                return (new List<string>(), new AdbOutput(ret.StdOut, ret.ErrorOut));

            var devicesConnectedStr = ret.StdOut.Substring(requiredString.Length);
            var connectedDevices = devicesConnectedStr
                .Split('\n')
                .Select(s => s.Substring(0, s.IndexOf("\t", StringComparison.Ordinal)).Replace("\n","").Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            return (connectedDevices, ret);
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

            _process.StartInfo.Arguments = $"-s {serial} shell mkdir {EscapeStringFix(devicePath)} {makeParentsFlag} -m {permission}";

            return await RunADBCommand();
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

            _process.StartInfo.Arguments = $"-s {serial} push \"{localPath}\" \"{devicePath}\"";

            return await RunADBCommand();
        }

        /// <summary>
        /// Copies files from devicePath to localPath
        /// </summary>
        /// <param name="devicePath">Files to copy from Android device</param>
        /// <param name="localPath">Files to copy to local machine</param>
        public static async Task<AdbOutput> Pull(string devicePath, string localPath, string serial)
        {
            ValidateADB();
            _process.StartInfo.Arguments = $"-s {serial} pull \"{devicePath}\" \"{localPath}\"";

            return await RunADBCommand();
        }
    }
}
