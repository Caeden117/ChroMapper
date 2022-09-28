using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
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
        private const string platformToolsDownloadGeneric =
            "https://dl.google.com/android/repository/platform-tools-latest-";

        private static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            fileName = Path.GetFileName(fileName);

            if (!Settings.Instance.IncludePathForADB) return null;

            var values = Environment.GetEnvironmentVariable("PATH");

            return values?.Split(Path.PathSeparator).Select(path => Path.Combine(path, fileName)).FirstOrDefault(File.Exists);
        }

        private static string GetADBUrl()
        {
#if UNITY_STANDALONE_WIN
            return platformToolsDownloadGeneric + "windows.zip";
#elif UNITY_STANDALONE_OSX
            return platformToolsDownloadGeneric + "darwin.zip";
#elif UNITY_STANDALONE_LINUX
            return platformToolsDownloadGeneric + "linux.zip";
            // Obviously Android is required
#elif PLATFORM_ANDROID
            return platformToolsDownloadGeneric + "linux.zip";
#endif
            // ReSharper disable once HeuristicUnreachableCode
#pragma warning disable CS0162
            throw new InvalidOperationException("How could this even happen?");
#pragma warning restore CS0162
        }

        private static bool IsWindows => Application.platform == RuntimePlatform.WindowsPlayer ||
                                                 Application.platform == RuntimePlatform.WindowsEditor;
        
        private static readonly Lazy<string> extractAdbPath = new Lazy<string>(() => Settings.AndroidPlatformTools);
        private static readonly Lazy<string> chroMapperAdbPath = new Lazy<string>(() => Path.Combine(extractAdbPath.Value, "platform-tools", "adb" + (IsWindows ? ".exe" : "")));

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
            var extractPath = extractAdbPath.Value!;

            var task = Task.Run(() =>
            {

                // Slap our downloaded bytes into a memory stream and slap that into a ZipArchive.
                var stream = new MemoryStream(downloaded);
                var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                // Create the directory for our song to go to.
                // Path.GetTempPath() should be compatible with Windows and UNIX.
                // See Microsoft docs on it.
                
                Directory.CreateDirectory(extractPath);

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
            var adbPath = chroMapperAdbPath.Value;
            var adbFolder = Path.GetDirectoryName(adbPath)!;
            if (!File.Exists(adbPath) && !Directory.Exists(adbFolder)) yield break;
            

            // Don't block main thread
            yield return Task.Run(async () =>
            {
                await KillServer();

                Directory.Delete(adbFolder, true);
            }).AsCoroutine();
        }

        
        public static bool IsAdbInstalled([CanBeNull] out string adbPath)
        {
            adbPath = GetFullPath(chroMapperAdbPath.Value);

            return adbPath != null;
        }

        private static Process BuildProcess(string arguments)
        {
            if (!IsAdbInstalled(out var adbPath) || adbPath == null)
                throw new InvalidOperationException($"Could not find {adbPath} in PATH or location on {Environment.OSVersion.Platform}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = adbPath,
                    CreateNoWindow = true,
                    Arguments = arguments
                }
            };

            if (process.StartInfo.FileName != adbPath)
                throw new InvalidOperationException(
                    $"UNITY IS BEING DUMB WHY IS PROCESS USING {process.StartInfo.FileName} INSTEAD OF {adbPath}");

            return process;
        }

        private static bool listeningToShutdown;
        private static void ListenToUnityShutdown()
        {
            if (!listeningToShutdown) return;
            
            listeningToShutdown = true;
            Application.quitting += async () =>
            {
                if (!IsAdbInstalled(out _)) return;

                await KillServer().ConfigureAwait(false);
            };
        }

        // surrounds the string as "\"{s}\""
        private static string EscapeStringFix(string s) => $"\"\\\"{s}\\\"";

        private static Task<AdbOutput> RunADBCommand(string arguments) =>
            Task.Run( () =>
            {
                ListenToUnityShutdown();

                using var process = BuildProcess(arguments);
                
                process.Start();

                var standardOutputBuilder = new StringBuilder();
                var errorOutputBuilder = new StringBuilder();

                process.OutputDataReceived += (_, args) =>
                {
                    if (!(args.Data is null)) { standardOutputBuilder.AppendLine(args.Data); }
                };

                process.ErrorDataReceived += (_, args) =>
                {
                    if (!(args.Data is null)) { errorOutputBuilder.AppendLine(args.Data); }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                process.CancelOutputRead();
                process.CancelErrorRead();

                return new AdbOutput(standardOutputBuilder.Replace("\r\n", "\n").ToString().Trim(), errorOutputBuilder.Replace("\r\n","\n").ToString().Trim());
            });

        /// <summary>
        /// Checks if the device is a Quest device.
        /// </summary>
        /// <param name="device">The device to check</param>
        /// <returns>True if Oculus is the manufacturer, may be naive in the future</returns>
        public static async Task<(bool, AdbOutput)> IsQuest(string device)
        {
            var ret = await RunADBCommand($"-s {device} shell getprop ro.product.manufacturer");

            return (ret.StdOut.Contains("Oculus"), ret);
        }
        
        /// <summary>
        /// Kills the ADB server
        /// </summary>
        /// <returns>ADB output</returns>
        public static async Task<AdbOutput> KillServer()
        {
            var ret = await RunADBCommand("kill-server");

            return ret;
        }

        /// <summary>
        /// Gets the model of the device. In my testing, a Q2 returns "Quest 2"
        /// </summary>
        /// <param name="device"></param>
        /// <returns>The model</returns>
        public static async Task<(string, AdbOutput)> GetModel(string device)
        {
            var ret = await RunADBCommand($"-s {device} shell getprop ro.product.model");

            return (ret.StdOut, ret);
        }

        /// <summary>
        /// Gets the list of devices connected
        /// </summary>
        /// <returns>List of devices</returns>
        public static async Task<(List<string>, AdbOutput)> GetDevices()
        {
            var ret = await RunADBCommand("devices");

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
            var makeParentsFlag = makeParents ? "-p" : "";
            
            return await RunADBCommand($"-s {serial} shell mkdir {EscapeStringFix(devicePath)} {makeParentsFlag} -m {permission}");
        }

        /// <summary>
        /// Copies files from localPath to devicePath
        /// </summary>
        /// <param name="devicePath">Files to copy from Android device</param>
        /// <param name="localPath">Files to copy to local machine</param>
        /// <param name="serial">The device</param>
        public static async Task<AdbOutput> Push(string localPath, string devicePath, string serial) 
            => await RunADBCommand($"-s {serial} push \"{localPath}\" \"{devicePath}\"");

        /// <summary>
        /// Copies files from devicePath to localPath
        /// </summary>
        /// <param name="devicePath">Files to copy from Android device</param>
        /// <param name="localPath">Files to copy to local machine</param>
        /// <param name="serial">device serial</param>
        public static async Task<AdbOutput> Pull(string devicePath, string localPath, string serial) 
            => await RunADBCommand($"-s {serial} pull \"{devicePath}\" \"{localPath}\"");

        public static async Task<AdbOutput> Initialize() => await RunADBCommand($"start-server");
    }
}
