using System;
using System.Runtime.InteropServices;
using System.Threading;

// Design references for this raw-input worker and its companion workaround:
// Peter "Durante" Thoman (PH3), message-only input threads and legacy-message overhead:
// https://ph3at.github.io/posts/Windows-Input/
// lorddesu, Unity RIDEV_NOLEGACY workaround and mouse-state repair:
// https://discussions.unity.com/t/performance-degradation-while-using-mouse-with-high-polling-rate-any-known-engine-version-without-the-problem/1508487/28
// Win32 packet layouts and message handling are documented by Microsoft:
// https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawmouse
// https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-input
//
// Unity consumes its thread's raw-input buffer before managed input callbacks. Because of that, we have to keep the mouse
// registration on a separate message thread so wheel and locked-cursor (aka right click camera panning) motion remain available,
// otherwise scroll wheel and right click pan do nothing.
internal sealed class WindowsRawMouseReader : IDisposable
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private const uint RawInputMessage = 0x00FF;
    private const uint CloseMessage = 0x0010;
    private static readonly WindowProcedure Procedure = HandleMessage;
    private static readonly uint PacketSize = (uint)Marshal.SizeOf<RawInput>();
    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<RawInputHeader>();
    private static WindowsRawMouseReader current;
    private readonly ManualResetEventSlim ready = new(false);
    private readonly object sync = new();
    private Thread thread;
    private IntPtr window;
    private int deltaX;
    private int deltaY;
    private int wheelX;
    private int wheelY;
    private Exception startupError;

    internal IntPtr Window => window;

    internal void Start()
    {
        current = this;
        thread = new Thread(Run) { IsBackground = true, Name = "CM raw mouse input" };
        thread.Start();
        ready.Wait();
        if (startupError != null)
            throw new InvalidOperationException("Could not create the raw mouse receiver.", startupError);
    }

    internal void Take(out int x, out int y, out int horizontalWheel, out int verticalWheel)
    {
        lock (sync)
        {
            x = deltaX;
            y = deltaY;
            horizontalWheel = wheelX;
            verticalWheel = wheelY;
            deltaX = deltaY = wheelX = wheelY = 0;
        }
    }

    private void Run()
    {
        var instance = GetModuleHandle(null);
        var className = "CMRawMouse" + GetCurrentThreadId();
        try
        {
            var definition = new WindowClass
            {
                Size = (uint)Marshal.SizeOf<WindowClass>(),
                Procedure = Marshal.GetFunctionPointerForDelegate(Procedure),
                Instance = instance,
                ClassName = className
            };
            if (RegisterClassEx(ref definition) == 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            window = CreateWindowEx(0, className, "", 0, 0, 0, 0, 0,
                new IntPtr(-3), IntPtr.Zero, instance, IntPtr.Zero);
            if (window == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            ready.Set();
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        catch (Exception error)
        {
            startupError = error;
            ready.Set();
        }
        finally
        {
            window = IntPtr.Zero;
            UnregisterClass(className, instance);
        }
    }

    [AOT.MonoPInvokeCallback(typeof(WindowProcedure))]
    private static IntPtr HandleMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == RawInputMessage)
        {
            var size = PacketSize;
            if (GetRawInputData(lParam, 0x10000003, out var input, ref size,
                    HeaderSize) != uint.MaxValue
                && input.Header.Type == 0)
            {
                var reader = current;
                lock (reader.sync)
                {
                    if ((input.Mouse.Flags & 1) == 0)
                    {
                        reader.deltaX += input.Mouse.X;
                        reader.deltaY += input.Mouse.Y;
                    }
                    if ((input.Mouse.ButtonFlags & 0x0400) != 0)
                        reader.wheelY += unchecked((short)input.Mouse.ButtonData);

                    if ((input.Mouse.ButtonFlags & 0x0800) != 0)
                        reader.wheelX += unchecked((short)input.Mouse.ButtonData);
                }
            }
        }
        else if (message == CloseMessage)
        {
            DestroyWindow(handle);
            PostQuitMessage(0);
            return IntPtr.Zero;
        }

        return DefWindowProc(handle, message, wParam, lParam);
    }

    public void Dispose()
    {
        if (window != IntPtr.Zero)
            PostMessage(window, CloseMessage, IntPtr.Zero, IntPtr.Zero);

        thread?.Join();
        ready.Dispose();
        current = null;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WindowProcedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Size;
        public uint Style;
        public IntPtr Procedure;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string MenuName;
        public string ClassName;
        public IntPtr SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window;
        public uint Id;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct RawMouse
    {
        [FieldOffset(0)] public ushort Flags;
        [FieldOffset(4)] public ushort ButtonFlags;
        [FieldOffset(6)] public ushort ButtonData;
        [FieldOffset(12)] public int X;
        [FieldOffset(16)] public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInput
    {
        public RawInputHeader Header;
        public RawMouse Mouse;
    }

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string module);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WindowClass definition);
    [DllImport("user32.dll", EntryPoint = "UnregisterClassW", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(string className, IntPtr instance);
    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint extendedStyle, string className, string name, uint style, int x, int y,
        int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll", EntryPoint = "GetMessageW")]
    private static extern int GetMessage(out Message message, IntPtr window, uint minimum, uint maximum);
    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(
        IntPtr input, uint command, out RawInput data, ref uint size, uint headerSize);
    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr window);
    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);
#else
    public void Dispose() { }
#endif
}
