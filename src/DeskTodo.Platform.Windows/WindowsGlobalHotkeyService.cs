using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Platform.Windows;

/// <summary>
/// Registers Quick Add's global shortcut (Ctrl+Shift+N) via User32's
/// <c>RegisterHotKey</c>/<c>WM_HOTKEY</c>. Passing <c>hWnd = NULL</c> delivers <c>WM_HOTKEY</c>
/// to the *calling thread's* message queue instead of a window's, per
/// <see href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerhotkey"/>
/// — so this runs its own dedicated background thread with a minimal Win32 message loop
/// (<c>GetMessage</c>/dispatch) rather than needing a hidden message-only window.
///
/// <b>Authored but not runtime-verified</b> — same caveat as
/// <c>DeskTodo.Platform.Windows.WindowsAutoStartService</c>: this dev environment has no
/// Windows machine to test against. The registration call is wrapped defensively, since an
/// unhandled exception on this background thread would otherwise crash the whole process —
/// a risk specific to unverified P/Invoke signatures, not something the rest of this
/// codebase's "trust framework guarantees" default applies to.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsGlobalHotkeyService(ILogger<WindowsGlobalHotkeyService> logger) : IGlobalHotkeyService
{
    private const int HotkeyId = 1;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkN = 0x4E;
    private const uint WmHotkey = 0x0312;

    private Thread? _messageLoopThread;
    private uint _messageLoopThreadId;

    public event EventHandler? Pressed;

    public bool Register()
    {
        if (_messageLoopThread is not null)
        {
            return true;
        }

        using var ready = new ManualResetEventSlim(false);
        var registered = false;

        _messageLoopThread = new Thread(() =>
        {
            try
            {
                _messageLoopThreadId = GetCurrentThreadId();
                registered = RegisterHotKey(IntPtr.Zero, HotkeyId, ModControl | ModShift | ModNoRepeat, VkN);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                logger.LogError(ex, "User32 global hotkey registration is unavailable on this system");
            }
            finally
            {
                ready.Set();
            }

            if (!registered)
            {
                return;
            }

            // RegisterHotKey/WM_HOTKEY are thread-affine — UnregisterHotKey must run on this
            // same thread, which is why it happens here (after the loop exits via the
            // WM_QUIT posted by Unregister()) rather than on whatever thread calls Unregister.
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == WmHotkey && msg.wParam.ToInt32() == HotkeyId)
                {
                    Pressed?.Invoke(this, EventArgs.Empty);
                }
            }

            UnregisterHotKey(IntPtr.Zero, HotkeyId);
        })
        {
            IsBackground = true,
            Name = "DeskTodo.GlobalHotkey",
        };
        _messageLoopThread.Start();

        ready.Wait(TimeSpan.FromSeconds(2));

        if (!registered)
        {
            logger.LogWarning("RegisterHotKey failed; likely already claimed by another app");
            _messageLoopThread = null;
        }

        return registered;
    }

    public void Unregister()
    {
        if (_messageLoopThreadId != 0)
        {
            PostThreadMessage(_messageLoopThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            _messageLoopThreadId = 0;
        }

        _messageLoopThread = null;
    }

    public void Dispose() => Unregister();

    private const uint WM_QUIT = 0x0012;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);
}
