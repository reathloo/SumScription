using System.Runtime.InteropServices;

namespace SumScription.App.Services;

/// <summary>Applies the development icon to an unbundled macOS process.</summary>
internal static class MacDockIcon
{
    private const string ObjectiveCLibrary = "/usr/lib/libobjc.A.dylib";

    /// <summary>Sets NSApplication.applicationIconImage when running outside an app bundle.</summary>
    public static bool TryApply(string iconPath)
    {
        if (!OperatingSystem.IsMacOS() || !File.Exists(iconPath))
        {
            return false;
        }

        nint allocatedImage = 0;
        var nativePath = Marshal.StringToCoTaskMemUTF8(iconPath);
        try
        {
            var application = Send(GetClass("NSApplication"), GetSelector("sharedApplication"));
            var path = SendPointer(
                GetClass("NSString"),
                GetSelector("stringWithUTF8String:"),
                nativePath);
            allocatedImage = Send(GetClass("NSImage"), GetSelector("alloc"));
            var image = SendPointer(
                allocatedImage,
                GetSelector("initWithContentsOfFile:"),
                path);

            if (application == 0 || image == 0)
            {
                return false;
            }

            SendVoidPointer(application, GetSelector("setApplicationIconImage:"), image);
            SendVoid(image, GetSelector("release"));
            allocatedImage = 0;
            return true;
        }
        catch (Exception) when (OperatingSystem.IsMacOS())
        {
            return false;
        }
        finally
        {
            if (allocatedImage != 0)
            {
                SendVoid(allocatedImage, GetSelector("release"));
            }

            Marshal.FreeCoTaskMem(nativePath);
        }
    }

    private static nint GetClass(string name) => ObjectiveCGetClass(name);

    private static nint GetSelector(string name) => SelectorRegisterName(name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_getClass", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ObjectiveCGetClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "sel_registerName", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint SelectorRegisterName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint Send(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint SendPointer(nint receiver, nint selector, nint argument);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SendVoid(nint receiver, nint selector);

    [DllImport(ObjectiveCLibrary, EntryPoint = "objc_msgSend", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SendVoidPointer(nint receiver, nint selector, nint argument);
}
