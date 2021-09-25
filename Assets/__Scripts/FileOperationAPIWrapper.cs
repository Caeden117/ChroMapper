﻿using System;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
///     For talking to native code for soft-deleting files
///     The windows code here is from https://stackoverflow.com/questions/3282418/send-a-file-to-the-recycle-bin
/// </summary>
public class FileOperationAPIWrapper
{
    /// <summary>
    ///     Possible flags for the SHFileOperation method.
    /// </summary>
    [Flags]
    public enum FileOperationFlags : ushort
    {
        /// <summary>
        ///     Do not show a dialog during the process
        /// </summary>
        FOF_SILENT = 0x0004,

        /// <summary>
        ///     Do not ask the user to confirm selection
        /// </summary>
        FOF_NOCONFIRMATION = 0x0010,

        /// <summary>
        ///     Delete the file to the recycle bin.  (Required flag to send a file to the bin
        /// </summary>
        FOF_ALLOWUNDO = 0x0040,

        /// <summary>
        ///     Do not show the names of the files or folders that are being recycled.
        /// </summary>
        FOF_SIMPLEPROGRESS = 0x0100,

        /// <summary>
        ///     Surpress errors, if any occur during the process.
        /// </summary>
        FOF_NOERRORUI = 0x0400,

        /// <summary>
        ///     Warn if files are too big to fit in the recycle bin and will need
        ///     to be deleted completely.
        /// </summary>
        FOF_WANTNUKEWARNING = 0x4000
    }

    /// <summary>
    ///     File Operation Function Type for SHFileOperation
    /// </summary>
    public enum FileOperationType : uint
    {
        /// <summary>
        ///     Move the objects
        /// </summary>
        FO_MOVE = 0x0001,

        /// <summary>
        ///     Copy the objects
        /// </summary>
        FO_COPY = 0x0002,

        /// <summary>
        ///     Delete (or recycle) the objects
        /// </summary>
        FO_DELETE = 0x0003,

        /// <summary>
        ///     Rename the object(s)
        /// </summary>
        FO_RENAME = 0x0004
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
        Justification = "Native struct? Should probably not be messed with.")]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

    [DllImport("/System/Library/Frameworks/CoreServices.framework/CoreServices", CharSet = CharSet.Auto)]
    private static extern int FSPathMoveObjectToTrashSync(string sourcePath, out string targetPath, uint options);

    /// <summary>
    ///     Send file to recycle bin
    /// </summary>
    /// <param name="path">Location of directory or file to recycle</param>
    /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
    public static bool Send(string path, FileOperationFlags flags)
    {
        try
        {
            var fs = new SHFILEOPSTRUCT
            {
                wFunc = FileOperationType.FO_DELETE,
                pFrom = path + '\0' + '\0',
                fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
            };
            SHFileOperation(ref fs);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     Send file to recycle bin.  Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
    /// </summary>
    /// <param name="path">Location of directory or file to recycle</param>
    public static bool Send(string path) =>
        Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);

    /// <summary>
    ///     Send file silently to recycle bin.  Surpress dialog, surpress errors, delete if too large.
    /// </summary>
    /// <param name="path">Location of directory or file to recycle</param>
    public static void MoveToRecycleBin(string path)
    {
        if (!TryWindows(path) && !TryMac(path)) File.Delete(path);
    }

    private static bool TryMac(string path)
    {
        try
        {
            return FSPathMoveObjectToTrashSync(path, out var trashPath, 0) == 0;
        }
        catch { }

        return false;
    }

    private static bool TryWindows(string path)
    {
        try
        {
            return Send(path,
                FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI |
                FileOperationFlags.FOF_SILENT);
        }
        catch { }

        return false;
    }

    private static bool DeleteFile(string path, FileOperationFlags flags)
    {
        try
        {
            var fs = new SHFILEOPSTRUCT
            {
                wFunc = FileOperationType.FO_DELETE,
                pFrom = path + '\0' + '\0',
                fFlags = flags
            };
            SHFileOperation(ref fs);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool DeleteCompletelySilent(string path) =>
        DeleteFile(path,
            FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI |
            FileOperationFlags.FOF_SILENT);


    /// <summary>
    ///     SHFILEOPSTRUCT for SHFileOperation from COM
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles",
        Justification = "Native struct? Should probably not be messed with.")]
    private struct SHFILEOPSTRUCT
    {
        public readonly IntPtr hwnd;
        [MarshalAs(UnmanagedType.U4)] public FileOperationType wFunc;
        public string pFrom;
        public readonly string pTo;
        public FileOperationFlags fFlags;
        [MarshalAs(UnmanagedType.Bool)] public readonly bool fAnyOperationsAborted;
        public readonly IntPtr hNameMappings;
        public readonly string lpszProgressTitle;
    }
}
