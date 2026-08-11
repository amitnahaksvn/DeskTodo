using System.Runtime.InteropServices;
using DeskTodo.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DeskTodo.Platform.Mac;

/// <summary>
/// Registers Quick Add's global shortcut (Cmd+Shift+N) via the Carbon framework's
/// <c>RegisterEventHotKey</c> — chosen over an <c>NSEvent</c> global monitor because that
/// requires a one-time user-granted Accessibility/Input Monitoring permission that can't be
/// granted programmatically, whereas <c>RegisterEventHotKey</c> needs no permission at all
/// and is what real-world .NET/Swift global-hotkey libraries use on macOS. Carbon itself is
/// deprecated, but this specific API has no modern replacement and remains present and
/// functional on current macOS — see docs/ARCHITECTURE.md's "Phase 22" section.
///
/// The Carbon event handler fires on the thread pumping the process's main run loop, which
/// for an Avalonia.Native-hosted app is the same thread as the UI/dispatcher thread — so
/// <see cref="Pressed"/> already fires "on the UI thread" in practice, but callers should
/// still marshal explicitly per the interface's documented contract, since that's the one
/// guarantee that holds identically across both platform implementations.
/// </summary>
public sealed class MacGlobalHotkeyService : IGlobalHotkeyService
{
    private const string CarbonLib = "/System/Library/Frameworks/Carbon.framework/Carbon";

    // Carbon's classic Menus.h modifier masks, used by RegisterEventHotKey's inHotKeyModifiers.
    private const uint CmdKey = 0x0100;
    private const uint ShiftKey = 0x0200;

    // kVK_ANSI_N from HIToolbox/Events.h — the US ANSI keyboard virtual-key code for 'N'.
    private const uint VkAnsiN = 0x2D;

    // FourCharCode constants from Carbon's Events.h/CarbonEventsCore.h.
    private const uint EventClassKeyboard = 0x6B657962; // 'keyb'
    private const uint EventHotKeyPressed = 5;
    private const uint EventParamDirectObject = 0x2D2D2D2D; // '----'
    private const uint TypeEventHotKeyID = 0x686B6964; // 'hkid'
    private const uint HotKeySignature = 0x4454646F; // 'DTdo' — arbitrary, just needs to be ours
    private const uint HotKeyId = 1;

    private readonly ILogger<MacGlobalHotkeyService> _logger;

    // Stored as a field, not a local, so the GC never collects the delegate while Carbon
    // still holds a raw function pointer to it — a classic P/Invoke callback pitfall that
    // would otherwise crash the process the next time the hotkey fires.
    private EventHandlerProc? _handlerProc;
    private IntPtr _handlerRef;
    private IntPtr _hotKeyRef;

    public MacGlobalHotkeyService(ILogger<MacGlobalHotkeyService> logger)
    {
        _logger = logger;
    }

    public event EventHandler? Pressed;

    public bool Register()
    {
        try
        {
            _handlerProc = HandleHotKeyEvent;

            // No NewEventHandlerUPP call here: on 64-bit macOS a UPP *is* the raw C function
            // pointer — Apple's headers `#define NewEventHandlerUPP(x) (x)` in 64-bit, and
            // the symbol isn't even exported from the shared library anymore (confirmed live:
            // calling it throws EntryPointNotFoundException). The trampoline only ever existed
            // for 32-bit compatibility.
            var handlerPointer = Marshal.GetFunctionPointerForDelegate(_handlerProc);

            var eventType = new EventTypeSpec { eventClass = EventClassKeyboard, eventKind = EventHotKeyPressed };
            var installStatus = InstallEventHandler(GetApplicationEventTarget(), handlerPointer, 1, [eventType], IntPtr.Zero, out _handlerRef);
            if (installStatus != 0)
            {
                _logger.LogWarning("InstallEventHandler failed with OSStatus {Status}; global shortcut unavailable", installStatus);
                return false;
            }

            var hotKeyId = new EventHotKeyID { signature = HotKeySignature, id = HotKeyId };
            var registerStatus = RegisterEventHotKey(VkAnsiN, CmdKey | ShiftKey, hotKeyId, GetApplicationEventTarget(), 0, out _hotKeyRef);
            if (registerStatus != 0)
            {
                _logger.LogWarning("RegisterEventHotKey failed with OSStatus {Status}; likely already claimed by another app", registerStatus);
                RemoveEventHandler(_handlerRef);
                _handlerRef = IntPtr.Zero;
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            _logger.LogError(ex, "Carbon global hotkey registration is unavailable on this system");
            return false;
        }
    }

    public void Unregister()
    {
        if (_hotKeyRef != IntPtr.Zero)
        {
            UnregisterEventHotKey(_hotKeyRef);
            _hotKeyRef = IntPtr.Zero;
        }

        if (_handlerRef != IntPtr.Zero)
        {
            RemoveEventHandler(_handlerRef);
            _handlerRef = IntPtr.Zero;
        }
    }

    public void Dispose() => Unregister();

    private int HandleHotKeyEvent(IntPtr inHandlerCallRef, IntPtr inEvent, IntPtr inUserData)
    {
        var status = GetEventParameter(inEvent, EventParamDirectObject, TypeEventHotKeyID, IntPtr.Zero, (uint)Marshal.SizeOf<EventHotKeyID>(), IntPtr.Zero, out var pressedId);
        if (status == 0 && pressedId.id == HotKeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EventHotKeyID
    {
        public uint signature;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct EventTypeSpec
    {
        public uint eventClass;
        public uint eventKind;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EventHandlerProc(IntPtr inHandlerCallRef, IntPtr inEvent, IntPtr inUserData);

    [DllImport(CarbonLib)]
    private static extern IntPtr GetApplicationEventTarget();

    [DllImport(CarbonLib)]
    private static extern int InstallEventHandler(IntPtr inTarget, IntPtr inHandler, int inNumTypes, EventTypeSpec[] inList, IntPtr inUserData, out IntPtr outHandlerRef);

    [DllImport(CarbonLib)]
    private static extern int RemoveEventHandler(IntPtr inHandlerRef);

    [DllImport(CarbonLib)]
    private static extern int RegisterEventHotKey(uint inHotKeyCode, uint inHotKeyModifiers, EventHotKeyID inHotKeyID, IntPtr inTarget, uint inOptions, out IntPtr outRef);

    [DllImport(CarbonLib)]
    private static extern int UnregisterEventHotKey(IntPtr inHotKey);

    [DllImport(CarbonLib)]
    private static extern int GetEventParameter(IntPtr inEvent, uint inName, uint inDesiredType, IntPtr outActualType, uint inBufferSize, IntPtr outActualSize, out EventHotKeyID outData);
}
