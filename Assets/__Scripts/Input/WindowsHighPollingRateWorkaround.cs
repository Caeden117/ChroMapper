using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Windows high-polling-rate workaround adapted from lorddesu's state-repair follow-up:
// https://discussions.unity.com/t/performance-degradation-while-using-mouse-with-high-polling-rate-any-known-engine-version-without-the-problem/1508487/28
// Legacy-input analysis by Peter "Durante" Thoman (PH3):
// https://ph3at.github.io/posts/Windows-Input/
//
// RIDEV_NOLEGACY suppresses Windows' legacy mouse-message generation. The mouse registration
// targets WindowsRawMouseReader's message thread, which collects actual wheel and relative-motion
// reports instead of assuming Unity's pointer-message backend still supplies those fields.
//
// Suppressing legacy messages also removes mouse-position/button updates needed by the UI,
// so registration must be paired with complete mouse-state delivery. It runs before dynamic input
// processing. Native mouse snapshots are ignored only during suppression, so they cannot cancel
// a held button or overwrite the collected motion/wheel data. Normal registration is restored
// outside the client area and on focus loss so Windows handles window controls and activation.
// Mouse consumers must use the new Input System. Legacy UnityEngine.Input mouse reads, including
// those in plugins, do not receive this replacement state.
// Polling button levels cannot recover a press/release pair completed between input updates.
// Registration is process-local and does not change Windows mouse settings. 
// Windows standalone builds activate this automatically. Editor builds do not.
// Never install this in the Editor, where it can disable the Editor's own input.
//
// Unity UUM-142550 includes 6000.3.15f1, ChroMapper uses 6000.3.13f1. The tracker lists a
// mitigation in 7000.0.0a2, not a complete fix, and no verified 6.3 fix as researched 2026-10-21:
// https://issuetracker.unity.com/issues/19661/play-mode-framerate-drops-significantly-when-moving-the-mouse-cursor-with-high-polling-rate
// 
// After we update to unity 7, recheck the physical-mouse reproduction , and see if the mouse lag
// still reproduces by watching framerate stutter while moving a 1000hz+ gaming mouse around in CM while 
// watching for frame stutter during playback on windows 11.
//
// !!!!!! NOTE THAT YOU CAN STILL REPRODUCE THE FRAME DROPS BY CLICKING OUT TO ANOTHER APPLICATION, 
// AND THEN MOVING YOUR MOUSE AROUND OVER THE CM WINDOW WHILE CM DOES NOT HAVE WINDOW FOCUS.
// This is because we use normal input while not in focus to avoid CM receiving raw input meant for other applications.
public sealed class WindowsHighPollingRateWorkaround : MonoBehaviour
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private const uint NoLegacyMouseMessages = 0x30;
    private static readonly int DeviceSize = Marshal.SizeOf<RawInputDevice>();

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr TargetWindow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    private IntPtr playerWindow;
    private bool suppressingLegacy;
    private WindowsRawMouseReader rawMouse;
    private float wheelScale;
    private RawInputDevice normalRegistration = new RawInputDevice { UsagePage = 0x01, Usage = 0x02 };
    private IntPtr arrowCursor;
    private CursorLockMode previousCursorLock;
    private readonly RawInputDevice[] devices = new RawInputDevice[1];

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        new GameObject("Windows high polling rate workaround")
            .AddComponent<WindowsHighPollingRateWorkaround>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        CaptureNormalRegistration();
        rawMouse = new WindowsRawMouseReader();
        try
        {
            rawMouse.Start();
        }
        catch (Exception error)
        {
            Debug.LogError($"[WindowsMousePolling] Raw mouse receiver failed: {error}");
            rawMouse.Dispose();
            rawMouse = null;
            return;
        }

        arrowCursor = LoadCursor(IntPtr.Zero, new IntPtr(32512));
        wheelScale = InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms
            ? 1f / 120f
            : 1f;
        OnApplicationFocus(Application.isFocused);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        InputSystem.onBeforeUpdate -= InjectMouseState;
        InputSystem.onEvent -= IgnoreNativeMouseState;
        if (rawMouse == null)
            return;

        if (!hasFocus)
        {
            RestoreRegistration("focus lost");
            return;
        }

        playerWindow = GetActiveWindow();
        if (playerWindow == IntPtr.Zero)
        {
            Debug.LogWarning("[WindowsMousePolling] No active player window. Mouse repair not enabled.");
            return;
        }

        // Leave the activation click in normal Windows input until its release is delivered.
        InputSystem.onBeforeUpdate += InjectMouseState;
        InputSystem.onEvent += IgnoreNativeMouseState;
    }

    private void OnDestroy()
    {
        InputSystem.onBeforeUpdate -= InjectMouseState;
        InputSystem.onEvent -= IgnoreNativeMouseState;
        RestoreRegistration("shutdown");
        rawMouse?.Dispose();
    }
#endif

    private void CaptureNormalRegistration()
    {
        uint count = 0;
        if (GetRegisteredRawInputDevices(null, ref count, DeviceSize) == uint.MaxValue)
            return;

        var registered = new RawInputDevice[count];
        if (GetRegisteredRawInputDevices(registered, ref count, DeviceSize) == uint.MaxValue)
            return;

        foreach (var device in registered)
        {
            if (device.UsagePage == 0x01 && device.Usage == 0x02)
            {
                normalRegistration = device;
                return;
            }
        }
    }

    private void RestoreRegistration(string reason)
    {
        if (!suppressingLegacy)
            return;

        devices[0] = normalRegistration;
        if (RegisterRawInputDevices(devices, 1, DeviceSize))
        {
            suppressingLegacy = false;
        }
        else
        {
            Debug.LogWarning($"[WindowsMousePolling] Could not restore mouse registration: reason={reason}, error={Marshal.GetLastWin32Error()}.");
        }
    }

    private bool ApplyRegistration(string reason)
    {
        devices[0] = new RawInputDevice
        {
            UsagePage = 0x01,
            Usage = 0x02,
            Flags = NoLegacyMouseMessages,
            TargetWindow = rawMouse.Window
        };
        if (!RegisterRawInputDevices(devices, 1, DeviceSize))
        {
            var error = Marshal.GetLastWin32Error();
            Debug.LogWarning($"[WindowsMousePolling] RIDEV_NOLEGACY registration failed: reason={reason}, error={error}.");
            return false;
        }

        suppressingLegacy = true;
        rawMouse.Take(out _, out _, out _, out _);
        SetCursor(arrowCursor);
        previousCursorLock = Cursor.lockState;
        return true;
    }

    private void InjectMouseState()
    {
        if (InputState.currentUpdateType != InputUpdateType.Dynamic)
            return;

        var mouse = Mouse.current;
        if (mouse == null || !GetCursorPos(out var point) || !ScreenToClient(playerWindow, ref point))
            return;

        // GetCursorPos is desktop-relative. Window chrome, window position, and other monitors
        // must be removed before converting Win32's top-left origin to Unity's bottom-left.
        var position = new Vector2(point.X, Screen.height - 1 - point.Y);
        var buttons = (IsPressed(0x01) ? 1 : 0)
            | (IsPressed(0x02) ? 2 : 0)
            | (IsPressed(0x04) ? 4 : 0)
            | (IsPressed(0x06) ? 8 : 0)
            | (IsPressed(0x05) ? 16 : 0);
        var insideClient = point.X >= 0 && point.Y >= 0 && point.X < Screen.width && point.Y < Screen.height;
        // Changing registration while a button is held can swallow the legacy release and leave
        // Windows' activation/drag state stuck. Switch only between complete clicks.
        if (buttons == 0)
        {
            if (insideClient && !suppressingLegacy)
                ApplyRegistration("client area, buttons released");
            else if (!insideClient)
                RestoreRegistration("outside client area");
        }

        if (!suppressingLegacy)
            return;

        if (previousCursorLock != Cursor.lockState)
        {
            previousCursorLock = Cursor.lockState;
            if (previousCursorLock == CursorLockMode.None)
                SetCursor(arrowCursor);
        }

        rawMouse.Take(out var x, out var y, out var wheelX, out var wheelY);
        ApplyMouseState(mouse, position, new Vector2(x, -y), new Vector2(wheelX, wheelY) * wheelScale, buttons);
    }

    private static bool IsPressed(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private void IgnoreNativeMouseState(InputEventPtr eventPointer, InputDevice device)
    {
        // With suppression active this reader owns the mouse state. Unity's incomplete native
        // snapshots can otherwise release a held button between our updates and cancel UI drags.
        if (suppressingLegacy)
            SuppressMouseStateEvent(eventPointer, device);
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint GetRegisteredRawInputDevices(
        [Out] RawInputDevice[] devices,
        ref uint deviceCount,
        int deviceSize);

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(
        [In] RawInputDevice[] devices,
        uint deviceCount,
        int deviceSize);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(IntPtr window, ref Point point);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll", EntryPoint = "LoadCursorW", ExactSpelling = true)]
    private static extern IntPtr LoadCursor(IntPtr instance, IntPtr cursorName);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr SetCursor(IntPtr cursor);
#endif

    internal static void SuppressMouseStateEvent(InputEventPtr eventPointer, InputDevice device)
    {
        if (device is Mouse && (eventPointer.IsA<StateEvent>() || eventPointer.IsA<DeltaStateEvent>()))
            eventPointer.handled = true;
    }

    internal static void ApplyMouseState(
        Mouse mouse,
        Vector2 position,
        Vector2 delta,
        Vector2 scroll,
        int buttons)
    {
        // Apply once per dynamic update. No queued repair competes with native button snapshots.
        InputState.Change(mouse, new MouseState
        {
            position = position,
            delta = delta,
            scroll = scroll,
            buttons = (ushort)buttons
        });
    }
}
